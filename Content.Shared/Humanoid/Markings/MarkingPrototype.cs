// SPDX-License-Identifier: MIT

using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings
{
    [Prototype]
    public sealed partial class MarkingPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = "uwu";

        public string Name { get; private set; } = default!;

        [DataField("bodyPart", required: true)]
        public HumanoidVisualLayers BodyPart { get; private set; } = default!;

        // RW edit start
        /// <summary>
        ///     Optional per-sprite body part targets. When specified, each sprite
        ///     is applied over its matching body layer instead of every sprite
        ///     using <see cref="BodyPart"/>.
        /// </summary>
        [DataField("bodyParts")]
        public List<HumanoidVisualLayers>? BodyParts { get; private set; }

        public HumanoidVisualLayers GetBodyPart(int spriteIndex)
        {
            if (BodyParts != null && spriteIndex >= 0 && spriteIndex < BodyParts.Count)
                return BodyParts[spriteIndex];

            return BodyPart;
        }

        public bool AppliesToBodyPart(HumanoidVisualLayers layer)
        {
            if (BodyPart == layer)
                return true;

            return BodyParts != null && BodyParts.Contains(layer);
        }
        // RW edit end

        [DataField("markingCategory", required: true)]
        public MarkingCategories MarkingCategory { get; private set; } = default!;

        [DataField("speciesRestriction")]
        public List<string>? SpeciesRestrictions { get; private set; }

        [DataField("sexRestriction")]
        public Sex? SexRestriction { get; private set; }

        // RW edit start
        /// <summary>
        /// List of usernames that are allowed to use this marking.
        /// If null or empty, the marking is available to everyone.
        /// </summary>
        [DataField("allowedUsers")]
        public List<string>? AllowedUsers { get; private set; }

        /// <summary>
        /// Minimum Boosty subscription tier required to use this marking.
        /// 0 = available to everyone.
        /// </summary>
        [DataField("minBoostyTier")]
        public int MinBoostyTier { get; private set; } = 0;
        // RW edit end

        [DataField("followSkinColor")]
        public bool FollowSkinColor { get; private set; } = false;

        [DataField("forcedColoring")]
        public bool ForcedColoring { get; private set; } = false;

        [DataField("coloring")]
        public MarkingColors Coloring { get; private set; } = new();

        /// <summary>
        /// Do we need to apply any displacement maps to this marking? Set to false if your marking is incompatible
        /// with a standard human doll, and is used for some special races with unusual shapes
        /// </summary>
        [DataField]
        public bool CanBeDisplaced { get; private set; } = true;

        [DataField("sprites", required: true)]
        public List<SpriteSpecifier> Sprites { get; private set; } = default!;

        /// Impstation start
        [DataField]

        public string? Shader { get; private set; } = null;
        /// Impstation end

        // RW edit start
        /// <summary>
        ///     If true, this marking can be rendered with a vertical two-color gradient
        ///     and the editor UI will expose a second color picker for it.
        ///     Defaults to false to avoid showing the toggle for markings that don't
        ///     visually benefit from it.
        /// </summary>
        [DataField("supportsGradient")]
        public bool SupportsGradient { get; private set; } = false;
        // RW edit end
        public Marking AsMarking()
        {
            return new Marking(ID, Sprites.Count);
        }
    }
}
