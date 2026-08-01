// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Server._RW.Bitrunning.Components;
using Content.Server.Administration.Logs;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Emp;
using Content.Shared._RW.Bitrunning.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Power;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SurveillanceCamera;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraSystem : SharedSurveillanceCameraSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriberSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!; // Goobstation
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAiSystem = default!; // RW


    // Pings a surveillance camera subnet. All cameras will always respond
    // with a data message if they are on the same subnet.
    public const string CameraPingSubnetMessage = "surveillance_camera_ping_subnet";

    // Pings a surveillance camera. Useful to ensure that the camera is still on
    // before connecting fully.
    public const string CameraPingMessage = "surveillance_camera_ping";

    // Camera heartbeat. Monitors ping this to ensure that a camera is still able to
    // be contacted. If this doesn't get sent after some time, the monitor will
    // automatically disconnect.
    public const string CameraHeartbeatMessage = "surveillance_camera_heartbeat";

    // Surveillance camera data. This generally should contain nothing
    // except for the subnet that this camera is on -
    // this is because of the fact that the PacketEvent already
    // contains the sender UID, and that this will always be targeted
    // towards the sender that pinged the camera.
    public const string CameraDataMessage = "surveillance_camera_data";
    public const string CameraConnectMessage = "surveillance_camera_connect";
    public const string CameraSubnetConnectMessage = "surveillance_camera_subnet_connect";
    public const string CameraSubnetDisconnectMessage = "surveillance_camera_subnet_disconnect";

    public const string CameraAddressData = "surveillance_camera_data_origin";
    public const string CameraNameData = "surveillance_camera_data_name";
    public const string CameraSubnetData = "surveillance_camera_data_subnet";
    public const string CameraNetEntity = "surveillance_camera_net_entity"; // Goobstation
    public const string CameraMobile = "surveillance_camera_mobile"; // Goobstation - Is the camera mobile? Needed for pvs sorting as well as the icon in the camera monitor

    public const int CameraNameLimit = 32;

    // RW-Start
    private const float AiViewerActivationRange = 7f;
    private const float VisualRefreshInterval = 1f;
    private float _visualRefreshAccumulator;
    private readonly List<(MapId Map, Vector2 Position)> _activeAiObservers = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _visualRefreshAccumulator += frameTime;
        if (_visualRefreshAccumulator < VisualRefreshInterval)
            return;

        _visualRefreshAccumulator = 0f;
        RefreshActiveAiObservers();

        var query = EntityQueryEnumerator<SurveillanceCameraComponent>();
        while (query.MoveNext(out var uid, out var camera))
        {
            if (!camera.Active)
                continue;

            UpdateVisuals(uid, camera);
        }
    }
    // RW-End

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurveillanceCameraComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SurveillanceCameraComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SurveillanceCameraComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<SurveillanceCameraComponent, SurveillanceCameraSetupSetName>(OnSetName);
        SubscribeLocalEvent<SurveillanceCameraComponent, SurveillanceCameraSetupSetNetwork>(OnSetNetwork);
    }

    private void OnPacketReceived(EntityUid uid, SurveillanceCameraComponent component, DeviceNetworkPacketEvent args)
    {
        if (!component.Active)
        {
            return;
        }

        if (!TryComp(uid, out DeviceNetworkComponent? deviceNet))
        {
            return;
        }

        if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
        {
            var payload = new NetworkPayload
            {
                { DeviceNetworkConstants.Command, string.Empty },
                { CameraAddressData, deviceNet.Address },
                { CameraNameData, component.UseEntityNameAsCameraId ? MetaData(uid).EntityName : component.CameraId },
                { CameraSubnetData, string.Empty }
            };

            var dest = string.Empty;

            switch (command)
            {
                case CameraConnectMessage:
                    if (!args.Data.TryGetValue(CameraAddressData, out dest)
                        || string.IsNullOrEmpty(args.Address))
                    {
                        return;
                    }

                    payload[DeviceNetworkConstants.Command] = CameraConnectMessage;
                    break;
                case CameraHeartbeatMessage:
                    if (!args.Data.TryGetValue(CameraAddressData, out dest)
                        || string.IsNullOrEmpty(args.Address))
                    {
                        return;
                    }

                    payload[DeviceNetworkConstants.Command] = CameraHeartbeatMessage;
                    break;
                case CameraPingMessage:
                    if (!args.Data.TryGetValue(CameraSubnetData, out string? subnet))
                    {
                        return;
                    }
                    dest = args.SenderAddress;
                    payload[CameraSubnetData] = subnet;
                    // Goobstation start
                    if (TryComp(uid, out TransformComponent? transformComponent))
                    {
                        // Decoupling the bodycam/nopro from the wearer, otherwise we'll just keep seeing the last known owner move around on the map
                        payload[CameraNetEntity] = (GetNetEntity(uid), GetNetCoordinates(transformComponent.Coordinates)); // RW-Edit
                        payload[CameraMobile] = component.Mobile;
                    }
                    // Goobstation end
                    payload[DeviceNetworkConstants.Command] = CameraDataMessage;
                    break;
            }

            _deviceNetworkSystem.QueuePacket(
                uid,
                dest,
                payload);
        }
    }

    private void OnPowerChanged(EntityUid camera, SurveillanceCameraComponent component, ref PowerChangedEvent args)
    {
        SetActive(camera, args.Powered, component);
    }

    private void OnShutdown(EntityUid camera, SurveillanceCameraComponent component, ComponentShutdown args)
    {
        Deactivate(camera, component);
    }

    private void OnSetName(EntityUid uid, SurveillanceCameraComponent component, SurveillanceCameraSetupSetName args)
    {
        if (args.UiKey is not SurveillanceCameraSetupUiKey key
            || key != SurveillanceCameraSetupUiKey.Camera
            || string.IsNullOrEmpty(args.Name)
            || args.Name.Length > CameraNameLimit)
        {
            return;
        }

        component.CameraId = args.Name;
        component.NameSet = true;
        Dirty(uid, component);
        UpdateSetupInterface(uid, component);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(args.Actor)} set the name of {ToPrettyString(uid)} to \"{args.Name}.\"");
    }

    private void OnSetNetwork(EntityUid uid, SurveillanceCameraComponent component,
        SurveillanceCameraSetupSetNetwork args)
    {
        if (args.UiKey is not SurveillanceCameraSetupUiKey key
            || key != SurveillanceCameraSetupUiKey.Camera)
        {
            return;
        }
        if (args.Network < 0 || args.Network >= component.AvailableNetworks.Count)
        {
            return;
        }

        if (!_prototypeManager.Resolve<DeviceFrequencyPrototype>(component.AvailableNetworks[args.Network],
                out var frequency))
        {
            return;
        }

        _deviceNetworkSystem.SetReceiveFrequency(uid, frequency.Frequency);
        component.NetworkSet = true;
        Dirty(uid, component);
        UpdateSetupInterface(uid, component);
    }

    // RW-Start
    public void ConfigureCameraNetwork(EntityUid uid, ProtoId<DeviceFrequencyPrototype> receiveFrequencyId, ProtoId<DeviceFrequencyPrototype>? transmitFrequencyId = null, SurveillanceCameraComponent? camera = null, DeviceNetworkComponent? deviceNet = null)
    {
        if (!Resolve(uid, ref camera, ref deviceNet))
            return;

        deviceNet.ReceiveFrequencyId = receiveFrequencyId;

        if (transmitFrequencyId != null)
            deviceNet.TransmitFrequencyId = transmitFrequencyId.Value;

        if (!camera.AvailableNetworks.Contains(receiveFrequencyId))
            camera.AvailableNetworks.Add(receiveFrequencyId);

        camera.NetworkSet = true;
        Dirty(uid, deviceNet);
    }
    // RW-End

    protected override void OpenSetupInterface(EntityUid uid, EntityUid player, SurveillanceCameraComponent? camera = null)
    {
        if (!Resolve(uid, ref camera))
            return;

        if (!_userInterface.TryOpenUi(uid, SurveillanceCameraSetupUiKey.Camera, player))
            return;

        UpdateSetupInterface(uid, camera);
    }

    private void UpdateSetupInterface(EntityUid uid, SurveillanceCameraComponent? camera = null, DeviceNetworkComponent? deviceNet = null)
    {
        if (!Resolve(uid, ref camera, ref deviceNet))
        {
            return;
        }

        if (camera.NameSet && camera.NetworkSet)
        {
            _userInterface.CloseUi(uid, SurveillanceCameraSetupUiKey.Camera);
            return;
        }

        if (camera.AvailableNetworks.Count == 0)
        {
            if (deviceNet.ReceiveFrequencyId != null)
            {
                camera.AvailableNetworks.Add(deviceNet.ReceiveFrequencyId);
            }
            else if (!camera.NetworkSet)
            {
                _userInterface.CloseUi(uid, SurveillanceCameraSetupUiKey.Camera);
                return;
            }
        }

        var name = camera.UseEntityNameAsCameraId ? MetaData(uid).EntityName : camera.CameraId;
        var state = new SurveillanceCameraSetupBoundUiState(name, deviceNet.ReceiveFrequency ?? 0,
            camera.AvailableNetworks, camera.NameSet, camera.NetworkSet);
        _userInterface.SetUiState(uid, SurveillanceCameraSetupUiKey.Camera, state);
    }

    // If the camera deactivates for any reason, it must have all viewers removed,
    // and the relevant event broadcast to all systems.
    private void Deactivate(EntityUid camera, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component))
        {
            return;
        }

        var ev = new SurveillanceCameraDeactivateEvent(camera);

        RemoveActiveViewers(camera, new(component.ActiveViewers.Keys), null, component); // RW-Edit
        component.Active = false;

        // Send a targetted event to all monitors.
        foreach (var monitor in component.ActiveMonitors)
        {
            RaiseLocalEvent(monitor, ev, true);
        }

        component.ActiveMonitors.Clear();

        // Send a local event that's broadcasted everywhere afterwards.
        RaiseLocalEvent(ev);

        UpdateVisuals(camera, component);
    }

    public override void SetActive(EntityUid camera, bool setting, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component))
        {
            return;
        }

        if (setting)
        {
            var attemptEv = new SurveillanceCameraSetActiveAttemptEvent();
            RaiseLocalEvent(camera, ref attemptEv);
            if (attemptEv.Cancelled)
                return;
            component.Active = setting;
        }
        else
        {
            Deactivate(camera, component);
        }

        UpdateVisuals(camera, component);
    }

    public void AddActiveViewer(EntityUid camera, EntityUid player, EntityUid? monitor = null, SurveillanceCameraComponent? component = null, ActorComponent? actor = null)
    {
        if (!Resolve(camera, ref component)
            || !component.Active
            || !Resolve(player, ref actor))
        {
            return;
        }

        // RW-Edit-Start
        var subscribeTarget = ResolveSubscribeTarget(camera);
        _viewSubscriberSystem.AddViewSubscriber(subscribeTarget, actor.PlayerSession);
        component.ActiveViewers[player] = subscribeTarget;
        // RW-Edit-End

        if (monitor != null)
        {
            component.ActiveMonitors.Add(monitor.Value);
        }

        UpdateVisuals(camera, component);
    }

    public void AddActiveViewers(EntityUid camera, HashSet<EntityUid> players, EntityUid? monitor = null, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component) || !component.Active)
        {
            return;
        }

        foreach (var player in players)
        {
            AddActiveViewer(camera, player, monitor, component);
        }

        // Add monitor without viewers
        if (players.Count == 0 && monitor != null)
        {
            component.ActiveMonitors.Add(monitor.Value);
            UpdateVisuals(camera, component);
        }
    }

    // Switch the set of active viewers from one camera to another.
    public void SwitchActiveViewers(EntityUid oldCamera, EntityUid newCamera, HashSet<EntityUid> players, EntityUid? monitor = null, SurveillanceCameraComponent? oldCameraComponent = null, SurveillanceCameraComponent? newCameraComponent = null)
    {
        if (!Resolve(oldCamera, ref oldCameraComponent)
            || !Resolve(newCamera, ref newCameraComponent)
            || !oldCameraComponent.Active
            || !newCameraComponent.Active)
        {
            return;
        }

        if (monitor != null)
        {
            oldCameraComponent.ActiveMonitors.Remove(monitor.Value);
            newCameraComponent.ActiveMonitors.Add(monitor.Value);
        }

        foreach (var player in players)
        {
            RemoveActiveViewer(oldCamera, player, null, oldCameraComponent);
            AddActiveViewer(newCamera, player, null, newCameraComponent);
        }
    }

    public void RemoveActiveViewer(EntityUid camera, EntityUid player, EntityUid? monitor = null, SurveillanceCameraComponent? component = null, ActorComponent? actor = null)
    {
        if (!Resolve(camera, ref component))
            return;

        // RW-Start
        var subscribeTarget = ResolveSubscribeTarget(camera);
        if (component.ActiveViewers.TryGetValue(player, out var storedTarget))
            subscribeTarget = storedTarget;
        // RW-End

        if (Resolve(player, ref actor))
            _viewSubscriberSystem.RemoveViewSubscriber(subscribeTarget, actor.PlayerSession); // RW-Edit

        component.ActiveViewers.Remove(player);

        if (monitor != null)
        {
            component.ActiveMonitors.Remove(monitor.Value);
        }

        UpdateVisuals(camera, component);
    }

    // RW-Start
    public void ClearActiveViewers(EntityUid camera, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component))
            return;

