// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Events;

/// <summary>
///     Add order to database.
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoConsoleAddOrderMessage : BoundUserInterfaceMessage
{
    // RW-Start
    public string? Requester;
    public string? DeliveryDestination;
    public string? Note;
    public bool SecuredDelivery;
    public bool PayPrivately;
    // RW-End
    public string CargoProductId;
    public int Amount;

    public CargoConsoleAddOrderMessage(string? requester, string? deliveryDestination, string? note, string cargoProductId, int amount, bool securedDelivery = false, bool payPrivately = false) // RW-Edit
    {
        Requester = requester;
        // RW-Start
        DeliveryDestination = deliveryDestination;
        Note = note;
        SecuredDelivery = securedDelivery;
        PayPrivately = payPrivately;
        // RW-End
        CargoProductId = cargoProductId;
        Amount = amount;
    }
}
