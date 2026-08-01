using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Network;
using Robust.Shared.Log;
using Robust.Shared.Player;

namespace Content.Server._RW.Discord;

public sealed class DiscordLinkChecker : IDiscordLinkChecker
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill _sawmill = default!;
    private readonly ConcurrentDictionary<NetUserId, (bool IsLinked, DateTime LastCheck)> _linkCache = new();
    private readonly ConcurrentDictionary<NetUserId, SemaphoreSlim> _checkLocks = new();
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);

    public void Initialize()
    {
        _sawmill = _log.GetSawmill("discord_link");
    }

    public async Task<bool> IsDiscordLinkedAsync(ICommonSession session)
    {
        var userId = session.UserId;

        var lockObj = _checkLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));

        await lockObj.WaitAsync();
        try
        {
            if (_linkCache.TryGetValue(userId, out var cached))
            {
                var age = DateTime.UtcNow - cached.LastCheck;
                if (age < _cacheExpiry)
                {
                    return cached.IsLinked;
                }
            }

            var isLinked = await _db.HasLinkedAccount(userId, CancellationToken.None);
            _linkCache[userId] = (isLinked, DateTime.UtcNow);
            return isLinked;
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to check Discord link for {userId}: {ex}");
            return false;
        }
        finally
        {
            lockObj.Release();
        }
    }

    public bool IsDiscordLinkedCached(NetUserId userId)
    {
        if (_linkCache.TryGetValue(userId, out var cached))
        {
            return cached.IsLinked;
        }

        return false;
    }

    public void Cleanup(NetUserId userId)
    {
        _linkCache.TryRemove(userId, out _);

        if (_checkLocks.TryRemove(userId, out var semaphore))
        {
            semaphore.Dispose();
        }
    }
}
