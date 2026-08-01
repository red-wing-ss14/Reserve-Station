using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using SysColor = System.Drawing.Color;

namespace Content.Server._RW.Booster;

public sealed class RwBoosterSystem : EntitySystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly Dictionary<NetUserId, SysColor?> _boosterColors = new();

    public override void Initialize()
    {
        base.Initialize();

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == Robust.Shared.Enums.SessionStatus.Connected)
        {
            await LoadBoosterData(args.Session);
        }
        else if (args.NewStatus == Robust.Shared.Enums.SessionStatus.Disconnected)
        {
            _boosterColors.Remove(args.Session.UserId);
        }
    }

    private async Task LoadBoosterData(ICommonSession session)
    {
        var color = await _db.GetBoosterColor(session.UserId, CancellationToken.None);
        if (color.HasValue)
        {
            _boosterColors[session.UserId] = SysColor.FromArgb(color.Value);
        }
        else
        {
            _boosterColors.Remove(session.UserId);
        }
    }

    public SysColor? GetOocColor(NetUserId userId)
    {
        return _boosterColors.TryGetValue(userId, out var color) ? color : null;
    }

    public bool HasCustomOocColor(NetUserId userId)
    {
        return _boosterColors.ContainsKey(userId);
    }
}
