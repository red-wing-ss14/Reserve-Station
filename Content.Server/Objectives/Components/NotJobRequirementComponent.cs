// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Objectives.Systems;
using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

/// <summary>
/// Requires that the player not have a certain job to have this objective.
/// </summary>
[RegisterComponent, Access(typeof(NotJobRequirementSystem))]
public sealed partial class NotJobRequirementComponent : Component
{

    /// <summary>
    /// List of job prototype IDs to ban from having this objective.
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? Job;

    /// <summary>
    /// IDs of jobs to ban from having this objective.
    /// Used by some downstream forks.
    /// </summary>
    // RW edit: support list-based job blacklist syntax in YAML prototypes.
    [DataField]
    public List<ProtoId<JobPrototype>> Jobs = new();

    // MisandryBox/JobObjectives - Double negative to not break compatibility
    [DataField]
    public bool Inverted;
}
