// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared._RW.Research;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Research.Components;
using Robust.Shared.Timing;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private void InitializeServer()
    {
        SubscribeLocalEvent<ResearchServerComponent, ComponentStartup>(OnServerStartup);
        SubscribeLocalEvent<ResearchServerComponent, MapInitEvent>(OnServerMapInit); // RW
        SubscribeLocalEvent<ResearchServerComponent, ComponentShutdown>(OnServerShutdown);
        SubscribeLocalEvent<ResearchServerComponent, TechnologyDatabaseModifiedEvent>(OnServerDatabaseModified);
        SubscribeLocalEvent<ResearchServerComponent, ExaminedEvent>(OnServerExamined); // RW
        SubscribeLocalEvent<ResearchClientComponent, GotEmaggedEvent>(OnResearchClientEmagged); // RW
    }

    private void OnServerStartup(EntityUid uid, ResearchServerComponent component, ComponentStartup args)
    {
        var unusedId = EntityQuery<ResearchServerComponent>(true)
            .Max(s => s.Id) + 1;
        component.Id = unusedId;

        // RW-Start
        EnsurePointBalance(component, "General");
        Dirty(uid, component);
        // RW-End
    }

    // RW-Start
    private void OnServerMapInit(EntityUid uid, ResearchServerComponent component, MapInitEvent args)
    {
        AssignServerName(component);
        LogNetworkEvent(uid, "network", Loc.GetString("research-netlog-server-joined", ("server", component.ServerName)));
        Dirty(uid, component);
        ConnectUnregisteredClientsOnServerGrid(uid);
        Timer.Spawn(0, () => SyncServerClients(uid));
    }

    private void SyncServerClients(EntityUid uid)
    {
        if (!TryComp<ResearchServerComponent>(uid, out var server))
            return;

        foreach (var client in server.Clients.ToArray())
        {
            if (TryComp<ResearchClientComponent>(client, out var clientComponent))
                SyncClientWithServer(client, clientComponent: clientComponent);
        }
    }

    private void ConnectUnregisteredClientsOnServerGrid(EntityUid serverUid)
    {
        var serverGrid = Transform(serverUid).GridUid;
        if (serverGrid is null)
            return;

        var query = EntityQueryEnumerator<ResearchClientComponent, TransformComponent>();
        while (query.MoveNext(out var clientUid, out var client, out var xform))
        {
            if (client.Server is not null || xform.GridUid != serverGrid)
                continue;

            TryAutoRegisterClient(clientUid, client);
        }
    }
    // RW-End

    private void OnServerShutdown(EntityUid uid, ResearchServerComponent component, ComponentShutdown args)
    {
        // RW-Start
        var survivingAuthority = GetNetworkServers(uid, component)
            .Where(s => s != uid)
            .OrderBy(ent => TryComp<ResearchServerComponent>(ent, out var comp) ? comp.Id : int.MaxValue)
            .FirstOrDefault();

        if (survivingAuthority != default)
        {
            LogNetworkEvent(
                survivingAuthority,
                "network",
                Loc.GetString("research-netlog-server-left", ("server", component.ServerName)));
        }
        // RW-End

        foreach (var client in new List<EntityUid>(component.Clients))
        {
            UnregisterClient(client, uid, serverComponent: component, dirtyServer: false);
        }
    }

    // RW-Start
    private void AssignServerName(ResearchServerComponent component)
    {
        if (!string.IsNullOrWhiteSpace(component.ServerName) && component.ServerName != "RND-Server")
            return;

        const string charset = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var chars = new char[6];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = charset[_random.Next(charset.Length)];
        }

        component.ServerName = $"RND-Server {new string(chars)}";
    }
    // RW-End

    private void OnServerDatabaseModified(EntityUid uid, ResearchServerComponent component, ref TechnologyDatabaseModifiedEvent args)
    {
        foreach (var client in component.Clients)
        {
            RaiseLocalEvent(client, ref args);
        }
    }

    // RW-Start
    private void OnResearchClientEmagged(EntityUid uid, ResearchClientComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        if (!TryGetClientServer(uid, out var serverUid, out _))
            return;

        var intrusionMessage = Loc.GetString("research-netlog-emag-device-interference", ("device", MetaData(uid).EntityName));
        var message = BuildCorruptedMessage(intrusionMessage);
        LogNetworkEvent(serverUid.Value, "security", message);
        args.Handled = true;
    }
    // RW-End

    private bool CanRun(EntityUid uid)
    {
        return this.IsPowered(uid, EntityManager);
    }

    private void UpdateServer(EntityUid uid, int time, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // RW-Start
        if (GetNetworkAuthority(uid, component) != uid)
            return;
        // RW-End

        if (!CanRun(uid))
            return;

        // RW-Edit-Start
        foreach (var generation in GetPointGenerationPerSecond(uid, component))
        {
            ModifyServerPoints(uid, generation.Type, generation.Amount * time, component);
        }
        // RW-Edit-End
    }

    /// <summary>
    /// Registers a client to the specified server.
    /// </summary>
    /// <param name="client">The client being registered</param>
    /// <param name="server">The server the client is being registered to</param>
    /// <param name="clientComponent"></param>
    /// <param name="serverComponent"></param>
    /// <param name="dirtyServer">Whether to dirty the server component after registration</param>
    private void RegisterClient(EntityUid client, EntityUid server, ResearchClientComponent? clientComponent = null, ResearchServerComponent? serverComponent = null,  bool dirtyServer = true) // RW-Edit: Was public
    {
        if (!Resolve(client, ref clientComponent, false) || !Resolve(server, ref serverComponent, false))
            return;

        // RW-Start
        var authorityServer = GetNetworkAuthority(server, serverComponent);
        if (authorityServer != server)
            serverComponent = null;

        server = authorityServer;
        if (!Resolve(server, ref serverComponent, false))
            return;
        // RW-End

        if (serverComponent.Clients.Contains(client))
            return;

        serverComponent.Clients.Add(client);
        clientComponent.Server = server;
        SyncClientWithServer(client, clientComponent: clientComponent);

        if (dirtyServer && !TerminatingOrDeleted(server))
            Dirty(server, serverComponent);

        var ev = new ResearchRegistrationChangedEvent(server);
        RaiseLocalEvent(client, ref ev);
    }

    /// <summary>
    /// Unregisterse a client from its server
    /// </summary>
    /// <param name="client"></param>
    /// <param name="clientComponent"></param>
    /// <param name="dirtyServer"></param>
    private void UnregisterClient(EntityUid client, ResearchClientComponent? clientComponent = null, bool dirtyServer = true) // RW-Edit: Was public
    {
        if (!Resolve(client, ref clientComponent))
            return;

        if (clientComponent.Server is not { } server)
            return;

        UnregisterClient(client, server, clientComponent, dirtyServer: dirtyServer);
    }

    /// <summary>
    /// Unregisters a client from its server
    /// </summary>
    /// <param name="client"></param>
    /// <param name="server"></param>
    /// <param name="clientComponent"></param>
    /// <param name="serverComponent"></param>
    /// <param name="dirtyServer"></param>
    private void UnregisterClient(EntityUid client, EntityUid server, ResearchClientComponent? clientComponent = null, ResearchServerComponent? serverComponent = null, bool dirtyServer = true) // RW-Edit: Was public
    {
        if (!Resolve(client, ref clientComponent, false) || !Resolve(server, ref serverComponent, false))
            return;

        serverComponent.Clients.Remove(client);
        clientComponent.Server = null;
        SyncClientWithServer(client, clientComponent: clientComponent);

        if (dirtyServer && !TerminatingOrDeleted(server))
        {
            Dirty(server, serverComponent);
        }

        var ev = new ResearchRegistrationChangedEvent(null);
        RaiseLocalEvent(client, ref ev);
    }

