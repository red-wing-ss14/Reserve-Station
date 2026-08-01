namespace Content.Server._RW.Morph.Objectives;

[RegisterComponent, Access(typeof(MorphReproduceConditionSystem))]
public sealed partial class MorphReproduceConditionComponent : Component
{
    [DataField(required: true)]
    public int Target;
}
