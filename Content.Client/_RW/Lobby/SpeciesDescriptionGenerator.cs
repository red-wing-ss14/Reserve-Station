using System.Globalization;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Sericulture;
using Content.Shared.Storage;
using Content.Shared.Temperature.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Client._RW.Lobby;

public enum SpeciesTraitKind
{
    Pro,
    Con,
    Neutral,
}

public readonly record struct SpeciesDescriptionLine(string Text, SpeciesTraitKind Kind);

public static class SpeciesDescriptionGenerator
{
    private const float HumanWalkSpeed = MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
    private const float HumanSprintSpeed = MovementSpeedModifierComponent.DefaultBaseSprintSpeed;
    private const float HumanHungerDecay = 0.01666666666f;
    private const float HumanThirstDecay = 0.1f;
    private const float HumanHeatThreshold = 325f;
    private const float HumanColdThreshold = 260f;
    private const float HumanHeatDamage = 1f;
    private const float HumanColdDamage = 0.1f;
    private const float HumanBurnDamage = 0.5f;
    private const float HumanFirestacksOnIgnite = 2f;
    private const float HumanStaminaThreshold = 100f;
    private const float FloatTolerance = 0.001f;
    private const float CoeffTolerance = 0.01f;

    public static IEnumerable<SpeciesDescriptionLine> Generate(
        SpeciesPrototype species,
        IPrototypeManager prototypes,
        IComponentFactory componentFactory)
    {
        if (!prototypes.TryIndex(species.Prototype, out EntityPrototype? mobProto))
            yield break;

        foreach (var line in GenerateDamageModifiers(mobProto, prototypes, componentFactory))
            yield return line;

        foreach (var line in GenerateTemperature(mobProto, prototypes, componentFactory))
            yield return line;

        foreach (var line in GenerateHungerAndThirst(mobProto, componentFactory))
            yield return line;

        foreach (var line in GenerateMovement(mobProto, componentFactory))
            yield return line;

        foreach (var line in GenerateStamina(mobProto, componentFactory))
            yield return line;

        foreach (var line in GeneratePuller(mobProto, componentFactory))
            yield return line;

        foreach (var line in GenerateFlammable(mobProto, prototypes, componentFactory))
            yield return line;

        foreach (var line in GenerateFlags(mobProto, componentFactory))
            yield return line;
    }

    private static SpeciesDescriptionLine Pro(string text) => new(text, SpeciesTraitKind.Pro);

    private static SpeciesDescriptionLine Con(string text) => new(text, SpeciesTraitKind.Con);

    private static SpeciesDescriptionLine Neutral(string text) => new(text, SpeciesTraitKind.Neutral);

    private static IEnumerable<SpeciesDescriptionLine> GenerateDamageModifiers(
        EntityPrototype mobProto,
        IPrototypeManager prototypes,
        IComponentFactory componentFactory)
    {
        if (!mobProto.TryGetComponent(out DamageableComponent? damageable, componentFactory))
            yield break;

        if (string.IsNullOrEmpty(damageable.DamageModifierSetId))
            yield break;

        if (!prototypes.TryIndex(damageable.DamageModifierSetId, out DamageModifierSetPrototype? modifierSet))
            yield break;

        foreach (var (damageType, coefficient) in modifierSet.Coefficients)
        {
            if (MathF.Abs(coefficient - 1f) < CoeffTolerance)
                continue;

            var damageName = prototypes.TryIndex<DamageTypePrototype>(damageType, out var damageProto)
                ? damageProto.LocalizedName
                : damageType;

            var percent = (int) MathF.Round(MathF.Abs(coefficient - 1f) * 100f);

            yield return coefficient < 1f
                ? Pro(Loc.GetString("ui-species-generated-damage-resistant", ("percent", percent), ("damage", damageName)))
                : Con(Loc.GetString("ui-species-generated-damage-vulnerable", ("percent", percent), ("damage", damageName)));
        }
    }

    private static IEnumerable<SpeciesDescriptionLine> GenerateTemperature(
        EntityPrototype mobProto,
        IPrototypeManager prototypes,
        IComponentFactory componentFactory)
    {
        if (!mobProto.TryGetComponent(out TemperatureDamageComponent? temperature, componentFactory))
            yield break;

        if (MathF.Abs(temperature.HeatDamageThreshold - HumanHeatThreshold) > FloatTolerance)
        {
            var celsius = KelvinToCelsius(temperature.HeatDamageThreshold);
            yield return temperature.HeatDamageThreshold > HumanHeatThreshold
                ? Pro(Loc.GetString("ui-species-generated-heat-threshold-higher", ("temperature", celsius)))
                : Con(Loc.GetString("ui-species-generated-heat-threshold-lower", ("temperature", celsius)));
        }

        if (MathF.Abs(temperature.ColdDamageThreshold - HumanColdThreshold) > FloatTolerance)
        {
            var celsius = KelvinToCelsius(temperature.ColdDamageThreshold);
            yield return temperature.ColdDamageThreshold > HumanColdThreshold
                ? Con(Loc.GetString("ui-species-generated-cold-threshold-higher", ("temperature", celsius)))
                : Pro(Loc.GetString("ui-species-generated-cold-threshold-lower", ("temperature", celsius)));
        }

        foreach (var line in GenerateTemperatureDamage(temperature.HeatDamage, HumanHeatDamage, true, prototypes))
            yield return line;

        foreach (var line in GenerateTemperatureDamage(temperature.ColdDamage, HumanColdDamage, false, prototypes))
            yield return line;
    }

