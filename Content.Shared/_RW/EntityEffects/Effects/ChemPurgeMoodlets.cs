using Content.Shared._RW.Mood;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._RW.EntityEffects.Effects;

/// <summary>
///     Removes all non-categorized moodlets from an entity(anything not "Static" like hunger & thirst).
/// </summary>
[UsedImplicitly]
public sealed partial class ChemPurgeMoodlets : EntityEffectBase<ChemPurgeMoodlets>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-purge-moodlets");

    [DataField]
    public bool RemovePermanentMoodlets;
}
