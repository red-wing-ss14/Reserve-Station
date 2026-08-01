using Robust.Shared.Containers;

namespace Content.Server._RW.Bitrunning.Components;

[RegisterComponent]
[Access(typeof(Systems.NetpodSystem))]
public sealed partial class NetpodContainerComponent : Component
{
    public ContainerSlot BodyContainer = default!;
}
