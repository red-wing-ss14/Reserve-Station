// SPDX-License-Identifier: MIT

using Content.Shared.Eye.Blinding.Components;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Eye.Blinding.Systems;

public sealed class TemporaryBlindnessSystem : EntitySystem
{
    public static readonly ProtoId<StatusEffectPrototype> BlindingStatusEffect = "TemporaryBlindness";

    [Dependency] private readonly BlindableSystem _blindableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemporaryBlindnessComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TemporaryBlindnessComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<TemporaryBlindnessComponent, CanSeeAttemptEvent>(OnBlindTrySee);
        // RW-Start
        SubscribeLocalEvent<TemporaryBlindnessComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<TemporaryBlindnessComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
        SubscribeLocalEvent<TemporaryBlindnessComponent, StatusEffectRelayedEvent<CanSeeAttemptEvent>>(OnBlindTrySeeRelayed);
        // RW-End
    }

    private void OnStartup(EntityUid uid, TemporaryBlindnessComponent component, ComponentStartup args)
    {
        // RW-Edit-Start
        if (!TryComp<StatusEffectComponent>(uid, out var status) || status.AppliedTo == null)
            return;

        _blindableSystem.UpdateIsBlind(status.AppliedTo.Value);
        // RW-Edit-End
    }

    private void OnShutdown(EntityUid uid, TemporaryBlindnessComponent component, ComponentShutdown args)
    {
        // RW-Edit-Start
        if (!TryComp<StatusEffectComponent>(uid, out var status) || status.AppliedTo == null)
            return;

        _blindableSystem.UpdateIsBlind(status.AppliedTo.Value);
        // RW-Edit-End
    }

    private static void OnBlindTrySee(EntityUid uid, TemporaryBlindnessComponent component, CanSeeAttemptEvent args) // RW-Edit: Static
    {
        if (component.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }

    // RW-Start
    private void OnStatusEffectApplied(Entity<TemporaryBlindnessComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _blindableSystem.UpdateIsBlind(args.Target);
    }

    private void OnStatusEffectRemoved(Entity<TemporaryBlindnessComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _blindableSystem.UpdateIsBlind(args.Target);
    }

    private static void OnBlindTrySeeRelayed(Entity<TemporaryBlindnessComponent> ent, ref StatusEffectRelayedEvent<CanSeeAttemptEvent> args)
    {
        if (ent.Comp.LifeStage <= ComponentLifeStage.Running)
            args.Args.Cancel();
    }
    // RW-End
}
