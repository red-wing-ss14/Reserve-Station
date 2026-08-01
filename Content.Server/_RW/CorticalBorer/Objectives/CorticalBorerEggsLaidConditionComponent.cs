namespace Content.Server._RW.CorticalBorer.Objectives;

[RegisterComponent, Access(typeof(CorticalBorerEggsLaidConditionSystem))]
public sealed partial class CorticalBorerEggsLaidConditionComponent : Component
{
    [DataField(required: true)]
    public int Target;
}
