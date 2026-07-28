namespace Content.Shared._RW.Discord;

public interface ISharedDiscordLinkManager
{
    bool IsLinked { get; }
    event Action<Guid>? CodeReceived;
    event Action? StatusUpdated;
}
