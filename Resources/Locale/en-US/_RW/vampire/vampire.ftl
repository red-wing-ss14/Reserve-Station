## Base actions

alerts-vampire-blood-name = Blood Drunk
alerts-vampire-blood-desc = Shows how much blood you've drunk. Extend your fangs and left-click a target to drink.

alerts-vampire-fed-name = Blood Fullness
alerts-vampire-fed-desc = Your current blood fullness. Drink blood to stay fed.

roles-antag-vamire-name = Vampire
roles-antag-vampire-description = Feed on the crew. Extend your fangs and drink their blood.

vampire-roundend-name = vampire

vampire-drink-start = You sink your fangs into {CAPITALIZE(THE($target))}.

vampire-not-enough-blood = Not enough blood.

vampire-mouth-covered = Your mouth is covered!

vampire-drink-invalid-target = You cannot drink blood from vampires or their thralls.

vampire-target-protected-by-faith = This person is protected by their faith!

vampire-drink-target-maxed = You have already drunk { $amount } units of blood from this target.

vampire-drink-target-hard-max = You have drunk the maximum amount of blood from this target ({ $amount } units).

vampire-full-power-achieved = Your vampiric essence surges full power achieved!

vampire-umbrae-full-power-fov = The shadows bend to your will. You can now see through walls!

vampire-role-greeting = You are a vampire!

    Your blood thirst compels you to feed on crew members. Use your abilities to turn other crew.

    Your fangs allow you to suck blood from humans. Blood will regenerate health and give you new abilities.

    Find something to accomplish during this shift!

# Objectives

objective-issuer-vampire = [color=crimson]Vampire[/color]

objective-condition-drain-title = Drain {$count} units of blood
objective-condition-drain-description = Drink {$count} units of blood from crew members using your fangs.

objective-vampire-thrall-obey-master-title = Obey your master, {$targetName}.

# Class selection action

ent-ActionClassSelectId =
    .name = Select vampire class
    .desc = Choose your vampire subclass

# Round end statistics

roundend-prepend-vampire-drained-low = The vampires barely fed this shift, draining only {$blood} units of blood.
roundend-prepend-vampire-drained-medium = The vampires had a decent meal, draining {$blood} units of blood.
roundend-prepend-vampire-drained-high = The vampires had a blood feast, draining {$blood} units of blood!
roundend-prepend-vampire-drained-critical = The vampires went on a feeding frenzy, draining a staggering {$blood} units of blood!
roundend-prepend-vampire-drained = No vampires managed to drain any significant amount of blood this round.
roundend-prepend-vampire-drained-named = {$name} was the most bloodthirsty vampire, draining {$number} units of blood total.

# Vampire class selection tooltips

vampire-class-hemomancer-tooltip = Hemomancer

    Focuses on blood magic and the manipulation of blood around you

vampire-class-umbrae-tooltip = Umbrae

    Focuses on darkness, stealth ambushing and mobility

vampire-class-gargantua-tooltip = Gargantua

    Focuses on tenacity and melee damage

vampire-class-dantalion-tooltip = Dantalion

    Focuses on thralling and illusions

# Hemomancer abilities

action-vampire-hemomancer-tendrils-wrong-place = Cannot cast there.

action-vampire-blood-barrier-wrong-place = Cannot place barriers there.

action-vampire-sanguine-pool-already-in = You are already in sanguine pool form!

action-vampire-sanguine-pool-invalid-tile = You cannot become a blood pool here.

action-vampire-sanguine-pool-enter = You transform into a pool of blood!

action-vampire-sanguine-pool-exit = You reform from the blood pool!

vampire-space-burn-warning = The harsh void light scorches your undead flesh!

action-vampire-blood-eruption-activated = You cause blood to erupt in spikes around you!

action-vampire-blood-bringers-rite-not-enough-power = You lack full vampiric power (need above 1000 total blood & 8 unique victims)

action-vampire-blood-brighters-rite-not-enough-blood = Not enough blood to activate blood bringers rite

action-vampire-blood-bringers-rite-start = Blood Bringers Rite activated!

action-vampire-blood-bringers-rite-stop = Blood bringers rite deactivated

action-vampire-blood-bringers-rite-stop-blood = Blood Bringers Rite deactivated - not enough blood

vampire-locate-result = Your senses trace { $target } to { $location }.

vampire-locate-not-same-sector = That person is not on your sector.

