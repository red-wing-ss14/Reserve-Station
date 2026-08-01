using Robust.Shared.GameStates;

namespace Content.Shared._RW.Doors.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PhantomAirtightParentComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity? ParentUid;
}
