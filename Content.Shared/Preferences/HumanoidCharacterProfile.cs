// SPDX-FileCopyrightText: 2019 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2020 20kdc <asdd2808@gmail.com>
// SPDX-FileCopyrightText: 2020 DamianX <DamianX@users.noreply.github.com>
// SPDX-FileCopyrightText: 2020 Víctor Aguilera Puerto <6766154+Zumorica@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Acruid <shatter66@gmail.com>
// SPDX-FileCopyrightText: 2021 Metal Gear Sloth <metalgearsloth@gmail.com>
// SPDX-FileCopyrightText: 2021 Remie Richards <remierichards@gmail.com>
// SPDX-FileCopyrightText: 2021 ShadowCommander <10494922+ShadowCommander@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Swept <sweptwastaken@protonmail.com>
// SPDX-FileCopyrightText: 2021 ike709 <ike709@github.com>
// SPDX-FileCopyrightText: 2021 ike709 <ike709@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 AJCM-git <60196617+AJCM-git@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Javier Guardia Fernández <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Moony <moonheart08@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Rane <60792108+Elijahrane@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 T-Stalker <43253663+DogZeroX@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Veritius <veritiusgaming@gmail.com>
// SPDX-FileCopyrightText: 2022 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Flipp Syder <76629141+vulppine@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Morb <14136326+Morb0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Ciac32 <aknoxlor@gmail.com>
// SPDX-FileCopyrightText: 2024 Debug <49997488+DebugOk@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Ed <96445749+TheShuEd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Firewatch <54725557+musicmanvr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Krunklehorn <42424291+Krunklehorn@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Mr. 27 <45323883+Dutch-VanDerLinde@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Mr. 27 <koolthunder019@gmail.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 PoTeletubby <151896601+PoTeletubby@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Whisper <121047731+QuietlyWhisper@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Winkarst <74284083+Winkarst-cpu@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 dffdff2423 <dffdff2423@gmail.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 BeBright <98597725+be1bright@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 BeBright <98597725+bebr3ght@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Hyper B <137433177+HyperB1@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 MarkerWicker <markerWicker@proton.me>
// SPDX-FileCopyrightText: 2025 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2025 SX-7 <sn1.test.preria.2002@gmail.com>
// SPDX-FileCopyrightText: 2025 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 āda <ss.adasts@gmail.com>
// SPDX-FileCopyrightText: 2025 Zekins <zekins3366@gmail.com>
// SPDX-FileCopyrightText: 2025 pheenty <fedorlukin2006@gmail.com>
//

