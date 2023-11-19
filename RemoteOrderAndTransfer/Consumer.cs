using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    internal class Consumer
    {
        private readonly IMyIntergridCommunicationSystem IGC;
        private readonly IMyGridTerminalSystem GridTerminalSystem;
        private readonly Logger logger;
        private readonly IMyGridProgramRuntimeInfo _runtime;

        private readonly IMyBroadcastListener _myBroadcastListener;
        private long _messageTargetId;
        private IMyProgrammableBlock Me { get; }

        private readonly Dictionary<string, long> itemsToOrder;

        public Consumer(Program program)
        {
            logger = program.logger;
            IGC = program.IGC;
            GridTerminalSystem = program.GridTerminalSystem;
            Me = program.Me;
            _runtime = program.Runtime;

            _myBroadcastListener = IGC.RegisterBroadcastListener(Config.MESSAGE_TAG_BROADCAST);
            _myBroadcastListener.SetMessageCallback();

            IGC.UnicastListener.SetMessageCallback();

            itemsToOrder = ParseOrder(Me.CustomData);
            SetDefaultOrderData();

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
                case UpdateType.Trigger:
                case UpdateType.Terminal:
                    {
                        if (Config.COMMAND_CONSUMER_ORDER.Equals(argument))
                        {
                            SendIdToProvider();
                        }
                    }
                    break;
            }
        }

        private void AcceptMessages()
        {
            while (_myBroadcastListener.HasPendingMessage)
            {
                var message = _myBroadcastListener.AcceptMessage();
                if (Config.MESSAGE_TAG_BROADCAST.Equals(message.Tag))
                {
                    _messageTargetId = long.Parse(message.Data.ToString());
                    Log($"received broadcast message from {_messageTargetId}");
                    PlaceOrder();
                    Log("Order was placed!");
                }
            }
        }

        private void SendIdToProvider()
        {
            Log($"sending id to provider");
            IGC.SendBroadcastMessage(Config.MESSAGE_TAG_BROADCAST, Me.EntityId);
        }

        private void PlaceOrder()
        {
            var orderStr = Me.CustomData;
            IGC.SendUnicastMessage(_messageTargetId, Config.MESSAGE_TAG_UNICAST_ORDER, itemsToOrder.ToImmutableDictionary());
        }

        private Dictionary<string, long> ParseOrder(string orderStr)
        {
            var orderLines = orderStr.Split('\n');

            var order = new Dictionary<string, long>();
            foreach (var orderLine in orderLines)
            {
                if (orderLine.Trim().Length > 0)
                {
                    var itemAndQuantity = orderLine.Split(' ');
                    if (itemAndQuantity.Length == 2)
                    {
                        var itemDisplayName = itemAndQuantity[0].Trim();
                        var itemType = Program.GetItemType(itemDisplayName);
                        if (itemType.HasValue)
                        {
                            var quantity = long.Parse(itemAndQuantity[1].Trim());
                            order.Add(Config.ITEM_TYPE_TO_NAME[itemType.Value], quantity);
                        }
                    }
                }
            }

            return order;
        }


        private void SetDefaultOrderData()
        {
            var itemTypes = Enum.GetValues(typeof(ItemType));

            var defaultOrder = new List<string>();

            foreach (ItemType itemType in itemTypes)
            {
                var itemName = Config.ITEM_TYPE_TO_NAME[itemType];
                if (!itemsToOrder.ContainsKey(itemName))
                {
                    var quantity = Config.DEFAULT_QUANTITIES.ContainsKey(itemType) ? Config.DEFAULT_QUANTITIES[itemType] : 0;
                    var displayName = (Config.IS_RU ? Config.RU_ITEM_NAMES : Config.EN_ITEM_NAMES)[itemType];
                    defaultOrder.Add($"{displayName} {quantity}");
                    
                    if (quantity > 0)
                    {
                        // save the same value in the initial order
                        itemsToOrder.Add(itemName, quantity);
                    }
                }
            }

            Me.CustomData = Me.CustomData + "\n\n" + string.Join("\n", defaultOrder);
        }

        private void Log(string message, bool append = true)
        {
            logger.Log("consumer::" + message, append);
        }
    }
}
