// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Text;
using Content.Shared.HealthExaminable;
using Content.Shared.Speech;
using Content.Server._EinsteinEngines.Language;
using Content.Server._RW.Zombies;
using Content.Server.Actions;
using Content.Server.Body.Systems;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Emoting.Systems;
using Content.Server.EUI;
using Content.Server.Roles;
using Content.Server.Speech.EntitySystems;
using Content.Server.Roles;
using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared._EinsteinEngines.Language.Events;
using Content.Shared._Shitmed.Damage;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Anomaly.Components;
using Content.Shared.Armor;
using Content.Shared.Bed.Sleep;
using Content.Shared.Blocking;
using Content.Shared.Cloning.Events;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Roles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Body.Components;
using Content.Server.Temperature.Components;
using Content.Server.Body.Components;
using Content.Server.Atmos.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Goobstation.Common.Traits;
using Content.Shared.Interaction.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Hands.Components;
using Content.Shared.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.CombatMode.Pacification;
using Content.Server.Speech.Components;
using Content.Goobstation.Shared.Sprinting;
using Content.Shared.Prying.Components;
using Content.Shared.Temperature.Components;
using Content.Server.Polymorph.Components;

// Goob end

namespace Content.Server.Zombies
{
    public sealed partial class ZombieSystem : SharedZombieSystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
        [Dependency] private readonly EmoteOnDamageSystem _emoteOnDamage = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedRoleSystem _role = default!;
        [Dependency] private readonly LanguageSystem _language = default!;
        [Dependency] private readonly EuiManager _eui = default!; // RW edit
        [Dependency] private readonly MobThresholdSystem _mobThreshold = default!; // RW edit

        public readonly ProtoId<NpcFactionPrototype> Faction = "Zombie";

        // RW start
        private const int PendingZombieStagePale = PendingZombieComponent.PendingZombieStagePale;
        private const int PendingZombieStageSlowed = PendingZombieComponent.PendingZombieStageSlowed;
        private const int PendingZombieStageFinal = PendingZombieComponent.PendingZombieStageFinal;
        private const string PendingZombieLanguage = "Zombie";
        private const double PendingZombieStageSlowedSeconds = 180;
        private const double PendingZombieStageFinalSeconds = 300;
        private const double PendingZombieDeathSeconds = 420;
        private const double PendingZombieForcedTransformSeconds = 540;
        private static readonly TimeSpan PendingZombieTickInterval = TimeSpan.FromSeconds(1);
        private static readonly Color PendingZombieGreenSkin = new(0.45f, 0.51f, 0.29f);
        private static readonly string[] PendingZombieGroans =
        [
            "accent-words-zombie-1",
            "accent-words-zombie-2",
            "accent-words-zombie-3",
            "accent-words-zombie-4",
            "accent-words-zombie-5",
            "accent-words-zombie-6",
            "accent-words-zombie-7",
            "accent-words-zombie-8",
            "accent-words-zombie-9",
            "accent-words-zombie-10",
        ];
        // RW end

        public const SlotFlags ProtectiveSlots =
            SlotFlags.FEET |
            SlotFlags.HEAD |
            SlotFlags.EYES |
            SlotFlags.GLOVES |
            SlotFlags.MASK |
            SlotFlags.NECK |
            SlotFlags.INNERCLOTHING |
            SlotFlags.OUTERCLOTHING;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ZombieComponent, EmoteEvent>(OnEmote, before:
                new[] { typeof(VocalSystem), typeof(BodyEmotesSystem) });

