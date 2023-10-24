using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    internal static class Logger
    {
        public static PrintMessage Echo;
        public static IMyGridTerminalSystem GridTerminalSystem;

        public delegate void PrintMessage(string message);

        const string TEXT_PANEL = "LCD Rover Drill";

        public static void Log(string text, bool append = true)
        {
            var TextPanel = GridTerminalSystem.GetBlockWithName(TEXT_PANEL) as IMyTextPanel;
            if (TextPanel != null)
            {
                TextPanel.WriteText(text + "\n", append);
            }
            else
            {
                Echo(text);
            }
        }
    }
}
