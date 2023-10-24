using NLog;
using Sandbox;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using VRage.Game.ModAPI;

namespace TestPlugin
{
    public class TestPlugin : TorchPluginBase, IWpfPlugin
    {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly string CONFIG_FILE_NAME = "TestPluginConfig.cfg";

        private TestPluginControl _control;
        public UserControl GetControl() => _control ?? (_control = new TestPluginControl(this));

        private Persistent<TestPluginConfig> _config;
        public TestPluginConfig Config => _config?.Data;

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            SetupConfig();

            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");

            Save();
        }

        public override void Update()
        {
            if (MySandboxGame.Static.SimulationFrameCounter % 100 == 0)
            {
                Log.Info("Update at tick " + MySandboxGame.Static.SimulationFrameCounter);
                Log.Info(MyAPIGateway.Gui.GetCurrentScreen);
                if (MyAPIGateway.Session.Player != null)
                {
                    Log.Info(MyAPIGateway.Session.Player.ToString());
                }

                // var gpsList = MyAPIGateway.Session.GPS.GetGpsList(MyAPIGateway.Session.Player.IdentityId);
            }
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {

            switch (state)
            {

                case TorchSessionState.Loaded:
                    Log.Info("Session Loaded!");
                    break;

                case TorchSessionState.Unloading:
                    Log.Info("Session Unloading!");
                    break;
            }
        }

        private void SetupConfig()
        {

            var configFile = Path.Combine(StoragePath, CONFIG_FILE_NAME);

            try
            {

                _config = Persistent<TestPluginConfig>.Load(configFile);

            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (_config?.Data == null)
            {

                Log.Info("Create Default Config, because none was found!");

                _config = new Persistent<TestPluginConfig>(configFile, new TestPluginConfig());
                _config.Save();
            }
        }

        public void Save()
        {
            try
            {
                _config.Save();
                Log.Info("Configuration Saved.");
            }
            catch (IOException e)
            {
                Log.Warn(e, "Configuration failed to save");
            }
        }
    }
}
