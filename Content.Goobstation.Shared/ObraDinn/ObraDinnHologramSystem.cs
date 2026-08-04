using Content.Shared._DV.Carrying;
using Content.Shared._Goobstation.Wizard.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Climbing.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Kitchen;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.Verbs;
using Content.Shared.SSDIndicator;
using Content.Shared.Storage.Components;
using Content.Shared.Strip.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Goobstation.Shared.ObraDinn;

/// <summary>
/// This handles temporary holograms from the obra dinn clock
/// </summary>
public sealed class ObraDinnHologramSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ObraDinnHologramComponent, ListenEvent>(Chat);
        SubscribeLocalEvent<ObraDinnHologramComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ObraDinnHologramComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ObraDinnHologramComponent, InsertIntoEntityStorageAttemptEvent>(OnStorage);
        // RW start
        // TODO: Revert when Goobstation fixes Obra Dinn holograms retaining physical interactability (e.g. buckling to beds, drag & drop).
        SubscribeLocalEvent<ObraDinnHologramComponent, GettingInteractedWithAttemptEvent>(OnGettingInteractedWith);
        SubscribeLocalEvent<ObraDinnHologramComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<ObraDinnHologramComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ObraDinnHologramComponent, DragDropTargetEvent>(OnDragDropTarget);
        SubscribeLocalEvent<ObraDinnHologramComponent, CanDropTargetEvent>(OnCanDropTarget);
        SubscribeLocalEvent<ObraDinnHologramComponent, CanDropDraggedEvent>(OnCanDropDragged);
        SubscribeLocalEvent<ObraDinnHologramComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        // RW end
    }

    private void Chat(Entity<ObraDinnHologramComponent> ent, ref ListenEvent arg)
    {
        if (!arg.Message.ToLower().Equals(ent.Comp.RealName.ToLower()))
            return;

        if (!Transform(ent.Owner).Coordinates.TryDistance(EntityManager,Transform(arg.Source).Coordinates, out var dist) || dist > ent.Comp.MinDistance )
            return;

        _metaData.SetEntityName(ent.Owner, ent.Comp.RealName);

        SpawnAtPosition(ent.Comp.SpawnEffect,Transform(ent).Coordinates);
        _audio.PlayPvs(ent.Comp.Sound, ent );
    }

    private void OnStartup(Entity<ObraDinnHologramComponent> ent, ref ComponentStartup arg)
    {
        SpawnAtPosition(ent.Comp.SpawnEffect,Transform(ent).Coordinates);
        _audio.PlayPvs(ent.Comp.Sound, ent );

        var listenerr = EnsureComp<ActiveListenerComponent>(ent.Owner);// needed for ListenEvent
        listenerr.Range=ent.Comp.MinDistance;


        // comps we dont want the hologram to have
        RemCompDeferred<PullableComponent>(ent);
        RemComp<WoundableComponent>(ent);
        if (TryComp<BodyComponent>(ent, out var body) && body.RootContainer.ContainedEntity is { } rootPart)
        {
            foreach (var (partId, _) in _body.GetBodyPartChildren(rootPart))
                RemComp<WoundableComponent>(partId);
        }

        RemCompDeferred<ActorComponent>(ent);
        RemCompDeferred<MindContainerComponent>(ent);
        RemCompDeferred<FixturesComponent>(ent);
        RemCompDeferred<PhysicsComponent>(ent);
        RemCompDeferred<StrippableComponent>(ent);
        RemCompDeferred<SSDIndicatorComponent>(ent);
        RemCompDeferred<GhostRoleMobSpawnerComponent>(ent);
        RemCompDeferred<DamageableComponent>(ent);
        RemCompDeferred<MobMoverComponent>(ent);
        RemCompDeferred<CarriableComponent>(ent);
        RemCompDeferred<HasJobIconsComponent>(ent);
        // RW start: bug-fixes #12 — RemComp Butcherable immediately; Body deferred to avoid WoundableVisuals cascade.
        RemComp<ButcherableComponent>(ent);
        RemComp<BloodstreamComponent>(ent);
        RemComp<SolutionContainerManagerComponent>(ent);
        RemCompDeferred<BodyComponent>(ent);
        RemCompDeferred<MobStateComponent>(ent);
        RemCompDeferred<BuckleComponent>(ent);
        RemCompDeferred<ClimbableComponent>(ent);
        RemCompDeferred<ClimbingComponent>(ent);
        RemCompDeferred<CuffableComponent>(ent);
        RemCompDeferred<HandsComponent>(ent);
        RemCompDeferred<InventoryComponent>(ent);
        RemCompDeferred<ReproductiveComponent>(ent);
        RemCompDeferred<ReproductivePartnerComponent>(ent);
        RemCompDeferred<StrapComponent>(ent);
        // RW end
    }

    private void OnShutdown(Entity<ObraDinnHologramComponent> ent, ref ComponentShutdown arg)
    {
        var effect =SpawnAtPosition(ent.Comp.SpawnEffect,Transform(ent).Coordinates);
        _audio.PlayPvs(ent.Comp.Sound, effect );
    }

    private void OnStorage(Entity<ObraDinnHologramComponent> ent, ref InsertIntoEntityStorageAttemptEvent arg)
    {
        arg.Cancelled = true;
    }

    // RW start
    // TODO: Revert when Goobstation fixes Obra Dinn holograms interactability upstream.
    private void OnGettingInteractedWith(Entity<ObraDinnHologramComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnInteractHand(Entity<ObraDinnHologramComponent> ent, ref InteractHandEvent args)
    {
        args.Handled = true;
    }

    private void OnInteractUsing(Entity<ObraDinnHologramComponent> ent, ref InteractUsingEvent args)
    {
        args.Handled = true;
    }

    private void OnDragDropTarget(Entity<ObraDinnHologramComponent> ent, ref DragDropTargetEvent args)
    {
        args.Handled = true;
    }

    private void OnCanDropTarget(Entity<ObraDinnHologramComponent> ent, ref CanDropTargetEvent args)
    {
        args.CanDrop = false;
        args.Handled = true;
    }

    private void OnCanDropDragged(Entity<ObraDinnHologramComponent> ent, ref CanDropDraggedEvent args)
    {
        args.CanDrop = false;
        args.Handled = true;
    }

    private void OnGetInteractionVerbs(Entity<ObraDinnHologramComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        var butcherText = Loc.GetString("butcherable-verb-name");
        args.Verbs.RemoveWhere(verb => verb.Text == butcherText);
    }
    // RW end
}

