using Content.Shared._RW.Mood;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RW.Traits.Components;

[RegisterComponent]
public sealed partial class MoodTraitComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<MoodEffectPrototype>> MoodEffects = new();
}
