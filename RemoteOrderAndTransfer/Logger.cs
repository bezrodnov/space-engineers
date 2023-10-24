using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI.Ingame;
using System;
using System.Linq;
using VRage.Game.GUI.TextPanel;

namespace IngameScript
{
    public class Logger
    {
        private IMyTextPanel _textPanel;

        public Logger(IMyTextPanel textPanel)
        {
            _textPanel = textPanel;
        }

        public void Log(string message, bool append = true)
        {
            _textPanel.ContentType = ContentType.TEXT_AND_IMAGE;
            _textPanel.WriteText(message + "\n", append);

            //var text = _textPanel.GetText();
            //if (text != null)
            //{
            //    var lines = text.Split('\n');
            //    if (lines.Length > 10)
            //    {
            //        var newLines = new string[10];
            //        Array.Copy(lines, lines.Length - 11, newLines, 0, 10);
            //        _textPanel.WriteText(String.Join("\n", newLines) + "\n");
            //    }
            //}
        }
    }
}
