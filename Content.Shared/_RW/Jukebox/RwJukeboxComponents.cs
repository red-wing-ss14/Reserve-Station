using System;
using System.Collections.Generic;
using Lidgren.Network;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared._RW.Jukebox;

[Serializable, NetSerializable]
public enum RwJukeboxUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum RwTapeCreatorUIKey : byte
{
    Key
}

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
public sealed partial class RwJukeboxComponent : Component
{
    public static string JukeboxContainerName = "rw_jukebox_tapes";

    [ViewVariables(VVAccess.ReadOnly)]
    public Container TapeContainer = default!;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool Playing { get; set; } = true;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool Paused { get; set; }
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public float Volume { get; set; }

    [DataField]
    public float MaxAudioRange { get; set; } = 20f;

    [DataField]
    public float RolloffFactor { get; set; } = 0.3f;

    [AutoNetworkedField]
    public AmourPlayingSongData? PlayingSongData { get; set; }
}

[Serializable, NetSerializable]
public sealed class AmourPlayingSongData
{
    public ResPath? SongPath;
    public string? SongName;
    public float PlaybackPosition;
    public float ActualSongLengthSeconds;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class RwJukeboxSong
{
    [DataField]
    public string? SongName;

    [DataField("path")]
    public ResPath? SongPath;

    [DataField]
    public float SongDurationSeconds;
}

[Serializable, NetSerializable]
public sealed class RwJukeboxRequestSongPlay : EntityEventArgs
{
    public string? SongName { get; set; }
    public ResPath? SongPath { get; set; }
    public NetEntity? Jukebox { get; set; }
    public float SongDuration { get; set; }
}

[Serializable, NetSerializable]
public sealed class RwJukeboxRequestStop : EntityEventArgs
{
    public NetEntity? JukeboxUid { get; set; }
}

[Serializable, NetSerializable]
public sealed class RwJukeboxStopPlaying : EntityEventArgs
{
    public NetEntity? JukeboxUid { get; set; }
}

[Serializable, NetSerializable]
public sealed class RwJukeboxSongUploadRequest : EntityEventArgs
{
    public string SongName = string.Empty;
    public List<byte> SongBytes = new();
    public NetEntity TapeCreatorUid;
}

public sealed class RwJukeboxSongUploadNetMessage : NetMessage
{
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public const int MaxDataLength = 10 * 1024 * 1024;

    public ResPath RelativePath { get; set; } = ResPath.Self;
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var dataLength = buffer.ReadVariableInt32();
        if (dataLength < 0 || dataLength > MaxDataLength)
            throw new InvalidOperationException(
                $"RwJukeboxSongUploadNetMessage: invalid data length {dataLength} (max {MaxDataLength}).");

        var remainingBytes = (buffer.LengthBits - buffer.Position) / 8;
        if (dataLength > remainingBytes)
            throw new InvalidOperationException(
                $"RwJukeboxSongUploadNetMessage: declared data length {dataLength} exceeds remaining buffer ({remainingBytes}).");
        Data = buffer.ReadBytes(dataLength);
        RelativePath = new ResPath(buffer.ReadString());
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        if (Data.Length > MaxDataLength)
            throw new InvalidOperationException(
                $"RwJukeboxSongUploadNetMessage: data length {Data.Length} exceeds max {MaxDataLength}.");

        buffer.WriteVariableInt32(Data.Length);
        buffer.Write(Data);
        buffer.Write(RelativePath.ToString());
    }
}

[Serializable, NetSerializable]
public sealed class RwJukeboxSongUploadResponse : EntityEventArgs
{
    public NetEntity TapeCreatorUid;
    public bool Success;
    public string? Message;
}

[Serializable, NetSerializable]
public class RwJukeboxStopRequest : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class RwJukeboxPlayPauseRequest : BoundUserInterfaceMessage { }
[Serializable, NetSerializable]
public class RwJukeboxRepeatToggled : BoundUserInterfaceMessage
{
    public bool NewState { get; }
    public RwJukeboxRepeatToggled(bool newState)
    {
        NewState = newState;
    }
}

[Serializable, NetSerializable]
public class RwJukeboxEjectRequest : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class RwJukeboxSetPlaybackPosition(float playbackPosition) : BoundUserInterfaceMessage
{
    public float PlaybackPosition { get; } = playbackPosition;
}

[Serializable, NetSerializable]
public sealed class RwJukeboxSetVolume(float volume) : BoundUserInterfaceMessage
{
    public float Volume { get; } = volume;
}
