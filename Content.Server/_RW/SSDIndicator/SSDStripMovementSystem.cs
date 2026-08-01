using Content.Shared._RW.SSDIndicator;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.CombatMode;
using Content.Shared.Ghost;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.SSDIndicator;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RW.SSDIndicator;

public sealed class SSDStripMovementSystem : EntitySystem
{
    private const float MaxStripWalkDistance = 2f;
    private const float MaxStripWalkDistanceSquared = MaxStripWalkDistance * MaxStripWalkDistance;

    private static readonly Direction[] MovementDirections =
    [
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West
    ];

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntityUid, StripStep> _stripSteps = new();
    private readonly List<EntityUid> _finishedSteps = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SSDIndicatorComponent, SSDStripAttemptedEvent>(OnSSDStripAttempted);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _finishedSteps.Clear();

        foreach (var (uid, step) in _stripSteps)
        {
            if (ShouldContinueStripStep(uid, step))
                continue;

            StopStripStep(uid, step);
            _finishedSteps.Add(uid);
        }

        foreach (var uid in _finishedSteps)
        {
            _stripSteps.Remove(uid);
        }
    }

    private void OnSSDStripAttempted(Entity<SSDIndicatorComponent> ent, ref SSDStripAttemptedEvent args)
    {
        if (!ent.Comp.IsSSD || IsAdminGhost(args.User))
            return;

        if (TryComp<BuckleComponent>(ent, out var buckle) && buckle.Buckled)
            _buckle.Unbuckle((ent.Owner, buckle), null);

        if (!TryComp<InputMoverComponent>(ent, out var mover))
            return;

        if (!TryComp<TransformComponent>(ent, out var xform))
            return;

        TryPunchStripper(ent.Owner, args.User);
        StartStripStep((ent.Owner, mover), args.Duration, _transform.GetMapCoordinates(ent.Owner, xform));
    }

    private bool IsAdminGhost(EntityUid uid)
        => TryComp<GhostComponent>(uid, out var ghost) && ghost.CanGhostInteract;

    private void TryPunchStripper(EntityUid uid, EntityUid user)
    {
        if (uid == user || !TryComp<MeleeWeaponComponent>(uid, out var melee))
            return;

        if (TryComp<HandsComponent>(uid, out var hands) &&
            _hands.TryGetActiveItem((uid, hands), out _) &&
            !_hands.TryDrop((uid, hands), checkActionBlocker: false))
        {
            return;
        }

        var wasInCombatMode = _combat.IsInCombatMode(uid);
        _combat.SetInCombatMode(uid, true);
        _melee.AttemptLightAttack(uid, uid, melee, user);
        _combat.SetInCombatMode(uid, wasInCombatMode);
    }

    private void StartStripStep(Entity<InputMoverComponent> ent, TimeSpan duration, MapCoordinates startCoordinates)
    {
        if (_stripSteps.Remove(ent.Owner, out var existingStep))
            StopStripStep(ent.Owner, existingStep);

        foreach (var movementDirection in MovementDirections)
        {
            _mover.SetVelocityDirection(ent, movementDirection, 0, false);
        }

        var previousWalkButtonEnabled = (ent.Comp.HeldMoveButtons & MoveButtons.Walk) != 0;
        var walkButtonEnabled = !ent.Comp.DefaultSprinting;
        var direction = GetRandomDirection();
        _mover.SetSprinting(ent, 0, walkButtonEnabled);
        _mover.SetVelocityDirection(ent, direction, 0, true);
        _stripSteps[ent.Owner] = new StripStep(direction, previousWalkButtonEnabled, _timing.CurTime + duration, startCoordinates);
    }

    private bool ShouldContinueStripStep(EntityUid uid, StripStep step)
    {
        if (_timing.CurTime >= step.EndTime || !Exists(uid))
            return false;

        if (!TryComp<TransformComponent>(uid, out var xform))
            return false;

        var currentCoordinates = _transform.GetMapCoordinates(uid, xform);
        if (currentCoordinates.MapId != step.StartCoordinates.MapId)
            return false;

        return (currentCoordinates.Position - step.StartCoordinates.Position).LengthSquared() < MaxStripWalkDistanceSquared;
    }

    private void StopStripStep(EntityUid uid, StripStep step)
    {
        if (!TryComp<InputMoverComponent>(uid, out var mover))
            return;

        _mover.SetVelocityDirection((uid, mover), step.Direction, ushort.MaxValue, false);
        _mover.SetSprinting((uid, mover), ushort.MaxValue, step.PreviousWalkButtonEnabled);
    }

    private Direction GetRandomDirection()
        => MovementDirections[_random.Next(MovementDirections.Length)];

    private readonly record struct StripStep(
        Direction Direction,
        bool PreviousWalkButtonEnabled,
        TimeSpan EndTime,
        MapCoordinates StartCoordinates);
}
