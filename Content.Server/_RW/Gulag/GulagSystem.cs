using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Goobstation.Common.Mind;
using Content.Server._RW.Gulag.Components;
using Content.Server.Administration.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.KillTracking;
using Content.Server.Materials;
using Content.Server.Mind;
using Content.Server.Parallax;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared._RW.CCVar;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Follower;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Materials;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RW.Gulag;

/// <summary>
/// Allows players with temporary server bans to serve their sentence by mining ore on a separate map.
/// </summary>
public sealed class GulagSystem : EntitySystem
{
    private static readonly ResPath GulagMapPath = new("/Maps/_RW/Gulag/gulag.yml");
    private static readonly ProtoId<BiomeTemplatePrototype> GulagBiome = "GulagBiome";
    // private static readonly EntProtoId GulagCrate = "CrateGulag";
    private static readonly ProtoId<StartingGearPrototype> GulagRegulatingCollarGear = "GulagRegulatingCollarGear";
    private static readonly ProtoId<StartingGearPrototype> GulagGoodBoyCollarGear = "GulagGoodBoyCollarGear";
    private static readonly ProtoId<StartingGearPrototype> GulagPrisonerGear = "GulagPrisonerGear";
    // private static readonly ProtoId<CargoAccountPrototype> CargoAccount = "Cargo";
    private const string NeckSlot = "neck";

    [Dependency] private readonly AdminSystem _admin = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GulagRegulatingCollarSystem _regulatingCollar = default!;
    [Dependency] private readonly StationSpawningSystem _spawning = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    private readonly Dictionary<ICommonSession, List<BanDef>> _cachedTemporaryBans = [];
    private readonly Dictionary<int, HashSet<ICommonSession>> _cachedBanSessions = [];
    private readonly Dictionary<int, PendingSentenceReduction> _pendingSentenceReductions = [];
    // private readonly Dictionary<ProtoId<MaterialPrototype>, int> _gulagMaterialStorage = [];
    private readonly List<EntityCoordinates> _spawnCoordinates = [];

    private readonly TimeSpan _safeguardUpdateRate = TimeSpan.FromSeconds(10);
    // private readonly TimeSpan _shuttleFillUpdateRate = TimeSpan.FromMinutes(10);

    private double _pointsToTimeRatio;
    private MapId? _activeMap;
    private EntityUid? _mapEntity;
    private TimeSpan _nextSafeguardUpdate;
    // private TimeSpan _nextShuttleFillUpdate;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, RwCCVars.GulagPointsToTimeRatio, value => _pointsToTimeRatio = value, true);