    private static IEnumerable<SpeciesDescriptionLine> GenerateTemperatureDamage(
        DamageSpecifier damage,
        float humanAmount,
        bool overheating,
        IPrototypeManager prototypes)
    {
        foreach (var (damageType, amount) in damage.DamageDict)
        {
            var amountFloat = (float) amount;
            if (MathF.Abs(amountFloat - humanAmount) < FloatTolerance)
                continue;

            var damageName = GetDamageName(prototypes, damageType);

            var delta = MathF.Abs(amountFloat - humanAmount);
            var formattedDelta = delta.ToString("0.##", CultureInfo.InvariantCulture);

            if (overheating)
            {
                yield return amountFloat > humanAmount
                    ? Con(Loc.GetString("ui-species-generated-overheat-damage-more", ("amount", formattedDelta), ("damage", damageName)))
                    : Pro(Loc.GetString("ui-species-generated-overheat-damage-less", ("amount", formattedDelta), ("damage", damageName)));
            }
            else
            {
                yield return amountFloat > humanAmount
                    ? Con(Loc.GetString("ui-species-generated-cold-damage-more", ("amount", formattedDelta), ("damage", damageName)))
                    : Pro(Loc.GetString("ui-species-generated-cold-damage-less", ("amount", formattedDelta), ("damage", damageName)));
            }
        }
    }

    private static IEnumerable<SpeciesDescriptionLine> GenerateHungerAndThirst(
        EntityPrototype mobProto,
        IComponentFactory componentFactory)
    {
        if (mobProto.TryGetComponent(out HungerComponent? hunger, componentFactory))
        {
            foreach (var line in GenerateRateComparison(
                hunger.BaseDecayRate,
                HumanHungerDecay,
                "ui-species-generated-hunger-faster",
                "ui-species-generated-hunger-slower"))
                yield return line;
        }

        if (mobProto.TryGetComponent(out ThirstComponent? thirst, componentFactory))
        {
            foreach (var line in GenerateRateComparison(
                thirst.BaseDecayRate,
                HumanThirstDecay,
                "ui-species-generated-thirst-faster",
                "ui-species-generated-thirst-slower"))
                yield return line;
        }
    }

    private static IEnumerable<SpeciesDescriptionLine> GenerateMovement(
        EntityPrototype mobProto,
        IComponentFactory componentFactory)
    {
        if (!mobProto.TryGetComponent(out MovementSpeedModifierComponent? movement, componentFactory))
            yield break;

        foreach (var line in GeneratePercentComparison(
            movement.BaseWalkSpeed,
            HumanWalkSpeed,
            "ui-species-generated-walk-speed-faster",
            "ui-species-generated-walk-speed-slower"))
            yield return line;

        foreach (var line in GeneratePercentComparison(
            movement.BaseSprintSpeed,
            HumanSprintSpeed,
            "ui-species-generated-sprint-speed-faster",
            "ui-species-generated-sprint-speed-slower"))
            yield return line;
    }

    private static IEnumerable<SpeciesDescriptionLine> GenerateStamina(
        EntityPrototype mobProto,
        IComponentFactory componentFactory)
    {
        if (!mobProto.TryGetComponent(out StaminaComponent? stamina, componentFactory))
            yield break;

        if (MathF.Abs(stamina.BaseCritThreshold - HumanStaminaThreshold) < FloatTolerance)
            yield break;

        var threshold = (int) MathF.Round(stamina.BaseCritThreshold);

        yield return stamina.BaseCritThreshold > HumanStaminaThreshold
            ? Pro(Loc.GetString("ui-species-generated-stamina-threshold-higher", ("stamina", threshold)))
            : Con(Loc.GetString("ui-species-generated-stamina-threshold-lower", ("stamina", threshold)));
    }

    private static IEnumerable<SpeciesDescriptionLine> GeneratePuller(
        EntityPrototype mobProto,
        IComponentFactory componentFactory)
    {
        if (!mobProto.TryGetComponent(out PullerComponent? puller, componentFactory))
            yield break;

        if (puller.NeedsHands)
            yield break;

        yield return Pro(Loc.GetString("ui-species-generated-pulling-without-hands"));
    }

