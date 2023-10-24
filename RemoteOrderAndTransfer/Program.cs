using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // CONFIG
        public static readonly string COMMAND_CONSUMER_ORDER = "order";
        public static readonly string TEXT_PANEL = "LCD [sex_ROAT]";
        public static readonly string PROVIDER_CONNECTOR = "Connector [sex_ROAT]";
        public static readonly string MESSAGE_TAG_BROADCAST = "sex_ROAT::broadcast";
        public static readonly string MESSAGE_TAG_UNICAST = "sex_ROAT::unicast";
        public static readonly ImmutableDictionary<string, string> ITEM_NAME_TO_TYPE = new Dictionary<string, string>() {
            { "SteelPlate", "Component/SteelPlate" },

            { "Камень", "Ore/Stone" },
            { "Кремний", "Ore/Silicon" },
            { "Железо", "Ore/Iron" },
            { "Лёд", "Ore/Ice" },
            { "Никель", "Ore/Nickel" },
            { "Серебро", "Ore/Silver" },
            { "Золото", "Ore/Gold" },
            { "Платина", "Ore/Platinum" },
            { "Кобальт", "Ore/Cobalt" },
            { "Уран", "Ore/Uranium" },
            { "Скрап", "Ore/Scrap" },
            { "Магний", "Ore/Magnesium" },
        }.ToImmutableDictionary();
        // END OF CONFIG

        public Logger logger;
        private IMyBroadcastListener _myBroadcastListener;
        public long MessageTargetId { get; private set; }

        private readonly Consumer _consumer;
        private readonly Provider _provider;

        public Program()
        {
            logger = new Logger(GetTextPanel());

            _myBroadcastListener = IGC.RegisterBroadcastListener(MESSAGE_TAG_BROADCAST);

            if ("consumer".Equals(Me.CustomData))
            {
                _consumer = new Consumer(this);
            }
            
            if ("provider".Equals(Me.CustomData))
            {
                _provider = new Provider(this);
            }

            if (IsProvider())
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }

            logger.Log("<< Initialized >>", false);
            var role = IsConsumer() ? "consumer" : "provider";
            logger.Log($"I am {role}");
        }

        public void Main(string argument)
        {
            AcceptMessages();

            if (argument != null && IsConsumer() && COMMAND_CONSUMER_ORDER.Equals(argument))
            {
                SendHandshakeMessage();
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }

            if (_provider != null) {
                _provider.CheckOrdersToFulfill();
            }
        }

        void AcceptMessages()
        {
            while (IGC.UnicastListener.HasPendingMessage)
            {
                var message = IGC.UnicastListener.AcceptMessage();
                logger.Log($"received unicast message: {message.Data}");
                if (IsProvider())
                {
                    var orderedItems = message.Data as ImmutableDictionary<string, int>;
                    _provider.AcceptOrder(orderedItems);
                }
            }

            while (_myBroadcastListener.HasPendingMessage)
            {
                var message = _myBroadcastListener.AcceptMessage();
                if (MESSAGE_TAG_BROADCAST.Equals(message.Tag))
                {
                    MessageTargetId = long.Parse(message.Data.ToString());
                    logger.Log($"received broadcast message from {MessageTargetId}");

                    if (IsProvider())
                    {
                        SendHandshakeMessage();
                    }
                    
                    if (IsConsumer())
                    {
                        _consumer.PlaceOrder();
                        logger.Log("Order was placed!");
                    }
                }
            }
        }

        public void SendHandshakeMessage() {
            IGC.SendBroadcastMessage(MESSAGE_TAG_BROADCAST, Me.EntityId);
        }

        public IMyTextPanel GetTextPanel()
        {
            return Utils.GetBlock<IMyTextPanel>(GridTerminalSystem, TEXT_PANEL, "Text Panel");
        }

        bool IsConsumer() { return _consumer != null; }

        bool IsProvider() { return _provider != null; }
    }
}
