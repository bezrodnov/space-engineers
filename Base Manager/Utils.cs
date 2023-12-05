using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using IngameScript.Tasks;

namespace IngameScript
{
    class Utils
    {
        public static string getBlockName(IMyEntity block)
        {
            return block is IMyTerminalBlock ? ((IMyTerminalBlock)block).CustomName : block.DisplayName;
        }

        public static string FormatNumber(double value)
        {
            return string.Format("{0:0.00}", value);
        }

        public static string FormatResourceAmount(string resourceType, float amount, int labelLength)
        {
            return string.Format("{0,-" + labelLength + "}{1,7} ", I18N(resourceType), FormatNumberNice(amount, 3));
        }

        public static string FormatTime(double seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);
            var parts = new List<string>();
            if (timeSpan.Hours != 0)
            {
                parts.Add($"{timeSpan.Hours} ч");
            }
            if (timeSpan.Minutes != 0)
            {
                parts.Add($"{timeSpan.Minutes} м");
            }
            if (timeSpan.Seconds != 0)
            {
                parts.Add($"{timeSpan.Seconds} сек");
            }

            return string.Join(", ", parts);
        }

        private static Dictionary<string, string> I18NMapping = new Dictionary<string, string>() {
           { "Stone", "Камень" },
           { "Silicon", "Кремний" },
           { "Iron", "Железо" },
           { "Ice", "Лёд" },
           { "Nickel", "Никель" },
           { "Silver", "Серебро" },
           { "Gold", "Золото" },
           { "Platinum", "Платина" },
           { "Cobalt", "Кобальт" },
           { "Uranium", "Уран" },
           { "Scrap", "Скрап" },
           { "Magnesium", "Магний" },
           { "Sand", "Песок" },
        };

        private static string I18N(string text)
        {
            return I18NMapping.ContainsKey(text) ? I18NMapping[text] : text;
        }

        private static string[] prefixes = { "f", "a", "p", "n", "μ", "m", string.Empty, "k", "M", "G", "T", "P", "E" };
        private static string FormatNumberNice(double x, int significant_digits)
        {
            //Check for special numbers and non-numbers
            if (double.IsInfinity(x) || double.IsNaN(x) || x == 0 || significant_digits <= 0)
            {
                return x.ToString();
            }
            // extract sign so we deal with positive numbers only
            int sign = Math.Sign(x);
            x = Math.Abs(x);
            // get scientific exponent, 10^3, 10^6, ...
            int sci = x == 0 ? 0 : (int)Math.Floor(Math.Log(x, 10) / 3) * 3;
            // scale number to exponent found
            x = x * Math.Pow(10, -sci);
            // find number of digits to the left of the decimal
            int dg = x == 0 ? 0 : (int)Math.Floor(Math.Log(x, 10)) + 1;
            // adjust decimals to display
            int decimals = Math.Min(significant_digits - dg, 15);
            // format for the decimals
            string fmt = new string('0', decimals);
            if (sci == 0)
            {
                //no exponent
                return string.Format("{0}{1:0." + fmt + "}",
                    sign < 0 ? "-" : string.Empty,
                    Math.Round(x, decimals));
            }
            // find index for prefix. every 3 of sci is a new index
            int index = sci / 3 + 6;
            if (index >= 0 && index < prefixes.Length)
            {
                // with prefix
                return string.Format("{0}{1:0." + fmt + "}{2}",
                    sign < 0 ? "-" : string.Empty,
                    Math.Round(x, decimals),
                    prefixes[index]);
            }
            // with 10^exp format
            return string.Format("{0}{1:0." + fmt + "}·10^{2}",
                sign < 0 ? "-" : string.Empty,
                Math.Round(x, decimals),
                sci);
        }
    }
}
