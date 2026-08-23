using System.Collections.Concurrent;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._RW.Loadouts;
using Content.Shared._RW.Loadouts.Effects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RW.Loadouts;

public sealed class BoostyTierManager : IBoostyTierManager
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IServerNetManager _netMgr = default!;

    private ISawmill? _sawmill;
    private bool _initialized;

    private readonly ConcurrentDictionary<Guid, (BoostyPlayerTier? Tier, DateTime CachedAt, bool IsError)> _cache = new();

    private readonly ConcurrentDictionary<Guid, Task> _inflight = new();

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ErrorCacheDuration = TimeSpan.FromSeconds(30);

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;
        _netMgr.RegisterNetMessage<MsgBoostyTierInfo>();
    }

    public BoostyPlayerTier? GetPlayerTier(ICommonSession session)
    {
        _sawmill ??= _log.GetSawmill("redwing.boosty");
        EnsureInitialized();

        var userId = session.UserId;

        if (_cache.TryGetValue(userId, out var cached))
        {
            var duration = cached.IsError ? ErrorCacheDuration : CacheDuration;
            if (DateTime.UtcNow - cached.CachedAt < duration)
            {
                return cached.Tier;
            }

            // Cache expired, refresh in background and return stale data
            EnsureRefresh(userId);
            return cached.Tier;
        }

        // Data not preloaded yet - trigger async refresh and return null
        // This shouldn't happen if PreloadPlayerTierAsync was called on connect
        _sawmill.Warning($"GetPlayerTier cache miss for {session.Name} ({userId}) - data wasn't preloaded!");
        EnsureRefresh(userId);
        return null;
    }

    private void EnsureRefresh(Guid userId)
    {
        // Use GetOrAdd to atomically check and add the task, avoiding race condition
        var task = _inflight.GetOrAdd(userId, id =>
        {
            _sawmill?.Debug($"EnsureRefresh: starting refresh task for {id}");
            var refreshTask = RefreshAsync(id);

            _ = refreshTask.ContinueWith(t =>
            {
                _inflight.TryRemove(id, out _);

                if (t.IsFaulted)
                    _sawmill?.Warning($"Failed to refresh Boosty tier for {id}: {t.Exception}");
                else
                    _sawmill?.Debug($"EnsureRefresh: completed for {id}");
            });

            return refreshTask;
        });

        if (task.IsCompleted)
            _sawmill?.Debug($"EnsureRefresh: task already completed for {userId}");
        else
            _sawmill?.Debug($"EnsureRefresh: task in progress for {userId}");
    }

    private async Task RefreshAsync(Guid userId)
    {
        try
        {
            _sawmill?.Debug($"RefreshAsync: querying database for {userId}");
            var boosterInfo = await _db.GetBoostyTierAsync(userId);

            BoostyPlayerTier? tier = null;
            if (boosterInfo != null)
            {
                _sawmill?.Debug($"RefreshAsync: found in DB - TierName={boosterInfo.TierName}, TierLevel={boosterInfo.TierLevel}, IsActive={boosterInfo.IsActive}");
                tier = new BoostyPlayerTier
                {
                    TierName = boosterInfo.TierName,
                    TierLevel = boosterInfo.TierLevel,
                    IsActive = boosterInfo.IsActive
                };
            }
            else
            {
                _sawmill?.Debug($"RefreshAsync: no record found in database for {userId}");
            }

            _cache[userId] = (tier, DateTime.UtcNow, IsError: false);
        }
        catch (Exception e)
        {
            _cache[userId] = (null, DateTime.UtcNow, IsError: true);
            _sawmill?.Warning($"Error querying Boosty tier for {userId}: {e}");
        }
    }

    public async Task PreloadPlayerTierAsync(ICommonSession session)
    {
        _sawmill ??= _log.GetSawmill("redwing.boosty");
        EnsureInitialized();

        var userId = session.UserId;
        _sawmill.Debug($"PreloadPlayerTierAsync called for {session.Name}, UserId={userId}");

        try
        {
            var boosterInfo = await _db.GetBoostyTierAsync(userId);

            BoostyPlayerTier? tier = null;
            if (boosterInfo != null)
            {
                tier = new BoostyPlayerTier
                {
                    TierName = boosterInfo.TierName,
                    TierLevel = boosterInfo.TierLevel,
                    IsActive = boosterInfo.IsActive
                };
                _sawmill.Debug($"Preloaded from DB: TierName={tier.TierName}, TierLevel={tier.TierLevel}, IsActive={tier.IsActive}");
            }
            else
            {
                _sawmill.Debug($"No data in DB for {userId}");
            }

            _cache[userId] = (tier, DateTime.UtcNow, IsError: false);

            // Send tier info to client
            SendTierInfoToClient(session, tier);
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Error preloading Boosty tier for {userId}: {e}");
            _cache[userId] = (null, DateTime.UtcNow, IsError: true);

            // Send empty tier info on error
            SendTierInfoToClient(session, null);
        }
    }

    private void SendTierInfoToClient(ICommonSession session, BoostyPlayerTier? tier)
    {
        var msg = new MsgBoostyTierInfo
        {
            IsActive = tier?.IsActive ?? false,
            TierLevel = tier?.TierLevel ?? 0,
            TierName = tier?.TierName ?? string.Empty
        };

        _netMgr.ServerSendMessage(msg, session.Channel);
        _sawmill?.Debug($"Sent Boosty tier info to {session.Name}: IsActive={msg.IsActive}, TierLevel={msg.TierLevel}, TierName={msg.TierName}");
    }

    public void InvalidateCache(Guid playerId)
    {
        _cache.TryRemove(playerId, out _);
    }

    public void ClearCache()
    {
        _cache.Clear();
    }
}
