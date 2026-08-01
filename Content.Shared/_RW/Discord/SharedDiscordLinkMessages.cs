using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RW.Discord;

public sealed class DiscordLinkRequestMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) { }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) { }
}

public sealed class DiscordLinkCodeMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public Guid Code { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Code = buffer.ReadGuid();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Code);
    }
}

public sealed class DiscordLinkStatusMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public bool IsLinked { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        IsLinked = buffer.ReadBoolean();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(IsLinked);
    }
}
