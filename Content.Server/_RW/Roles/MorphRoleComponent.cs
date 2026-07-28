using Content.Shared.Roles;
using Content.Shared.Roles.Components;

namespace Content.Server._RW.Roles;

/// <summary>
///     Added to mind role entities to tag that they are a morph.
/// </summary>
[RegisterComponent]
public sealed partial class MorphRoleComponent : BaseMindRoleComponent;