vampire-locate-unknown = Unknown area

vampire-locate-no-targets = No prey can be sensed on this sector.

predator-sense-title = Predator Sense

vampire-locate-search-placeholder = Search...

vampiric-claws-remove-popup = You make claws disappear.

# Umbrae abilities

action-vampire-cloak-of-darkness-start = You blend into the shadows!

action-vampire-cloak-of-darkness-stop = You step out of the shadows.

action-vampire-shadow-snare-placed = You set a shadow snare trap.

action-vampire-shadow-snare-wrong-place = You can't place a trap here.

action-vampire-shadow-snare-scatter = You scattered the shadow trap.

vampire-shadow-snare-oldest-removed = Your old shadow snare dissipates.

ent-shadow-snare-ensnare = shadow snare

action-vampire-shadow-anchor-returned = You returned to the shadow anchor

action-vampire-shadow-anchor-installed = You've secured a spot in the shadows

action-vampire-shadow-boxing-start = You begin shadow boxing.

action-vampire-shadow-boxing-stop = Shadow boxing has been stoped.

action-vampire-shadow-boxing-ends = Shadow boxing ends.

action-vampire-dark-passage-wrong-place = The darkness here is impenetrable...

action-vampire-dark-passage-activated = You slipped through the darkness...

action-vampire-extinguish-activated = You absorbed the light around you...({$count})

action-vampire-eternal-darkness-not-enough-blood = You have run out of blood to sustain eternal darkness.

action-vampire-eternal-darkness-start = You conjured eternal darkness...

action-vampire-eternal-darkness-stop = The eternal darkness has dissipated...

# Dantalion

vampire-enthrall-start = You reach into {CAPITALIZE(THE($target))}'s mind...

vampire-enthrall-success = {CAPITALIZE(THE($target))} bends the knee and becomes your thrall.

vampire-enthrall-target = Your mind is overwhelmed by vampiric domination!

vampire-enthrall-limit = You cannot control any more thralls.

vampire-enthrall-invalid = That target cannot be enthralled.

vampire-thrall-released = The vampiric hold over you fades.

vampire-pacify-invalid = That target cannot be pacified.

vampire-pacify-success = {CAPITALIZE(THE($target))} succumbs to your overwhelming serenity.

vampire-pacify-target = A crushing calm drowns your will to fight!

vampire-subspace-swap-thrall = You cannot subspace swap with your thralls.

vampire-subspace-swap-dead = That mind is beyond your reach.

vampire-subspace-swap-failed = The subspace rift fizzles uselessly.

vampire-subspace-swap-success = Space twists as you trade places with {CAPITALIZE(THE($target))}!

vampire-subspace-swap-target = Reality warps and you are torn into a new position!

vampire-rally-thralls-success = {$count ->
    [one] Your call rallies a thrall back to your side!
   *[other] Your call rallies {$count} thralls back to your side!
}

vampire-rally-thralls-none = None of your thralls can answer the call.

vampire-thrall-holy-water-freed = The holy water purges the vampires hold on your mind!

vampire-blood-bond-start = Rivers of blood knit you to your thralls.

vampire-blood-bond-stop = You let the blood bond fall slack.

vampire-blood-bond-no-thralls = You have no enthralled servants to bond with.

vampire-blood-bond-stop-blood = The bond shreds itself; you lack the blood to sustain it.

action-vampire-not-enough-power = Your power is insufficient (need >1000 total blood & 8 unique victims).

# Gargantua

vampire-blood-swell-start = Your muscles swell with unholy power

vampire-blood-swell-end = The blood rage subsides.

vampire-blood-rush-start = Blood surges through your limbs!

vampire-blood-rush-end = Your supernatural speed fades.

vampire-seismic-stomp-activate = The ground shudders beneath your fury!

vampire-overwhelming-force-start = Your presence becomes immovable.

vampire-overwhelming-force-stop = You relax your iron grip.

vampire-overwhelming-force-too-heavy = This object is far too heavy to move!

vampire-overwhelming-force-door-pried = You wrench the door open with brute strength.

vampire-demonic-grasp-hit = A demonic claw seizes you!

vampire-demonic-grasp-pull = The claw drags you toward the vampire!

vampire-charge-start = You barrel forward with unstoppable force!

vampire-charge-impact = You crash into {CAPITALIZE(THE($target))} with devastating force!

