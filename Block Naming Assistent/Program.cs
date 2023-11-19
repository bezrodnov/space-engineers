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
        private delegate void RenamingFunction();
        List<RenamingFunction> renamingFunctions = new List<RenamingFunction>();
        int currentRenamingFunctionIndex = -1;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            renamingFunctions.Add(RenameRemoteControls);
            renamingFunctions.Add(RenameConnectors);
            renamingFunctions.Add(RenameCollectors);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update100)
            {
                currentRenamingFunctionIndex++;
                if (currentRenamingFunctionIndex >= renamingFunctions.Count)
                {
                    currentRenamingFunctionIndex = 0;
                }

                renamingFunctions[currentRenamingFunctionIndex]();
            }
        }

        private void RenameRemoteControls()
        {
            Echo("Renaming remote controls...\n");

            var remoteControls = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType(remoteControls);
            remoteControls.ForEach(block =>
            {
                var suffix = $" [{block.CubeGrid.CustomName}]";
                if (!block.CustomName.EndsWith(suffix))
                {
                    block.CustomName = block.CustomName + suffix;
                }
            });
        }

        private void RenameConnectors()
        {
            Echo("Renaming connectors...\n");

            var connectors = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType(connectors);
            connectors.ForEach(block =>
            {
                if (block.IsConnected && block.CubeGrid.Equals(Me.CubeGrid))
                {
                    block.CustomName = $"Connector [{block.OtherConnector.CubeGrid.CustomName}]";
                }
            });
        }

        private void RenameCollectors()
        {
            Echo("Renaming collectors...\n");

            var collectors = new List<IMyCollector>();
            GridTerminalSystem.GetBlocksOfType(collectors);
            collectors.ForEach(block =>
            {
                if (block is IMyShipConnector)
                {
                    return;
                }
                block.CustomName = $"Collector [{block.CubeGrid.CustomName}]";
            });
        }
    }
}
