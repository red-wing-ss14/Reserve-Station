namespace Content.Server._RW.Morph.Objectives;

[RegisterComponent, Access(typeof(MorphDevourLivingConditionSystem))]
public sealed partial class MorphDevourLivingConditionComponent : Component
{
    [DataField(required: true)]
    public int Target;
}
