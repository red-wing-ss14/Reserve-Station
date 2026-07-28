// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Goobstation.Common.Pirates;
using Content.Server._RW.Economy.Components;
using Content.Server.Access.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Station.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Labels.Components;
using Content.Shared.Paper;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.Station.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems
{
    public sealed partial class CargoSystem
    {
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly EmagSystem _emag = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        // RW-Start
        [Dependency] private readonly IdCardSystem _idCard = default!;
        [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
        [Dependency] private readonly StorageSystem _storage = default!;
        // RW-End

        // RW-Start
        private ISawmill _sawmill = default!;
        private const double PrivatePurchaseMarkup = 1.10;
        // RW-End

        private void InitializeConsole()
        {
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleAddOrderMessage>(OnAddOrderMessage);
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleRemoveOrderMessage>(OnRemoveOrderMessage);
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleApproveOrderMessage>(OnApproveOrderMessage);
            SubscribeLocalEvent<CargoOrderConsoleComponent, BoundUIOpenedEvent>(OnOrderUIOpened);
            SubscribeLocalEvent<CargoOrderConsoleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<CargoOrderConsoleComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<CargoOrderConsoleComponent, GotEmaggedEvent>(OnEmagged);
        }

        private void OnInteractUsingCash(EntityUid uid, CargoOrderConsoleComponent component, ref InteractUsingEvent args)
        {
            // RW-Edit-Start
            if (!TryComp<CashComponent>(args.Used, out var cash))
                return;

            var total = (long) cash.Value;
            if (TryComp<StackComponent>(args.Used, out var stack))
                total *= stack.Count;

            if (total <= 0)
                return;

            var amount = (int) Math.Min(total, int.MaxValue);
            // RW-Edit-End

            var stationUid = _station.GetOwningStation(uid); // RW-Edit

            if (!TryComp(stationUid, out StationBankAccountComponent? bank))
                return;

            _audio.PlayPvs(ApproveSound, uid);
            UpdateBankAccount((stationUid.Value, bank), amount, component.Account); // RW-Edit
            QueueDel(args.Used);
            args.Handled = true;
        }

        private void OnInteractUsingSlip(Entity<CargoOrderConsoleComponent> ent, ref InteractUsingEvent args, CargoSlipComponent slip)
        {
            if (slip.OrderQuantity <= 0)
                return;

            var stationUid = _station.GetOwningStation(ent);

            if (!TryGetOrderDatabase(stationUid, out var orderDatabase))
                return;

            if (!_protoMan.TryIndex(slip.Product, out var product))
            {
                Log.Error($"Tried to add invalid cargo product {slip.Product} as order!");
                return;
            }

            if (!ent.Comp.AllowedGroups.Contains(product.Group))
                return;

            var orderId = GenerateOrderId(orderDatabase);
            var data = new CargoOrderData(orderId, product.Product, product.Name, product.Cost + (slip.SecuredDelivery ? ent.Comp.SecureOrderCost : 0), slip.OrderQuantity, slip.Requester, slip.DeliveryDestination, slip.Note, slip.Account, product.Cooldown, slip.SecuredDelivery); // RW-Edit

            if (!TryAddOrder(stationUid.Value, ent.Comp.Account, data, orderDatabase))
            {
                PlayDenySound(ent, ent.Comp);
                return;
            }

            // Log order addition
            _audio.PlayPvs(ent.Comp.ScanSound, ent);
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(args.User):user} inserted order slip [orderId:{data.OrderId}, quantity:{data.OrderQuantity}, product:{data.ProductId}, requester:{data.Requester}, deliveryDestination: {data.DeliveryDestination}, note:{data.Note}] "); // RW-Edit
            QueueDel(args.Used);
            args.Handled = true;
        }

        private void OnInteractUsing(EntityUid uid, CargoOrderConsoleComponent component, ref InteractUsingEvent args)
        {
            if (HasComp<CashComponent>(args.Used))
            {
                OnInteractUsingCash(uid, component, ref args);
            }
            else if (TryComp<CargoSlipComponent>(args.Used, out var slip) && component.Mode == CargoOrderConsoleMode.DirectOrder)
            {
                OnInteractUsingSlip((uid, component), ref args, slip);
            }
        }

        private void OnInit(EntityUid uid, CargoOrderConsoleComponent orderConsole, ComponentInit args)
        {
            var station = _station.GetOwningStation(uid);
            UpdateOrderState(uid, station);
        }

        private void OnEmagged(Entity<CargoOrderConsoleComponent> ent, ref GotEmaggedEvent args)
        {
            if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
                return;

            if (_emag.CheckFlag(ent, EmagType.Interaction))
                return;

            // RW-Start
            ent.Comp.EditableRequesterName = true;
            Dirty(ent);
            // RW-End

            args.Handled = true;
        }

        private void UpdateConsole()
        {
            var stationQuery = EntityQueryEnumerator<StationBankAccountComponent>();
            while (stationQuery.MoveNext(out var uid, out var bank))
            {
                // RW-Edit-Start
                while (Timing.CurTime >= bank.NextIncomeTime)
                {
                    bank.NextIncomeTime += bank.IncomeDelay;

                    var balanceToAdd = (int) Math.Round(bank.IncreasePerSecond * bank.IncomeDelay.TotalSeconds);
                    UpdateBankAccount((uid, bank), balanceToAdd, bank.RevenueDistribution);
                }

                foreach (var (account, nextFundingTime) in bank.NextBudgetFundingTime.ToArray())
                {
                    if (Timing.CurTime < nextFundingTime)
                        continue;

                    if (!_protoMan.TryIndex(account, out var accountProto))
                    {
                        bank.NextBudgetFundingTime.Remove(account);
                        continue;
                    }

                    bank.NextBudgetFundingTime[account] = nextFundingTime + accountProto.BudgetFundingDelay;

                    if (accountProto.BudgetFundingAmount <= 0)
                        continue;

                    UpdateBankAccount((uid, bank),
                        accountProto.BudgetFundingAmount,
                        new Dictionary<ProtoId<CargoAccountPrototype>, double>
                    {
                        { account, 1.0 },
                    });
                }
                // RW-Edit-End
            }
        }

        #region Interface

        // RW-Start
        private static int GetOrderBaseCost(CargoOrderData order)
        {
            return order.Price * order.OrderQuantity;
        }

        private static int GetOrderFinalCost(CargoOrderData order)
        {
            var baseCost = GetOrderBaseCost(order);
            if (!order.PaidPrivately)
                return baseCost;

            return (int) Math.Round(baseCost * PrivatePurchaseMarkup, MidpointRounding.AwayFromZero);
        }
        // RW-End

        private void OnApproveOrderMessage(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleApproveOrderMessage args)
        {
            if (args.Actor is not { Valid: true } player)
                return;

            if (component.Mode != CargoOrderConsoleMode.DirectOrder)
                return;

            if (!_accessReaderSystem.IsAllowed(player, uid))
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-order-not-allowed"));
                PlayDenySound(uid, component);
                return;
            }

            // goob edit - resource siphon blocking access
            var eqe = EntityQueryEnumerator<ResourceSiphonComponent>();
            while (eqe.MoveNext(out var sip))
            {
                // it's over. the crew won't know what hit em.
                if (sip.Active)
                {
                    ConsolePopup(args.Actor, Loc.GetString("console-block-something"));
                    PlayDenySound(uid, component);
                    return;
                }
            }
            // goob edit end

            var station = _station.GetOwningStation(uid);

            // No station to deduct from.
            if (!TryComp(station, out StationBankAccountComponent? bank) ||
                !TryComp(station, out StationDataComponent? stationData) ||
                !TryGetOrderDatabase(station, out var orderDatabase))
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-station-not-found"));
                PlayDenySound(uid, component);
                return;
            }

            // Find our order again. It might have been dispatched or approved already
            var order = orderDatabase.Orders[component.Account].Find(order => args.OrderId == order.OrderId && !order.Approved);
            if (order == null || !_protoMan.Resolve(order.Account, out var account))
            {
                return;
            }

            // Invalid order
            if (!_protoMan.HasIndex<EntityPrototype>(order.ProductId))
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-invalid-product"));
                PlayDenySound(uid, component);
                return;
            }

            var amount = GetOutstandingOrderCount((station.Value, orderDatabase), order.Account);
            var capacity = orderDatabase.Capacity;

            // Too many orders, avoid them getting spammed in the UI.
            if (amount >= capacity)
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-too-many"));
                PlayDenySound(uid, component);
                return;
            }

            // Cap orders so someone can't spam thousands.
            var cappedAmount = Math.Min(capacity - amount, order.OrderQuantity);

            if (cappedAmount != order.OrderQuantity)
            {
                order.OrderQuantity = cappedAmount;
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-snip-snip"));
                PlayDenySound(uid, component);
            }

            var baseCost = GetOrderBaseCost(order); // RW
            var finalCost = GetOrderFinalCost(order); // RW-Edit: was cost
            var accountBalance = GetBalanceFromAccount((station.Value, bank), order.Account);
            Entity<StationAccountComponent>? privateAccount = null; // RW

            // RW-Edit-Start
            if (order.PaidPrivately)
            {
                if (string.IsNullOrWhiteSpace(order.PrivateBuyerAccountId) || !_bank.TryFindAccountById(order.PrivateBuyerAccountId, out var buyerAccount))
                {
                    ConsolePopup(args.Actor, Loc.GetString("cargo-console-invalid-bank-account"));
                    PlayDenySound(uid, component);
                    return;
                }

                privateAccount = buyerAccount;
                if (buyerAccount.Comp.Balance < finalCost)
                {
                    ConsolePopup(args.Actor, Loc.GetString("cargo-console-private-insufficient-funds", ("cost", finalCost)));
                    PlayDenySound(uid, component);
                    return;
                }
            }
            else if (finalCost > accountBalance)
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", finalCost)));
                PlayDenySound(uid, component);
                return;
            }
            // RW-Edit-End

            // GoobStation - cooldown on Cargo Orders (specifically gamba)
            if (order.Cooldown > 0)
            {
                if (orderDatabase.ProductCooldownTime.TryGetValue(order.ProductId, out var cooldownTime) && cooldownTime > _timing.CurTime)
                {
                    var timeLeft = (cooldownTime - _timing.CurTime);
                    (int count, string units) timeInfo = (timeLeft.Minutes > 0) ? (timeLeft.Minutes, "minutes") : (timeLeft.Seconds, "seconds");
                    ConsolePopup(args.Actor, Loc.GetString("cargo-console-cooldown-active", ("product", order.ProductName), ("timeCount", timeInfo.count), ("timeUnits", timeInfo.units)));
                    PlayDenySound(uid, component);
                    return;
                }
                if (order.OrderQuantity > 1)
                {
                    ConsolePopup(args.Actor, Loc.GetString("cargo-console-cooldown-count", ("product", order.ProductName)));
                    PlayDenySound(uid, component);
                    return;
                }
            }

            var ev = new FulfillCargoOrderEvent((station.Value, stationData), order, (uid, component));
            RaiseLocalEvent(ref ev);
            ev.FulfillmentEntity ??= station.Value;

            if (!ev.Handled)
            {
                ev.FulfillmentEntity = TryFulfillOrder((station.Value, stationData), order.Account, order, orderDatabase);

                if (ev.FulfillmentEntity == null)
                {
                    ConsolePopup(args.Actor, Loc.GetString("cargo-console-unfulfilled"));
                    PlayDenySound(uid, component);
                    return;
                }
            }

            // RW-Start
            if (order.PaidPrivately && (privateAccount == null || !_bank.Withdraw(privateAccount.Value, finalCost, "cargo-private-purchase", reasonData: $"order:{order.OrderId}|product:{order.ProductId}")))
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-payment-failed"));
                PlayDenySound(uid, component);
                return;
            }
            // RW-End

            // GoobStation - cooldown on Cargo Orders (specifically gamba)
            if (order.Cooldown > 0)
            {
                orderDatabase.ProductCooldownTime[order.ProductId] = _timing.CurTime + TimeSpan.FromSeconds(order.Cooldown);
            }

            order.Approved = true;
            _audio.PlayPvs(ApproveSound, uid);

            if (!_emag.CheckFlag(uid, EmagType.Interaction))
            {
                var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(uid, player);
                RaiseLocalEvent(tryGetIdentityShortInfoEvent);
                order.SetApproverData(tryGetIdentityShortInfoEvent.Title);

                var message = Loc.GetString("cargo-console-unlock-approved-order-broadcast",
                    ("productName", Loc.GetString(order.ProductName)),
                    ("orderAmount", order.OrderQuantity),
                    ("approver", order.Approver ?? string.Empty),
                    ("cost", finalCost)); // RW-Edit
                _radio.SendRadioMessage(uid, message, account.RadioChannel, uid, escapeMarkup: false);
                if (CargoOrderConsoleComponent.BaseAnnouncementChannel != account.RadioChannel)
                    _radio.SendRadioMessage(uid, message, CargoOrderConsoleComponent.BaseAnnouncementChannel, uid, escapeMarkup: false);
            }

            ConsolePopup(args.Actor, Loc.GetString("cargo-console-trade-station", ("destination", MetaData(ev.FulfillmentEntity.Value).EntityName)));

            // Log order approval
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(player):user} approved order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order.ProductId}, requester:{order.Requester}, deliveryDestination: {order.DeliveryDestination}, note:{order.Note}] on account {order.Account} with balance at {accountBalance}"); // RW-Edit

            // RW-Start
            if (order.PaidPrivately)
            {
                var fee = finalCost - baseCost;
                if (fee > 0)
                    UpdateBankAccount((station.Value, bank), fee, order.Account);

                _adminLogger.Add(LogType.Action, LogImpact.Low, $"Private cargo order approved [orderId:{order.OrderId}, product:{order.ProductId}, buyerAccount:{order.PrivateBuyerAccountId}, buyerName:{order.PrivateBuyerName}, baseCost:{baseCost}, finalCost:{finalCost}, fee:{fee}]");
            }
            else
            {
                UpdateBankAccount((station.Value, bank), -finalCost, order.Account);
            }
            // RW-End

            orderDatabase.Orders[component.Account].Remove(order);
