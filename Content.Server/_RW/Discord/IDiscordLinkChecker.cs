using System.Threading.Tasks;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RW.Discord;

public interface IDiscordLinkChecker
{
    void Initialize();
    Task<bool> IsDiscordLinkedAsync(ICommonSession session);
    bool IsDiscordLinkedCached(NetUserId userId);
    void Cleanup(NetUserId userId);
}
