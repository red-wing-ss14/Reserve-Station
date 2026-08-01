using Content.Shared._RW.Mood;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._RW.EntityEffects.Effects;

/// <summary>
///     Adds a moodlet to an entity.
/// </summary>
[UsedImplicitly]
public sealed partial class ChemAddMoodlet : EntityEffectBase<ChemAddMoodlet>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var moodPrototype = prototype.Index<MoodEffectPrototype>(MoodPrototype.Id);
        return Loc.GetString("reagent-effect-guidebook-add-moodlet",
            ("amount", moodPrototype.MoodChange),
            ("timeout", moodPrototype.Timeout));
    }

    /// <summary>
    ///     The mood prototype to be applied to the using entity.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MoodEffectPrototype> MoodPrototype;
}
