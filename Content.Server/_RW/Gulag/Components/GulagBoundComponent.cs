namespace Content.Server._RW.Gulag.Components;

/// <summary>
/// Marks a player entity that must remain on the gulag map.
/// </summary>
[RegisterComponent, Access(typeof(GulagSystem))]
public sealed partial class GulagBoundComponent : Component;
