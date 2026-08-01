using Content.Shared._RW.Mood;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._RW.EntityEffects.Effects;

/// <summary>
///     Removes a moodlet from an entity if present.
/// </summary>
[UsedImplicitly]
public sealed partial class ChemRemoveMoodlet : EntityEffectBase<ChemRemoveMoodlet>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var moodPrototype = prototype.Index<MoodEffectPrototype>(MoodPrototype.Id);
        return Loc.GetString("reagent-effect-guidebook-remove-moodlet",
            ("name", moodPrototype.Description()));
    }

    /// <summary>
    ///     The mood prototype to be removed from the entity.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MoodEffectPrototype> MoodPrototype;
}
