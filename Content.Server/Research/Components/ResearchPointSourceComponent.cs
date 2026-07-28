// SPDX-License-Identifier: MIT

namespace Content.Server.Research.Components;

[RegisterComponent]
public sealed partial class ResearchPointSourceComponent : Component
{
    // RW-Start
    [DataField]
    public string PointType = "General";
    // RW-End

    [DataField("pointspersecond"), ViewVariables(VVAccess.ReadWrite)]
    public int PointsPerSecond;

    // RW-Start
    [DataField]
    public string? RequiredInfrastructure;
    // RW-End

    [DataField("active"), ViewVariables(VVAccess.ReadWrite)]
    public bool Active;
}