    private static IEnumerable<SpeciesDescriptionLine> GenerateFlammable(
        EntityPrototype mobProto,
        IPrototypeManager prototypes,
        IComponentFactory componentFactory)
    {
        if (!mobProto.TryGetComponent(out FlammableComponent? flammable, componentFactory))
            yield break;

        if (MathF.Abs(flammable.FirestacksOnIgnite - HumanFirestacksOnIgnite) < FloatTolerance)
        {
            foreach (var line in GenerateBurningDamage(flammable.Damage, prototypes))
                yield return line;

            yield break;
        }

        var percent = (int) MathF.Round(MathF.Abs(flammable.FirestacksOnIgnite - HumanFirestacksOnIgnite) / HumanFirestacksOnIgnite * 100f);

        yield return flammable.FirestacksOnIgnite > HumanFirestacksOnIgnite
            ? Con(Loc.GetString("ui-species-generated-ignites-easier", ("percent", percent)))
            : Pro(Loc.GetString("ui-species-generated-ignites-harder", ("percent", percent)));

        foreach (var line in GenerateBurningDamage(flammable.Damage, prototypes))
            yield return line;
    }

    private static IEnumerable<SpeciesDescriptionLine> GenerateBurningDamage(DamageSpecifier damage, IPrototypeManager prototypes)
    {
        foreach (var (damageType, amount) in damage.DamageDict)
        {
            var amountFloat = (float) amount;
            if (MathF.Abs(amountFloat - HumanBurnDamage) < FloatTolerance)
                continue;

            var damageName = GetDamageName(prototypes, damageType);
            var delta = MathF.Abs(amountFloat - HumanBurnDamage);
            var formattedDelta = delta.ToString("0.##", CultureInfo.InvariantCulture);

            yield return amountFloat > HumanBurnDamage
                ? Con(Loc.GetString("ui-species-generated-burning-damage-more", ("amount", formattedDelta), ("damage", damageName)))
                : Pro(Loc.GetString("ui-species-generated-burning-damage-less", ("amount", formattedDelta), ("damage", damageName)));
        }
    }

    private static IEnumerable<SpeciesDescriptionLine> GenerateFlags(
        EntityPrototype mobProto,
        IComponentFactory componentFactory)
    {
        if (mobProto.Components.ContainsKey("Uncloneable"))
            yield return Con(Loc.GetString("ui-species-generated-uncloneable"));

        if (mobProto.TryGetComponent(out SericultureComponent? _, componentFactory))
            yield return Pro(Loc.GetString("ui-species-generated-sericulture"));

        if (mobProto.TryGetComponent(out JumpAbilityComponent? _, componentFactory))
            yield return Pro(Loc.GetString("ui-species-generated-jump-ability"));

        if (mobProto.TryGetComponent(out StorageComponent? _, componentFactory))
            yield return Pro(Loc.GetString("ui-species-generated-internal-storage"));

        if (!mobProto.Components.ContainsKey("Perishable"))
            yield return Pro(Loc.GetString("ui-species-generated-no-rotting"));
    }

    private static IEnumerable<SpeciesDescriptionLine> GenerateRateComparison(
        float value,
        float humanValue,
        string fasterKey,
        string slowerKey)
    {
        if (MathF.Abs(value - humanValue) < FloatTolerance)
            yield break;

        var percent = (int) MathF.Round(MathF.Abs(value - humanValue) / humanValue * 100f);

        yield return value > humanValue
            ? Con(Loc.GetString(fasterKey, ("percent", percent)))
            : Pro(Loc.GetString(slowerKey, ("percent", percent)));
    }

    private static IEnumerable<SpeciesDescriptionLine> GeneratePercentComparison(
        float value,
        float humanValue,
        string fasterKey,
        string slowerKey)
    {
        if (MathF.Abs(value - humanValue) < FloatTolerance)
            yield break;

        var percent = (int) MathF.Round(MathF.Abs(value - humanValue) / humanValue * 100f);

        yield return value > humanValue
            ? Pro(Loc.GetString(fasterKey, ("percent", percent)))
            : Con(Loc.GetString(slowerKey, ("percent", percent)));
    }

    private static string KelvinToCelsius(float kelvin)
    {
        return ((int) MathF.Round(kelvin - 273.15f)).ToString(CultureInfo.InvariantCulture);
    }

    private static string GetDamageName(IPrototypeManager prototypes, string damageType)
    {
        return prototypes.TryIndex<DamageTypePrototype>(damageType, out var damageProto)
            ? damageProto.LocalizedName
            : damageType;
    }
}
