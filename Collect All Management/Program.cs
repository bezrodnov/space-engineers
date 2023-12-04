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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        readonly static string CONNECTOR_NAME = "Connector [collect all]";
        readonly IMyShipConnector connector;

        public Program()
        {
            var connectors = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType(connectors, b => b.CubeGrid.Equals(Me.CubeGrid) && b.CustomName.Equals(CONNECTOR_NAME));
            connector = connectors.Count > 0 ? connectors[0] : null;

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (connector != null)
            {
                if (connector.GetInventory().IsFull && connector.CollectAll)
                {
                    // turn off collecting just once in case collector got bugged
                    connector.CollectAll = false;
                }
                else
                {
                    connector.CollectAll = !connector.IsConnected;
                }
            }
        }
    }
}
