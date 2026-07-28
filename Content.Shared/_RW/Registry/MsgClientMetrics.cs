using System.Collections.Generic;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RW.Registry;

public sealed class MsgClientMetrics : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public const int MaxSignatures = 128;

    public List<string> ClientSignatures { get; set; } = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadInt32();

        if (count > MaxSignatures) 
        {
            count = MaxSignatures; 
        }
        
        if (count < 0) count = 0;

        ClientSignatures = new List<string>(count);
        for (var i = 0; i < count; i++)
            ClientSignatures.Add(buffer.ReadString());
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        var count = ClientSignatures.Count > MaxSignatures ? MaxSignatures : ClientSignatures.Count;
        
        buffer.Write(count);
        for (var i = 0; i < count; i++)
            buffer.Write(ClientSignatures[i]);
    }
}
