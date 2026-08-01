using Content.Shared._DV.VendingMachines;
using Robust.Shared.GameStates;

namespace Content.Shared._RW.Bitrunning.Components;

/// <summary>
/// Makes a <see cref="ShopVendorComponent"/> use bitrunning points to buy items.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BitrunningPointsVendorComponent : Component;
