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
        public Logger logger;

        private readonly Consumer _consumer;
        private readonly Provider _provider;

        public Program()
        {
            logger = new Logger(GetTextPanel(), Echo);

            var options = Me.CustomData.Split('\n');
            foreach (var option in options)
            {
                var keyAndValue = option.Split('=');
                if (keyAndValue.Length == 2)
                {
                    var key = keyAndValue[0];
                    var value = keyAndValue[1];
                    if (key == "provider")
                    {
                        if (value.ToLower().Equals("true"))
                        {
                            _provider = new Provider(this);
                        }
                    }
                    else if (key.Equals("consumer"))
                    {
                        if (value.ToLower().Equals("true"))
                        {
                            _consumer = new Consumer(this);
                        }
                    }
                }
            }
        }

        public void Main(string argument, UpdateType updateType)
        {
            if ("clear_console".Equals(argument))
            {
                logger.Clear();
                return;
            }

            _provider?.Main(argument, updateType);
            _consumer?.Main(argument, updateType);
        }

        private IMyTextPanel GetTextPanel()
        {
            return Utils.FindBlock<IMyTextPanel>(GridTerminalSystem, Config.TEXT_PANEL);
        }

        public static ItemType? GetItemType(string displayName)
        {
            foreach (var item in Config.RU_ITEM_NAMES)
            {
                if (item.Value.Equals(displayName))
                {
                    return item.Key;
                }
            }

            foreach (var item in Config.EN_ITEM_NAMES)
            {
                if (item.Value.Equals(displayName))
                {
                    return item.Key;
                }
            }

            return null;
        }
    }
}
