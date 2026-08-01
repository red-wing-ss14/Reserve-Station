using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._RW.Discord;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RW.Discord;

public sealed class DiscordLinkSystem : EntitySystem
{
    [Dependency] private readonly IDiscordLinkChecker _discordLinkChecker = default!;
    [Dependency] private readonly IServerDbManager _db = default!;    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    private readonly Dictionary<NetUserId, TimeSpan> _lastRequest = new();
    private readonly TimeSpan _minimumWait = TimeSpan.FromSeconds(0.5);

    public override void Initialize()
    {
        base.Initialize();

        _discordLinkChecker.Initialize();

        _net.RegisterNetMessage<DiscordLinkRequestMsg>(OnLinkRequest);
        _net.RegisterNetMessage<DiscordLinkCodeMsg>();
        _net.RegisterNetMessage<DiscordLinkStatusMsg>();

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        _userDb.AddOnLoadPlayer(LoadDataAsync);
        _userDb.AddOnFinishLoad(OnPlayerLoaded);
    }

    private async Task LoadDataAsync(ICommonSession session, CancellationToken cancel)
    {
        await _discordLinkChecker.IsDiscordLinkedAsync(session);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Connected)
        {
            _ = SendLinkStatus(e.Session);
        }

        if (e.NewStatus == SessionStatus.Disconnected)
        {
            var userId = e.Session.UserId;
            _lastRequest.Remove(userId);
            _discordLinkChecker.Cleanup(userId);
        }
    }
    private void OnPlayerLoaded(ICommonSession session)
    {
        _ = SendLinkStatus(session);
    }

    public async Task SendLinkStatus(ICommonSession session)
    {
        try
        {
            var isLinked = await _discordLinkChecker.IsDiscordLinkedAsync(session);
            var msg = new DiscordLinkStatusMsg { IsLinked = isLinked };
            _net.ServerSendMessage(msg, session.Channel);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to send Discord link status to {session.Name}: {ex}");
        }
    }

    private void OnLinkRequest(DiscordLinkRequestMsg message)
    {
        var userId = message.MsgChannel.UserId;
        var time = _timing.RealTime;

        if (_lastRequest.TryGetValue(userId, out var last) && last + _minimumWait > time)
            return;

        _lastRequest[userId] = time;

        var code = Guid.NewGuid();
        _db.SetLinkingCode(userId.UserId, code);

        var response = new DiscordLinkCodeMsg { Code = code };
        _net.ServerSendMessage(response, message.MsgChannel);
    }
}
