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
        private readonly IMyIntergridCommunicationSystem IGC;
        private readonly IMyGridTerminalSystem GridTerminalSystem;
        private readonly Logger logger;
        private readonly IMyGridProgramRuntimeInfo _runtime;

        private readonly IMyBroadcastListener _myBroadcastListener;
        private long _messageTargetId;

        private ImmutableDictionary<string, int> orderedItems;
        private Dictionary<string, int> processedItems;
        private IMyProgrammableBlock Me { get; }

        public Provider(Program program)
        {
            logger = program.logger;
            IGC = program.IGC;
            GridTerminalSystem = program.GridTerminalSystem;
            Me = program.Me;
            _runtime = program.Runtime;

            _myBroadcastListener = IGC.RegisterBroadcastListener(Program.MESSAGE_TAG_BROADCAST);
            _myBroadcastListener.SetMessageCallback();

            IGC.UnicastListener.SetMessageCallback();

            logger.Clear();
        }

        public void Main(string argument, UpdateType updateType)
        {
            switch (updateType)
            {
                case UpdateType.IGC:
                    {
                        AcceptMessages();
                        break;
                    }
                case UpdateType.Update100:
                    {
                        CheckOrdersToFulfill();
                        break;
                    }
            }
        }

        private void AcceptMessages()
        {
            while (IGC.UnicastListener.HasPendingMessage)
            {
                var message = IGC.UnicastListener.AcceptMessage();
                Log($"received unicast message: {message.Data}");
                var orderedItems = message.Data as ImmutableDictionary<string, int>;
                AcceptOrder(orderedItems);
            }

            while (_myBroadcastListener.HasPendingMessage)
            {
                var message = _myBroadcastListener.AcceptMessage();
                if (Program.MESSAGE_TAG_BROADCAST.Equals(message.Tag))
                {
                    _messageTargetId = long.Parse(message.Data.ToString());
                    Log($"received broadcast message from {_messageTargetId}");
                    SendIdToConsumer();
                }
            }
        }

        private void SendIdToConsumer()
        {
            Log($"sending id to consumer");
            IGC.SendBroadcastMessage(Program.MESSAGE_TAG_BROADCAST, Me.EntityId);
        }

        private void CheckOrdersToFulfill()
        {
            if (orderedItems != null)
            {
                if (IsOrderFulfilled())
                {
                    Log("Order was fulfilled!");
                    orderedItems = null;
                    processedItems = null;
                    CollectAll(false);
                    _runtime.UpdateFrequency = UpdateFrequency.None;
                    return;
                }

                TransferOrderedItems();
            }
        }

        private void AcceptOrder(ImmutableDictionary<string, int> orderedItems)
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

            _runtime.UpdateFrequency = UpdateFrequency.Update100;

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
                GridTerminalSystem.GetBlocksOfType(containers);
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
            CollectAll(true);
        }

        private void ClearConnector()
        {
            var connector = GetConnector();
            CollectAll(false);
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
            GridTerminalSystem.GetBlocksOfType(containers);
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
            return Utils.GetBlock<IMyShipConnector>(GridTerminalSystem, Program.PROVIDER_CONNECTOR, "Connector");
        }

        private void CollectAll(bool collectAll)
        {
            GetConnector().CollectAll = collectAll;
            Log(collectAll ? "Collecting all" : "Not collecting all");
        }

        private void Log(string message, bool append = true)
        {
            logger.Log("provider::" + message, append);
        }
    }
}
