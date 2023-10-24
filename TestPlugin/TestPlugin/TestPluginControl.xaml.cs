using System.Windows;
using System.Windows.Controls;

namespace TestPlugin
{
    public partial class TestPluginControl : UserControl
    {

        private TestPlugin Plugin { get; }

        private TestPluginControl()
        {
            InitializeComponent();
        }

        public TestPluginControl(TestPlugin plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Save();
        }
    }
}
