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
        public static readonly bool IS_CONSUMER = false;
        public static readonly bool IS_PROVIDER = false;
        public static readonly bool IS_RU = true; 
        public static readonly string COMMAND_CONSUMER_ORDER = "order";
        public static readonly string TEXT_PANEL = "LCD [sex_Trans]";
        public static readonly string PROVIDER_CONNECTOR = "Connector [sex_Trans]";
        public static readonly string MESSAGE_TAG_BROADCAST = "sex_Trans::broadcast::handshake";
        public static readonly string MESSAGE_TAG_UNICAST_ORDER = "sex_Trans::unicast::order";
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

        private readonly Consumer _consumer;
        private readonly Provider _provider;

        public Program()
        {
            logger = new Logger(GetTextPanel());

            if (IS_CONSUMER)
            {
                _consumer = new Consumer(this);
            }

            if (IS_PROVIDER)
            {
                _provider = new Provider(this);
            }
        }

        public void Main(string argument, UpdateType updateType)
        {
            _provider?.Main(argument, updateType);
            _consumer?.Main(argument, updateType);
        }

        private IMyTextPanel GetTextPanel()
        {
            return Utils.GetBlock<IMyTextPanel>(GridTerminalSystem, TEXT_PANEL, "Text Panel");
        }
    }
}
