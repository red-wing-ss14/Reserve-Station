using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Shared._RW.Registry;
using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RW.Registry;

public sealed class ClientMetricsManager
{
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _plyMgr = default!;
    [Dependency] private readonly IConnectionManager _connectionManager = default!;

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgClientMetrics>(OnClientMetricsReceived);
    }

    private async void OnClientMetricsReceived(MsgClientMetrics message)
    {
        try
        {
            var channel = message.MsgChannel;
            var session = _plyMgr.GetSessionByChannel(channel);
            if (session == null) return;

            if (message.ClientSignatures.Count > MsgClientMetrics.MaxSignatures)
            {
                return;
            }

            var reportedGuids = new List<Guid>();

            foreach (var sig in message.ClientSignatures)
            {
                if (Guid.TryParse(sig, out var guid))
                {
                    reportedGuids.Add(guid);
                }
            }

            if (reportedGuids.Count == 0)
                return;

            var bannedId = await _db.FindFirstClientRecord(reportedGuids);

            if (bannedId != null)
            {
                channel.Disconnect("Server connection refused.");
            }
        }
        catch (Exception)
        {

        }
    }
}
