using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRageRender;

namespace IngameScript
{
    internal class Provider
    {
        private Program _program;
        private ImmutableDictionary<string, int> orderedItems;
        private Dictionary<string, int> processedItems;

        public Provider(Program program)
        {
            _program = program;
        }

        public void CheckOrdersToFulfill()
        {
            if (orderedItems != null)
            {
                if (IsOrderFulfilled())
                {
                    Log("Order was fulfilled!");
                    orderedItems = null;
                    processedItems = null;
                    return;
                }

                TransferOrderedItems();
            }
        }

        public void AcceptOrder(ImmutableDictionary<string, int> orderedItems)
        {
            this.orderedItems = orderedItems;
            processedItems = new Dictionary<string, int>();
            foreach (var orderedItem in orderedItems)
            {
                var orderedItemType = orderedItem.Key;
                if (!processedItems.ContainsKey(orderedItemType))
                {
                    processedItems.Add(orderedItemType, 0);
                }
            }

            ClearConnector();
        }

        private void TransferOrderedItems()
        {
            if (!IsConnectorEmpty())
            {
                Log("Waiting for connector to get emptied");
                return;
            }

            var connector = GetConnector();
            var connectorInventory = connector.GetInventory();
            if (!connector.IsConnected) { connector.Connect(); }

            foreach (var orderedItem in orderedItems)
            {
                var orderedItemType = orderedItem.Key;
                var orderedQuantity = orderedItem.Value;

                var remainingRequiredQuantity = orderedQuantity - processedItems[orderedItemType];
                if (remainingRequiredQuantity == 0)
                {
                    continue;
                }

                var containers = new List<IMyCargoContainer>();
                _program.GridTerminalSystem.GetBlocksOfType(containers);
                foreach (var container in containers)
                {
                    var inventory = container.GetInventory();
                    if (inventory.ItemCount == 0 || !inventory.IsConnectedTo(connectorInventory))
                    {
                        continue;
                    }

                    var containerItems = new List<MyInventoryItem>();
                    inventory.GetItems(containerItems);
                    foreach (var item in containerItems)
                    {
                        if (item.Type.ToString().EndsWith(orderedItemType))
                        {
                            Log($"found matching item: {orderedItemType}!");
                            var amountToTransfer = (VRage.MyFixedPoint)Math.Min(item.Amount.ToIntSafe(), remainingRequiredQuantity);
                            if (inventory.TransferItemTo(connectorInventory, item, amountToTransfer))
                            {
                                remainingRequiredQuantity -= amountToTransfer.ToIntSafe();
                                processedItems[orderedItemType] = amountToTransfer.ToIntSafe();
                                Log($"Transfered {amountToTransfer.ToIntSafe()} of {orderedItemType}\nNeed {remainingRequiredQuantity} more of these xD");
                                if (remainingRequiredQuantity == 0) break;
                            }
                        }
                    }

                    if (remainingRequiredQuantity == 0) break;
                }
            }

            connector.Disconnect();
            connector.CollectAll = true;
            Log("Collecting all");
        }

        private void ClearConnector()
        {
            var connector = GetConnector();
            connector.CollectAll = false;
            Log("Not collecting all");
            if (!connector.IsConnected) { connector.Connect(); }

            var connectorInventory = connector.GetInventory();
            var connectorInventoryItems = new List<MyInventoryItem>();
            connectorInventory.GetItems(connectorInventoryItems);
            if (connectorInventoryItems.Count == 0)
            {
                return;
            }

            var containers = new List<IMyCargoContainer>();
            _program.GridTerminalSystem.GetBlocksOfType(containers);
            foreach (var container in containers)
            {
                if (container.IsFunctional)
                {
                    var containerInventory = container.GetInventory();
                    if (connectorInventory.IsConnectedTo(containerInventory))
                    {
                        foreach (var item in connectorInventoryItems)
                        {
                            containerInventory.TransferItemFrom(connectorInventory, item, item.Amount);
                        }
                    }
                }
            }
        }

        private bool IsConnectorEmpty()
        {
            var connector = GetConnector();
            var connectorInventory = connector.GetInventory();
            var connectorInventoryItems = new List<MyInventoryItem>();
            connectorInventory.GetItems(connectorInventoryItems);
            return connectorInventoryItems.Count == 0;
        }

        private bool IsOrderFulfilled()
        {
            if (processedItems == null || orderedItems == null || orderedItems.Count != processedItems.Count)
            {
                return false;
            }

            foreach (var orderedItem in processedItems)
            {
                if (orderedItems[orderedItem.Key] != orderedItem.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private IMyShipConnector GetConnector()
        {
            return Utils.GetBlock<IMyShipConnector>(_program.GridTerminalSystem, Program.PROVIDER_CONNECTOR, "Connector");
        }

        private void Log(string message, bool append = true)
        {
            _program.logger.Log(message, append);
        }
    }
}
