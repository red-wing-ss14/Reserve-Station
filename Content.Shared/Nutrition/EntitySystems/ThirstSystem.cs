// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Alert;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusIcon;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using Content.Shared._RW.Mood;
using Content.Shared.CCVar;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Shared.Nutrition.EntitySystems;

[UsedImplicitly]
public sealed class ThirstSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;
    [Dependency] private readonly IConfigurationManager _config = default!; // RW
    [Dependency] private readonly INetManager _net = default!; // RW
    [Dependency] private readonly MobStateSystem _mobState = default!; // RW

    private static readonly ProtoId<SatiationIconPrototype> ThirstIconOverhydratedId = "ThirstIconOverhydrated";
    private static readonly ProtoId<SatiationIconPrototype> ThirstIconThirstyId = "ThirstIconThirsty";
    private static readonly ProtoId<SatiationIconPrototype> ThirstIconParchedId = "ThirstIconParched";

    // RW-Start
    private static readonly HashSet<ThirstThreshold> MovementThresholds = new()
    {
        ThirstThreshold.Dead,
        ThirstThreshold.Parched,
    };

    private SatiationIconPrototype? _overhydratedIcon;
    private SatiationIconPrototype? _thirstyIcon;
    private SatiationIconPrototype? _parchedIcon;
    // RW-End

    public override void Initialize()
    {
        base.Initialize();

        // RW-Start
        _prototype.TryIndex(ThirstIconOverhydratedId, out _overhydratedIcon);
        _prototype.TryIndex(ThirstIconThirstyId, out _thirstyIcon);
        _prototype.TryIndex(ThirstIconParchedId, out _parchedIcon);
        // RW-End

        SubscribeLocalEvent<ThirstComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<ThirstComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ThirstComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnMapInit(EntityUid uid, ThirstComponent component, MapInitEvent args)
    {
        // Do not change behavior unless starting value is explicitly defined
        if (component.CurrentThirst < 0)
        {
            component.CurrentThirst = _random.Next(
                (int) component.ThirstThresholds[ThirstThreshold.Thirsty] + 10,
                (int) component.ThirstThresholds[ThirstThreshold.Okay] - 1);

            DirtyField(uid, component, nameof(ThirstComponent.CurrentThirst));
        }
        component.NextUpdateTime = _timing.CurTime + component.NextUpdateTime;
        component.CurrentThirstThreshold = GetThirstThreshold(component, component.CurrentThirst);
        component.LastThirstThreshold = ThirstThreshold.Okay; // TODO: Potentially change this -> Used Okay because no effects.
        // TODO: Check all thresholds make sense and throw if they don't.
        UpdateEffects(uid, component);

        DirtyFields(uid, component, null, nameof(ThirstComponent.NextUpdateTime), nameof(ThirstComponent.CurrentThirstThreshold), nameof(ThirstComponent.LastThirstThreshold));

        // RW-Edit-Start
        if (TryComp(uid, out MovementSpeedModifierComponent? moveMod))
            _movement.RefreshMovementSpeedModifiers(uid, moveMod);
        // RW-Edit-End
    }

    private void OnRefreshMovespeed(EntityUid uid, ThirstComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        // RW-Start
        if (_config.GetCVar(CCVars.MoodEnabled))
            return;
        // RW-End

        // TODO: This should really be taken care of somewhere else
        if (_jetpack.IsUserFlying(uid))
            return;

        var mod = component.CurrentThirstThreshold <= ThirstThreshold.Parched ? 0.75f : 1.0f;
        args.ModifySpeed(mod, mod);
    }

    private void OnRejuvenate(EntityUid uid, ThirstComponent component, RejuvenateEvent args)
    {
        SetThirst(uid, component, component.ThirstThresholds[ThirstThreshold.Okay]);
    }

    // RW-Edit-Start
    private ThirstThreshold GetThirstThreshold(ThirstComponent component, float amount)
    {
        if (amount <= component.ThirstThresholds[ThirstThreshold.Dead])
            return ThirstThreshold.Dead;

        if (amount <= component.ThirstThresholds[ThirstThreshold.Parched])
            return ThirstThreshold.Parched;

        if (amount <= component.ThirstThresholds[ThirstThreshold.Thirsty])
            return ThirstThreshold.Thirsty;

        return amount <= component.ThirstThresholds[ThirstThreshold.Okay]
            ? ThirstThreshold.Okay
            : ThirstThreshold.OverHydrated;
    }
    // RW-Edit-End

    public void ModifyThirst(EntityUid uid, ThirstComponent component, float amount)
    {
        SetThirst(uid, component, component.CurrentThirst + amount);
    }

    public void SetThirst(EntityUid uid, ThirstComponent component, float amount)
    {
        component.CurrentThirst = Math.Clamp(amount,
            component.ThirstThresholds[ThirstThreshold.Dead],
            component.ThirstThresholds[ThirstThreshold.OverHydrated]
        );

        DirtyField(uid, component, nameof(ThirstComponent.CurrentThirst));
    }

    // RW-Edit-Start
    private static bool IsMovementThreshold(ThirstThreshold threshold)
    {
        return MovementThresholds.Contains(threshold);
    }
    // RW-Edit-End

    // RW-Edit-Start
    public bool TryGetStatusIconPrototype(ThirstComponent component, [NotNullWhen(true)] out SatiationIconPrototype? prototype)
    {
        prototype = component.CurrentThirstThreshold switch
        {
            ThirstThreshold.OverHydrated => _overhydratedIcon,
            ThirstThreshold.Thirsty => _thirstyIcon,
            ThirstThreshold.Parched => _parchedIcon,
            _ => null,
        };

        return prototype != null;
    }
    // RW-Edit-End

    private void UpdateEffects(EntityUid uid, ThirstComponent component)
    {
        // RW-Start
        var wasMovementAffected = IsMovementThreshold(component.LastThirstThreshold);
        var isMovementAffected = IsMovementThreshold(component.CurrentThirstThreshold);
        // RW-End

        if (wasMovementAffected != isMovementAffected && TryComp(uid, out MovementSpeedModifierComponent? movementSlowdownComponent)) // RW-Edit
        {
            // RW-Edit-Start
            if (!_config.GetCVar(CCVars.MoodEnabled))
                _movement.RefreshMovementSpeedModifiers(uid, movementSlowdownComponent);
            // RW-Edit-End
        }

        // RW-Start
        if (_config.GetCVar(CCVars.MoodEnabled) && _net.IsServer)
            RaiseLocalEvent(uid, new MoodEffectEvent("Thirst" + component.CurrentThirstThreshold));
        // RW-End

        // Update UI
        if (ThirstComponent.ThirstThresholdAlertTypes.TryGetValue(component.CurrentThirstThreshold, out var alertId))
        {
            _alerts.ShowAlert(uid, alertId);
        }
        else
        {
            _alerts.ClearAlertCategory(uid, component.ThirstyCategory);
        }

        // RW-Edit-Start
        component.LastThirstThreshold = component.CurrentThirstThreshold;

        var newDecayRate = component.CurrentThirstThreshold switch
        {
            ThirstThreshold.OverHydrated => component.BaseDecayRate * 1.2f,
            ThirstThreshold.Okay => component.BaseDecayRate,
            ThirstThreshold.Thirsty => component.BaseDecayRate * 0.8f,
            ThirstThreshold.Parched => component.BaseDecayRate * 0.6f,
            ThirstThreshold.Dead => component.ActualDecayRate,
            _ => throw new ArgumentOutOfRangeException(),
        };

        if (Math.Abs(component.ActualDecayRate - newDecayRate) > 0.001f)
        {
            component.ActualDecayRate = newDecayRate;
            DirtyField(uid, component, nameof(ThirstComponent.ActualDecayRate));
        }

        DirtyField(uid, component, nameof(ThirstComponent.LastThirstThreshold));
        // RW-Edit-End
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ThirstComponent>();
        while (query.MoveNext(out var uid, out var thirst))
        {
            if (_timing.CurTime < thirst.NextUpdateTime)
                continue;

            thirst.NextUpdateTime += thirst.UpdateRate;

            // RW-Start
            if (_mobState.IsDead(uid))
                continue;
            // RW-End

            var oldThirst = thirst.CurrentThirst; // RW
            ModifyThirst(uid, thirst, -thirst.ActualDecayRate);

            // RW-Start
            if (Math.Abs(oldThirst - thirst.CurrentThirst) < 1e-6f)
                continue;
            // RW-End

            var calculatedThirstThreshold = GetThirstThreshold(thirst, thirst.CurrentThirst);

            if (calculatedThirstThreshold == thirst.CurrentThirstThreshold)
                continue;

            thirst.CurrentThirstThreshold = calculatedThirstThreshold;
            UpdateEffects(uid, thirst);
        }
    }
}
