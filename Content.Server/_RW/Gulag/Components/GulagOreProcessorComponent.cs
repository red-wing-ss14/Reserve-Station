namespace Content.Server._RW.Gulag.Components;

/// <summary>
/// Marks a material storage that converts inserted ore into sentence reduction.
/// </summary>
[RegisterComponent, Access(typeof(GulagSystem))]
public sealed partial class GulagOreProcessorComponent : Component;
