// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanoidCharacterAppearance : ICharacterAppearance, IEquatable<HumanoidCharacterAppearance>
{
    [DataField("hair")]
    public string HairStyleId { get; set; } = HairStyles.DefaultHairStyle;

    [DataField]
    public Color HairColor { get; set; } = Color.Black;

    // Amour edit start: optional second-stop gradient color for hair.
    [DataField]
    public Color HairColor2 { get; set; } = Color.Black;

    [DataField]
    public bool HairUseGradient { get; set; } = false;

    public const float MinHairGradientBlur = 0.5f;

    [DataField]
    public float HairGradientPosition { get; set; } = Marking.DefaultGradientPosition;

    [DataField]
    public float HairGradientBlur { get; set; } = Marking.DefaultGradientBlur;
    // Amour edit end

    [DataField("facialHair")]
    public string FacialHairStyleId { get; set; } = HairStyles.DefaultFacialHairStyle;

    [DataField]
    public Color FacialHairColor { get; set; } = Color.Black;

    // Amour edit start: optional second-stop gradient color for facial hair.
    [DataField]
    public Color FacialHairColor2 { get; set; } = Color.Black;

    [DataField]
    public bool FacialHairUseGradient { get; set; } = false;

    [DataField]
    public float FacialHairGradientPosition { get; set; } = Marking.DefaultGradientPosition;

    [DataField]
    public float FacialHairGradientBlur { get; set; } = Marking.DefaultGradientBlur;
    // Amour edit end
    [DataField]
    public Color EyeColor { get; set; } = Color.Black;

    [DataField]
    public Color SkinColor { get; set; } = Color.FromHsv(new Vector4(0.07f, 0.2f, 1f, 1f));

    [DataField]
    public List<Marking> Markings { get; set; } = new();

    public HumanoidCharacterAppearance(string hairStyleId,
        Color hairColor,
        string facialHairStyleId,
        Color facialHairColor,
        Color eyeColor,
        Color skinColor,
        List<Marking> markings)
    {
        HairStyleId = hairStyleId;
        HairColor = ClampColor(hairColor);
        FacialHairStyleId = facialHairStyleId;
        FacialHairColor = ClampColor(facialHairColor);
        EyeColor = ClampColor(eyeColor);
        SkinColor = ClampColor(skinColor);
        Markings = markings;
    }

    public HumanoidCharacterAppearance(HumanoidCharacterAppearance other) :
        this(other.HairStyleId, other.HairColor, other.FacialHairStyleId, other.FacialHairColor, other.EyeColor, other.SkinColor, new(other.Markings))
    {
        // Amour start
        HairColor2 = other.HairColor2;
        HairUseGradient = other.HairUseGradient;
        HairGradientPosition = other.HairGradientPosition;
        HairGradientBlur = ClampHairGradientBlur(other.HairGradientBlur);
        FacialHairColor2 = other.FacialHairColor2;
        FacialHairUseGradient = other.FacialHairUseGradient;
        FacialHairGradientPosition = other.FacialHairGradientPosition;
        FacialHairGradientBlur = other.FacialHairGradientBlur;
        // Amour end
    }

    public HumanoidCharacterAppearance WithHairStyleName(string newName)
    {
        return new(newName, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings)
        {
            // Amour start
            HairColor2 = HairColor2,
            HairUseGradient = HairUseGradient,
            HairGradientPosition = HairGradientPosition,
            HairGradientBlur = ClampHairGradientBlur(HairGradientBlur),
            FacialHairColor2 = FacialHairColor2,
            FacialHairUseGradient = FacialHairUseGradient,
            FacialHairGradientPosition = FacialHairGradientPosition,
            FacialHairGradientBlur = FacialHairGradientBlur,
            // Amour end
        };
    }
    public HumanoidCharacterAppearance WithHairColor(Color newColor)
    {
        return new(HairStyleId, newColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings)
        {
  // Amour edit start
            HairColor2 = HairColor2,
            HairUseGradient = HairUseGradient,
            HairGradientPosition = HairGradientPosition,
            HairGradientBlur = ClampHairGradientBlur(HairGradientBlur),
            FacialHairColor2 = FacialHairColor2,
            FacialHairUseGradient = FacialHairUseGradient,
            FacialHairGradientPosition = FacialHairGradientPosition,
            FacialHairGradientBlur = FacialHairGradientBlur,
        };
    }

    public HumanoidCharacterAppearance WithHairColor2(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings)
        {
            HairColor2 = newColor,
            HairUseGradient = HairUseGradient,
            HairGradientPosition = HairGradientPosition,
            HairGradientBlur = ClampHairGradientBlur(HairGradientBlur),
            FacialHairColor2 = FacialHairColor2,
            FacialHairUseGradient = FacialHairUseGradient,
            FacialHairGradientPosition = FacialHairGradientPosition,
            FacialHairGradientBlur = FacialHairGradientBlur,
        };
    }

    public HumanoidCharacterAppearance WithHairUseGradient(bool value)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings)
        {
            HairColor2 = HairColor2,
            HairUseGradient = value,
            HairGradientPosition = HairGradientPosition,
            HairGradientBlur = ClampHairGradientBlur(HairGradientBlur),
            FacialHairColor2 = FacialHairColor2,
            FacialHairUseGradient = FacialHairUseGradient,
            FacialHairGradientPosition = FacialHairGradientPosition,
            FacialHairGradientBlur = FacialHairGradientBlur,
        };
    }

    public HumanoidCharacterAppearance WithHairGradientPosition(float value)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings)
        {
            HairColor2 = HairColor2,
            HairUseGradient = HairUseGradient,
            HairGradientPosition = Marking.ClampGradientPosition(value),
            HairGradientBlur = ClampHairGradientBlur(HairGradientBlur),
            FacialHairColor2 = FacialHairColor2,
            FacialHairUseGradient = FacialHairUseGradient,
            FacialHairGradientPosition = FacialHairGradientPosition,
            FacialHairGradientBlur = FacialHairGradientBlur,
        };
    }

    public HumanoidCharacterAppearance WithHairGradientBlur(float value)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings)
        {
            HairColor2 = HairColor2,
            HairUseGradient = HairUseGradient,
            HairGradientPosition = HairGradientPosition,
            HairGradientBlur = ClampHairGradientBlur(value),
            FacialHairColor2 = FacialHairColor2,
            FacialHairUseGradient = FacialHairUseGradient,
            FacialHairGradientPosition = FacialHairGradientPosition,
            FacialHairGradientBlur = FacialHairGradientBlur,
        };
    }

    public HumanoidCharacterAppearance WithFacialHairColor2(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings)
        {
            HairColor2 = HairColor2,
            HairUseGradient = HairUseGradient,
            HairGradientPosition = HairGradientPosition,
            HairGradientBlur = ClampHairGradientBlur(HairGradientBlur),
            FacialHairColor2 = newColor,
            FacialHairUseGradient = FacialHairUseGradient,
            FacialHairGradientPosition = FacialHairGradientPosition,
            FacialHairGradientBlur = FacialHairGradientBlur,
        };
    }

    public HumanoidCharacterAppearance WithFacialHairUseGradient(bool value)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings)
        {
            HairColor2 = HairColor2,
            HairUseGradient = HairUseGradient,
            HairGradientPosition = HairGradientPosition,
            HairGradientBlur = ClampHairGradientBlur(HairGradientBlur),
            FacialHairColor2 = FacialHairColor2,
            FacialHairUseGradient = value,
            FacialHairGradientPosition = FacialHairGradientPosition,
            FacialHairGradientBlur = FacialHairGradientBlur,
        };
    }

    public HumanoidCharacterAppearance WithFacialHairGradientPosition(float value)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings)
        {
            HairColor2 = HairColor2,
            HairUseGradient = HairUseGradient,
            HairGradientPosition = HairGradientPosition,
            HairGradientBlur = ClampHairGradientBlur(HairGradientBlur),
            FacialHairColor2 = FacialHairColor2,
            FacialHairUseGradient = FacialHairUseGradient,
            FacialHairGradientPosition = Marking.ClampGradientPosition(value),
            FacialHairGradientBlur = FacialHairGradientBlur,
        };
    }

    public HumanoidCharacterAppearance WithFacialHairGradientBlur(float value)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings)
        {
            HairColor2 = HairColor2,
            HairUseGradient = HairUseGradient,
            HairGradientPosition = HairGradientPosition,
            HairGradientBlur = ClampHairGradientBlur(HairGradientBlur),
            FacialHairColor2 = FacialHairColor2,
            FacialHairUseGradient = FacialHairUseGradient,
            FacialHairGradientPosition = FacialHairGradientPosition,
            FacialHairGradientBlur = Marking.ClampGradientBlur(value),
        };
    }
    // Amour edit end

    public HumanoidCharacterAppearance WithFacialHairStyleName(string newName)
    {
        return new(HairStyleId, HairColor, newName, FacialHairColor, EyeColor, SkinColor, Markings)
        {
            // Amour start
            HairColor2 = HairColor2,
            HairUseGradient = HairUseGradient,
            HairGradientPosition = HairGradientPosition,
            HairGradientBlur = ClampHairGradientBlur(HairGradientBlur),
            FacialHairColor2 = FacialHairColor2,
            FacialHairUseGradient = FacialHairUseGradient,
            FacialHairGradientPosition = FacialHairGradientPosition,
            FacialHairGradientBlur = FacialHairGradientBlur,
            // Amour end
        };
    }

    public HumanoidCharacterAppearance WithFacialHairColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, newColor, EyeColor, SkinColor, Markings)
        {
            // Amour start
            HairColor2 = HairColor2,
            HairUseGradient = HairUseGradient,
            HairGradientPosition = HairGradientPosition,
            HairGradientBlur = ClampHairGradientBlur(HairGradientBlur),
            FacialHairColor2 = FacialHairColor2,
            FacialHairUseGradient = FacialHairUseGradient,
            FacialHairGradientPosition = FacialHairGradientPosition,
            FacialHairGradientBlur = FacialHairGradientBlur,
            // Amour end
        };
    }

    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, newColor, SkinColor, Markings)
        {
            // Amour start
            HairColor2 = HairColor2,
            HairUseGradient = HairUseGradient,
            HairGradientPosition = HairGradientPosition,
            HairGradientBlur = ClampHairGradientBlur(HairGradientBlur),
            FacialHairColor2 = FacialHairColor2,
            FacialHairUseGradient = FacialHairUseGradient,
            FacialHairGradientPosition = FacialHairGradientPosition,
            FacialHairGradientBlur = FacialHairGradientBlur,
            // Amour end
        };
    }

    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return new HumanoidCharacterAppearance(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, newColor, Markings)
        {
            // Amour start
            HairColor2 = HairColor2,
            HairUseGradient = HairUseGradient,
            HairGradientPosition = HairGradientPosition,
            HairGradientBlur = ClampHairGradientBlur(HairGradientBlur),
            FacialHairColor2 = FacialHairColor2,
            FacialHairUseGradient = FacialHairUseGradient,
            FacialHairGradientPosition = FacialHairGradientPosition,
            FacialHairGradientBlur = FacialHairGradientBlur,
            // Amour end
        };
    }

    public HumanoidCharacterAppearance WithMarkings(List<Marking> newMarkings)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, newMarkings)
        {
            // Amour start
            HairColor2 = HairColor2,
            HairUseGradient = HairUseGradient,
            HairGradientPosition = HairGradientPosition,
            HairGradientBlur = ClampHairGradientBlur(HairGradientBlur),
            FacialHairColor2 = FacialHairColor2,
            FacialHairUseGradient = FacialHairUseGradient,
            FacialHairGradientPosition = FacialHairGradientPosition,
            FacialHairGradientBlur = FacialHairGradientBlur,
            // Amour end
        };
    }

    public static HumanoidCharacterAppearance DefaultWithSpecies(string species)
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var speciesPrototype = protoMan.Index<SpeciesPrototype>(species);
        var skinColoration = protoMan.Index(speciesPrototype.SkinColoration).Strategy;
        var skinColor = skinColoration.InputType switch
        {
            SkinColorationStrategyInput.Unary => skinColoration.FromUnary(speciesPrototype.DefaultHumanSkinTone),
            SkinColorationStrategyInput.Color => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
            _ => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
        };

        return new(
            HairStyles.DefaultHairStyle,
            Color.Black,
            HairStyles.DefaultFacialHairStyle,
            Color.Black,
            Color.Black,
            skinColor,
            new()
        );
    }

    private static IReadOnlyList<Color> _realisticEyeColors = new List<Color>
    {
        Color.Brown,
        Color.Gray,
        Color.Azure,
        Color.SteelBlue,
        Color.Black
    };

    public static HumanoidCharacterAppearance Random(string species, Sex sex)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var markingManager = IoCManager.Resolve<MarkingManager>();
        var hairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.Hair, species).Keys.ToList();
        var facialHairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.FacialHair, species).Keys.ToList();

        var newHairStyle = hairStyles.Count > 0
            ? random.Pick(hairStyles)
            : HairStyles.DefaultHairStyle.Id;

        var newFacialHairStyle = facialHairStyles.Count == 0 || sex == Sex.Female
            ? HairStyles.DefaultFacialHairStyle.Id
            : random.Pick(facialHairStyles);

        var newHairColor = random.Pick(HairStyles.RealisticHairColors);
        newHairColor = newHairColor
            .WithRed(RandomizeColor(newHairColor.R))
            .WithGreen(RandomizeColor(newHairColor.G))
            .WithBlue(RandomizeColor(newHairColor.B));

        // TODO: Add random markings

        var newEyeColor = random.Pick(_realisticEyeColors);

        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var skinType = protoMan.Index<SpeciesPrototype>(species).SkinColoration;
        var strategy = protoMan.Index(skinType).Strategy;

        var newSkinColor = strategy.InputType switch
        {
            SkinColorationStrategyInput.Unary => strategy.FromUnary(random.NextFloat(0f, 100f)),
            SkinColorationStrategyInput.Color => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
            _ => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
        };

        return new HumanoidCharacterAppearance(newHairStyle, newHairColor, newFacialHairStyle, newHairColor, newEyeColor, newSkinColor, new());

        float RandomizeColor(float channel)
        {
            return MathHelper.Clamp01(channel + random.Next(-25, 25) / 100f);
        }
    }

    public static Color ClampColor(Color color)
    {
        return new(color.RByte, color.GByte, color.BByte);
    }

    public static float ClampHairGradientBlur(float blur) =>
        float.IsNaN(blur) ? Marking.DefaultGradientBlur : Math.Clamp(blur, MinHairGradientBlur, 1f);

    public static HumanoidCharacterAppearance EnsureValid(HumanoidCharacterAppearance appearance, string species, Sex sex)
    {
        var hairStyleId = appearance.HairStyleId;
        var facialHairStyleId = appearance.FacialHairStyleId;

        var hairColor = ClampColor(appearance.HairColor);
        var facialHairColor = ClampColor(appearance.FacialHairColor);
        var eyeColor = ClampColor(appearance.EyeColor);

        var proto = IoCManager.Resolve<IPrototypeManager>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        if (!markingManager.MarkingsByCategory(MarkingCategories.Hair).ContainsKey(hairStyleId))
        {
            hairStyleId = HairStyles.DefaultHairStyle;
        }

        if (!markingManager.MarkingsByCategory(MarkingCategories.FacialHair).ContainsKey(facialHairStyleId))
        {
            facialHairStyleId = HairStyles.DefaultFacialHairStyle;
        }

        var markingSet = new MarkingSet();
        var skinColor = appearance.SkinColor;
        if (proto.TryIndex(species, out SpeciesPrototype? speciesProto))
        {
            markingSet = new MarkingSet(appearance.Markings, speciesProto.MarkingPoints, markingManager, proto);
            markingSet.EnsureValid(markingManager);

            var strategy = proto.Index(speciesProto.SkinColoration).Strategy;
            skinColor = strategy.EnsureVerified(skinColor);

            markingSet.EnsureSpecies(species, skinColor, markingManager, null); // Amour add null
            markingSet.EnsureSexes(sex, markingManager);
        }

        return new HumanoidCharacterAppearance(
            hairStyleId,
            hairColor,
            facialHairStyleId,
            facialHairColor,
            eyeColor,
            skinColor,
            markingSet.GetForwardEnumerator().ToList())
        {
            // Amour start
            HairColor2 = ClampColor(appearance.HairColor2),
            HairUseGradient = appearance.HairUseGradient,
            HairGradientPosition = Marking.ClampGradientPosition(appearance.HairGradientPosition),
            HairGradientBlur = ClampHairGradientBlur(appearance.HairGradientBlur),
            FacialHairColor2 = ClampColor(appearance.FacialHairColor2),
            FacialHairUseGradient = appearance.FacialHairUseGradient,
            FacialHairGradientPosition = Marking.ClampGradientPosition(appearance.FacialHairGradientPosition),
            FacialHairGradientBlur = Marking.ClampGradientBlur(appearance.FacialHairGradientBlur),
            // Amour end
        };
    }

    public bool MemberwiseEquals(ICharacterAppearance maybeOther)
    {
        if (maybeOther is not HumanoidCharacterAppearance other) return false;
        if (HairStyleId != other.HairStyleId) return false;
        if (!HairColor.Equals(other.HairColor)) return false;
        if (FacialHairStyleId != other.FacialHairStyleId) return false;
        if (!FacialHairColor.Equals(other.FacialHairColor)) return false;
        if (!EyeColor.Equals(other.EyeColor)) return false;
        if (!SkinColor.Equals(other.SkinColor)) return false;
        if (!Markings.SequenceEqual(other.Markings)) return false;
        // Amour edit start
        if (!HairColor2.Equals(other.HairColor2)) return false;
        if (HairUseGradient != other.HairUseGradient) return false;
        if (!HairGradientPosition.Equals(other.HairGradientPosition)) return false;
        if (!HairGradientBlur.Equals(other.HairGradientBlur)) return false;
        if (!FacialHairColor2.Equals(other.FacialHairColor2)) return false;
        if (FacialHairUseGradient != other.FacialHairUseGradient) return false;
        if (!FacialHairGradientPosition.Equals(other.FacialHairGradientPosition)) return false;
        if (!FacialHairGradientBlur.Equals(other.FacialHairGradientBlur)) return false;
        // Amour edit end
        return true;
    }

    public bool Equals(HumanoidCharacterAppearance? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return HairStyleId == other.HairStyleId &&
               HairColor.Equals(other.HairColor) &&
               FacialHairStyleId == other.FacialHairStyleId &&
               FacialHairColor.Equals(other.FacialHairColor) &&
               EyeColor.Equals(other.EyeColor) &&
               SkinColor.Equals(other.SkinColor) &&
               Markings.SequenceEqual(other.Markings) &&
               // Amour start
               HairColor2.Equals(other.HairColor2) &&
               HairUseGradient == other.HairUseGradient &&
               HairGradientPosition.Equals(other.HairGradientPosition) &&
               HairGradientBlur.Equals(other.HairGradientBlur) &&
               FacialHairColor2.Equals(other.FacialHairColor2) &&
               FacialHairUseGradient == other.FacialHairUseGradient &&
               FacialHairGradientPosition.Equals(other.FacialHairGradientPosition) &&
               FacialHairGradientBlur.Equals(other.FacialHairGradientBlur);
               // Amour end
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is HumanoidCharacterAppearance other && Equals(other);
    }

    public override int GetHashCode()
    {
        // Amour edit: include gradient fields
        var baseHash = HashCode.Combine(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings);
        var hairHash = HashCode.Combine(HairColor2, HairUseGradient, HairGradientPosition, HairGradientBlur);
        var facialHairHash = HashCode.Combine(FacialHairColor2, FacialHairUseGradient, FacialHairGradientPosition, FacialHairGradientBlur);
        return HashCode.Combine(baseHash, hairHash, facialHairHash);
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }
}
