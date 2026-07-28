// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Server.Chemistry.Components;
using Content.Goobstation.Shared.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._RW.Construction.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.Power.Components;
using Content.Server.Chemistry.Components;
using Content.Shared.Popups;
using Content.Server.Tools;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Content.Goobstation.Maths.FixedPoint;

namespace Content.Goobstation.Server.Chemistry.EntitySystems
{
    /// <summary>
    /// Contains all the server-side logic for reagent dispensers.
    /// <seealso cref="EnergyReagentDispenserComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class EnergyReagentDispenserSystem : EntitySystem
    {
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly BatterySystem _battery = default!;
        // RW start
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly ToolSystem _toolSystem = default!;
        [Dependency] private readonly ChemicalLinkSystem _chemicalLinkSystem = default!;
        // RW end

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EnergyReagentDispenserComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<EnergyReagentDispenserComponent, EnergyReagentDispenserSetDispenseAmountMessage>(OnSetDispenseAmountMessage);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, EnergyReagentDispenserDispenseReagentMessage>(OnDispenseReagentMessage);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, EnergyReagentDispenserClearContainerSolutionMessage>(OnClearContainerSolutionMessage);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, PowerChangedEvent>(OnPowerChanged);

            SubscribeLocalEvent<EnergyReagentDispenserComponent, MapInitEvent>(OnMapInit, before: [typeof(ItemSlotsSystem)]);

            // RW start
            SubscribeLocalEvent<EnergyReagentDispenserComponent, InteractUsingEvent>(OnInteractUsing);
            // RW end

            // RW-Start
            SubscribeLocalEvent<EnergyReagentDispenserComponent, RefreshPartsEvent>(OnPartsRefresh);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, UpgradeExamineEvent>(OnUpgradeExamine);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, GotEmaggedEvent>(OnEmaged);
            // RW-End
        }

        private void SubscribeUpdateUiState<T>(Entity<EnergyReagentDispenserComponent> ent, ref T ev) => UpdateUiState(ent);

