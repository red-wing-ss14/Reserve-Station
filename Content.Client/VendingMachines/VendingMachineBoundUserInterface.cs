// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using System.Linq;
using Content.Client._Funkystation.VendingMachines;

namespace Content.Client.VendingMachines
{
    public sealed class VendingMachineBoundUserInterface : BoundUserInterface, IVendingMachineBoundUi // Funky change
    {
        [ViewVariables]
        private VendingMachineMenu? _menu;

        [ViewVariables]
        private List<VendingMachineInventoryEntry> _cachedInventory = new();

        public VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindowCenteredLeft<VendingMachineMenu>();
            _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
            _menu.OnItemSelected += OnItemSelected;
            Refresh();
        }

        public void Refresh()
        {
            var enabled = EntMan.TryGetComponent(Owner, out VendingMachineComponent? bendy) && !bendy.Ejecting;

            var system = EntMan.System<VendingMachineSystem>();
            _cachedInventory = system.GetAllInventory(Owner);

            _menu?.Populate(_cachedInventory, enabled);
        }

        // RW-Start
        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (message is not VendingMachineInventoryUpdateMessage update)
                return;

            _cachedInventory = update.Inventory;
            var enabled = EntMan.TryGetComponent(Owner, out VendingMachineComponent? bendy) && !bendy.Ejecting;
            _menu?.SetBalance(update.Balance);
            _menu?.Populate(_cachedInventory, enabled);
        }
        // RW-End

        public void UpdateAmounts()
        {
            var enabled = EntMan.TryGetComponent(Owner, out VendingMachineComponent? bendy) && !bendy.Ejecting;

            var system = EntMan.System<VendingMachineSystem>();
            // RW-Start
            var updatedInventory = system.GetAllInventory(Owner);
            ApplyDisplayPrices(updatedInventory);
            // RW-End
            _cachedInventory = updatedInventory; // RW-Edit
            _menu?.UpdateAmounts(_cachedInventory, enabled);
        }

        private void OnItemSelected(GUIBoundKeyEventArgs args, ListData data)
        {
            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            if (data is not VendorItemsListData { ItemIndex: var itemIndex })
                return;

            if (_cachedInventory.Count == 0)
                return;

            var selectedItem = _cachedInventory.ElementAtOrDefault(itemIndex);

            if (selectedItem == null)
                return;

            SendPredictedMessage(new VendingMachineEjectMessage(selectedItem.Type, selectedItem.ID));
        }

        // RW-Start
        private void ApplyDisplayPrices(List<VendingMachineInventoryEntry> updatedInventory)
        {
            foreach (var updated in updatedInventory)
            {
                var current = _cachedInventory.FirstOrDefault(entry => entry.ID == updated.ID && entry.Type == updated.Type);

                if (current != null)
                    updated.DisplayPrice = current.DisplayPrice;
            }
        }
        // RW-End

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (_menu == null)
                return;

            _menu.OnItemSelected -= OnItemSelected;
            _menu.OnClose -= Close;
            _menu.Dispose();
        }
    }
}