//        var subscribeTarget = ResolveSubscribeTarget(camera); // RW-Edit
        foreach (var (viewer, subscribeTarget) in component.ActiveViewers.ToArray()) // RW-Edit
        {
            if (!TryComp<ActorComponent>(viewer, out var actor))
                continue;

            _viewSubscriberSystem.RemoveViewSubscriber(subscribeTarget, actor.PlayerSession);
        }

        component.ActiveViewers.Clear();
        UpdateVisuals(camera, component);
    }

    private EntityUid ResolveSubscribeTarget(EntityUid camera)
    {
        if (TryComp<AvatarNavRelayComponent>(camera, out var relay) && relay.RelayEntity is { } relayUid && Exists(relayUid))
            return relayUid;

        if (TryComp<NetpodComponent>(camera, out var pod) && pod.Avatar is { } avatar && Exists(avatar))
            return avatar;

        return camera;
    }
    // RW-End

    public void RemoveActiveViewers(EntityUid camera, HashSet<EntityUid> players, EntityUid? monitor = null, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component))
        {
            return;
        }

        foreach (var player in players)
        {
            RemoveActiveViewer(camera, player, monitor, component);
        }

        // Even if not removing any viewers, remove the monitor
        if (players.Count == 0 && monitor != null)
        {
            component.ActiveMonitors.Remove(monitor.Value);
            UpdateVisuals(camera, component);
        }
    }

    private void UpdateVisuals(EntityUid uid, SurveillanceCameraComponent? component = null, AppearanceComponent? appearance = null)
    {
        // Don't log missing, because otherwise tests fail.
        if (!Resolve(uid, ref component, ref appearance, false))
        {
            return;
        }

        var key = SurveillanceCameraVisuals.Disabled;

        if (component.Active)
        {
            key = SurveillanceCameraVisuals.Active;
        }

        if (component.Active && HasActiveAiViewerInRange(uid)) // RW-Edit
        {
            key = SurveillanceCameraVisuals.InUse;
        }

        _appearance.SetData(uid, SurveillanceCameraVisualsKey.Key, key, appearance);
    }
    // RW-Start
    private void RefreshActiveAiObservers()
    {
        _activeAiObservers.Clear();

        var coreQuery = EntityQueryEnumerator<StationAiCoreComponent>();
        while (coreQuery.MoveNext(out var coreUid, out var coreComp))
        {
            if (coreComp.RemoteEntity == null)
                continue;

            // Only active AI with mind should affect camera visuals
            if (!_stationAiSystem.TryGetHeld((coreUid, coreComp), out var held) || !HasComp<ActorComponent>(held))
                continue;

            if (!TryComp(coreComp.RemoteEntity.Value, out TransformComponent? observerXform))
                continue;

            _activeAiObservers.Add((observerXform.MapID, _transformSystem.GetWorldPosition(observerXform)));
        }
    }

    private bool HasActiveAiViewerInRange(EntityUid camera)
    {
        if (!TryComp(camera, out TransformComponent? cameraXform))
            return false;

        var cameraMap = cameraXform.MapID;
        var cameraPos = _transformSystem.GetWorldPosition(cameraXform);
        var activationRangeSquared = AiViewerActivationRange * AiViewerActivationRange;
        foreach (var observer in _activeAiObservers)
        {
            if (observer.Map != cameraMap)
                continue;

            if ((observer.Position - cameraPos).LengthSquared() <= activationRangeSquared)
                return true;
        }

        return false;
    }
    // RW-End
}

public sealed class OnSurveillanceCameraViewerAddEvent : EntityEventArgs
{

}

public sealed class OnSurveillanceCameraViewerRemoveEvent : EntityEventArgs
{

}

// What happens when a camera deactivates.
public sealed class SurveillanceCameraDeactivateEvent : EntityEventArgs
{
    public EntityUid Camera { get; }

    public SurveillanceCameraDeactivateEvent(EntityUid camera)
    {
        Camera = camera;
    }
}

[ByRefEvent]
public record struct SurveillanceCameraSetActiveAttemptEvent(bool Cancelled);
