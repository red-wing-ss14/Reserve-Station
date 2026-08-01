// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Ghost;
using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Pointing;
#if SERVER
using Content.Server.Mobs;
#endif
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.Body.Systems
{
    public sealed class BrainSystem : EntitySystem
    {
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;
        [Dependency] private readonly SharedBodySystem _bodySystem = default!; // Shitmed Change
        [Dependency] private readonly MetaDataSystem _metaData = default!; // RW
        [Dependency] private readonly MobStateSystem _mobState = default!; // RW
#if SERVER
        [Dependency] private readonly DeathgaspSystem _deathgasp = default!; // RW
#endif

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BrainComponent, OrganAddedToBodyEvent>(HandleAddition);
        // Shitmed Change Start
            SubscribeLocalEvent<BrainComponent, OrganRemovedFromBodyEvent>(HandleRemoval);
            SubscribeLocalEvent<BrainComponent, PointAttemptEvent>(OnPointAttempt);
            SubscribeLocalEvent<DebrainedComponent, ExaminedEvent>(OnBodyExamined); // RW
        }

        private void HandleRemoval(EntityUid uid, BrainComponent brain, ref OrganRemovedFromBodyEvent args)
        {
            if (TerminatingOrDeleted(uid)
                || TerminatingOrDeleted(args.OldBody))
                return;

            // goob start
            var remEv = new BeforeBrainRemovedEvent();
            RaiseLocalEvent(args.OldBody, ref remEv);

            if (remEv.Blocked)
                return;

            // goob end

            brain.Active = false;
            if (!CheckOtherBrains(args.OldBody))
            {
                TryRenameBrain((uid, brain), args.OldBody); // RW
                // Prevents revival, should kill the user within a given timespan too.
                EnsureComp<DebrainedComponent>(args.OldBody);
#if SERVER
                // RW-Start: Debraining should instantly kill
                if (!_mobState.IsDead(args.OldBody))
                {
                    _mobState.ChangeMobState(args.OldBody, MobState.Dead);
                    _deathgasp.Deathgasp(args.OldBody);
                }
                // RW-End
#endif
                HandleMind(uid, args.OldBody);
            }
        }

        private void HandleAddition(EntityUid uid, BrainComponent brain, ref OrganAddedToBodyEvent args)
        {
            if (TerminatingOrDeleted(uid)
                || TerminatingOrDeleted(args.Body))
                return;

            // goob start
            var addEv = new BeforeBrainAddedEvent();
            RaiseLocalEvent(args.Body, ref addEv);

            if (addEv.Blocked)
                return;

            // goob end

            if (!CheckOtherBrains(args.Body))
            {
                TryRenameBrain((uid, brain), args.Body); // RW
                RemComp<DebrainedComponent>(args.Body);
                HandleMind(args.Body, uid, brain);
            }
        }


        private void HandleMind(EntityUid newEntity, EntityUid oldEntity, BrainComponent? brain = null)
        {
            if (TerminatingOrDeleted(newEntity) || TerminatingOrDeleted(oldEntity))
                return;

            EnsureComp<MindContainerComponent>(newEntity);
            EnsureComp<MindContainerComponent>(oldEntity);

            var ghostOnMove = EnsureComp<GhostOnMoveComponent>(newEntity);
            ghostOnMove.MustBeDead = HasComp<MobStateComponent>(newEntity); // Don't ghost living players out of their bodies.

            if (!_mindSystem.TryGetMind(oldEntity, out var mindId, out var mind))
                return;

            _mindSystem.TransferTo(mindId, newEntity, mind: mind);
            // RW-Edit-Start
            if (brain != null)
                brain.Active = true;
            // RW-Edit-End
        }

        private bool CheckOtherBrains(EntityUid entity)
        {
            var hasOtherBrains = false;
            if (TryComp<BodyComponent>(entity, out var body))
            {
                if (TryComp<BrainComponent>(entity, out _))
                    hasOtherBrains = true;
                else
                {
                    foreach (var (organ, _) in _bodySystem.GetBodyOrgans(entity, body))
                    {
                        if (TryComp<BrainComponent>(organ, out var brain) && brain.Active)
                        {
                            hasOtherBrains = true;
                            break;
                        }
                    }
                }
            }

            return hasOtherBrains;
        }

        // Shitmed Change End
        private void OnPointAttempt(Entity<BrainComponent> ent, ref PointAttemptEvent args)
        {
            args.Cancel();
        }

        // RW-Start
        private void TryRenameBrain(Entity<BrainComponent> ent, EntityUid playerEntity)
        {
            if (ent.Comp.Renamed || TerminatingOrDeleted(ent))
                return;

            var newName = Loc.GetString("comp-brain-name", ("name", Name(ent)), ("player", Name(playerEntity)));
            _metaData.SetEntityName(ent, newName);

            ent.Comp.Renamed = true;
        }

        private void OnBodyExamined(Entity<DebrainedComponent> ent, ref ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("comp-brain-examine-debrained", ("entity", ent)));
        }
        // RW-End
    }
}
