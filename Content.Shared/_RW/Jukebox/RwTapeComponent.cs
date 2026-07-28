using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._RW.Jukebox;

[RegisterComponent, NetworkedComponent]
public sealed partial class RwTapeComponent : Component
{
    [DataField("songs")]
    public List<RwJukeboxSong> Songs { get; set; } = new();
}

[Serializable, NetSerializable]
public sealed partial class RwTapeComponentState : ComponentState
{
    public List<RwJukeboxSong> Songs { get; set; } = new();
}
