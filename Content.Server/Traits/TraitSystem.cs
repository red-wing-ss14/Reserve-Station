// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._EinsteinEngines.Language;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.Traits;

public sealed class TraitSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    // When the player is spawned in, add all trait components selected during character creation
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Check if player's job allows to apply traits
        if (args.JobId == null ||
            !_prototypeManager.Resolve<JobPrototype>(args.JobId, out var protoJob) ||
            !protoJob.ApplyTraits)
        {
            return;
        }

        ApplyTraits(args.Mob, args.Profile); // RW-Edit
    }

    // RW-Edit-Start
    public void ApplyTraits(EntityUid mob, HumanoidCharacterProfile profile)
    {
        foreach (var traitId in profile.TraitPreferences)
        {
            if (!_prototypeManager.TryIndex(traitId, out var traitPrototype))
            {
                Log.Warning($"No trait found with ID {traitId}!");
                continue;
            }

            if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, mob) ||
                (traitPrototype.Blacklist != null && _whitelistSystem.IsValid(traitPrototype.Blacklist, mob)))
                continue;

            // Begin Goobstation: Species trait support
            if (traitPrototype.IncludedSpecies.Count > 0 && !traitPrototype.IncludedSpecies.Contains(profile.Species) ||
                traitPrototype.ExcludedSpecies.Contains(profile.Species))
                continue;
            // End Goobstation: Species trait support

            // Add all components required by the prototype
            if (traitPrototype.Components.Count > 0)
                EntityManager.AddComponents(mob, traitPrototype.Components, false);

            // Add all JobSpecials required by the prototype
            foreach (var special in traitPrototype.Specials)
            {
                special.AfterEquip(mob);
            }

            // Einstein Engines - Language begin (remove this if trait system refactor)
            // Remove/Add Languages required by the prototype
            var language = EntityManager.System<LanguageSystem>();

            if (traitPrototype.RemoveLanguagesSpoken is not null)
            {
                foreach (var lang in traitPrototype.RemoveLanguagesSpoken)
                {
                    language.RemoveLanguage(mob, lang, true, false);
                }
            }

            if (traitPrototype.RemoveLanguagesUnderstood is not null)
            {
                foreach (var lang in traitPrototype.RemoveLanguagesUnderstood)
                {
                    language.RemoveLanguage(mob, lang, false);
                }
            }

            if (traitPrototype.LanguagesSpoken is not null)
            {
                foreach (var lang in traitPrototype.LanguagesSpoken)
                {
                    language.AddLanguage(mob, lang, true, false);
                }
            }

            if (traitPrototype.LanguagesUnderstood is not null)
            {
                foreach (var lang in traitPrototype.LanguagesUnderstood)
                {
                    language.AddLanguage(mob, lang, false);
                }
            }
            // Einstein Engines - Language end

            // Add item required by the trait
            if (traitPrototype.TraitGear == null)
                continue;

            if (!TryComp(mob, out HandsComponent? handsComponent))
                continue;

            var coords = Transform(mob).Coordinates;
            var inhandEntity = Spawn(traitPrototype.TraitGear, coords);
            _sharedHandsSystem.TryPickup(mob,
                inhandEntity,
                checkActionBlocker: false,
                handsComp: handsComponent);
        }
    }
    // RW-Edit-End
}
