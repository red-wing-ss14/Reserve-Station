using Content.Shared._RW.Discord;

namespace Content.Server._RW.Discord;

public sealed class ServerDiscordLinkManager : ISharedDiscordLinkManager
{
    public bool IsLinked => true;

    public event Action<Guid>? CodeReceived;
    public event Action? StatusUpdated;
}
