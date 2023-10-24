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
        private Action<string> _echo;

        public Logger(IMyTextPanel textPanel, Action<string> echo)
        {
            _textPanel = textPanel;
            _echo = echo;
        }

        public void Log(string message, bool append = true)
        {
            if (_textPanel != null)
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
            else
            {
                _echo(message);
            }

        }

        public void Clear()
        {
            _textPanel?.WriteText("");
        }
    }
}
