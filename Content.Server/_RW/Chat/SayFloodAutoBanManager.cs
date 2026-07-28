using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Shared.Database;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RW.Chat;

public sealed class SayFloodAutoBanManager
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private const int WindowSeconds = 60;
    private const int LimitPerWindow = 70;
    private const string BanReason = "Твинк";
    private const string BanningAdminName = "Meowklka";

    private readonly Dictionary<NetUserId, Queue<TimeSpan>> _history = new();
    private readonly HashSet<NetUserId> _banned = new();
    private ISawmill _sawmill = default!;

    private NetUserId? _banningAdminId;
    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("say_flood_autoban");
    }

    public void RegisterSayUsage(ICommonSession player)
    {
        var userId = player.UserId;

        if (_banned.Contains(userId))
            return;

        var now = _timing.RealTime;
        var threshold = now - TimeSpan.FromSeconds(WindowSeconds);

        if (!_history.TryGetValue(userId, out var queue))
        {
            queue = new Queue<TimeSpan>();
            _history[userId] = queue;
        }

        while (queue.Count > 0 && queue.Peek() < threshold)
            queue.Dequeue();

        queue.Enqueue(now);

        if (queue.Count <= LimitPerWindow)
            return;

        _banned.Add(userId);
        _history.Remove(userId);

        _ = IssueBanAsync(player);
    }

    private async Task IssueBanAsync(ICommonSession player)
    {
        try
        {
            var banningAdmin = await ResolveBanningAdminAsync();

            var banInfo = new CreateServerBanInfo(BanReason);
            banInfo.AddUser(player.UserId, player.Name);
            banInfo.WithBanningAdmin(banningAdmin);
            banInfo.WithSeverity(NoteSeverity.High);

            _banManager.CreateServerBan(banInfo);
        }
        catch (Exception e)
        {
            _sawmill.Error("Failed to issue an automatic say flood ban to {0} ({1}): {2}", player.UserId, player.Name, e);
        }
    }

    private async Task<NetUserId?> ResolveBanningAdminAsync()
    {
        if (_banningAdminId.HasValue)
            return _banningAdminId;

        var data = await _locator.LookupIdByNameAsync(BanningAdminName);
        if (data == null)
            return null;

        _banningAdminId = data.UserId;
        return _banningAdminId;
    }
}