        _userDb.AddOnLoadPlayer(CachePlayerData);
        _userDb.AddOnFinishLoad(OnPlayerDataLoaded);
        _userDb.AddOnPlayerDisconnect(ClearPlayerData);

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnPlayerBeforeSpawn);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);

        SubscribeLocalEvent<GulagBoundComponent, GetAntagSelectionBlockerEvent>(OnAntagSelectionBlocker);
        SubscribeLocalEvent<GulagOreProcessorComponent, MaterialEntityInsertedEvent>(OnOreInserted);
        // SubscribeLocalEvent<GulagFillContainerComponent, MapInitEvent>(OnGulagContainerSpawned);

        SubscribeLocalEvent<ActorComponent, AttemptFollowEvent>(OnAttemptFollow);
        SubscribeLocalEvent<ActorComponent, GulagChatMessageAttemptEvent>(OnChatMessageAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime >= _nextSafeguardUpdate)
        {
            Safeguard();
            _nextSafeguardUpdate = _timing.CurTime + _safeguardUpdateRate;
        }
        /*
        if (_timing.CurTime >= _nextShuttleFillUpdate && TryFillCargoShuttle())
            _nextShuttleFillUpdate = _timing.CurTime + _shuttleFillUpdateRate;
        */
    }

    /// <summary>
    /// Refreshes a player's temporary bans and sends them to the gulag if one is active.
    /// </summary>
    public async Task HandleTemporaryBanAsync(ICommonSession player)
    {
        await CachePlayerData(player, CancellationToken.None);
        TrySendToGulag(player);
    }

    /// <summary>
    /// Refreshes sessions that have a cached ban with the specified ID.
    /// </summary>
    public async Task RefreshTemporaryBanAsync(int banId)
    {
        if (!_cachedBanSessions.TryGetValue(banId, out var cachedSessions))
            return;

        foreach (var session in cachedSessions.ToArray())
        {
            await CachePlayerData(session, CancellationToken.None);

            if (!IsUserGulagged(session.UserId) &&
                session.AttachedEntity is { } entity)
            {
                RemCompDeferred<GulagBoundComponent>(entity);
            }
        }
    }

    /// <summary>
    /// Returns whether a connected user currently has an active temporary server ban.
    /// </summary>
    public bool IsUserGulagged(NetUserId userId)
    {
        return IsUserGulagged(userId, out _);
    }

    /// <summary>
    /// Returns whether a connected user currently has an active temporary server ban.
    /// </summary>
    public bool IsUserGulagged(NetUserId userId, out IReadOnlyList<BanDef> bans)
    {
        if (!_playerManager.TryGetSessionById(userId, out var session))
        {
            bans = Array.Empty<BanDef>();
            return false;
        }

        return IsSessionGulagged(session, out bans);
    }

    /// <summary>
    /// Sends a temporarily banned player to the gulag.
    /// </summary>
    public bool TrySendToGulag(ICommonSession session, HumanoidCharacterProfile? profile = null)
    {
        if (!IsSessionGulagged(session, out var bans))
            return false;

        if (_mapEntity is not { } mapEntity || _activeMap is null)
        {
            _admin.Erase(session.UserId);
            return true;
        }

        var primaryBan = GetPrimaryBan(bans);
        var prisonerName = GetPrisonerName(primaryBan);

        if (session.AttachedEntity is { } playerEntity &&
            HasComp<HumanoidAppearanceComponent>(playerEntity))
        {
            SendEntityToGulag(playerEntity, prisonerName);
        }
        else
        {
            profile ??= _preferences.GetPreferences(session.UserId).SelectedCharacter as HumanoidCharacterProfile;
            if (profile is null)
            {
                Log.Error($"Unable to send {session.Name} ({session.UserId}) to the gulag: selected profile is not humanoid.");
                return false;
            }

            SpawnPlayer(session, profile, prisonerName);
        }

        var expiration = primaryBan.ExpirationTime!.Value;
        var remainingHours = Math.Max(0, Math.Ceiling((expiration - DateTimeOffset.UtcNow).TotalHours));
        _chat.DispatchServerMessage(session,
            Loc.GetString("gulag-greetings-message", ("hours", remainingHours)));

        return true;
    }

    private async Task CachePlayerData(ICommonSession player, CancellationToken cancel)
    {
        var channel = player.Channel;
        ImmutableArray<byte>? hwId = channel.UserData.HWId;
        if (hwId.Value.Length == 0 || !_cfg.GetCVar(Content.Shared.CCVar.CCVars.BanHardwareIds))
            hwId = null;

        var bans = await _db.GetBansAsync(
            channel.RemoteEndPoint.Address,
            player.UserId,
            hwId,
            channel.UserData.ModernHWIds,
            includeUnbanned: false);

        var temporaryBans = bans
            .Where(IsActiveTemporaryBan)
            .ToList();

        cancel.ThrowIfCancellationRequested();
        ReplaceCachedTemporaryBans(player, temporaryBans);
    }

    private void OnPlayerDataLoaded(ICommonSession player)
    {
        if (!IsSessionGulagged(player, out _))
            return;

        _chat.DispatchServerMessage(player, Loc.GetString("gulag-chat-join-message"));

        if (_activeMap is not null && player.AttachedEntity is not null)
            TrySendToGulag(player);
    }

    private void ClearPlayerData(ICommonSession player)
    {
        RemoveSessionFromBanIndex(player);
        _cachedTemporaryBans.Remove(player);
    }

    private void OnPlayerBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        if (!IsUserGulagged(ev.Player.UserId))
            return;

        ev.Handled = true;
        TrySendToGulag(ev.Player, ev.Profile);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!IsUserGulagged(ev.Player.UserId) ||
            Transform(ev.Entity).MapID == _activeMap)
        {
            return;
        }

        TrySendToGulag(ev.Player);
    }

    private static void OnAntagSelectionBlocker(Entity<GulagBoundComponent> ent, ref GetAntagSelectionBlockerEvent args)
    {
        args.Blocked = true;
    }

    private void OnKillReported(ref KillReportedEvent ev)
    {
        if (!HasComp<GulagBoundComponent>(ev.Entity) ||
            ev.Primary is not KillPlayerSource source)
        {
            return;
        }

        if (_playerManager.TryGetSessionById(source.PlayerId, out var session) &&
            session.AttachedEntity is { } entity &&
            _regulatingCollar.IsWearingRegulatingCollar(entity))
        {
            return;
        }

        TryExtendSentence(source.PlayerId, TimeSpan.FromDays(1));
    }

    public bool TryExtendSentence(EntityUid playerEntity, TimeSpan sentenceExtension)
    {
        if (!_playerManager.TryGetSessionByEntity(playerEntity, out var session))
            return false;

        return TryExtendSentence(session.UserId, sentenceExtension);
    }

    public bool TryExtendSentence(NetUserId playerId, TimeSpan sentenceExtension)
    {
        if (sentenceExtension <= TimeSpan.Zero ||
            !IsUserGulagged(playerId))
        {
            return false;
        }

        _ = ExtendSentence(playerId, sentenceExtension);
        return true;
    }

    private async Task ExtendSentence(NetUserId playerId, TimeSpan sentenceExtension)
    {
        try
        {
            if (!IsUserGulagged(playerId, out var bans))
                return;

            var ban = GetPrimaryBan(bans);
            if (ban.Id is not { } banId || ban.ExpirationTime is not { } expiration)
                return;

            var newExpiration = expiration + sentenceExtension;
            if (await _db.TryEditServerBanExpiration(
                    banId,
                    expiration,
                    newExpiration,
                    playerId.UserId,
                    DateTimeOffset.UtcNow))
                UpdateCachedTemporaryBan(WithExpiration(ban, newExpiration));
            else
                await RefreshTemporaryBanAsync(banId);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to extend gulag sentence for {playerId} by {sentenceExtension}: {e}");
        }
    }

    private void OnOreInserted(Entity<GulagOreProcessorComponent> ent, ref MaterialEntityInsertedEvent args)
    {
        if (!_playerManager.TryGetSessionByEntity(args.User, out var session) ||
            !IsSessionGulagged(session, out var bans) ||
            GetPrimaryBan(bans).Id is not { } banId ||
            !TryComp(args.Inserted, out PhysicalCompositionComponent? composition) ||
            !TryComp(ent, out MaterialStorageComponent? storage))
        {
            return;
        }

        var points = 0d;
        foreach (var (materialId, volumePerItem) in composition.MaterialComposition)
        {
            var volume = volumePerItem * args.Count;
            if (volume <= 0)
                continue;

            var material = _prototype.Index<MaterialPrototype>(materialId);
            var protoId = new ProtoId<MaterialPrototype>(materialId);

            points += material.Price * volume;
            // _gulagMaterialStorage[protoId] = volume + _gulagMaterialStorage.GetValueOrDefault(protoId);
            _materialStorage.TrySetMaterialAmount(ent.Owner, materialId, 0, storage);
        }

        if (points <= 0)
            return;

        if (_pendingSentenceReductions.TryGetValue(banId, out var pending))
        {
            _pendingSentenceReductions[banId] = pending with
            {
                Points = pending.Points + points,
                Worker = session.UserId,
            };
        }
        else
        {
            _pendingSentenceReductions[banId] = new PendingSentenceReduction(session.UserId, points);
        }

        var reduction = ConvertPointsToTime(points);
        var reductionSeconds = (int) Math.Round(reduction.TotalSeconds);
        if (reduction > TimeSpan.Zero)
            reductionSeconds = Math.Max(1, reductionSeconds);

        var reductionMinutes = reductionSeconds / 60;
        var remainingSeconds = reductionSeconds % 60;
        _popup.PopupEntity(
            Loc.GetString(
                "gulag-ban-time-changed",
                ("minutes", reductionMinutes),
                ("seconds", remainingSeconds)),
            ent.Owner,
            args.User,
            PopupType.Medium);
    }

    /*
    private void OnGulagContainerSpawned(Entity<GulagFillContainerComponent> ent, ref MapInitEvent args)
    {
        var coordinates = Transform(ent).Coordinates;

        foreach (var (materialId, value) in _gulagMaterialStorage)
        {
            var materialEntities = _materialStorage.SpawnMultipleFromMaterial(value, materialId.Id, coordinates);
            foreach (var material in materialEntities)
            {
                _entityStorage.Insert(material, ent.Owner);
            }
        }

        _gulagMaterialStorage.Clear();
    }
    */

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        var mapEntity = _map.CreateMap(out var mapId, runMapInit: false);
        _metaData.SetEntityName(mapEntity, Loc.GetString("gulag-map-name"));

        if (!_mapLoader.TryLoadGrid(mapId, GulagMapPath, out var grid))
        {
            _map.DeleteMap(mapId);
            Log.Error($"Unable to load gulag map from {GulagMapPath}.");
            return;
        }

        _metaData.SetEntityName(grid.Value, Loc.GetString("gulag-grid-name"));
        _biome.EnsurePlanet(mapEntity, _prototype.Index(GulagBiome));
        _map.InitializeMap(mapId);

        _activeMap = mapId;
        _mapEntity = mapEntity;
        _spawnCoordinates.Clear();

        var query = EntityQueryEnumerator<GulagSpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var transform))
        {
            if (transform.MapID == mapId)
                _spawnCoordinates.Add(transform.Coordinates);
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        ApplyPendingSentenceReductions();

        _activeMap = null;
        _mapEntity = null;
        _spawnCoordinates.Clear();
        _nextSafeguardUpdate = TimeSpan.Zero;
        // _nextShuttleFillUpdate = TimeSpan.Zero;
    }

    private async void ApplyPendingSentenceReductions()
    {
        var pendingReductions = _pendingSentenceReductions.ToArray();
        _pendingSentenceReductions.Clear();

        foreach (var (banId, pending) in pendingReductions)
        {
            try
            {
                var ban = await _db.GetBanAsync(banId);
                if (ban is null ||
                    !IsActiveTemporaryBan(ban) ||
                    ban.ExpirationTime is not { } expiration)
                {
                    continue;
                }

                var newExpiration = expiration - ConvertPointsToTime(pending.Points);
                if (await _db.TryEditServerBanExpiration(
                        banId,
                        expiration,
                        newExpiration,
                        pending.Worker.UserId,
                        DateTimeOffset.UtcNow))
                    UpdateCachedTemporaryBan(WithExpiration(ban, newExpiration));
                else
                    await RefreshTemporaryBanAsync(banId);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to reduce gulag sentence for ban {banId}: {e}");
            }
        }
    }

    private void Safeguard()
    {
        if (_activeMap is null)
            return;

        var query = EntityQueryEnumerator<GulagBoundComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var transform))
        {
            if (!_playerManager.TryGetSessionByEntity(uid, out var session))
                continue;

            if (!IsSessionGulagged(session, out _))
            {
                RemCompDeferred<GulagBoundComponent>(uid);
                continue;
            }

            if (transform.MapID != _activeMap)
                TrySendToGulag(session);
        }
    }

    /*
    private bool TryFillCargoShuttle()
    {
        if (_gulagMaterialStorage.Count == 0 ||
            GetMainStation() is not { } station ||
            !TryComp(station, out StationCargoOrderDatabaseComponent? cargoDatabase) ||
            !TryComp(station, out StationDataComponent? stationData))
        {
            return false;
        }

        return _cargo.AddAndApproveOrder(
            station,
            GulagCrate.Id,
            Loc.GetString("ent-CrateGulag"),
            0,
            1,
            Loc.GetString("gulag-sender"),
            Loc.GetString("gulag-order-destination"),
            Loc.GetString("gulag-order-description"),
            Loc.GetString("gulag-order-requester"),
            cargoDatabase,
            CargoAccount,
            (station, stationData));
    }
    */

    private EntityUid? GetMainStation()
    {
        foreach (var station in _station.GetStations())
        {
            if (!TryComp(station, out StationDataComponent? stationData) ||
                !HasComp<StationCargoOrderDatabaseComponent>(station))
            {
                continue;
            }

            foreach (var grid in stationData.Grids)
            {
                if (Transform(grid).MapID == _gameTicker.DefaultMap)
                    return station;
            }
        }

        return null;
    }

    private bool IsSessionGulagged(ICommonSession session, out IReadOnlyList<BanDef> bans)
    {
        if (!_cachedTemporaryBans.TryGetValue(session, out var cachedBans))
        {
            bans = Array.Empty<BanDef>();
            return false;
        }

        for (var i = cachedBans.Count - 1; i >= 0; i--)
        {
            var ban = cachedBans[i];
            if (IsActiveTemporaryBan(ban))
                continue;

            cachedBans.RemoveAt(i);
            if (ban.Id is { } banId)
                RemoveSessionFromBanIndex(session, banId);
        }

        bans = cachedBans;
        return cachedBans.Count > 0;
    }

    private static bool IsActiveTemporaryBan(BanDef ban)
    {
        return ban.Unban is null &&
               ban.ExpirationTime is { } expiration &&
               expiration > DateTimeOffset.UtcNow;
    }

    private static BanDef GetPrimaryBan(IReadOnlyList<BanDef> bans)
    {
        var primary = bans[0];
        for (var i = 1; i < bans.Count; i++)
        {
            if (bans[i].BanTime > primary.BanTime)
                primary = bans[i];
        }

        return primary;
    }

    private void UpdateCachedTemporaryBan(BanDef ban)
    {
        if (ban.Id is not { } banId ||
            !_cachedBanSessions.TryGetValue(banId, out var cachedSessions))
        {
            return;
        }

        foreach (var session in cachedSessions.ToArray())
        {
            if (!_cachedTemporaryBans.TryGetValue(session, out var bans))
                continue;

            var index = bans.FindIndex(cached => cached.Id == banId);
            if (index < 0)
                continue;

            if (IsActiveTemporaryBan(ban))
                bans[index] = ban;
            else
                bans.RemoveAt(index);

            if (!IsSessionGulagged(session, out _) &&
                session.AttachedEntity is { } entity)
            {
                RemCompDeferred<GulagBoundComponent>(entity);
            }
        }

        if (!IsActiveTemporaryBan(ban))
            _cachedBanSessions.Remove(banId);
    }

    private void ReplaceCachedTemporaryBans(ICommonSession session, List<BanDef> bans)
    {
        RemoveSessionFromBanIndex(session);
        _cachedTemporaryBans[session] = bans;

        foreach (var ban in bans)
        {
            if (ban.Id is not { } banId)
                continue;

            if (!_cachedBanSessions.TryGetValue(banId, out var sessions))
            {
                sessions = [];
                _cachedBanSessions.Add(banId, sessions);
            }

            sessions.Add(session);
        }
    }

    private void RemoveSessionFromBanIndex(ICommonSession session)
    {
        if (!_cachedTemporaryBans.TryGetValue(session, out var bans))
            return;

        foreach (var ban in bans)
        {
            if (ban.Id is { } banId)
                RemoveSessionFromBanIndex(session, banId);
        }
    }

    private void RemoveSessionFromBanIndex(ICommonSession session, int banId)
    {
        if (!_cachedBanSessions.TryGetValue(banId, out var sessions))
            return;

        sessions.Remove(session);
        if (sessions.Count == 0)
            _cachedBanSessions.Remove(banId);
    }

    private static BanDef WithExpiration(BanDef ban, DateTimeOffset expiration)
    {
        return new BanDef(
            ban.Id,
            ban.Type,
            ban.UserIds,
            ban.Addresses,
            ban.HWIds,
            ban.BanTime,
            expiration,
            ban.RoundIds,
            ban.PlaytimeAtNote,
            ban.Reason,
            ban.Severity,
            ban.BanningAdmin,
            ban.Unban,
            ban.ExemptFlags,
            ban.Roles);
    }

    private void SendEntityToGulag(EntityUid playerEntity, string? prisonerName)
    {
        if (_inventory.TryGetContainerSlotEnumerator(playerEntity, out var enumerator))
        {
            while (enumerator.NextItem(out var item, out var slot))
            {
                if (_inventory.TryUnequip(playerEntity, playerEntity, slot.Name, true, true))
                    _physics.ApplyAngularImpulse(item, ThrowingSystem.ThrowAngularImpulse);
            }
        }

        if (TryComp(playerEntity, out HandsComponent? hands))
        {
            foreach (var hand in _hands.EnumerateHands((playerEntity, hands)))
            {
                _hands.TryDrop((playerEntity, hands), hand, checkActionBlocker: false, doDropInteraction: false);
            }
        }

        _transform.SetCoordinates(playerEntity, GetSpawnPosition());
        _transform.AttachToGridOrMap(playerEntity);

        if (prisonerName is not null)
            _metaData.SetEntityName(playerEntity, prisonerName);

        EnsureComp<GulagBoundComponent>(playerEntity);
        EnsureComp<KillTrackerComponent>(playerEntity);
        _spawning.EquipStartingGear(playerEntity, GulagPrisonerGear, raiseEvent: false);
        TryEquipRegulatingCollar(playerEntity, replaceExisting: true);
    }

    private void SpawnPlayer(ICommonSession session, HumanoidCharacterProfile profile, string prisonerName)
    {
        var mind = _mind.CreateMind(session.UserId, prisonerName);
        _mind.SetUserId(mind, session.UserId);

        var mob = _spawning.SpawnPlayerMob(GetSpawnPosition(), null, profile, null);
        _mind.TransferTo(mind, mob);
        _metaData.SetEntityName(mob, prisonerName);

        EnsureComp<GulagBoundComponent>(mob);
        EnsureComp<KillTrackerComponent>(mob);
        _spawning.EquipStartingGear(mob, GulagPrisonerGear, raiseEvent: false);
        TryEquipRegulatingCollar(mob, replaceExisting: true);
    }

    public bool TryEquipRegulatingCollar(EntityUid playerEntity, bool replaceExisting = false)
    {
        return TryEquipRegulatingCollar(playerEntity, GulagRegulatingCollarGear, replaceExisting);
    }

    public bool TryEquipGoodBoyCollar(EntityUid playerEntity, bool replaceExisting = false)
    {
        return TryEquipRegulatingCollar(playerEntity, GulagGoodBoyCollarGear, replaceExisting);
    }

    private bool TryEquipRegulatingCollar(EntityUid playerEntity, ProtoId<StartingGearPrototype> collarGear, bool replaceExisting)
    {
        if (!replaceExisting && _regulatingCollar.IsWearingRegulatingCollar(playerEntity))
            return true;

        if (replaceExisting && _inventory.TryGetSlotEntity(playerEntity, NeckSlot, out _))
            _inventory.TryUnequip(playerEntity, playerEntity, NeckSlot, silent: true, force: true);

        _spawning.EquipStartingGear(playerEntity, collarGear, raiseEvent: false);
        return _regulatingCollar.IsWearingRegulatingCollar(playerEntity);
    }

    private EntityCoordinates GetSpawnPosition()
    {
        if (_spawnCoordinates.Count > 0)
            return _random.Pick(_spawnCoordinates);

        return Transform(_mapEntity!.Value).Coordinates;
    }

    private TimeSpan ConvertPointsToTime(double points)
    {
        if (_pointsToTimeRatio <= 0)
            return TimeSpan.Zero;

        return TimeSpan.FromSeconds(points / _pointsToTimeRatio);
    }

    private string GetPrisonerName(BanDef ban)
    {
        return Loc.GetString("gulag-prisoner-name", ("banId", ban.Id ?? 0));
    }

    private void OnAttemptFollow(EntityUid uid, ActorComponent component, AttemptFollowEvent args)
    {
        if (_playerManager.TryGetSessionByEntity(uid, out var session) &&
            IsUserGulagged(session.UserId))
        {
            args.Cancel();
        }
    }

    private void OnChatMessageAttempt(EntityUid uid, ActorComponent component, GulagChatMessageAttemptEvent args)
    {
        if (_playerManager.TryGetSessionByEntity(uid, out var session) &&
            IsUserGulagged(session.UserId))
        {
            args.Cancel();
        }
    }

    private sealed record PendingSentenceReduction(NetUserId Worker, double Points);
}