//            UpdateBankAccount((station.Value, bank), -cost, order.Account); // RW-Edit
            UpdateOrders(station.Value);
        }

        private EntityUid? TryFulfillOrder(Entity<StationDataComponent> stationData, ProtoId<CargoAccountPrototype> account, CargoOrderData order, StationCargoOrderDatabaseComponent orderDatabase)
        {
            // No slots at the trade station
            _listEnts.Clear();
            GetTradeStations(stationData, ref _listEnts);
            EntityUid? tradeDestination = null;

            // Try to fulfill from any station where possible, if the pad is not occupied.
            foreach (var trade in _listEnts)
            {
                var tradePads = GetCargoPallets(trade, BuySellType.Buy);
                _random.Shuffle(tradePads);

                var freePads = GetFreeCargoPallets(trade, tradePads);
                if (freePads.Count >= order.OrderQuantity) //check if the station has enough free pallets
                {
                    // RW-Start
                    EntityUid? previousCrate = null;
                    List<string>? excessItems = null;
                    // RW-End

                    foreach (var pad in freePads)
                    {
                        var coordinates = new EntityCoordinates(trade, pad.Transform.LocalPosition);

                        // RW-Start
                        if (previousCrate is not null
                            && TryComp<EntityStorageComponent>(previousCrate, out var entityStorage)
                            && entityStorage.Contents.Count >= entityStorage.Capacity)
                        {
                            previousCrate = null;
                        }
                        // RW-End

                        // RW-Edit-Start
                        if (!FulfillOrder(order,
                                account,
                                coordinates,
                                orderDatabase.PrinterOutput,
                                out var nextCrate,
                                out excessItems,
                                previousCrate,
                                excessItems))
                            continue;

                        previousCrate = nextCrate; // CorvaxGoob-CargoFeatures
                        tradeDestination = trade;
                        order.NumDispatched++;
                        if (order.OrderQuantity <= order.NumDispatched) //Spawn a crate on free pellets until the order is fulfilled.
                            break;
                        // RW-Edit-End
                    }
                }

                if (tradeDestination != null)
                    break;
            }

            return tradeDestination;
        }

        private void GetTradeStations(StationDataComponent data, ref List<EntityUid> ents)
        {
            foreach (var gridUid in data.Grids)
            {
                if (!_tradeQuery.HasComponent(gridUid))
                    continue;

                ents.Add(gridUid);
            }
        }

        private void OnRemoveOrderMessage(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleRemoveOrderMessage args)
        {
            var station = _station.GetOwningStation(uid);

            if (component.Mode != CargoOrderConsoleMode.DirectOrder)
                return;

            if (!TryGetOrderDatabase(station, out var orderDatabase))
                return;

            RemoveOrder(station.Value, component.Account, args.OrderId, orderDatabase);
        }

        private void OnAddOrderMessageSlipPrinter(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleAddOrderMessage args, CargoProductPrototype product, string requester) // RW-Edit: requester
        {
            if (!_protoMan.Resolve(component.Account, out var account))
                return;

            if (Timing.CurTime < component.NextPrintTime)
                return;

            var label = Spawn(account.AcquisitionSlip, Transform(uid).Coordinates);
            component.NextPrintTime = Timing.CurTime + component.PrintDelay;
            _audio.PlayPvs(component.PrintSound, uid);

            var paper = EnsureComp<PaperComponent>(label);
            var msg = new FormattedMessage();

            msg.AddMarkupPermissive(Loc.GetString("cargo-acquisition-slip-body",
                ("product", product.Name),
                ("description", product.Description),
                ("unit", product.Cost),
                ("amount", args.Amount),
                // RW-Edit-Start
                ("cost", product.Cost * args.Amount + (args.SecuredDelivery ? component.SecureOrderCost : 0)),
                ("orderer", requester),
                ("destination", args.DeliveryDestination ?? Loc.GetString("cargo-console-paper-delivery-destination-default")),
                ("note", args.Note ?? Loc.GetString("cargo-console-paper-note-default"))));
                // RW-Edit-End
            _paperSystem.SetContent((label, paper), msg.ToMarkup());

            var slip = EnsureComp<CargoSlipComponent>(label);
            slip.Product = product.ID;
            // RW-Edit-Start
            slip.Requester = requester;
            slip.DeliveryDestination = args.DeliveryDestination;
            slip.Note = args.Note;
            slip.SecuredDelivery = args.SecuredDelivery;
            // RW-Edit-End
            slip.OrderQuantity = args.Amount;
            slip.Account = component.Account;
        }

        private void OnAddOrderMessage(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleAddOrderMessage args)
        {
            if (args.Actor is not { Valid: true } player)
                return;

            if (args.Amount <= 0)
                return;

            var stationUid = _station.GetOwningStation(uid);

            if (!TryGetOrderDatabase(stationUid, out var orderDatabase))
                return;

            if (!TryComp<StationBankAccountComponent>(stationUid, out var bank))
                return;

            if (!_protoMan.TryIndex<CargoProductPrototype>(args.CargoProductId, out var product))
            {
                Log.Error($"Tried to add invalid cargo product {args.CargoProductId} as order!");
                return;
            }

            if (!GetAvailableProducts((uid, component)).Contains(args.CargoProductId))
                return;

            // RW-Start
            if (args.SecuredDelivery)
                args.SecuredDelivery = CanBeSecuredDelivery((uid, component), _protoMan.Index<CargoProductPrototype>(args.CargoProductId));

            string requester = string.Empty;
            if (component.EditableRequesterName && args.Requester is not null)
                requester = args.Requester;
            else
                requester = GenerateRequesterName((uid, component), args.Actor);
            // RW-End

            if (component.Mode == CargoOrderConsoleMode.PrintSlip)
            {
                OnAddOrderMessageSlipPrinter(uid, component, args, product, requester); // RW-Edit: requester
                return;
            }

            var targetAccount = component.Mode == CargoOrderConsoleMode.SendToPrimary ? bank.PrimaryAccount : component.Account;

            // RW-Start
            string? privateBuyerAccountId = null;
            string? privateBuyerName = null;
            if (args.PayPrivately)
            {
                if (!_idCard.TryFindIdCard(player, out var idCard))
                {
                    ConsolePopup(args.Actor, Loc.GetString("cargo-console-no-id-detected"));
                    PlayDenySound(uid, component);
                    return;
                }

                if (string.IsNullOrWhiteSpace(idCard.Comp.BankAccountId))
                {
                    ConsolePopup(args.Actor, Loc.GetString("cargo-console-no-linked-bank-account"));
                    PlayDenySound(uid, component);
                    return;
                }

                if (!_bank.TryFindAccountById(idCard.Comp.BankAccountId, out var privateBuyerAccount))
                {
                    ConsolePopup(args.Actor, Loc.GetString("cargo-console-invalid-bank-account"));
                    PlayDenySound(uid, component);
                    return;
                }

                privateBuyerAccountId = idCard.Comp.BankAccountId;
                privateBuyerName = privateBuyerAccount.Comp.OwnerName;
            }
            // RW-End

            var data = GetOrderData(args, product, GenerateOrderId(orderDatabase), component.Account, requester, args.SecuredDelivery ? component.SecureOrderCost : default); // RW-Edit

            // RW-Start
            if (privateBuyerAccountId != null && privateBuyerName != null)
                data.SetPrivateBuyerData(privateBuyerAccountId, privateBuyerName);
            // RW-End

            if (!TryAddOrder(stationUid.Value, targetAccount, data, orderDatabase))
            {
                PlayDenySound(uid, component);
                return;
            }

            // Log order addition
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(player):user} added order [orderId:{data.OrderId}, quantity:{data.OrderQuantity}, product:{data.ProductId}, requester:{data.Requester}, deliveryDestination: {data.DeliveryDestination}, note:{data.Note}]"); // RW-Edit

        }

        private void OnOrderUIOpened(EntityUid uid, CargoOrderConsoleComponent component, BoundUIOpenedEvent args)
        {
            var station = _station.GetOwningStation(uid);
            UpdateOrderState(uid, station);
        }

        #endregion

        private void UpdateOrderState(EntityUid consoleUid, EntityUid? station)
        {
            if (!TryComp<CargoOrderConsoleComponent>(consoleUid, out var console))
                return;

            if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDatabase))
                return;

            if (_uiSystem.HasUi(consoleUid, CargoConsoleUiKey.Orders))
            {
                _uiSystem.SetUiState(consoleUid,
                    CargoConsoleUiKey.Orders,
                    new CargoConsoleInterfaceState(
                    MetaData(station.Value).EntityName,
                    GetOutstandingOrderCount((station!.Value, orderDatabase), console.Account),
                    orderDatabase.Capacity,
                    GetNetEntity(station.Value),
                    RelevantOrders((station!.Value, orderDatabase), (consoleUid, console)),
                    GetAvailableProducts((consoleUid, console))
                ));
            }
        }

        /// <summary>
        /// Gets orders relevant to this account, i.e. orders on the account directly or orders on behalf of the account in the primary account.
        /// </summary>
        private List<CargoOrderData> RelevantOrders(Entity<StationCargoOrderDatabaseComponent> station, Entity<CargoOrderConsoleComponent> console)
        {
            if (!TryComp<StationBankAccountComponent>(station, out var bank))
                return [];

            var ourOrders = station.Comp.Orders[console.Comp.Account];

            if (console.Comp.Account == bank.PrimaryAccount)
                return ourOrders;

            var otherOrders = station.Comp.Orders[bank.PrimaryAccount].Where(order => order.Account == console.Comp.Account);

            return ourOrders.Concat(otherOrders).ToList();
        }

        private void ConsolePopup(EntityUid actor, string text)
        {
            _popup.PopupCursor(text, actor);
        }

        private void PlayDenySound(EntityUid uid, CargoOrderConsoleComponent component)
        {
            if (_timing.CurTime >= component.NextDenySoundTime)
            {
                component.NextDenySoundTime = _timing.CurTime + component.DenySoundDelay;
                _audio.PlayPvs(_audio.ResolveSound(component.ErrorSound), uid);
            }
        }

        private static CargoOrderData GetOrderData(CargoConsoleAddOrderMessage args, CargoProductPrototype cargoProduct, int id, ProtoId<CargoAccountPrototype> account, string requester, int extraPrice = 0) // RW-Edit
        {
            // GoobStation - cooldown on Cargo Orders (specifically gamba)
            return new CargoOrderData(id, cargoProduct.Product, cargoProduct.Name, cargoProduct.Cost + extraPrice, args.Amount, requester, args.DeliveryDestination, args.Note, account, cargoProduct.Cooldown, args.SecuredDelivery); // RW-Edit
        }

        public int GetOutstandingOrderCount(Entity<StationCargoOrderDatabaseComponent> station, ProtoId<CargoAccountPrototype> account)
        {
            var amount = 0;

            if (!TryComp<StationBankAccountComponent>(station, out var bank))
                return amount;

            foreach (var order in station.Comp.Orders[account])
            {
                if (!order.Approved)
                    continue;
                amount += order.OrderQuantity - order.NumDispatched;
            }

            if (account == bank.PrimaryAccount)
                return amount;

            foreach (var order in station.Comp.Orders[bank.PrimaryAccount])
            {
                if (order.Account != account)
                    continue;
                if (!order.Approved)
                    continue;
                amount += order.OrderQuantity - order.NumDispatched;
            }

            return amount;
        }

        /// <summary>
        /// Updates all of the cargo-related consoles for a particular station.
        /// This should be called whenever orders change.
        /// </summary>
        private void UpdateOrders(EntityUid dbUid)
        {
            // Order added so all consoles need updating.
            var orderQuery = AllEntityQuery<CargoOrderConsoleComponent>();

            while (orderQuery.MoveNext(out var uid, out var _))
            {
                var station = _station.GetOwningStation(uid);
                if (station != dbUid)
                    continue;

                UpdateOrderState(uid, station);
            }
        }

        public bool AddAndApproveOrder(
            EntityUid dbUid,
            string spawnId,
            string name,
            int cost,
            int qty,
            string sender,
            string? deliveryDestination, // RW
            string? note, // RW
            string dest,
            StationCargoOrderDatabaseComponent component,
            ProtoId<CargoAccountPrototype> account,
            Entity<StationDataComponent> stationData,
            bool securedDelivery = false // RW
        )
        {
            DebugTools.Assert(_protoMan.HasIndex<EntityPrototype>(spawnId));
            // Make an order
            var id = GenerateOrderId(component);
            // GoobStation - cooldown on Cargo Orders (specifically gamba)
            var order = new CargoOrderData(id, spawnId, name, cost, qty, sender, deliveryDestination, note, account, 0, securedDelivery); // RW-Edit

            // Approve it now
            order.SetApproverData(dest, sender);
            order.Approved = true;

            // Log order addition
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                    $"AddAndApproveOrder {note} added order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order.ProductId}, requester:{order.Requester}, deliveryDestination:{order.DeliveryDestination}]"); // RW-Edit

            // Add it to the list
            return TryAddOrder(dbUid, account, order, component) && TryFulfillOrder(stationData, account, order, component).HasValue;
        }

        public bool TryAddOrder(EntityUid dbUid, ProtoId<CargoAccountPrototype> account, CargoOrderData data, StationCargoOrderDatabaseComponent component)
        {
            component.Orders[account].Add(data);
            UpdateOrders(dbUid);
            return true;
        }

        public static int GenerateOrderId(StationCargoOrderDatabaseComponent orderDB)
        {
            // We need an arbitrary unique ID to identify orders, since they may
            // want to be cancelled later.
            return ++orderDB.NumOrdersCreated;
        }

        public void RemoveOrder(EntityUid dbUid, ProtoId<CargoAccountPrototype> account, int index, StationCargoOrderDatabaseComponent orderDB)
        {
            var sequenceIdx = orderDB.Orders[account].FindIndex(order => order.OrderId == index);
            if (sequenceIdx != -1)
            {
                orderDB.Orders[account].RemoveAt(sequenceIdx);
            }
            UpdateOrders(dbUid);
        }

        public void ClearOrders(StationCargoOrderDatabaseComponent component)
        {
            if (component.Orders.Count == 0)
                return;

            component.Orders.Clear();
        }

        private static bool PopFrontOrder(StationCargoOrderDatabaseComponent orderDB, ProtoId<CargoAccountPrototype> account, [NotNullWhen(true)] out CargoOrderData? orderOut)
        {
            var orderIdx = orderDB.Orders[account].FindIndex(order => order.Approved);
            if (orderIdx == -1)
            {
                orderOut = null;
                return false;
            }

            orderOut = orderDB.Orders[account][orderIdx];
            orderOut.NumDispatched++;

            if (orderOut.NumDispatched >= orderOut.OrderQuantity)
            {
                // Order is complete. Remove from the queue.
                orderDB.Orders[account].RemoveAt(orderIdx);
            }
            return true;
        }

        /// <summary>
        /// Tries to fulfill the next outstanding order.
        /// </summary>
        [PublicAPI]
        private bool FulfillNextOrder(StationCargoOrderDatabaseComponent orderDB, ProtoId<CargoAccountPrototype> account, EntityCoordinates spawn, string? paperProto)
        {
            if (!PopFrontOrder(orderDB, account, out var order))
                return false;

            return FulfillOrder(order, account, spawn, paperProto);
        }

        /// <summary>
        /// Fulfills the specified cargo order and spawns paper attached to it.
        /// </summary>
        private bool FulfillOrder(CargoOrderData order, ProtoId<CargoAccountPrototype> account, EntityCoordinates spawn, string? paperProto, out EntityUid? nextCrate, out List<string>? excessItemsOut, EntityUid? previousCrate = null, List<string>? excessItemsIn = null) // RW-Edit
        {
            // RW-Edit-Start
            EntityUid? item;
            nextCrate = null;
            excessItemsOut = null;

            if (!_protoMan.TryIndex(account, out var accountProto))
                return false;

            var productProto = _protoMan.Index<EntityPrototype>(order.ProductId);

            if (previousCrate is not null)
                item = previousCrate;
            else if (order.SecuredDelivery && accountProto.SecureCratePrototype is not null
                && (productProto.TryGetComponent<ItemComponent>(out var itemComponent) || productProto.HasComponent<EntityStorageComponent>()))
            {
                item = Spawn(accountProto.SecureCratePrototype, spawn);

                if (itemComponent is not null)
                    _entityStorage.Insert(Spawn(productProto.ID), item.Value);
            }
            else
            {
                item = EntityManager.CreateEntityUninitialized(productProto.ID, spawn);

                RemComp<StorageFillComponent>(item.Value);

                EntityManager.InitializeAndStartEntity(item.Value);
            }

            if (productProto.TryGetComponent<StorageFillComponent>(out var storageFill)
                && TryComp<EntityStorageComponent>(item, out var crateEntityStorage))
            {
                nextCrate = item;

                var entitiesExcess = new List<string>();
                var doExcessFill = false;

                if (excessItemsIn is not null)
                {
                    foreach (var excessItem in excessItemsIn)
                    {
                        _storage.Insert(item.Value, Spawn(excessItem), out _);
                    }
                }

                var spawns = EntitySpawnCollection.GetSpawns(storageFill.Contents, _random);
                foreach (var contentItem in spawns)
                {
                    if (crateEntityStorage.Contents.Count >= crateEntityStorage.Capacity || spawns.Count > crateEntityStorage.Capacity) // RW-Edit
                        doExcessFill = true;

                    if (doExcessFill)
                    {
                        entitiesExcess.Add(contentItem);
                        continue;
                    }

                    _entityStorage.Insert(Spawn(contentItem), item.Value);
                }

                if (entitiesExcess.Count != 0)
                    excessItemsOut = entitiesExcess;
            }

            // Ensure the item doesn't start anchored
            _transformSystem.Unanchor(item.Value, Transform(item.Value));

            if (previousCrate is not null)
                return true;

            var printed = Spawn(paperProto, spawn);

            if (!TryComp<PaperComponent>(printed, out var paper))
                return true;

            var itemName = productProto.Name;

            // fill in the order data
            var val = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", order.OrderId), ("detailName", itemName), ("detailQuantity", order.OrderQuantity));

            _metaSystem.SetEntityName(printed, val);
            _paperSystem.SetContent((printed, paper),
                Loc.GetString(
                    "cargo-console-paper-print-text",
                    ("orderNumber", order.OrderId),
                    ("itemName", itemName),
                    ("orderQuantity", order.OrderQuantity),
                    ("requester", order.Requester),
                    ("destination", string.IsNullOrWhiteSpace(order.DeliveryDestination) ? Loc.GetString("cargo-console-paper-delivery-destination-default") : order.DeliveryDestination), // CorvaxGoob-CargoFeatures
                    ("note", string.IsNullOrWhiteSpace(order.Note) ? Loc.GetString("cargo-console-paper-note-default") : order.Note), // CorvaxGoob-CargoFeatures
                    ("account", Loc.GetString(accountProto.Name)),
                    ("accountcode", Loc.GetString(accountProto.Code)),
                    ("approver", string.IsNullOrWhiteSpace(order.Approver) ? Loc.GetString("cargo-console-paper-approver-default") : order.Approver), // RW-Edit
                    ("privateBuyerLine", order.PaidPrivately ? Loc.GetString("cargo-console-paper-private-buyer", ("buyer", order.PrivateBuyerName ?? string.Empty)) : string.Empty))); // RW

            // attempt to attach the label to the item
            if (TryComp<PaperLabelComponent>(item, out var label))
                _slots.TryInsert(item.Value, label.LabelSlot, printed, null);

            return true;
            // RW-Edit-End
        }

        // RW-Start
        private bool FulfillOrder(CargoOrderData order, ProtoId<CargoAccountPrototype> account, EntityCoordinates spawn, string? paperProto)
        {
            return FulfillOrder(order, account, spawn, paperProto, out _, out _);
        }
        // RW-End

        public List<ProtoId<CargoProductPrototype>> GetAvailableProducts(Entity<CargoOrderConsoleComponent> ent)
        {
            if (_station.GetOwningStation(ent) is not { } station ||
                !TryComp<StationCargoOrderDatabaseComponent>(station, out var db))
            {
                return new List<ProtoId<CargoProductPrototype>>();
            }

            var products = new List<ProtoId<CargoProductPrototype>>();

            // Note that a market must be both on the station and on the console to be available.
            var markets = ent.Comp.AllowedGroups.Intersect(db.Markets).ToList();
            foreach (var product in _protoMan.EnumeratePrototypes<CargoProductPrototype>())
            {
                if (!markets.Contains(product.Group))
                    continue;

                products.Add(product.ID);
            }

            return products;
        }

        #region Station

        public bool TryGetOrderDatabase([NotNullWhen(true)] EntityUid? stationUid, [MaybeNullWhen(false)] out StationCargoOrderDatabaseComponent dbComp)
        {
            return TryComp(stationUid, out dbComp);
        }

        #endregion
    }
}
