using Content.Server._RW.Gravity.Systems;

namespace Content.Server._RW.Gravity.Components;

[RegisterComponent]
[Access(typeof(GravitySourceSystem))]
public sealed partial class GravitySourceComponent : Component
{
    [ViewVariables]
    public bool Active;
}
