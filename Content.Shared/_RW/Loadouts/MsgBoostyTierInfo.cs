using Content.Shared._RW.Loadouts.Effects;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RW.Loadouts;

/// <summary>
/// Network message sent from server to client to sync Boosty tier information.
/// </summary>
public sealed class MsgBoostyTierInfo : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    /// <summary>
    /// Whether the player has an active Boosty subscription.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The tier level (0 if not subscribed).
    /// </summary>
    public int TierLevel { get; set; }

    /// <summary>
    /// The tier name (empty if not subscribed).
    /// </summary>
    public string TierName { get; set; } = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        IsActive = buffer.ReadBoolean();
        TierLevel = buffer.ReadInt32();
        TierName = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(IsActive);
        buffer.Write(TierLevel);
        buffer.Write(TierName);
    }
}