            SubscribeLocalEvent<ZombieComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<ZombieComponent, MobStateChangedEvent>(OnMobState);
            SubscribeLocalEvent<ZombieComponent, CloningEvent>(OnZombieCloning);
            SubscribeLocalEvent<ZombieComponent, TryingToSleepEvent>(OnSleepAttempt);
            SubscribeLocalEvent<ZombieComponent, GetCharactedDeadIcEvent>(OnGetCharacterDeadIC);
            SubscribeLocalEvent<ZombieComponent, GetCharacterUnrevivableIcEvent>(OnGetCharacterUnrevivableIC);
            SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnStartup); // RW edit
            SubscribeLocalEvent<ZombieComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<ZombieComponent, MindRemovedMessage>(OnMindRemoved);

            SubscribeLocalEvent<PendingZombieComponent, MapInitEvent>(OnPendingMapInit);
            SubscribeLocalEvent<PendingZombieComponent, BeforeRemoveAnomalyOnDeathEvent>(OnBeforeRemoveAnomalyOnDeath);

            SubscribeLocalEvent<IncurableZombieComponent, MapInitEvent>(OnPendingMapInit);

            SubscribeLocalEvent<ZombifyOnDeathComponent, MobStateChangedEvent>(OnDamageChanged);

            // Goob Edit - Prevent Zombies Speaking/Understanding Languages
            SubscribeLocalEvent<ZombieComponent, DetermineEntityLanguagesEvent>(OnLanguageApply);
            SubscribeLocalEvent<ZombieComponent, ComponentShutdown>(OnShutdown);

            // RW start
            SubscribeLocalEvent<PendingZombieComponent, ComponentShutdown>(OnPendingShutdown);
            SubscribeLocalEvent<PendingZombieComponent, DetermineEntityLanguagesEvent>(OnPendingLanguageApply);
            SubscribeLocalEvent<PendingZombieComponent, AccentGetEvent>(OnPendingAccentGet);
            SubscribeLocalEvent<PendingZombieComponent, HealthBeingExaminedEvent>(OnPendingHealthExamined);
            // RW end
            // more goob something something unzombify this shit needs cleanup
            SubscribeLocalEvent<ZombieComponent, EntityUnZombifiedEvent>(OnUnZombifyEvent);
        }

        private void OnBeforeRemoveAnomalyOnDeath(Entity<PendingZombieComponent> ent, ref BeforeRemoveAnomalyOnDeathEvent args)
        {
            // Pending zombies (e.g. infected non-zombies) do not remove their hosted anomaly on death.
            // Current zombies DO remove the anomaly on death.
            args.Cancelled = true;
        }

        private void OnPendingMapInit(EntityUid uid, IncurableZombieComponent component, MapInitEvent args)
        {
            _actions.AddAction(uid, ref component.Action, component.ZombifySelfActionPrototype);
            _faction.AddFaction(uid, Faction);

            if (HasComp<ZombieComponent>(uid) || HasComp<ZombieImmuneComponent>(uid))
                return;

            EnsureComp(uid, out PendingZombieComponent pendingComp);

            pendingComp.GracePeriod = pendingComp.MinInitialInfectedGrace == pendingComp.MaxInitialInfectedGrace
                ? pendingComp.MinInitialInfectedGrace
                : _random.Next(pendingComp.MinInitialInfectedGrace, pendingComp.MaxInitialInfectedGrace);
        }

        private void OnPendingMapInit(EntityUid uid, PendingZombieComponent component, MapInitEvent args)
        {
            if (_mobState.IsDead(uid))
            {
                ZombifyEntity(uid);
                return;
            }

            component.NextTick = _timing.CurTime + TimeSpan.FromSeconds(1f);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var curTime = _timing.CurTime;

            // Hurt the living infected
            var query = EntityQueryEnumerator<PendingZombieComponent, DamageableComponent, MobStateComponent>();
            while (query.MoveNext(out var uid, out var comp, out var damage, out _))
            {
                // Process only once per second
                if (comp.NextTick > curTime)
                    continue;

                comp.NextTick = curTime + PendingZombieTickInterval;

                if (comp.GracePeriod > TimeSpan.Zero)
                {
                    comp.GracePeriod -= PendingZombieTickInterval;
                    if (comp.GracePeriod > TimeSpan.Zero)
                    {
                        continue;
                    }

                    comp.GracePeriod = TimeSpan.Zero;
                }

                // RW start
                comp.ActiveDuration += PendingZombieTickInterval;
                var seconds = comp.ActiveDuration.TotalSeconds;

                var newStage = seconds >= PendingZombieStageFinalSeconds
                    ? PendingZombieStageFinal
                    : seconds >= PendingZombieStageSlowedSeconds
                        ? PendingZombieStageSlowed
                        : PendingZombieStagePale;

                if (newStage != comp.CurrentStage)
                {
                    comp.CurrentStage = newStage;
                    ApplyPendingZombieStage(uid, comp, newStage);
                }
                else if (comp.CurrentStage == PendingZombieStageSlowed)
                {
                    UpdatePendingZombieStageTwoAppearance(uid, comp, seconds);
                }

                Dirty(uid, comp);

                if (comp.CurrentStage == PendingZombieStageFinal)
                {
                    if (seconds >= PendingZombieForcedTransformSeconds)
                    {
                        ZombifyEntity(uid);
                        continue;
                    }

                    if (_mobThreshold.TryGetThresholdForState(uid, MobState.Dead, out var deadThreshold)
                        && deadThreshold.HasValue)
                    {
                        var damageLeft = deadThreshold.Value - damage.TotalDamage;
                        if (damageLeft > 0)
                        {
                            var timeLeft = PendingZombieDeathSeconds - seconds;
                            if (timeLeft > 0)
                            {
                                var dmgTick = damageLeft.Float() / (float) timeLeft;
                                var dmgSpec = new DamageSpecifier();
                                dmgSpec.DamageDict.Add("Poison", dmgTick);
                                _damageable.TryChangeDamage(uid, dmgSpec, true, false, damage, targetPart: TargetBodyPart.All, splitDamage: SplitDamageBehavior.SplitEnsureAll);
                            }
                        }
                    }
                }
                // RW end
            }

            // Heal the zombified
            var zombQuery = EntityQueryEnumerator<ZombieComponent, DamageableComponent, MobStateComponent>();
            while (zombQuery.MoveNext(out var uid, out var comp, out var damage, out var mobState))
            {
                // Process only once per second
                if (comp.NextTick + TimeSpan.FromSeconds(1) > curTime)
                    continue;

                comp.NextTick = curTime;

                if (_mobState.IsDead(uid, mobState))
                    continue;

                var multiplier = _mobState.IsCritical(uid, mobState)
                    ? comp.PassiveHealingCritMultiplier
                    : 1f;

                // Gradual healing for living zombies.
                _damageable.TryChangeDamage(uid,
                    comp.PassiveHealing * multiplier,
                    true,
                    false,
                    damage,
                    ignoreBlockers: true, // Shitmed Change
                    targetPart: TargetBodyPart.All, // Shitmed Change
                    splitDamage: SplitDamageBehavior.SplitEnsureAll); // Shitmed Change
            }
        }

        private void OnSleepAttempt(EntityUid uid, ZombieComponent component, ref TryingToSleepEvent args)
        {
            args.Cancelled = true;
        }

        private void OnGetCharacterDeadIC(EntityUid uid, ZombieComponent component, ref GetCharactedDeadIcEvent args)
        {
            args.Dead = true;
        }

        private void OnGetCharacterUnrevivableIC(EntityUid uid, ZombieComponent component, ref GetCharacterUnrevivableIcEvent args)
        {
            args.Unrevivable = true;
        }

        private void OnStartup(EntityUid uid, ZombieComponent component, ComponentStartup args)
        {
            if (component.EmoteSoundsId == null
                || TerminatingOrDeleted(uid)) // Goob Change
                return;

            // Goobstation Change Start
            var comp = EnsureComp<LanguageSpeakerComponent>(uid); // Ensure they can speak language before adding language.
            if (!string.IsNullOrEmpty(component.ForcedLanguage)) // Should never be false, but security either way.
                comp.CurrentLanguage = component.ForcedLanguage;
            _language.UpdateEntityLanguages(uid);
            // Goobstation Change End
        }

        private void OnEmote(EntityUid uid, ZombieComponent component, ref EmoteEvent args)
        {
            // always play zombie emote sounds and ignore others
            if (args.Handled)
                return;

            _protoManager.Resolve(component.EmoteSoundsId, out var sounds);

            args.Handled = _chat.TryPlayEmoteSound(uid, sounds, args.Emote);
        }

        private void OnMobState(EntityUid uid, ZombieComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState == MobState.Alive)
            {
                // Groaning when damaged
                EnsureComp<EmoteOnDamageComponent>(uid);
                _emoteOnDamage.AddEmote(uid, "Scream");

                // Random groaning
                EnsureComp<AutoEmoteComponent>(uid);
                _autoEmote.AddEmote(uid, "ZombieGroan");
            }
            else
            {
                // Stop groaning when damaged
                _emoteOnDamage.RemoveEmote(uid, "Scream");

                // Stop random groaning
                _autoEmote.RemoveEmote(uid, "ZombieGroan");
            }
        }

        private bool IsUserBlocking(BlockingUserComponent? component) // Goobstation
        {
            return (TryComp<BlockingComponent>(component?.BlockingItem, out var blockComp) && blockComp.IsBlocking);
        }

        private float GetZombieInfectionChance(EntityUid uid, ZombieComponent zombieComponent)
        {
            var chance = zombieComponent.BaseZombieInfectionChance;

            var armorEv = new CoefficientQueryEvent(ProtectiveSlots);
            RaiseLocalEvent(uid, armorEv);
            foreach (var resistanceEffectiveness in zombieComponent.ResistanceEffectiveness.DamageDict)
            {
                if (armorEv.DamageModifiers.Coefficients.TryGetValue(resistanceEffectiveness.Key, out var coefficient))
                {
                    // Scale the coefficient by the resistance effectiveness, very descriptive I know
                    // For example. With 30% slash resist (0.7 coeff), but only a 60% resistance effectiveness for slash,
                    // you'll end up with 1 - (0.3 * 0.6) = 0.82 coefficient, or a 18% resistance
                    var adjustedCoefficient = 1 - ((1 - coefficient) * resistanceEffectiveness.Value.Float());
                    chance *= adjustedCoefficient;
                }
            }

            var zombificationResistanceEv = new ZombificationResistanceQueryEvent(ProtectiveSlots);
            RaiseLocalEvent(uid, zombificationResistanceEv);
            chance *= zombificationResistanceEv.TotalCoefficient;

            return MathF.Max(chance, zombieComponent.MinZombieInfectionChance);
        }

        private void OnMeleeHit(Entity<ZombieComponent> entity, ref MeleeHitEvent args)
        {
            if (!args.IsHit)
                return;

            var cannotSpread = HasComp<NonSpreaderZombieComponent>(args.User);

            foreach (var uid in args.HitEntities)
            {
                if (args.User == uid)
                    continue;

                if (!TryComp<MobStateComponent>(uid, out var mobState))
                    continue;

                if (HasComp<ZombieComponent>(uid) || HasComp<IncurableZombieComponent>(uid))
                {
                    // Don't infect, don't deal damage, do not heal from bites, don't pass go!
                    args.Handled = true;
                    continue;
                }

                if (_mobState.IsAlive(uid, mobState))
                {
                    _damageable.TryChangeDamage(args.User, entity.Comp.HealingOnBite, true, false);

                    // If we cannot infect the living target, the zed will just heal itself.
                    if (HasComp<ZombieImmuneComponent>(uid) || cannotSpread ||
                        !_random.Prob(GetZombieInfectionChance(uid, entity.Comp)))
                        continue;


                    if (TryComp<BlockingUserComponent>(entity, out var blockingUser) &&
                        IsUserBlocking(
                            blockingUser)) // Goobstation edit - prevents infection if user is actively blocking
                        return;

                    EnsureComp<PendingZombieComponent>(uid);
                    EnsureComp<ZombifyOnDeathComponent>(uid);
                }
                else
                {
                    if (HasComp<ZombieImmuneComponent>(uid) || cannotSpread)
                        continue;

                    // If the target is dead and can be infected, infect.
                    ZombifyEntity(uid);
                    args.Handled = true;
                }
            }
        }

        private void OverrideComp<T>(EntityUid target, EntityUid source) where T : IComponent // Goob, for below function
        {
            if (!TryComp(source, out T? toCopy))
            {
                RemComp<T>(target);
                return;
            }

            CopyComp<T>(source, target, toCopy);
        }

        /// <summary>
        ///     This is the function to call if you want to unzombify an entity.
        /// </summary>
        /// <param name="source">the entity having the ZombieComponent</param>
        /// <param name="target">the entity you want to unzombify (different from source in case of cloning, for example)</param>
        /// <param name="zombiecomp"></param>
        /// <remarks>
        ///     this currently only restore the skin/eye color from before zombified
        ///     TODO: completely rethink how zombies are done to allow reversal.
        /// </remarks>
        public bool UnZombify(EntityUid source, EntityUid target, ZombieComponent? zombiecomp)
        {
            if (!Resolve(source, ref zombiecomp))
                return false;

            foreach (var (layer, info) in zombiecomp.BeforeZombifiedCustomBaseLayers)
            {
                _humanoidAppearance.SetBaseLayerColor(target, layer, info.Color);
                _humanoidAppearance.SetBaseLayerId(target, layer, info.Id);
            }
            if (TryComp<HumanoidAppearanceComponent>(target, out var appcomp))
            {
                appcomp.EyeColor = zombiecomp.BeforeZombifiedEyeColor;
            }
            _humanoidAppearance.SetSkinColor(target, zombiecomp.BeforeZombifiedSkinColor, false);
            _bloodstream.ChangeBloodReagents(target, zombiecomp.BeforeZombifiedBloodReagents);

            return true;
        }

        private void OnZombieCloning(Entity<ZombieComponent> ent, ref CloningEvent args)
        {
            // Goob - trolled, just use cure
            //UnZombify(ent.Owner, args.CloneUid, ent.Comp);
        }

        // Make sure players that enter a zombie (for example via a ghost role or the mind swap spell) count as an antagonist.
        private void OnMindAdded(Entity<ZombieComponent> ent, ref MindAddedMessage args)
        {
            if (!_role.MindHasRole<ZombieRoleComponent>(args.Mind))
                _role.MindAddRole(args.Mind, "MindRoleZombie", mind: args.Mind.Comp);

            // RW start
            if (_player.TryGetSessionById(args.Mind.Comp.UserId, out var session))
                _eui.OpenEui(new ZombieRoleEui(), session);
            // RW end
        }

        // Remove the role when getting cloned, getting gibbed and borged, or leaving the body via any other method.
        private void OnMindRemoved(Entity<ZombieComponent> ent, ref MindRemovedMessage args)
        {
            _role.MindRemoveRole<ZombieRoleComponent>((args.Mind.Owner, args.Mind.Comp));
        }

        #region Goob Changes

        /// <summary>
        /// Tries to cure the entity of zombification by reverting its polymorph
        /// </summary>
        /// <param name="ent">Entity to cure.</param>
        /// <param name="currentUid">Entity to use now, differs if succeeded.</param>
        /// <returns></returns>
        private bool TryCureZombie(Entity<ZombieComponent> ent, out EntityUid currentUid)
        {
            if (TryComp(ent, out PolymorphedEntityComponent? comp)
                && _polymorph.Revert((ent, comp)) is { } uid)
                currentUid = uid;
            else
                currentUid = ent.Owner;
            return currentUid != ent.Owner;
        }

        private void OnUnZombifyEvent(Entity<ZombieComponent> ent, ref EntityUnZombifiedEvent args)
        {
            bool success = TryCureZombie(ent, out EntityUid currentUid);
            _popup.PopupEntity(
                Loc.GetString($"zombie-cure-{(success ? "success" : "failed")}"),
                currentUid,
                PopupType.Medium
            );

            // we want to make sure this is added to the reverted ent
            if (args.Inoculate)
                EnsureComp<ZombieImmuneComponent>(currentUid);
        }

        /// <summary>
        ///     This forces the languages to reset and apply only the current language for the entity based on Zombie Component.
        /// </summary>
        private void OnLanguageApply(Entity<ZombieComponent> ent, ref DetermineEntityLanguagesEvent args)
        {
            if (ent.Comp.LifeStage is ComponentLifeStage.Removing
                or ComponentLifeStage.Stopping
                or ComponentLifeStage.Stopped)
                return;

            // Clear the languages and then apply the forced language.
            args.SpokenLanguages.Clear();
            args.UnderstoodLanguages.Clear();
            args.SpokenLanguages.Add(ent.Comp.ForcedLanguage);
            args.UnderstoodLanguages.Add(ent.Comp.ForcedLanguage);
        }

        // When comp is removed, reset languages.
        private void OnShutdown(Entity<ZombieComponent> ent, ref ComponentShutdown args)
        {
            if (TerminatingOrDeleted(ent))
                return;

            _language.UpdateEntityLanguages(ent.Owner); // This uses ent.Owner because UpdateEntityLanguages checks for <LanguageSpeakerComponent>.
        }

        #endregion

        // RW start
        private void OnPendingShutdown(EntityUid uid, PendingZombieComponent component, ComponentShutdown args)
        {
            if (TerminatingOrDeleted(uid) || HasComp<ZombieComponent>(uid))
                return;

            if (component.OriginalSkinColor is { } originalColor && TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            {
                _humanoidAppearance.SetSkinColor(uid, originalColor, verify: false, humanoid: humanoid);
            }

            if (component.CurrentStage >= PendingZombieStageSlowed)
                _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);

            if (component.CurrentStage >= PendingZombieStageFinal)
                _language.UpdateEntityLanguages(uid);
        }

        private void OnPendingLanguageApply(EntityUid uid, PendingZombieComponent component, ref DetermineEntityLanguagesEvent args)
        {
            if (HasComp<ZombieComponent>(uid))
                return;

            if (component.CurrentStage == PendingZombieStageFinal)
            {
                args.SpokenLanguages.Remove(PendingZombieLanguage);
                args.UnderstoodLanguages.Add(PendingZombieLanguage);
            }
        }

        private void OnPendingAccentGet(EntityUid uid, PendingZombieComponent component, AccentGetEvent args)
        {
            if (HasComp<ZombieComponent>(uid))
                return;

            if (component.CurrentStage >= PendingZombieStageSlowed)
            {
                var message = args.Message;
                var words = message.Split(' ');
                var replacedAny = false;

                for (var i = 0; i < words.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(words[i]))
                        continue;

                    if (_random.Prob(0.25f))
                    {
                        var groanKey = _random.Pick(PendingZombieGroans);
                        words[i] = Loc.GetString(groanKey);
                        replacedAny = true;
                    }
                }

                if (replacedAny)
                {
                    message = string.Join(" ", words);
                }

                args.Message = Accentuate(message, 0.8f);
            }
        }

        private void OnPendingHealthExamined(EntityUid uid, PendingZombieComponent component, ref HealthBeingExaminedEvent args)
        {
            if (component.CurrentStage == PendingZombieStagePale)
            {
                args.Message.PushNewline();
                args.Message.AddMarkupOrThrow(Loc.GetString("bloodstream-component-looks-pale", ("target", uid)));
            }
            else if (component.CurrentStage == PendingZombieStageSlowed)
            {
                args.Message.PushNewline();
                args.Message.AddMarkupOrThrow(Loc.GetString("zombie-infection-looks-greenish", ("target", uid)));
            }
        }

        private void ApplyPendingZombieStage(EntityUid uid, PendingZombieComponent component, int newStage)
        {
            if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            {
                component.OriginalSkinColor ??= humanoid.SkinColor;
                var originalColor = component.OriginalSkinColor.Value;

                var skinColor = newStage switch
                {
                    PendingZombieStagePale => Color.InterpolateBetween(originalColor, Color.White, 0.4f),
                    PendingZombieStageSlowed => GetPendingZombieStageTwoSkinColor(component, 0),
                    PendingZombieStageFinal => PendingZombieGreenSkin,
                    _ => humanoid.SkinColor,
                };

                _humanoidAppearance.SetSkinColor(uid, skinColor, verify: false, humanoid: humanoid);
            }

            if (newStage == PendingZombieStagePale)
            {
                _popup.PopupEntity(Loc.GetString("zombie-infection-stage-pale"), uid, uid);
            }
            else if (newStage == PendingZombieStageSlowed)
            {
                _popup.PopupEntity(Loc.GetString("zombie-infection-stage-slowed"), uid, uid);
                _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
            }
            else if (newStage == PendingZombieStageFinal)
            {
                _popup.PopupEntity(Loc.GetString("zombie-infection-stage-final"), uid, uid);
                EnsureComp<LanguageSpeakerComponent>(uid);
                _language.UpdateEntityLanguages(uid);
            }
        }

        private void UpdatePendingZombieStageTwoAppearance(EntityUid uid, PendingZombieComponent component, double seconds)
        {
            if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
                return;

            var stageProgress = Math.Clamp(
                (seconds - PendingZombieStageSlowedSeconds) / (PendingZombieStageFinalSeconds - PendingZombieStageSlowedSeconds),
                0,
                1);

            _humanoidAppearance.SetSkinColor(uid, GetPendingZombieStageTwoSkinColor(component, stageProgress), verify: false, humanoid: humanoid);
        }

        private Color GetPendingZombieStageTwoSkinColor(PendingZombieComponent component, double stageProgress)
        {
            var originalColor = component.OriginalSkinColor ?? Color.White;
            var paleColor = Color.InterpolateBetween(originalColor, Color.White, 0.4f);
            return Color.InterpolateBetween(paleColor, PendingZombieGreenSkin, (float) stageProgress);
        }

        private string Accentuate(string message, float scale)
        {
            var sb = new StringBuilder();

            foreach (var character in message)
            {
                if (_random.Prob(scale / 3f))
                {
                    var lower = char.ToLowerInvariant(character);
                    var newString = lower switch
                    {
                        'o' => "u",
                        's' => "ch",
                        'a' => "ah",
                        'u' => "oo",
                        'c' => "k",
                        // Russian
                        'о' => "у",
                        'с' => "ш",
                        'а' => "ах",
                        'у' => "уу",
                        'и' => "ы",
                        'е' => "ээ",
                        _ => $"{character}",
                    };

                    sb.Append(newString);
                }

                if (_random.Prob(scale / 20f))
                {
                    if (character == ' ')
                    {
                        sb.Append(Loc.GetString("slur-accent-confused"));
                    }
                    else if (character == '.')
                    {
                        sb.Append(' ');
                        sb.Append(Loc.GetString("slur-accent-burp"));
                    }
                }

                if (!_random.Prob(scale * 3 / 20))
                {
                    sb.Append(character);
                    continue;
                }

                var next = _random.Next(1, 3) switch
                {
                    1 => "'",
                    2 => $"{character}{character}",
                    _ => $"{character}{character}{character}",
                };

                sb.Append(next);
            }

            return sb.ToString();
        }
        // RW end
    }
}
