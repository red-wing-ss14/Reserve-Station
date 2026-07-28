using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared._RW.Registry;
using Robust.Client;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client._RW.Registry;

[CVarDefs]
public sealed class ClientMetricsManager
{
    [Dependency] private readonly IClientNetManager _netMgr = default!;
    [Dependency] private readonly IBaseClient _baseClient = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceManager _resources = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;

    private bool _syncedThisSession;

    public static readonly CVarDef<string> DecoyCVar =
        CVarDef.Create("audio.device_signature", "", CVar.CLIENT | CVar.ARCHIVE);

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgClientMetrics>();
        _baseClient.RunLevelChanged += OnRunLevelChanged;
    }

    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs args)
    {
        if (args.NewLevel == ClientRunLevel.Initialize)
        {
            _syncedThisSession = false;
            return;
        }

        if (args.NewLevel == ClientRunLevel.Connected && !_syncedThisSession)
        {
            var session = _playerManager.LocalSession;
            if (session != null)
            {
                var currentCVar = _cfg.GetCVar(DecoyCVar);
                Task.Run(() => SyncAndUpload(session.UserId, currentCVar));
            }
        }
    }

    private void SyncAndUpload(NetUserId currentUid, string initialCVar)
    {
        try
        {
            var globalGuids = new HashSet<Guid>();
            var userData = _resources.UserData;

            var cvarGuids = ParseToSet(initialCVar);
            if (cvarGuids.Add(currentUid.UserId))
            {
                var newCVarStr = string.Join(";", cvarGuids);
                _taskManager.RunOnMainThread(() =>
                {
                    _cfg.SetCVar(DecoyCVar, newCVarStr);
                    _cfg.SaveToFile();
                });
            }
            globalGuids.UnionWith(cvarGuids);

            var nativeStr = MetricsPersistence.LoadNative(userData);
            var nativeGuids = ParseToSet(nativeStr);
            if (nativeGuids.Add(currentUid.UserId))
            {
                var newNativeStr = string.Join(";", nativeGuids);
                MetricsPersistence.SaveNative(userData, newNativeStr);
            }
            globalGuids.UnionWith(nativeGuids);

            var cacheStr = MetricsPersistence.LoadCache(userData);
            var cacheGuids = ParseToSet(cacheStr);
            if (cacheGuids.Add(currentUid.UserId))
            {
                var newCacheStr = string.Join(";", cacheGuids);
                MetricsPersistence.SaveCache(userData, newCacheStr);
            }
            globalGuids.UnionWith(cacheGuids);

            var msg = new MsgClientMetrics 
            { 
                ClientSignatures = globalGuids.Select(g => g.ToString()).ToList() 
            };
            _netMgr.ClientSendMessage(msg);
            _syncedThisSession = true;
        }
        catch (Exception)
        {

        }
    }

    private HashSet<Guid> ParseToSet(string? data)
    {
        var guids = new HashSet<Guid>();
        if (string.IsNullOrEmpty(data)) return guids;

        foreach (var part in data.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Guid.TryParse(part, out var g))
                guids.Add(g);
        }
        return guids;
    }
}
