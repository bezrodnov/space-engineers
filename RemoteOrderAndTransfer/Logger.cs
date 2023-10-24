using Sandbox.ModAPI.Ingame;
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
        }
    }
}
