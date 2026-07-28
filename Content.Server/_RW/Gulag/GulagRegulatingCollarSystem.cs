using Content.Server._RW.Gulag.Components;
using Content.Server.Chat.Systems;
using Content.Server.Electrocution;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared.Chat;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RW.Gulag;

public sealed class GulagRegulatingCollarSystem : EntitySystem
{
    private const string NeckSlot = "neck";

    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly GulagSystem _gulag = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly WoundSystem _wounds = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidAppearanceComponent, DamageChangedEvent>(OnHumanoidDamaged, after: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<HumanoidAppearanceComponent, MobStateChangedEvent>(OnHumanoidMobStateChanged);
        SubscribeLocalEvent<HumanoidAppearanceComponent, GulagChatMessageAttemptEvent>(OnChatMessageAttempt);
    }

    public bool TryGetWornCollar(EntityUid wearer, out Entity<GulagRegulatingCollarComponent> collar)
    {
        collar = default;

        if (!_inventory.TryGetSlotEntity(wearer, NeckSlot, out var collarUid) ||
            !TryComp(collarUid.Value, out GulagRegulatingCollarComponent? collarComponent))
        {
            return false;
        }

        collar = (collarUid.Value, collarComponent);
        return true;
    }

    public bool IsWearingRegulatingCollar(EntityUid wearer)
    {
        return TryGetWornCollar(wearer, out _);
    }

    public bool TryPunishChatMessage(EntityUid wearer)
    {
        if (!TryGetWornCollar(wearer, out var collar) ||
            collar.Comp.LiquidationTriggered)
        {
            return false;
        }

        ShockWearer(wearer, collar);
        Speak(collar, collar.Comp.SpeechPunishmentMessage);
        return true;
    }

    private void OnHumanoidDamaged(EntityUid uid, HumanoidAppearanceComponent component, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased ||
            args.Origin is not { } user ||
            user == uid ||
            !TryGetWornCollar(user, out var collar) ||
            collar.Comp.LiquidationTriggered)
        {
            return;
        }

        TryPunishHarm(user, collar);
    }

    private void OnHumanoidMobStateChanged(EntityUid uid, HumanoidAppearanceComponent component, ref MobStateChangedEvent args)
    {
        if (args.NewMobState < MobState.SoftCritical ||
            args.OldMobState >= args.NewMobState ||
            args.Origin is not { } user ||
            user == uid ||
            !TryGetWornCollar(user, out var collar))
        {
            return;
        }

        TryLiquidate(user, collar);
    }

    private void OnChatMessageAttempt(Entity<HumanoidAppearanceComponent> ent, ref GulagChatMessageAttemptEvent args)
    {
        if (TryPunishChatMessage(ent))
            args.Cancel();
    }

    private bool TryPunishHarm(EntityUid wearer, Entity<GulagRegulatingCollarComponent> collar)
    {
        if (collar.Comp.LiquidationTriggered)
            return false;

        if (!collar.Comp.WarnedForHarm)
        {
            collar.Comp.WarnedForHarm = true;
            Speak(collar, collar.Comp.HarmWarningMessage);
            return true;
        }

        ShockWearer(wearer, collar);
        _gulag.TryExtendSentence(wearer, collar.Comp.HarmSentenceExtension);
        return true;
    }

    private bool TryLiquidate(EntityUid wearer, Entity<GulagRegulatingCollarComponent> collar)
    {
        if (collar.Comp.LiquidationTriggered)
            return false;

        collar.Comp.LiquidationTriggered = true;
        Speak(collar, collar.Comp.LiquidationMessage);
        _gulag.TryExtendSentence(wearer, collar.Comp.LiquidationSentenceExtension);

        var collarUid = collar.Owner;
        var beepSound = collar.Comp.BeepSound;
        var beepCount = collar.Comp.BeepCount;
        var beepInterval = collar.Comp.BeepInterval;
        var liquidationEffect = collar.Comp.LiquidationEffect;

        for (var i = 0; i < beepCount; i++)
        {
            var delay = TimeSpan.FromTicks(beepInterval.Ticks * i);
            Timer.Spawn(delay, () => TryPlayBeep(collarUid, beepSound));
        }

        Timer.Spawn(TimeSpan.FromTicks(beepInterval.Ticks * beepCount),
            () => DoLiquidation(wearer, liquidationEffect));

        return true;
    }

    private void ShockWearer(EntityUid wearer, Entity<GulagRegulatingCollarComponent> collar)
    {
        _electrocution.TryDoElectrocution(
            wearer,
            collar.Owner,
            collar.Comp.ShockDamage,
            collar.Comp.ShockTime,
            true,
            ignoreInsulation: true);
    }

    private void Speak(Entity<GulagRegulatingCollarComponent> collar, LocId message)
    {
        _chat.TrySendInGameICMessage(
            collar.Owner,
            Loc.GetString(message),
            InGameICChatType.Speak,
            hideChat: true,
            checkRadioPrefix: false,
            ignoreActionBlocker: true);
    }

    private void TryPlayBeep(EntityUid collar, SoundSpecifier sound)
    {
        if (!Exists(collar))
            return;

        _audio.PlayPvs(sound, collar);
    }

    private void DoLiquidation(EntityUid wearer, EntProtoId liquidationEffect)
    {
        if (!Exists(wearer))
            return;

        Spawn(liquidationEffect, _transform.GetMapCoordinates(wearer));
        Decapitate(wearer);
    }

    private void Decapitate(EntityUid wearer)
    {
        foreach (var part in _body.GetBodyChildren(wearer))
        {
            if (part.Component.PartType != BodyPartType.Head)
                continue;

            if (TryComp<WoundableComponent>(part.Id, out var woundable) &&
                woundable.ParentWoundable is { } parent)
            {
                _wounds.AmputateWoundable(parent, part.Id, woundable);
            }

            return;
        }
    }
}
