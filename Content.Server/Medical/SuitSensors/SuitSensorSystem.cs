// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Server.Access.Systems;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.CrewMonitoring;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Shared.Verbs;
using Content.Shared.Emp;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Medical.SuitSensors;

public sealed class SuitSensorSystem : SharedSuitSensorSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServerSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
//    [Dependency] private readonly IPrototypeManager _proto = default!; // RW-Edit: _proto don't used
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var sensors = EntityQueryEnumerator<SuitSensorComponent, DeviceNetworkComponent>();

        while (sensors.MoveNext(out var uid, out var sensor, out var device))
        {
            if (device.TransmitFrequency is null)
                continue;

            // check if sensor is ready to update
            if (curTime < sensor.NextUpdate)
                continue;
            sensor.NextUpdate += sensor.UpdateRate;

            if (!CheckSensorAssignedStation((uid, sensor)))
                continue;

            // get sensor status
            var status = GetSensorState((uid, sensor));
            if (status == null)
                continue;

            //Retrieve active server address if the sensor isn't connected to a server
            if (sensor.ConnectedServer == null)
            {
                if (!_singletonServerSystem.TryGetActiveServerAddress<CrewMonitoringServerComponent>(sensor.StationId!.Value, out var address))
                    continue;

                sensor.ConnectedServer = address;
            }

            // Send it to the connected server
            var payload = SuitSensorToPacket(status);

            // Clear the connected server if its address isn't on the network
            if (!_deviceNetworkSystem.IsAddressPresent(device.DeviceNetId, sensor.ConnectedServer))
            {
                sensor.ConnectedServer = null;
                continue;
            }

            _deviceNetworkSystem.QueuePacket(uid, sensor.ConnectedServer, payload, device: device);
        }
    }


    private Verb CreateVerb(EntityUid uid, SuitSensorComponent component, EntityUid userUid, SuitSensorMode mode)
    {
        return new Verb
        {
            Text = GetModeName(mode),
            Disabled = component.Mode == mode,
            Priority = -(int) mode, // sort them in descending order
            Category = VerbCategory.SetSensor,
            Act = () => TrySetSensor((uid, component), mode, userUid)
        };
    }

    private string GetModeName(SuitSensorMode mode)
    {
        string name;
        switch (mode)
        {
            case SuitSensorMode.SensorOff:
                name = "suit-sensor-mode-off";
                break;
            case SuitSensorMode.SensorBinary:
                name = "suit-sensor-mode-binary";
                break;
            case SuitSensorMode.SensorVitals:
                name = "suit-sensor-mode-vitals";
                break;
            case SuitSensorMode.SensorCords:
                name = "suit-sensor-mode-cords";
                break;
            default:
                return "";
        }

        return Loc.GetString(name);
    }

    public void TrySetSensor(Entity<SuitSensorComponent> sensors, SuitSensorMode mode, EntityUid userUid)
    {
        var comp = sensors.Comp;

        if (!Resolve(sensors, ref comp))
            return;

        if (comp.User == null || userUid == comp.User)
            SetSensor(sensors, mode, userUid);
        else
        {
            var doAfterEvent = new SuitSensorChangeDoAfterEvent(mode);
            var doAfterArgs = new DoAfterArgs(EntityManager, userUid, comp.SensorsTime, doAfterEvent, sensors)
            {
                BreakOnMove = true,
                BreakOnDamage = true
            };

            _doAfterSystem.TryStartDoAfter(doAfterArgs);
        }
    }

    private void OnSuitSensorDoAfter(Entity<SuitSensorComponent> sensors, ref SuitSensorChangeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        SetSensor(sensors, args.Mode, args.User);
    }

    public void SetSensor(Entity<SuitSensorComponent> sensors, SuitSensorMode mode, EntityUid? userUid = null)
    {
        var comp = sensors.Comp;

        comp.Mode = mode;

        if (userUid != null)
        {
            var msg = Loc.GetString("suit-sensor-mode-state", ("mode", GetModeName(mode)));
            _popupSystem.PopupEntity(msg, sensors, userUid.Value);
        }
    }

    /// <summary>
    ///     Set all suit sensors on the equipment someone is wearing to the specified mode.
    /// </summary>
    public void SetAllSensors(EntityUid target, SuitSensorMode mode, SlotFlags slots = SlotFlags.All )
    {
        // iterate over all inventory slots
        var slotEnumerator = _inventory.GetSlotEnumerator(target, slots);
        while (slotEnumerator.NextItem(out var item, out _))
        {
            if (TryComp<SuitSensorComponent>(item, out var sensorComp))
                SetSensor((item, sensorComp), mode);
        }
    }

    public SuitSensorStatus? GetSensorState(EntityUid uid, SuitSensorComponent? sensor = null, TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref sensor, ref transform))
            return null;

        // check if sensor is enabled and worn by user
        if (sensor.User == null || !HasComp<MobStateComponent>(sensor.User) || transform.GridUid == null) // RW-Edit: check if sensor worn by user
            return null;

        // try to get mobs id from ID slot
        var userName = Loc.GetString("suit-sensor-component-unknown-name");
        var userJob = Loc.GetString("suit-sensor-component-unknown-job");
        var userJobIcon = "JobIconNoId";
        var userJobDepartments = new List<string>();

        if (_idCardSystem.TryFindIdCard(sensor.User.Value, out var card))
        {
            if (card.Comp.FullName != null)
                userName = card.Comp.FullName;
            if (card.Comp.LocalizedJobTitle != null)
                userJob = card.Comp.LocalizedJobTitle;
            userJobIcon = card.Comp.JobIcon;

            // RW-Edit-Start: sorting by departments
            foreach (var department in card.Comp.JobDepartments)
                userJobDepartments.Add(department);
            // RW-Edit-End
        }

        // get health mob state
        var isAlive = false;
        if (TryComp(sensor.User.Value, out MobStateComponent? mobState))
            isAlive = !_mobStateSystem.IsDead(sensor.User.Value, mobState);

        // get mob total damage
        var totalDamage = 0;
        if (TryComp<DamageableComponent>(sensor.User.Value, out var damageable))
            totalDamage = _mobThresholdSystem.CheckVitalDamage(sensor.User.Value, damageable).Int(); // Goobstation

        // Get mob total damage crit threshold
        int? totalDamageThreshold = null;
        if (_mobThresholdSystem.TryGetThresholdForState(sensor.User.Value, MobState.SoftCritical, out var critThreshold)) // RW-Edit
            totalDamageThreshold = critThreshold.Value.Int();

        // finally, form suit sensor status
        // RW-Edit-Start: always store full data for all modes; mode-based projection is done server-side in UpdateUserInterface before sending to client
        var status = new SuitSensorStatus(GetNetEntity(sensor.User.Value), GetNetEntity(uid), userName, userJob, userJobIcon, userJobDepartments, sensor.Mode);
                status.IsAlive = isAlive;
                status.TotalDamage = totalDamage;
                status.TotalDamageThreshold = totalDamageThreshold;
                EntityCoordinates coordinates;
                var xformQuery = GetEntityQuery<TransformComponent>();

                if (transform.GridUid != null)
                {
                    coordinates = new EntityCoordinates(transform.GridUid.Value,
                        Vector2.Transform(_transform.GetWorldPosition(transform, xformQuery),
                            _transform.GetInvWorldMatrix(xformQuery.GetComponent(transform.GridUid.Value), xformQuery)));
                }
                else if (transform.MapUid != null)
                {
                    coordinates = new EntityCoordinates(transform.MapUid.Value,
                        _transform.GetWorldPosition(transform, xformQuery));
                }
                else
                {
                    coordinates = EntityCoordinates.Invalid;
                }

                status.Coordinates = GetNetCoordinates(coordinates);
                status.IsCommandTracker = sensor.CommandTracker; //Goob station
        // RW-Edit-End

        return status;
    }

    /// <summary>
    ///     Serialize create a device network package from the suit sensors status.
    /// </summary>
    public NetworkPayload SuitSensorToPacket(SuitSensorStatus status)
    {
        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdUpdatedState,
            [SuitSensorConstants.NET_NAME] = status.Name,
            [SuitSensorConstants.NET_JOB] = status.Job,
            [SuitSensorConstants.NET_JOB_ICON] = status.JobIcon,
            [SuitSensorConstants.NET_JOB_DEPARTMENTS] = status.JobDepartments,
            [SuitSensorConstants.NET_IS_ALIVE] = status.IsAlive,
            [SuitSensorConstants.NET_IS_COMMAND] = status.IsCommandTracker, //Goob station
            [SuitSensorConstants.NET_SUIT_SENSOR_UID] = status.SuitSensorUid,
            [SuitSensorConstants.NET_OWNER_UID] = status.OwnerUid,
            [SuitSensorConstants.NET_SUIT_SENSOR_MODE] = status.Mode, // RW
        };

        if (status.TotalDamage != null)
            payload.Add(SuitSensorConstants.NET_TOTAL_DAMAGE, status.TotalDamage);
        if (status.TotalDamageThreshold != null)
            payload.Add(SuitSensorConstants.NET_TOTAL_DAMAGE_THRESHOLD, status.TotalDamageThreshold);
        if (status.Coordinates != null)
            payload.Add(SuitSensorConstants.NET_COORDINATES, status.Coordinates);

        return payload;
    }

    /// <summary>
    ///     Try to create the suit sensors status from the device network message
    /// </summary>
    public SuitSensorStatus? PacketToSuitSensor(NetworkPayload payload)
    {
        // check command
        if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return null;
        if (command != DeviceNetworkConstants.CmdUpdatedState)
            return null;

        // check name, job and alive
        if (!payload.TryGetValue(SuitSensorConstants.NET_NAME, out string? name)) return null;
        if (!payload.TryGetValue(SuitSensorConstants.NET_JOB, out string? job)) return null;
        if (!payload.TryGetValue(SuitSensorConstants.NET_JOB_ICON, out string? jobIcon)) return null;
        if (!payload.TryGetValue(SuitSensorConstants.NET_JOB_DEPARTMENTS, out List<string>? jobDepartments)) return null;
        if (!payload.TryGetValue(SuitSensorConstants.NET_IS_ALIVE, out bool? isAlive)) return null;
        if (!payload.TryGetValue(SuitSensorConstants.NET_IS_COMMAND, out bool iscommand)) return null; //Goob station
        if (!payload.TryGetValue(SuitSensorConstants.NET_SUIT_SENSOR_UID, out NetEntity suitSensorUid)) return null;
        if (!payload.TryGetValue(SuitSensorConstants.NET_OWNER_UID, out NetEntity ownerUid)) return null;
        if (!payload.TryGetValue(SuitSensorConstants.NET_SUIT_SENSOR_MODE, out SuitSensorMode mode)) return null; // RW

        // try get total damage and cords (optionals)
        payload.TryGetValue(SuitSensorConstants.NET_TOTAL_DAMAGE, out int? totalDamage);
        payload.TryGetValue(SuitSensorConstants.NET_TOTAL_DAMAGE_THRESHOLD, out int? totalDamageThreshold);
        payload.TryGetValue(SuitSensorConstants.NET_COORDINATES, out NetCoordinates? coords);

        var status = new SuitSensorStatus(ownerUid, suitSensorUid, name, job, jobIcon, jobDepartments, mode) // RW-Edit
        {
            IsAlive = isAlive.Value,
            TotalDamage = totalDamage,
            TotalDamageThreshold = totalDamageThreshold,
            Coordinates = coords,
            IsCommandTracker = iscommand,//Goob station
        };
        return status;
    }
}
