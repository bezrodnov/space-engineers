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
        private Program _program;
        private ImmutableDictionary<string, int> orderedItems;

        public Consumer(Program program)
        {
            _program = program;
        }

        public void PlaceOrder()
        {
            var textPanel = _program.GetTextPanel();
            var orderStr = textPanel.CustomData;
            orderedItems = ParseOrder(orderStr).ToImmutableDictionary();

            _program.IGC.SendUnicastMessage(_program.MessageTargetId, Program.MESSAGE_TAG_UNICAST, orderedItems);
            _program.Runtime.UpdateFrequency = UpdateFrequency.None;
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
                    order.Add(item, (int) quantity);
                }
            }

            return order;
        }

        private void Log(string message, bool append = true)
        {
            _program.logger.Log(message, append);
        }
    }
}
