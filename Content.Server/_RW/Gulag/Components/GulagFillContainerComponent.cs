namespace Content.Server._RW.Gulag.Components;

/// <summary>
/// Marks a cargo container that should be filled with materials mined in the gulag.
/// </summary>
[RegisterComponent, Access(typeof(GulagSystem))]
public sealed partial class GulagFillContainerComponent : Component;