/* // RW-Edit
    /// <summary>
    /// Gets the amount of points generated by all the server's sources in a second.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public int GetPointsPerSecond(EntityUid uid, ResearchServerComponent? component = null)
    {
        var points = 0;

        if (!Resolve(uid, ref component))
            return points;

        if (!CanRun(uid))
            return points;

        var ev = new ResearchServerGetPointsPerSecondEvent(uid, points);
        foreach (var client in component.Clients)
        {
            RaiseLocalEvent(client, ref ev);
        }
        RaiseLocalEvent(uid, ref ev); // Goobstation: We raise on the server as well in case its working as a point source.
        return ev.Points;
    }
*/

    // RW-Start
    public List<ResearchPointAmount> GetPointGenerationPerSecond(EntityUid uid, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component) || !CanRun(uid))
            return new List<ResearchPointAmount>();

        var generation = new Dictionary<string, int>();
        foreach (var networkServer in GetNetworkServers(uid, component))
        {
            var ev = new ResearchServerGetPointsPerSecondByTypeEvent(networkServer, new());

            if (TryComp<ResearchServerComponent>(networkServer, out var networkServerComp))
            {
                foreach (var client in networkServerComp.Clients)
                {
                    RaiseLocalEvent(client, ref ev);
                }
            }

            RaiseLocalEvent(networkServer, ref ev);

            foreach (var amount in ev.Points)
            {
                generation.TryAdd(amount.Type, 0);
                generation[amount.Type] += amount.Amount;
            }
        }

        return generation.Select(x => new ResearchPointAmount { Type = x.Key, Amount = x.Value }).ToList();
    }
    // RW-End

    /// <summary>
    /// Adds a specified number of points to a server.
    /// </summary>
    /// <param name="uid">The server</param>
    /// <param name="points">The amount of points being added</param>
    /// <param name="component"></param>
    public void ModifyServerPoints(EntityUid uid, int points, ResearchServerComponent? component = null)
    // RW-Start
    {
        ModifyServerPoints(uid, "General", points, component);
    }
    // RW-End

    public void ModifyServerPoints(EntityUid uid, string type, int points, ResearchServerComponent? component = null) // RW
    {
        if (points == 0)
            return;

        if (!Resolve(uid, ref component))
            return;

        // RW-Start
        var authorityServer = GetNetworkAuthority(uid, component);
        if (authorityServer != uid)
        {
            uid = authorityServer;
            component = null;

            if (!Resolve(uid, ref component, false))
                return;
        }

        EnsurePointBalance(component, type);
        var totalByType = 0;
        for (var i = 0; i < component.PointBalances.Count; i++)
        {
            if (component.PointBalances[i].Type != type)
                continue;

            var balance = component.PointBalances[i];
            balance.Amount += points;
            component.PointBalances[i] = balance;
            totalByType = balance.Amount;
            break;
        }

        component.Points = GetPointBalance(uid, "General", component);
        // RW-End
        var ev = new ResearchServerPointsChangedEvent(uid, component.Points, points);
        var typeEv = new ResearchServerPointTypeChangedEvent(uid, type, totalByType, points); // RW
        foreach (var client in component.Clients)
        {
            RaiseLocalEvent(client, ref ev);
            RaiseLocalEvent(client, ref typeEv); // RW
        }
        Dirty(uid, component);
    }

    // RW-Start
    private int GetPointBalance(EntityUid uid, string type, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return 0;

        foreach (var balance in component.PointBalances)
        {
            if (balance.Type == type)
                return balance.Amount;
        }

        return 0;
    }

    private bool HasSufficientPoints(EntityUid uid, IEnumerable<ResearchPointAmount> costs, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        var aggregatedCosts = costs
            .GroupBy(cost => cost.Type)
            .Select(group => new ResearchPointAmount
            {
                Type = group.Key,
                Amount = group.Sum(cost => cost.Amount),
            })
            .ToList();

        foreach (var cost in aggregatedCosts)
        {
            if (GetPointBalance(uid, cost.Type, component) < cost.Amount)
                return false;
        }

        return true;
    }

    private void TryConsumePoints(EntityUid uid, IEnumerable<ResearchPointAmount> costs, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var costList = costs
            .GroupBy(cost => cost.Type)
            .Select(group => new ResearchPointAmount
            {
                Type = group.Key,
                Amount = group.Sum(cost => cost.Amount),
            })
            .ToList();

        if (!HasSufficientPoints(uid, costList, component))
            return;

        foreach (var cost in costList)
        {
            ModifyServerPoints(uid, cost.Type, -cost.Amount, component);
        }
    }

    private IEnumerable<EntityUid> GetNetworkServers(EntityUid uid, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return [uid];

        var servers = new List<EntityUid>();
        var query = EntityQueryEnumerator<ResearchServerComponent>();
        while (query.MoveNext(out var serverUid, out var serverComp))
        {
            if (serverComp.NetworkId != component.NetworkId)
                continue;

            servers.Add(serverUid);
        }

        return servers;
    }

    private EntityUid GetNetworkAuthority(EntityUid uid, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return uid;

        return GetNetworkServers(uid, component)
            .OrderBy(ent => TryComp<ResearchServerComponent>(ent, out var comp) ? comp.Id : int.MaxValue)
            .FirstOrDefault(uid);
    }

    public string GetResearchLogUserName(EntityUid? user)
    {
        if (user is not { } uid)
            return Loc.GetString("research-netlog-user-system");

        return TryComp(uid, out MetaDataComponent? meta)
            ? meta.EntityName
            : ToPrettyString(uid);
    }

    public void LogNetworkEvent(EntityUid uid, string category, string message, EntityUid? actor = null, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var servers = GetNetworkServers(uid, component).ToList();
        if (servers.Count == 0)
            servers.Add(uid);

        foreach (var serverUid in servers)
        {
            if (!TryComp(serverUid, out ResearchServerComponent? serverComponent))
                continue;

            serverComponent.Logs.Add(new ResearchLogEntry
            {
                Timestamp = _timing.CurTime,
                Category = category,
                Message = message,
                Actor = actor.HasValue ? GetNetEntity(actor.Value) : null,
            });

            if (serverComponent.Logs.Count > 100)
                serverComponent.Logs.RemoveAt(0);

            Dirty(serverUid, serverComponent);
        }
    }

    private static void EnsurePointBalance(ResearchServerComponent component, string type)
    {
        if (component.PointBalances.Any(balance => balance.Type == type))
            return;

        component.PointBalances.Add(new ResearchPointAmount
        {
            Type = type,
            Amount = 0,
        });
    }

    private void OnServerExamined(EntityUid uid, ResearchServerComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var generation = GetPointGenerationPerSecond(uid, component);
        var points = generation.Sum(x => x.Amount);
        var typePoints = string.Join(", ",
            generation
                .OrderBy(x => x.Type)
                .Select(x => Loc.GetString("research-server-examine-type-entry",
                    ("type", LocalizePointType(x.Type)),
                    ("amount", x.Amount))));

        var msg = Loc.GetString("research-server-examine",
            ("name", component.ServerName),
            ("points", points));

        if (!string.IsNullOrEmpty(typePoints))
            msg += "\n" + typePoints;

        args.PushMarkup(msg);
    }

    private string LocalizePointType(string type)
    {
        var key = $"research-point-type-{type.ToLowerInvariant()}";
        return Loc.TryGetString(key, out var localized) ? localized : type;
    }
    // RW-End
}