using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared._RW.TTS;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Goobstation.Common.Barks; // Goob Station - Barks
using Content.Shared.Traits;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// Character profile. Looks immutable, but uses non-immutable semantics internally for serialization/code sanity purposes.
    /// </summary>
    [DataDefinition]
    [Serializable, NetSerializable]
    public sealed partial class HumanoidCharacterProfile : ICharacterProfile
    {
        private static readonly Regex RestrictedNameRegex = new(@"[^А-Яа-яёЁA-Za-z0-9' -]", RegexOptions.Compiled); // RW-Edit
        private static readonly Regex ICNameCaseRegex = new(@"^(?<word>\w)|\b(?<word>\w)(?=\w*$)");

        /// <summary>
        /// Job preferences for initial spawn.
        /// </summary>
        [DataField]
        private Dictionary<ProtoId<JobPrototype>, JobPriority> _jobPriorities = new()
        {
            {
                SharedGameTicker.FallbackOverflowJob, JobPriority.High
            }
        };

        /// <summary>
        /// Antags we have opted in to.
        /// </summary>
        [DataField]
        private HashSet<ProtoId<AntagPrototype>> _antagPreferences = new();

        /// <summary>
        /// Enabled traits.
        /// </summary>
        [DataField]
        private HashSet<ProtoId<TraitPrototype>> _traitPreferences = new();

        /// <summary>
        /// <see cref="_loadouts"/>
        /// </summary>
        public IReadOnlyDictionary<string, RoleLoadout> Loadouts => _loadouts;

        // RW edit start
        /// <summary>
        /// Prototype ID for the base crew loadout.
        /// </summary>
        public const string BaseLoadoutProtoId = "BaseCrew";

        /// <summary>
        /// Base loadout applied to all crew job loadouts, unless overridden.
        /// Stored separately from per-role overrides.
        /// </summary>
        public RoleLoadout BaseLoadout => _baseLoadout;
        // RW edit end

        [DataField]
        private Dictionary<string, RoleLoadout> _loadouts = new();

        // RW edit start
        /// <summary>
        /// Base loadout applied to all crew jobs.
        /// </summary>
        [DataField]
        private RoleLoadout _baseLoadout = new(BaseLoadoutProtoId);
        // RW edit end

        [DataField]
        public string Name { get; set; } = "John Doe";

        /// <summary>
        /// Detailed text that can appear for the character if <see cref="CCVars.FlavorText"/> is enabled.
        /// </summary>
        [DataField]
        public string FlavorText { get; set; } = string.Empty;

        // RW-Start
        [DataField]
        public string CharacterFlavorText { get; set; } = string.Empty;

        [DataField]
        public string GreenFlavorText { get; set; } = string.Empty;

        [DataField]
        public string YellowFlavorText { get; set; } = string.Empty;

        [DataField]
        public string RedFlavorText { get; set; } = string.Empty;

        // RW-End

        /// <summary>
        /// Associated <see cref="SpeciesPrototype"/> for this profile.
        /// </summary>
        [DataField]
        public ProtoId<SpeciesPrototype> Species { get; set; } = SharedHumanoidAppearanceSystem.DefaultSpecies;

        [DataField] // Goob Station - Barks
        public ProtoId<BarkPrototype> BarkVoice { get; set; } = SharedHumanoidAppearanceSystem.DefaultBarkVoice; // Goob Station - Barks

        [DataField] // RW - TTS
        public ProtoId<TTSVoicePrototype> Voice { get; set; } = string.Empty; // RW - TTS

        [DataField]
        public int Age { get; set; } = 18;

        [DataField]
        public Sex Sex { get; private set; } = Sex.Male;

        // RW port: WD Slim body types START
        [DataField]
        public string BodyType { get; set; } = SharedHumanoidAppearanceSystem.DefaultBodyType;
        // RW port: WD Slim body types END
        [DataField]
        public Gender Gender { get; private set; } = Gender.Male;

        // begin Goobstation: port EE height/width sliders
        [DataField]
        public float Height { get; private set; }

        [DataField]
        public float Width { get; private set; }
        // end Goobstation: port EE height/width sliders

        /// <summary>
        /// <see cref="Appearance"/>
        /// </summary>
        public ICharacterAppearance CharacterAppearance => Appearance;

        /// <summary>
        /// Stores markings, eye colors, etc for the profile.
        /// </summary>
        [DataField]
        public HumanoidCharacterAppearance Appearance { get; set; } = new();

        /// <summary>
        /// When spawning into a round what's the preferred spot to spawn.
        /// </summary>
        [DataField]
        public SpawnPriorityPreference SpawnPriority { get; private set; } = SpawnPriorityPreference.None;

        /// <summary>
        /// <see cref="_jobPriorities"/>
        /// </summary>
        public IReadOnlyDictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities => _jobPriorities;

        /// <summary>
        /// <see cref="_antagPreferences"/>
        /// </summary>
        public IReadOnlySet<ProtoId<AntagPrototype>> AntagPreferences => _antagPreferences;

        /// <summary>
        /// <see cref="_traitPreferences"/>
        /// </summary>
        public IReadOnlySet<ProtoId<TraitPrototype>> TraitPreferences => _traitPreferences;

        /// <summary>
        /// If we're unable to get one of our preferred jobs do we spawn as a fallback job or do we stay in lobby.
        /// </summary>
        [DataField]
        public PreferenceUnavailableMode PreferenceUnavailable { get; private set; } =
            PreferenceUnavailableMode.SpawnAsOverflow;
        public HumanoidCharacterProfile(
            string name,
            string flavortext,
            // RW-Start
            string characterflavortext,
            string greenflavortext,
            string yellowflavortext,
            string redflavortext,
            // RW-End
            string species,
            float height, // Goobstation: port EE height/width sliders
            float width, // Goobstation: port EE height/width sliders
            int age,
            Sex sex,
            string bodyType, // RW port: WD Slim body types
            Gender gender,
            HumanoidCharacterAppearance appearance,
            SpawnPriorityPreference spawnPriority,
            Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities,
            PreferenceUnavailableMode preferenceUnavailable,
            HashSet<ProtoId<AntagPrototype>> antagPreferences,
            HashSet<ProtoId<TraitPrototype>> traitPreferences,
            RoleLoadout? baseLoadout, // RW edit
            Dictionary<string, RoleLoadout> loadouts,
            ProtoId<BarkPrototype> barkVoice, // Goob Station - Barks
            ProtoId<TTSVoicePrototype> voice) // RW - TTS
        {
            Name = name;
            FlavorText = flavortext;
            // RW-Start
            CharacterFlavorText = characterflavortext;
            GreenFlavorText = greenflavortext;
            YellowFlavorText = yellowflavortext;
            RedFlavorText = redflavortext;
            // RW-End
            Species = species;
            Height = height; // Goobstation: port EE height/width sliders
            Width = width; // Goobstation: port EE height/width sliders
            Age = age;
            Sex = sex;
            BodyType = bodyType; // RW port: WD Slim body types
            Gender = gender;
            Appearance = appearance;
            SpawnPriority = spawnPriority;
            _jobPriorities = jobPriorities;
            PreferenceUnavailable = preferenceUnavailable;
            _antagPreferences = antagPreferences;
            _traitPreferences = traitPreferences;
            _baseLoadout = baseLoadout ?? new RoleLoadout(BaseLoadoutProtoId); // RW edit
            _loadouts = loadouts;
            BarkVoice = barkVoice; // Goob Station - Barks
            Voice = voice; // RW - TTS

            var hasHighPrority = false;
            foreach (var (key, value) in _jobPriorities)
            {
                if (value == JobPriority.Never)
                    _jobPriorities.Remove(key);
                else if (value != JobPriority.High)
                    continue;

                if (hasHighPrority)
                    _jobPriorities[key] = JobPriority.Medium;

                hasHighPrority = true;
            }
        }

        /// <summary>Copy constructor</summary>
        public HumanoidCharacterProfile(HumanoidCharacterProfile other)
            : this(other.Name,
                other.FlavorText,
                // RW-Start
                other.CharacterFlavorText,
                other.GreenFlavorText,
                other.YellowFlavorText,
                other.RedFlavorText,
                // RW-End
                other.Species,
                other.Height, // Goobstation: port EE height/width sliders
                other.Width, // Goobstation: port EE height/width sliders
                other.Age,
                other.Sex,
                other.BodyType, // RW port: WD Slim body types
                other.Gender,
                other.Appearance.Clone(),
                other.SpawnPriority,
                new Dictionary<ProtoId<JobPrototype>, JobPriority>(other.JobPriorities),
                other.PreferenceUnavailable,
                new HashSet<ProtoId<AntagPrototype>>(other.AntagPreferences),
                new HashSet<ProtoId<TraitPrototype>>(other.TraitPreferences),
                other.BaseLoadout.Clone(), // RW edit
                other.Loadouts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone()), // RW edit - deep copy loadouts
                other.BarkVoice, // Goob Station - Barks
                other.Voice) // RW - TTS
        {
        }

        /// <summary>
        ///     Get the default humanoid character profile, using internal constant values.
        ///     Defaults to <see cref="SharedHumanoidAppearanceSystem.DefaultSpecies"/> for the species.
        /// </summary>
        /// <returns></returns>
        public HumanoidCharacterProfile()
        {
        }

        /// <summary>
        ///     Return a default character profile, based on species.
        /// </summary>
        /// <param name="species">The species to use in this default profile. The default species is <see cref="SharedHumanoidAppearanceSystem.DefaultSpecies"/>.</param>
        /// <returns>Humanoid character profile with default settings.</returns>
        public static HumanoidCharacterProfile DefaultWithSpecies(string? species = null)
        {
            species ??= SharedHumanoidAppearanceSystem.DefaultSpecies;
            // RW start
            var bodyType = SharedHumanoidAppearanceSystem.DefaultBodyType;
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            if (prototypeManager.TryIndex<SpeciesPrototype>(species, out var speciesPrototype) &&
                speciesPrototype.BodyTypes.Count > 0)
            {
                bodyType = speciesPrototype.BodyTypes.First();
            }
            // RW end

            return new()
            {
                Species = species,
                BodyType = bodyType, // RW port: WD Slim body types
                Appearance = HumanoidCharacterAppearance.DefaultWithSpecies(species),
            };
        }

        // TODO: This should eventually not be a visual change only.
        public static HumanoidCharacterProfile Random(HashSet<string>? ignoredSpecies = null)
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var random = IoCManager.Resolve<IRobustRandom>();

            var species = random.Pick(prototypeManager
                .EnumeratePrototypes<SpeciesPrototype>()
                .Where(x => ignoredSpecies == null ? x.RoundStart : x.RoundStart && !ignoredSpecies.Contains(x.ID))
                .ToArray()
            ).ID;

            return RandomWithSpecies(species);
        }

        public static HumanoidCharacterProfile RandomWithSpecies(string? species = null)
        {
            species ??= SharedHumanoidAppearanceSystem.DefaultSpecies;

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var random = IoCManager.Resolve<IRobustRandom>();

            var sex = Sex.Unsexed;
            var age = 18;
            var height = 1f; // Goobstation: port EE height/width sliders
            var width = 1f; // Goobstation: port EE height/width sliders
            var bodyType = SharedHumanoidAppearanceSystem.DefaultBodyType; // RW port: WD Slim body types
            if (prototypeManager.TryIndex<SpeciesPrototype>(species, out var speciesPrototype))
            {
                sex = random.Pick(speciesPrototype.Sexes);
                age = random.Next(speciesPrototype.MinAge, speciesPrototype.OldAge); // people don't look and keep making 119 year old characters with zero rp, cap it at middle aged
                height = random.NextFloat(speciesPrototype.MinHeight, speciesPrototype.MaxHeight); // Goobstation: port EE height/width sliders
                width = random.NextFloat(speciesPrototype.MinWidth, speciesPrototype.MaxWidth); // Goobstation: port EE height/width sliders
                bodyType = speciesPrototype.BodyTypes.First(); // RW port: WD Slim body types
            }

            // Goob Station - Barks Start
            var barkvoiceId = random.Pick(prototypeManager
                .EnumeratePrototypes<BarkPrototype>()
                .Where(o => o.RoundStart && (o.SpeciesWhitelist is null || o.SpeciesWhitelist.Contains(species)))
                .ToArray()
            );
            //  Goob Station - Barks End

            var gender = Gender.Epicene;

            switch (sex)
            {
                case Sex.Male:
                    gender = Gender.Male;
                    break;
                case Sex.Female:
                    gender = Gender.Female;
                    break;
            }

            var name = GetName(species, gender);


            return new HumanoidCharacterProfile()
            {
                Name = name,
                Sex = sex,
                BodyType = bodyType, // RW port: WD Slim body types
                Age = age,
                Gender = gender,
                Species = species,
                Width = width, // Goobstation: port EE height/width sliders
                Height = height, // Goobstation: port EE height/width sliders
                Appearance = HumanoidCharacterAppearance.Random(species, sex),
                BarkVoice = barkvoiceId, // Goob Station - Barks
            };
        }

        public HumanoidCharacterProfile WithName(string name)
        {
            return new(this) { Name = name };
        }

        public HumanoidCharacterProfile WithFlavorText(string flavorText)
        {
            return new(this) { FlavorText = flavorText };
        }

        public HumanoidCharacterProfile WithCharacterText(string characterFlavorText)
        {
            return new(this) { CharacterFlavorText = characterFlavorText };
        }

        public HumanoidCharacterProfile WithGreenPreferencesText(string greenFlavorText)
        {
            return new(this) { GreenFlavorText = greenFlavorText };
        }

        public HumanoidCharacterProfile WithYellowPreferencesText(string yellowFlavorText)
        {
            return new(this) { YellowFlavorText = yellowFlavorText };
        }

        public HumanoidCharacterProfile WithRedPreferencesText(string redFlavorText)
        {
            return new(this) { RedFlavorText = redFlavorText };
        }

        // RW-End

        public HumanoidCharacterProfile WithAge(int age)
        {
            return new(this) { Age = age };
        }

        public HumanoidCharacterProfile WithSex(Sex sex)
        {
            return new(this) { Sex = sex };
        }

        // RW port: WD Slim body types START
        public HumanoidCharacterProfile WithBodyType(string bodyType)
        {
            return new HumanoidCharacterProfile(this) { BodyType = bodyType };
        }
        // RW port: WD Slim body types END
        public HumanoidCharacterProfile WithGender(Gender gender)
        {
            return new(this) { Gender = gender };
        }

        public HumanoidCharacterProfile WithSpecies(string species)
        {
            return new(this) { Species = species };
        }

        // begin Goobstation: port EE height/width sliders
        public HumanoidCharacterProfile WithHeight(float height)
        {
            return new(this) { Height = height };
        }
        public HumanoidCharacterProfile WithWidth(float width)
        {
            return new(this) { Width = width };
        }
        // end Goobstation: port EE height/width sliders

        public HumanoidCharacterProfile WithCharacterAppearance(HumanoidCharacterAppearance appearance)
        {
            return new(this) { Appearance = appearance };
        }

        public HumanoidCharacterProfile WithSpawnPriorityPreference(SpawnPriorityPreference spawnPriority)
        {
            return new(this) { SpawnPriority = spawnPriority };
        }

        // Goob Station - Barks Start
        public HumanoidCharacterProfile WithBarkVoice(BarkPrototype barkVoice)
        {
            return new(this) { BarkVoice = barkVoice };
        }
        // Goob Station - Barks End

        // RW - TTS Start
        public HumanoidCharacterProfile WithVoice(TTSVoicePrototype voice)
        {
            return new(this) { Voice = voice };
        }
        // RW - TTS End

        public HumanoidCharacterProfile WithJobPriorities(IEnumerable<KeyValuePair<ProtoId<JobPrototype>, JobPriority>> jobPriorities)
        {
            var dictionary = new Dictionary<ProtoId<JobPrototype>, JobPriority>(jobPriorities);
            var hasHighPrority = false;

            foreach (var (key, value) in dictionary)
            {
                if (value == JobPriority.Never)
                    dictionary.Remove(key);
                else if (value != JobPriority.High)
                    continue;

                if (hasHighPrority)
                    dictionary[key] = JobPriority.Medium;

                hasHighPrority = true;
            }

            return new(this)
            {
                _jobPriorities = dictionary
            };
        }

        public HumanoidCharacterProfile WithJobPriority(ProtoId<JobPrototype> jobId, JobPriority priority)
        {
            var dictionary = new Dictionary<ProtoId<JobPrototype>, JobPriority>(_jobPriorities);
            if (priority == JobPriority.Never)
            {
                dictionary.Remove(jobId);
            }
            else if (priority == JobPriority.High)
            {
                // There can only ever be one high priority job.
                foreach (var (job, value) in dictionary)
                {
                    if (value == JobPriority.High)
                        dictionary[job] = JobPriority.Medium;
                }

                dictionary[jobId] = priority;
            }
            else
            {
                dictionary[jobId] = priority;
            }

            return new(this)
            {
                _jobPriorities = dictionary,
            };
        }

        public HumanoidCharacterProfile WithPreferenceUnavailable(PreferenceUnavailableMode mode)
        {
            return new(this) { PreferenceUnavailable = mode };
        }

        public HumanoidCharacterProfile WithAntagPreferences(IEnumerable<ProtoId<AntagPrototype>> antagPreferences)
        {
            return new(this)
            {
                _antagPreferences = new(antagPreferences),
            };
        }

        public HumanoidCharacterProfile WithAntagPreference(ProtoId<AntagPrototype> antagId, bool pref)
        {
            var list = new HashSet<ProtoId<AntagPrototype>>(_antagPreferences);
            if (pref)
            {
                list.Add(antagId);
            }
            else
            {
                list.Remove(antagId);
            }

            return new(this)
            {
                _antagPreferences = list,
            };
        }

        public HumanoidCharacterProfile WithTraitPreference(ProtoId<TraitPrototype> traitId, IPrototypeManager protoManager)
        {
            // null category is assumed to be default.
            if (!protoManager.TryIndex(traitId, out var traitProto))
                return new(this);

            var category = traitProto.Category;

            // Category not found so dump it.
            TraitCategoryPrototype? traitCategory = null;

            if (category != null && !protoManager.Resolve(category, out traitCategory))
                return new(this);

            var list = new HashSet<ProtoId<TraitPrototype>>(_traitPreferences) { traitId };

            if (traitCategory == null || traitCategory.MaxTraitPoints < 0)
            {
                return new(this)
                {
                    _traitPreferences = list,
                };
            }

            var count = 0;
            foreach (var trait in list)
            {
                // If trait not found or another category don't count its points.
                if (!protoManager.TryIndex<TraitPrototype>(trait, out var otherProto) ||
                    otherProto.Category != traitCategory)
                {
                    continue;
                }

                count += otherProto.Cost;
            }

            if (count > traitCategory.MaxTraitPoints && traitProto.Cost != 0)
            {
                return new(this);
            }

            return new(this)
            {
                _traitPreferences = list,
            };
        }

        public HumanoidCharacterProfile WithoutTraitPreference(ProtoId<TraitPrototype> traitId, IPrototypeManager protoManager)
        {
            var list = new HashSet<ProtoId<TraitPrototype>>(_traitPreferences);
            list.Remove(traitId);

            return new(this)
            {
                _traitPreferences = list,
            };
        }

        public string Summary =>
            Loc.GetString(
                "humanoid-character-profile-summary",
                ("name", Name),
                ("gender", Gender.ToString().ToLowerInvariant()),
                ("age", Age)
            );

        public bool MemberwiseEquals(ICharacterProfile maybeOther)
        {
            if (maybeOther is not HumanoidCharacterProfile other) return false;
            if (Name != other.Name) return false;
            if (Age != other.Age) return false;
            if (Sex != other.Sex) return false;
            if (BodyType != other.BodyType) return false; // RW port: WD Slim body types
            if (Gender != other.Gender) return false;
            if (Species != other.Species) return false;
            if (Height != other.Height) return false; // Goobstation: port EE height/width sliders
            if (Width != other.Width) return false; // Goobstation: port EE height/width sliders
            if (BarkVoice != other.BarkVoice) return false; // Goob Station - Barks
            if (Voice != other.Voice) return false; // RW - TTS
            if (PreferenceUnavailable != other.PreferenceUnavailable) return false;
            if (SpawnPriority != other.SpawnPriority) return false;
            // RW edit - use content-based comparison instead of SequenceEqual for dictionaries/sets
            if (_jobPriorities.Count != other._jobPriorities.Count ||
                !_jobPriorities.All(kvp => other._jobPriorities.TryGetValue(kvp.Key, out var val) && val == kvp.Value))
                return false;
            if (!_antagPreferences.SetEquals(other._antagPreferences)) return false;
            if (!_traitPreferences.SetEquals(other._traitPreferences)) return false;
            if (Loadouts.Count != other.Loadouts.Count ||
                !Loadouts.All(kvp => other.Loadouts.TryGetValue(kvp.Key, out var val) && kvp.Value.Equals(val)))
                return false;
            // RW edit end
            if (!BaseLoadout.Equals(other.BaseLoadout)) return false; // RW edit
            if (FlavorText != other.FlavorText) return false;
            // RW-Start
            if (CharacterFlavorText != other.CharacterFlavorText) return false;
            if (GreenFlavorText != other.GreenFlavorText) return false;
            if (YellowFlavorText != other.YellowFlavorText) return false;
            if (RedFlavorText != other.RedFlavorText) return false;
            // RW-End
            return Appearance.MemberwiseEquals(other.Appearance);
        }

        public void EnsureValid(ICommonSession session, IDependencyCollection collection)
        {
            var configManager = collection.Resolve<IConfigurationManager>();
            var prototypeManager = collection.Resolve<IPrototypeManager>();

            if (!prototypeManager.TryIndex(Species, out var speciesPrototype) || speciesPrototype.RoundStart == false)
            {
                Species = SharedHumanoidAppearanceSystem.DefaultSpecies;
                speciesPrototype = prototypeManager.Index(Species);
            }

            var sex = Sex switch
            {
                Sex.Male => Sex.Male,
                Sex.Female => Sex.Female,
                Sex.Unsexed => Sex.Unsexed,
                _ => Sex.Male // Invalid enum values.
            };

            // ensure the species can be that sex and their age fits the founds
            if (!speciesPrototype.Sexes.Contains(sex))
                sex = speciesPrototype.Sexes[0];

            var age = Math.Clamp(Age, speciesPrototype.MinAge, speciesPrototype.MaxAge);

            var gender = Gender switch
            {
                Gender.Epicene => Gender.Epicene,
                Gender.Female => Gender.Female,
                Gender.Male => Gender.Male,
                Gender.Neuter => Gender.Neuter,
                _ => Gender.Epicene // Invalid enum values.
            };

            // RW port: WD Slim body types START
            var bodyTypeStr = BodyType;
            if (!speciesPrototype.BodyTypes.Contains(bodyTypeStr))
            {
                bodyTypeStr = speciesPrototype.BodyTypes.First();
            }
            // RW port: WD Slim body types END

            string name;
            var maxNameLength = configManager.GetCVar(CCVars.MaxNameLength);
            if (string.IsNullOrEmpty(Name))
            {
                name = GetName(Species, gender);
            }
            else if (Name.Length > maxNameLength)
            {
                name = Name[..maxNameLength];
            }
            else
            {
                name = Name;
            }

            name = name.Trim();

            if (configManager.GetCVar(CCVars.RestrictedNames))
            {
                name = RestrictedNameRegex.Replace(name, string.Empty);
            }

            if (configManager.GetCVar(CCVars.ICNameCase))
            {
                // This regex replaces the first character of the first and last words of the name with their uppercase version
                name = ICNameCaseRegex.Replace(name, m => m.Groups["word"].Value.ToUpper());
            }

            if (string.IsNullOrEmpty(name))
            {
                name = GetName(Species, gender);
            }


            string flavortext;
            var maxFlavorTextLength = configManager.GetCVar(CCVars.MaxFlavorTextLength);
            if (FlavorText.Length > maxFlavorTextLength)
            {
                flavortext = FlavorText[..maxFlavorTextLength]; // RW-Edit: Remove RemoveMarkupOrThrow
            }
            else
            {
                flavortext = FlavorText; // RW-Edit: Remove RemoveMarkupOrThrow
            }

            // RW-Start
            string characterDescription;
            var maxCharacterDescriptionLength = configManager.GetCVar(CCVars.CharacterDescriptionLength);
            if (CharacterFlavorText.Length > maxCharacterDescriptionLength)
            {
                characterDescription = CharacterFlavorText[..maxCharacterDescriptionLength];
            }
            else
            {
                characterDescription = CharacterFlavorText;
            }

            string greenPreferences;
            var maxGreenPreferencesLength = configManager.GetCVar(CCVars.GreenPreferencesLength);
            if (GreenFlavorText.Length > maxGreenPreferencesLength)
            {
                greenPreferences = GreenFlavorText[..maxGreenPreferencesLength];
            }
            else
            {
                greenPreferences = GreenFlavorText;
            }

            string yellowPreferences;
            var maxYellowPreferencesLength = configManager.GetCVar(CCVars.YellowPreferencesLength);
            if (YellowFlavorText.Length > maxYellowPreferencesLength)
            {
                yellowPreferences = YellowFlavorText[..maxYellowPreferencesLength];
            }
            else
            {
                yellowPreferences = YellowFlavorText;
            }

            string redPreferences;
            var maxRedPreferencesLength = configManager.GetCVar(CCVars.RedPreferencesLength);
            if (RedFlavorText.Length > maxRedPreferencesLength)
            {
                redPreferences = RedFlavorText[..maxRedPreferencesLength];
            }
            else
            {
                redPreferences = RedFlavorText;
            }


            // RW-End

            // begin Goobstation: port EE height/width sliders
            var height = Height;
            if (speciesPrototype != null)
                height = Math.Clamp(Height, speciesPrototype.MinHeight, speciesPrototype.MaxHeight);

            var width = Width;
            if (speciesPrototype != null)
                width = Math.Clamp(Width, speciesPrototype.MinWidth, speciesPrototype.MaxWidth);
            // end Goobstation: port EE height/width sliders

            var appearance = HumanoidCharacterAppearance.EnsureValid(Appearance, Species, Sex);

            // RW edit start - validate markings for AllowedUsers and MinBoostyTier
            ValidateMarkingsForUser(appearance, session, collection);
            // RW edit end

            var prefsUnavailableMode = PreferenceUnavailable switch
            {
                PreferenceUnavailableMode.StayInLobby => PreferenceUnavailableMode.StayInLobby,
                PreferenceUnavailableMode.SpawnAsOverflow => PreferenceUnavailableMode.SpawnAsOverflow,
                _ => PreferenceUnavailableMode.StayInLobby // Invalid enum values.
            };

            var spawnPriority = SpawnPriority switch
            {
                SpawnPriorityPreference.None => SpawnPriorityPreference.None,
                SpawnPriorityPreference.Arrivals => SpawnPriorityPreference.Arrivals,
                SpawnPriorityPreference.Cryosleep => SpawnPriorityPreference.Cryosleep,
                _ => SpawnPriorityPreference.None // Invalid enum values.
            };

            var priorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>(JobPriorities
                .Where(p => prototypeManager.TryIndex<JobPrototype>(p.Key, out var job) && job.SetPreference && p.Value switch
                {
                    JobPriority.Never => false, // Drop never since that's assumed default.
                    JobPriority.Low => true,
                    JobPriority.Medium => true,
                    JobPriority.High => true,
                    _ => false
                }));

            var hasHighPrio = false;
            foreach (var (key, value) in priorities)
            {
                if (value != JobPriority.High)
                    continue;

                if (hasHighPrio)
                    priorities[key] = JobPriority.Medium;
                hasHighPrio = true;
            }

            var antags = AntagPreferences
                .Where(id => prototypeManager.TryIndex(id, out var antag) && antag.SetPreference)
                .ToList();

            var traits = TraitPreferences
                         .Where(prototypeManager.HasIndex)
                         .ToList();

            Name = name;
            FlavorText = flavortext;
            // RW-Start
            CharacterFlavorText = characterDescription;
            GreenFlavorText = greenPreferences;
            YellowFlavorText = yellowPreferences;
            RedFlavorText = redPreferences;
            // RW-End
            Age = age;
            Height = height; // Goobstation: port EE height/width sliders
            Width = width; // Goobstation: port EE height/width sliders
            Sex = sex;
            Gender = gender;
            BodyType = bodyTypeStr; // RW port: WD Slim body types
            Appearance = appearance;
            SpawnPriority = spawnPriority;

            _jobPriorities.Clear();

            foreach (var (job, priority) in priorities)
            {
                _jobPriorities.Add(job, priority);
            }

            PreferenceUnavailable = prefsUnavailableMode;

            _antagPreferences.Clear();
            _antagPreferences.UnionWith(antags);

            _traitPreferences.Clear();
            _traitPreferences.UnionWith(GetValidTraits(traits, prototypeManager));

            // Checks prototypes exist for all loadouts and dump / set to default if not.
            var toRemove = new ValueList<string>();

            // RW edit start
            // Validate base loadout.
            _baseLoadout.Role = BaseLoadoutProtoId;
            _baseLoadout.EnsureValid(this, session, collection);
            // RW edit end

            foreach (var (roleName, loadouts) in _loadouts)
            {
                if (!prototypeManager.HasIndex<RoleLoadoutPrototype>(roleName))
                {
                    toRemove.Add(roleName);
                    continue;
                }

                // This happens after we verify the prototype exists
                // These values are set equal in the database and we need to make sure they're equal here too!
                loadouts.Role = roleName;
                loadouts.EnsureValid(this, session, collection);
            }

            foreach (var value in toRemove)
            {
                _loadouts.Remove(value);
            }
        }

        // RW edit start
        /// <summary>
        /// Validates markings for AllowedUsers and MinBoostyTier restrictions.
        /// Removes markings that the user doesn't have access to.
        /// </summary>
        private void ValidateMarkingsForUser(HumanoidCharacterAppearance appearance, ICommonSession session, IDependencyCollection collection)
        {
            var markingManager = collection.Resolve<MarkingManager>();
            var userName = session.Name;

            // Handle localhost@ prefix for local development environment
            if (userName.StartsWith("localhost@", StringComparison.OrdinalIgnoreCase))
                userName = userName.Substring("localhost@".Length);

            // Get Boosty tier (only available on server)
            int playerTierLevel = 0;
            if (collection.TryResolveType<Content.Shared._RW.Loadouts.Effects.IBoostyTierManager>(out var tierManager))
            {
                var tierInfo = tierManager.GetPlayerTier(session);
                playerTierLevel = tierInfo?.IsActive == true ? tierInfo.TierLevel : 0;
            }

            // Collect indices of markings to remove (in reverse order)
            var toRemove = new List<int>();

            for (var i = 0; i < appearance.Markings.Count; i++)
            {
                var marking = appearance.Markings[i];
                if (!markingManager.TryGetMarking(marking, out var prototype))
                    continue;

                // Check AllowedUsers
                if (prototype.AllowedUsers != null && prototype.AllowedUsers.Count > 0)
                {
                    var isAllowed = prototype.AllowedUsers.Any(u =>
                        string.Equals(u, userName, StringComparison.OrdinalIgnoreCase));
                    if (!isAllowed)
                    {
                        toRemove.Add(i);
                        continue;
                    }
                }

                // Check MinBoostyTier
                if (prototype.MinBoostyTier > 0 && playerTierLevel < prototype.MinBoostyTier)
                {
                    toRemove.Add(i);
                }
            }

            // Remove invalid markings (in reverse order to preserve indices)
            foreach (var index in toRemove.OrderByDescending(x => x))
            {
                appearance.Markings.RemoveAt(index);
            }
        }
        // RW edit end

        /// <summary>
        /// Takes in an IEnumerable of traits and returns a List of the valid traits.
        /// </summary>
        public List<ProtoId<TraitPrototype>> GetValidTraits(IEnumerable<ProtoId<TraitPrototype>> traits, IPrototypeManager protoManager)
        {
            // Track points count for each group.
            var groups = new Dictionary<string, int>();
            var result = new List<ProtoId<TraitPrototype>>();

            foreach (var trait in traits)
            {
                if (!protoManager.TryIndex(trait, out var traitProto))
                    continue;

                // Always valid.
                if (traitProto.Category == null)
                {
                    result.Add(trait);
                    continue;
                }

                // No category so dump it.
                if (!protoManager.Resolve(traitProto.Category, out var category))
                    continue;

                var existing = groups.GetOrNew(category.ID);
                existing += traitProto.Cost;

                // Too expensive.
                if (existing > category.MaxTraitPoints)
                    continue;

                groups[category.ID] = existing;
                result.Add(trait);
            }

            return result;
        }

        public ICharacterProfile Validated(ICommonSession session, IDependencyCollection collection)
        {
            var profile = new HumanoidCharacterProfile(this);
            profile.EnsureValid(session, collection);
            return profile;
        }

        // sorry this is kind of weird and duplicated,
        /// working inside these non entity systems is a bit wack
        public static string GetName(string species, Gender gender)
        {
            var namingSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<NamingSystem>();
            return namingSystem.GetName(species, gender);
        }
        public bool Equals(HumanoidCharacterProfile? other)
        {
            if (other is null)
                return false;

            return ReferenceEquals(this, other) || MemberwiseEquals(other);
        }

        public override bool Equals(object? obj)
        {
            return obj is HumanoidCharacterProfile other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(_jobPriorities);
            hashCode.Add(_antagPreferences);
            hashCode.Add(_traitPreferences);
            hashCode.Add(_baseLoadout);
            hashCode.Add(_loadouts);
            hashCode.Add(Name);
            hashCode.Add(FlavorText);
            // RW-Start
            hashCode.Add(CharacterFlavorText);
            hashCode.Add(GreenFlavorText);
            hashCode.Add(YellowFlavorText);
            hashCode.Add(RedFlavorText);
            // RW-End
            hashCode.Add(Species);
            hashCode.Add(Height); // Goobstation: port EE height/width sliders
            hashCode.Add(Width); // Goobstation: port EE height/width sliders
            hashCode.Add(Age);
            hashCode.Add((int) Sex);
            hashCode.Add(BodyType); // RW port: WD Slim body types
            hashCode.Add((int) Gender);
            hashCode.Add(Appearance);
            hashCode.Add(BarkVoice); // Goob Station - Barks
            hashCode.Add(Voice); // RW - TTS
            hashCode.Add((int) SpawnPriority);
            hashCode.Add((int) PreferenceUnavailable);
            return hashCode.ToHashCode();
        }

        public void SetLoadout(RoleLoadout loadout)
        {
            _loadouts[loadout.Role.Id] = loadout;
        }

        // RW edit start
        public void SetBaseLoadout(RoleLoadout loadout)
        {
            _baseLoadout = loadout;
        }
        // RW edit end

        public HumanoidCharacterProfile WithLoadout(RoleLoadout loadout)
        {
            // Deep copies so we don't modify the DB profile.
            var copied = new Dictionary<string, RoleLoadout>();

            foreach (var proto in _loadouts)
            {
                if (proto.Key == loadout.Role)
                    continue;

                copied[proto.Key] = proto.Value.Clone();
            }

            copied[loadout.Role] = loadout.Clone();
            var profile = Clone();
            profile._loadouts = copied;
            return profile;
        }

        // RW edit start
        /// <summary>
        /// Removes the per-role loadout overrides for the specified role.
        /// This makes the role inherit purely from <see cref="BaseLoadout"/> again.
        /// </summary>
        public HumanoidCharacterProfile WithoutLoadout(string roleId)
        {
            var copied = new Dictionary<string, RoleLoadout>();

            foreach (var (key, value) in _loadouts)
            {
                if (key == roleId)
                    continue;

                copied[key] = value.Clone();
            }

            var profile = Clone();
            profile._loadouts = copied;
            return profile;
        }

        public HumanoidCharacterProfile WithBaseLoadout(RoleLoadout loadout)
        {
            var profile = Clone();
            profile._baseLoadout = loadout.Clone();
            return profile;
        }
        // RW edit end

        public RoleLoadout GetLoadoutOrDefault(string id, ICommonSession? session, ProtoId<SpeciesPrototype>? species, IEntityManager entManager, IPrototypeManager protoManager)
        {
            if (!_loadouts.TryGetValue(id, out var loadout))
            {
                loadout = new RoleLoadout(id);
                loadout.SetDefault(this, session, protoManager, force: true);
            }

            loadout.SetDefault(this, session, protoManager);
            return loadout;
        }

        // RW edit start
        public RoleLoadout GetEffectiveLoadout(string roleId, ICommonSession? session, IPrototypeManager protoManager)
        {
            var effective = new RoleLoadout(roleId);
            effective.SetDefault(this, session, protoManager, force: true);

            var baseCopy = _baseLoadout.Clone();
            baseCopy.Role = BaseLoadoutProtoId;
            baseCopy.SetDefault(this, session, protoManager);

            foreach (var (group, baseSel) in baseCopy.SelectedLoadouts)
            {
                effective.SelectedLoadouts[group] = new List<Loadout>(baseSel);
            }

            if (baseCopy.EntityName != null)
                effective.EntityName = baseCopy.EntityName;

            if (_loadouts.TryGetValue(roleId, out var overrides))
            {
                overrides.SetDefault(this, session, protoManager);

                foreach (var group in overrides.OverriddenGroups)
                {
                    if (overrides.SelectedLoadouts.TryGetValue(group, out var sel))
                        effective.SelectedLoadouts[group] = new List<Loadout>(sel);
                }

                if (overrides.EntityNameOverridden)
                    effective.EntityName = overrides.EntityName;
            }

            return effective;
        }
        // RW edit end

        // RW-End

        public HumanoidCharacterProfile Clone()
        {
            return new HumanoidCharacterProfile(this);
        }
    }
}