vampire-blood-swell-cancel-shoot = Your fingers don`t fit in the trigger guard!!

vampire-holy-place-burn = The sacred ground sears your unholy flesh!

alerts-vampire-blood-swell-name = Blood Swell
alerts-vampire-blood-swell-desc = Your muscles surge with unholy power.

alerts-vampire-blood-rush-name = Blood Rush
alerts-vampire-blood-rush-desc = Supernatural speed courses through your limbs.

# Admin antag verb

admin-verb-text-make-vampire = Make Vampire
admin-verb-make-vampire = Make the target into a vampire.

# Guidebook vampire entries

guide-entry-vampire = Vampire
guide-entry-vampire-progression = Vampire Progression
guide-entry-vampire-classes = Vampire Classes
guide-entry-vampire-counterplay = Countering Vampires

# Vampire prototype localization

mind-role-vampire-name = Vampire Role

ent-vampiric-claws-name = vampiric claws
ent-vampiric-claws-desc = Blood-forged claws that siphon vitae on hit. They dissipate after 15 swings, or if dispelled.

ent-vampire-decoy-name = vampire decoy

ent-vampire-sanguine-pool-name = sanguine pool
ent-vampire-sanguine-pool-desc = A sentient puddle of vampiric blood.

objective-vampire-survive-name = Survive
objective-vampire-survive-desc = I must survive no matter what.

objective-vampire-escape-name = Escape to CentComm alive and unrestrained.
objective-vampire-escape-desc = I need to escape on the evacuation shuttle without being captured.

objective-vampire-kill-random-desc = Do it however you like, just make sure they do not reach CentComm.

objective-vampire-thrall-obey-name = Obey your master
objective-vampire-thrall-obey-desc = You are enthralled. Follow your master's commands.

# Entity prototype localizations (ent- keys are auto-matched to prototype IDs)

ent-ActionVampireToggleFangs =
    .name = Toggle Fangs (Toggle)
    .desc = Extend or retract your fangs to drink blood from victims.

ent-ActionVampireGlare =
    .name = Glare (Free)
    .desc = Paralyze and mute nearby targets, dealing stamina damage over time.

ent-ActionVampireRejuvenateI =
    .name = Rejuvenate (Free)
    .desc = Instantly remove stuns and recover 100 stamina damage.

ent-ActionVampireRejuvenateII =
    .name = Rejuvenate (Free)
    .desc = Instantly remove stuns and recover 100 stamina, plus purge harmful reagents (10u) and heal 20 brute, 20 burn, 20 toxin, and 30 oxy loss.

ent-ActionVampireHemomancerClaws =
    .name = Vampiric Claws
    .desc = Create undroppable claws of blood. Each hit grants +5 blood. Has 15 swings. Use in hand to dispel.

ent-ActionVampireSanguinePool =
    .name = Sanguine Pool
    .desc = Transform into a pool of blood for 8 seconds, allowing movement through doors and windows.

ent-ActionVampireHemomancerTendrils =
    .name = Blood Tendrils
    .desc = After a short delay, tendrils erupt in a 3x3 area, poisoning and heavily slowing victims.

ent-ActionVampireBloodBarrier =
    .name = Blood Barrier
    .desc = Create 3 blood barriers at the target location. Vampires can pass through them.

ent-ActionVampirePredatorSense =
    .name = Predator Sense
    .desc = Hunt down your prey, there is nowhere to hide...

ent-ActionVampireBloodEruption =
    .name = Blood Eruption (100)
    .desc = Cause any blood within 4 tiles of you to erupt, dealing 50 brute damage to anyone standing on it.

ent-ActionVampireBloodBringersRite =
    .name = Blood Bringers Rite (Toggle)
    .desc = When toggled, everyone around you begins to bleed profusely. You drain their blood and rejuvenate with it.

ent-ActionVampireCloakOfDarkness =
    .name = Cloak of Darkness (Toggle)
    .desc = Toggle invisibility and speed boost that scales with darkness. Stronger in dark areas and weaker in bright light.

ent-ActionVampireShadowSnare =
    .name = Shadow Snare (20)
    .desc = Place a fragile shadow trap at target location. Damages, blinds (20s), and heavily slows a non-vampire humanoid who steps on it.

ent-ActionVampireShadowAnchor =
    .name = Shadow Anchor (20)
    .desc = First use places a shadow anchor beacon (2 min). Second use while it exists instantly returns you to it and consumes it.

ent-ActionVampireShadowBoxing =
    .name = Shadow Boxing (50)
    .desc = Target someone to have your shadow bats beat them up. You must stay within 4 tiles for this to work.

ent-ActionVampireDarkPassage =
    .name = Dark Passage (20)
    .desc = Teleport to target location through shadows.

ent-ActionVampireExtinguish =
    .name = Extinguish Lights (0)
    .desc = Destroy all light sources within 3 tiles, gaining 5 blood per light.

ent-ActionVampireEternalDarkness =
    .name = Eternal Darkness (Toggle)
    .desc = When toggled, shroud the area around you in darkness and slowly lower nearby body temperatures.

ent-ActionVampireEnthrall =
    .name = Enthrall (150)
    .desc = Channel for 15 seconds on a humanoid target to bind them to your will. Cancels if either of you moves.

ent-ActionVampirePacify =
    .name = Pacify (10)
    .desc = Flood a victim's mind with bliss, pacifying them for 40 seconds.

ent-ActionVampireSubspaceSwap =
    .name = Subspace Swap (30)
    .desc = Select a target within 7 tiles to swap positions, slowing them for 4 seconds.

ent-ActionVampireDecoy =
    .name = Decoy (30)
    .desc = Leave behind a fragile duplicate that blinds attackers when harmed while you vanish into invisibility.

ent-ActionVampireRallyThralls =
    .name = Rally Thralls (100)
    .desc = Command thralls within 7 tiles to shake off stuns, wake up, and regain stamina.

ent-ActionVampireBloodBond =
    .name = Blood Bond (Toggle)
    .desc = Toggle a blood tether to nearby thralls, redistributing damage between you at the cost of 2.5 blood per second.

ent-ActionVampireMassHysteria =
    .name = Mass Hysteria (70)
    .desc = Flood every nearby mind (except thralls) with terror, flashing them and cursing them with hallucinations for 30 seconds.

ent-ActionVampireBloodSwell =
    .name = Blood Swell (30)
    .desc = For 30 seconds reduce brute damage by 60%, stamina and burn by 50%, and halve stun times. Cannot use guns.

ent-ActionVampireBloodRush =
    .name = Blood Rush (30)
    .desc = For 10 seconds, double your movement speed.

ent-ActionVampireSeismicStomp =
    .name = Seismic Stomp (30)
    .desc = Slam the ground, knocking down and throwing all creatures within 3 tiles away. Destroys floor tiles.

ent-ActionVampireOverwhelmingForce =
    .name = Overwhelming Force (Toggle)
    .desc = Automatically pry open unpowered doors. While active, you cannot be pushed or pulled. Costs 5 blood per door.

ent-ActionVampireDemonicGrasp =
    .name = Demonic Grasp (20)
    .desc = Launch a demonic hand up to 15 tiles. Immobilizes the target for 5 seconds and can pull in combat mode.

ent-ActionVampireCharge =
    .name = Charge (30)
    .desc = Charge until hitting an obstacle or void. Creatures take 60 brute and are thrown 5 tiles. Structures take 150 damage.

ent-vampire-effect-blood-tendrils-name = blood tendrils

ent-vampire-effect-shadow-punch-name = shadow punch

ent-vampire-effect-blood-barrier-name = blood barrier
ent-vampire-effect-blood-barrier-desc = A barrier made of solidified blood that blocks movement.

ent-vampire-effect-transformation-out-name = vampire transformation out

ent-vampire-effect-transformation-in-name = vampire transformation in

ent-vampire-effect-blood-eruption-name = blood eruption

ent-vampire-effect-drain-beam-name = drain beam
ent-vampire-effect-drain-beam-desc = A crimson beam of life-draining energy.

ent-vampire-effect-drain-beam-visual-name = drain beam visual
ent-vampire-effect-drain-beam-visual-desc = A smooth client-side vampire drain beam.

ent-vampire-effect-shadow-anchor-name = shadow anchor
ent-vampire-effect-shadow-anchor-desc = A pulsing knot of shadow you can return to.

ent-vampire-effect-shadow-snare-name = shadow snare
ent-vampire-effect-shadow-snare-desc = A nearly invisible trap made of condensed shadows.

ent-vampire-effect-shadow-tendrils-name = shadow tendrils
ent-vampire-effect-shadow-tendrils-desc = Dark tendrils binding your legs.