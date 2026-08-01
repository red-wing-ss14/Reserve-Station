using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._RW.Ghost.Roles.Components;

[RegisterComponent]
public sealed partial class SsdAmnesiacGhostRoleComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public NetUserId OriginalUserId;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid OriginalMind;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan MakeAvailableAt;

    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<JobPrototype>? Job;

    [ViewVariables(VVAccess.ReadOnly)]
    public string JobName = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool RoleCreated;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Taken;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool RequireNukeOperative;
}
