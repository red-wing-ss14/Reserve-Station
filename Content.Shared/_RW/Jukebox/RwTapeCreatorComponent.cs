using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared._RW.Jukebox;

[RegisterComponent, NetworkedComponent]
public sealed partial class RwTapeCreatorComponent : Component
{
    [DataField("coins")]
    public int CoinBalance { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Recording { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public NetEntity? InsertedTape { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public Container TapeContainer { get; set; } = default!;
}

[Serializable, NetSerializable]
public sealed class RwTapeCreatorComponentState : ComponentState
{
    public int CoinBalance { get; set; }
    public bool Recording { get; set; }
    public NetEntity? InsertedTape { get; set; }
}
