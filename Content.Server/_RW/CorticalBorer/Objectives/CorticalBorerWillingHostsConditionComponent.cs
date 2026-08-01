namespace Content.Server._RW.CorticalBorer.Objectives;

[RegisterComponent, Access(typeof(CorticalBorerWillingHostsConditionSystem))]
public sealed partial class CorticalBorerWillingHostsConditionComponent : Component
{
    [DataField(required: true)]
    public int Target;
}
