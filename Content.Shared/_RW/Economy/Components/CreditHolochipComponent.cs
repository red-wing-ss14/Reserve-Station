using Robust.Shared.Serialization;

namespace Content.Shared._RW.Economy.Components;

[RegisterComponent]
public sealed partial class CreditHolochipComponent : Component;

[Serializable, NetSerializable]
public enum CreditHolochipVisuals
{
    BaseState,
    OverlayState,
    BaseColor,
}
