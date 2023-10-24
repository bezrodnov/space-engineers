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

        private ImmutableDictionary<string, int> orderedItems;

        public Consumer(Program program)
        {
            logger = program.logger;
            IGC = program.IGC;
            GridTerminalSystem = program.GridTerminalSystem;
            Me = program.Me;
            _runtime = program.Runtime;

            _myBroadcastListener = IGC.RegisterBroadcastListener(Program.MESSAGE_TAG_BROADCAST);
            IGC.UnicastListener.SetMessageCallback();

            _runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateType)
        {
            //Log($"update type = {updateType}");

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
                        if (Program.COMMAND_CONSUMER_ORDER.Equals(argument))
                        {
                            SendIdToProvider();
                        }
                    }
                    break;
                default:
                    AcceptMessages();
                    break;
            }
        }

        private void AcceptMessages()
        {
            while (_myBroadcastListener.HasPendingMessage)
            {
                var message = _myBroadcastListener.AcceptMessage();
                if (Program.MESSAGE_TAG_BROADCAST.Equals(message.Tag))
                {
                    _messageTargetId = long.Parse(message.Data.ToString());
                    Log($"received broadcast message from {_messageTargetId}");
                    PlaceOrder();
                    Log("Order was placed!");
                }
            }
        }

        private void PlaceOrder()
        {
            var orderStr = Me.CustomData;
            orderedItems = ParseOrder(orderStr).ToImmutableDictionary();
            IGC.SendUnicastMessage(_messageTargetId, Program.MESSAGE_TAG_UNICAST_ORDER, orderedItems);
        }

        private Dictionary<string, int> ParseOrder(string orderStr)
        {
            var orderLines = orderStr.Split('\n');

            var order = new Dictionary<string, int>();
            foreach (var orderLine in orderLines)
            {
                if (orderLine.Trim().Length > 0)
                {
                    Log($"Order line: {orderLine}");
                    var itemAndQuantity = orderLine.Split(' ');
                    var item = itemAndQuantity[0];
                    var quantity = long.Parse(itemAndQuantity[1]);
                    order.Add(item, (int)quantity);
                }
            }

            return order;
        }

        private void SendIdToProvider()
        {
            Log($"sending id to provider");
            IGC.SendBroadcastMessage(Program.MESSAGE_TAG_BROADCAST, Me.EntityId);
        }
        private void Log(string message, bool append = true)
        {
            logger.Log("consumer::" + message, append);
        }

    }
}
