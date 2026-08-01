using Content.Server.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared._Shitmed.Humanoid.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Systems;

public sealed class MetabolismLimitSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetabolizerComponent, OrganAddedToBodyEvent>(OnOrganAddedToBody);
        SubscribeLocalEvent<MetabolizerComponent, OrganRemovedFromBodyEvent>(OnOrganRemovedFromBody);
        SubscribeLocalEvent<HumanoidAppearanceComponent, ProfileLoadFinishedEvent>(OnProfileLoadFinished);
    }

    private void OnProfileLoadFinished(EntityUid uid, HumanoidAppearanceComponent humanoid, ProfileLoadFinishedEvent args)
    {
        if (!_prototypeManager.TryIndex<SpeciesPrototype>(humanoid.Species, out var species))
            return;

        if (species.MetabolismLimit is not { } limit || limit <= 0)
            return;

        // Update all metabolizer organs in the body
        foreach (var (organId, organComp) in _bodySystem.GetBodyOrgans(uid))
        {
            if (!TryComp<MetabolizerComponent>(organId, out var metabolizer))
                continue;

            metabolizer.PrototypeMaxReagentsProcessable ??= metabolizer.MaxReagentsProcessable;
            metabolizer.MaxReagentsProcessable = limit;
        }
    }

    private void OnOrganAddedToBody(Entity<MetabolizerComponent> ent, ref OrganAddedToBodyEvent args)
    {
        ent.Comp.PrototypeMaxReagentsProcessable ??= ent.Comp.MaxReagentsProcessable;

        if (TryComp<HumanoidAppearanceComponent>(args.Body, out var humanoid)
            && _prototypeManager.TryIndex<SpeciesPrototype>(humanoid.Species, out var species)
            && species.MetabolismLimit is { } limit
            && limit > 0)
        {
            ent.Comp.MaxReagentsProcessable = limit;
        }
        else
        {
            ent.Comp.MaxReagentsProcessable = ent.Comp.PrototypeMaxReagentsProcessable.Value;
        }

        ent.Comp.CurrentReagentIndex = 0;
    }

    private void OnOrganRemovedFromBody(Entity<MetabolizerComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        if (ent.Comp.PrototypeMaxReagentsProcessable is { } original)
            ent.Comp.MaxReagentsProcessable = original;

        ent.Comp.CurrentReagentIndex = 0;
    }
}