        private void UpdateUiState(Entity<EnergyReagentDispenserComponent> reagentDispenser)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedEnergyReagentDispenser.OutputSlotName);
            var outputContainerInfo = BuildOutputContainerInfo(outputContainer);
            var inventory = GetInventory(reagentDispenser.Comp);
            var batteryCharge = 0f;
            var batteryMaxCharge = 0f;
            var currentReceivingEnergy = 0f;
            var usingBattery = false;
            var idleUse = 0f;
            var hasPower = false;

            if (TryComp<BatteryComponent>(reagentDispenser, out var battery))
            {
                batteryCharge = _battery.GetCharge((reagentDispenser.Owner, battery));
                batteryMaxCharge = battery.MaxCharge;
            }

            if (TryComp<ApcPowerReceiverBatteryComponent>(reagentDispenser, out var apcPower))
            {
                currentReceivingEnergy = apcPower.BatteryRechargeRate;
                usingBattery = apcPower.Enabled;
                idleUse = apcPower.IdleLoad;
            }

            if (TryComp<ApcPowerReceiverComponent>(reagentDispenser, out var apc))
                hasPower = apc.Powered;

            var state = new EnergyReagentDispenserBoundUserInterfaceState(
                outputContainerInfo,
                GetNetEntity(outputContainer),
                inventory,
                reagentDispenser.Comp.DispenseAmount,
                batteryCharge,
                batteryMaxCharge,
                currentReceivingEnergy,
                idleUse,
                usingBattery,
                hasPower
            );
            _userInterfaceSystem.SetUiState(reagentDispenser.Owner, EnergyReagentDispenserUiKey.Key, state);
        }

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out _, out var solution))
            {
                return new ContainerInfo(Name(container.Value), solution.Volume, solution.MaxVolume)
                {
                    Reagents = solution.Contents,
                };
            }

            return null;
        }

        private List<EnergyReagentInventoryItem> GetInventory(EnergyReagentDispenserComponent comp)
        {
            var inventory = new List<EnergyReagentInventoryItem>();

            foreach (var (reagentId, cost) in comp.Reagents)
            {
                if (!_prototypeManager.TryIndex<ReagentPrototype>(reagentId, out var reagentProto))
                    continue;

                inventory.Add(new EnergyReagentInventoryItem(
                    reagentId,
                    reagentProto.LocalizedName,
                    cost,
                    reagentProto.SubstanceColor
                ));
            }

            inventory.Sort((a, b) => string.Compare(a.ReagentLabel, b.ReagentLabel, StringComparison.Ordinal));
            return inventory;
        }

        private void OnSetDispenseAmountMessage(Entity<EnergyReagentDispenserComponent> reagentDispenser, ref EnergyReagentDispenserSetDispenseAmountMessage message)
        {
            reagentDispenser.Comp.DispenseAmount = message.EnergyReagentDispenserDispenseAmount;
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnPowerChanged(Entity<EnergyReagentDispenserComponent> reagentDispenser, ref PowerChangedEvent args) =>
            UpdateUiState(reagentDispenser);

        private void OnDispenseReagentMessage(Entity<EnergyReagentDispenserComponent> reagentDispenser, ref EnergyReagentDispenserDispenseReagentMessage message)
        {
            if (!TryComp<BatteryComponent>(reagentDispenser, out var battery))
                return;

            var amount = (int) reagentDispenser.Comp.DispenseAmount;
            var currentCharge = _battery.GetCharge((reagentDispenser.Owner, battery));

            // RW start
            if (TryComp<ChemicalLinkComponent>(reagentDispenser.Owner, out var link) && link.LinkedDevice is { } linkedMaster && Exists(linkedMaster))
            {
                if (_transformSystem.InRange(reagentDispenser.Owner, linkedMaster, 1.5f))
                {
                    if (_solutionContainerSystem.TryGetSolution(linkedMaster, SharedChemMaster.BufferSolutionName, out var dstSoln, out var dstSolution))
                    {
                        var actualAmount = FixedPoint2.Min(amount, dstSolution.AvailableVolume);
                        if (actualAmount > 0)
                        {
                            var powerForActual = GetPowerCostForReagent(message.ReagentId, actualAmount.Float(), reagentDispenser.Comp);
                            if (currentCharge >= powerForActual)
                            {
                                dstSolution.CanReact = false; // RW
                                var linkedSol = new Solution(message.ReagentId, actualAmount);
                                _solutionContainerSystem.AddSolution(dstSoln.Value, linkedSol);
                                _battery.SetCharge((reagentDispenser.Owner, battery), currentCharge - powerForActual);
                            }
                            else
                            {
                                _audioSystem.PlayPvs(reagentDispenser.Comp.PowerSound, reagentDispenser, AudioParams.Default.WithVolume(-2f));
                                return;
                            }
                        }
                        else if (dstSolution.AvailableVolume <= 0)
                        {
                            _popupSystem.PopupEntity(Loc.GetString("chemical-linker-buffer-full"), reagentDispenser.Owner, message.Actor);
                        }
                    }
                    UpdateUiState(reagentDispenser);
                    ClickSound(reagentDispenser);
                    return;
                }
                else
                {
                    _popupSystem.PopupEntity(Loc.GetString("chemical-linker-dispenser-too-far"), reagentDispenser.Owner, message.Actor);
                    return;
                }
            }
            // RW end

            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedEnergyReagentDispenser.OutputSlotName);
            if (outputContainer is not { Valid: true }
                || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution, out _))
                return;

            var powerRequired = GetPowerCostForReagent(message.ReagentId, amount, reagentDispenser.Comp);

            if (currentCharge < powerRequired)
            {
                _audioSystem.PlayPvs(reagentDispenser.Comp.PowerSound, reagentDispenser, AudioParams.Default.WithVolume(-2f));
                return;
            }


            var sol = new Solution(message.ReagentId, amount);
            if (!_solutionContainerSystem.TryAddSolution(solution.Value, sol))
                return;

            _battery.SetCharge((reagentDispenser.Owner, battery), currentCharge - powerRequired);
            ClickSound(reagentDispenser);
            UpdateUiState(reagentDispenser);
        }

        private void OnClearContainerSolutionMessage(Entity<EnergyReagentDispenserComponent> reagentDispenser, ref EnergyReagentDispenserClearContainerSolutionMessage message)
        {
            var outputContainerNullable = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedEnergyReagentDispenser.OutputSlotName);
            if (outputContainerNullable is not { Valid: true } outputContainer
                || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer, out var solution, out var soln))
                return;

            var refundedPower = soln.Sum(reagent => GetPowerCostForReagent(reagent.Reagent.Prototype, reagent.Quantity.Float(), reagentDispenser.Comp)) // RW-Edit // RW
                                * reagentDispenser.Comp.RefundEnergyEfficiency; // RW
            if (refundedPower > 0)
            {
                _battery.TryGetBatteryComponent(reagentDispenser, out var batteryComponent, out _);
                if (batteryComponent != null)
                    _battery.SetCharge((reagentDispenser.Owner, batteryComponent), _battery.GetCharge((reagentDispenser.Owner, batteryComponent)) + refundedPower);
            }


            _solutionContainerSystem.RemoveAllSolution(solution.Value);
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void ClickSound(Entity<EnergyReagentDispenserComponent> reagentDispenser) =>
            _audioSystem.PlayPvs(reagentDispenser.Comp.ClickSound, reagentDispenser, AudioParams.Default.WithVolume(-2f));

        // RW start
        private static float GetPowerCostForReagent(string reagentId, float amount, EnergyReagentDispenserComponent comp)
        {
            return comp.Reagents.TryGetValue(reagentId, out var cost)
                ? cost * amount * comp.FinalEnergyCostMultiplier // RW-Edit
                : float.MaxValue;
        }
        // RW end

        private void OnMapInit(Entity<EnergyReagentDispenserComponent> entity, ref MapInitEvent args)
        {
            _itemSlotsSystem.AddItemSlot(entity.Owner,
                SharedEnergyReagentDispenser.OutputSlotName,
                entity.Comp.EnergyBeakerSlot);

            // RW-Start
            if (!TryComp<ApcPowerReceiverBatteryComponent>(entity, out var apcBattery))
                return;

            entity.Comp.BaseRechargeRate = apcBattery.BatteryRechargeRate;
            entity.Comp.FinalRechargeRate = apcBattery.BatteryRechargeRate;
            // RW-End
        }

        // RW start
        private void OnInteractUsing(EntityUid uid, EnergyReagentDispenserComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!_toolSystem.HasQuality(args.Used, SharedToolSystem.PulseQuality))
                return;

            args.Handled = true;
            _chemicalLinkSystem.HandleLinking(args.Used, uid, args.User, isDispenser: true);
        }
        // RW end

        // RW-Start
        private void OnPartsRefresh(EntityUid uid, EnergyReagentDispenserComponent component, RefreshPartsEvent args)
        {
            var capacitorTier = args.GetPartRating(component.CapacitorPart);
            var matterBinTier = args.GetPartRating(component.MatterBinPart);

            component.FinalRechargeRate = component.BaseRechargeRate * RefreshPartsEvent.GetPositiveTierMultiplier(capacitorTier);
            component.FinalEnergyCostMultiplier = RefreshPartsEvent.GetLinearMultiplier(matterBinTier, 0.1f, 0.5f, 1.2f);

            UpdateUiState((uid, component));
        }

        private static void OnUpgradeExamine(EntityUid uid, EnergyReagentDispenserComponent component, UpgradeExamineEvent args)
        {
            var rechargeMultiplier = component.BaseRechargeRate <= 0f
                ? 1f
                : component.FinalRechargeRate / component.BaseRechargeRate;

            args.AddPercentageUpgrade("machine-upgrade-charging-speed", rechargeMultiplier);
            args.AddPercentageUpgrade("machine-upgrade-energy-cost", component.FinalEnergyCostMultiplier);
        }

        private void OnEmaged(Entity<EnergyReagentDispenserComponent> ent, ref GotEmaggedEvent args)
        {
            if (ent.Comp.ReagentsEmagged == null || ent.Comp.Emagged)
                return;

            foreach (var (name, price) in ent.Comp.ReagentsEmagged)
            {
                ent.Comp.Reagents.Add(name, price);
            }

            args.Handled = true;
            ent.Comp.Emagged = true;
            UpdateUiState(ent);
        }
        // RW-End
    }
}
