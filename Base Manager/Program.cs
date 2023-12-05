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

    partial class Program : MyGridProgram
    {
        public static readonly float POWER_RATIO_SAFETY_THRESHOLD = 0.2f;
        public static readonly string DISABLE_AUTO_TURN_ON_TAG = "disable_auto_turn_on";
        public static readonly string POWER_STATS_DISPLAYS_TAG = "power_stats";
        public static readonly string ORE_STATS_DISPLAYS_TAG = "ore_stats";
        public static readonly string COMPONENT_STATS_DISPLAYS_TAG = "component_stats";
        public static readonly string INVENTORY_MANAGEMENT_IGNORE_TAG = "-inventory";

        public static readonly bool PRINT_DEBUG = true;

        public Dictionary<string, HashSet<IMyShipConnector>> _connectorsByTags;
        public Dictionary<string, HashSet<IMyTextPanel>> _textPannelsByTags;
        public Dictionary<string, HashSet<IMyCockpit>> CockpitsByTags;

        public Dictionary<string, HashSet<IMyBatteryBlock>> BatteriesByTags;
        public List<IMyCargoContainer> CargoContainerBlocks;
        public List<IMyCargoContainer> CurrentBaseCargoContainerBlocks;
        public List<IMyAssembler> AssemblerBlocks;
        public List<IMyGasGenerator> GasGeneratorBlocks;
        public List<IMyRefinery> RefineryBlocks;
        public bool isEnergyBalanceEnabled = true;
        public bool isInventoryManagementEnabled = false;


        public PowerStats powerStats = new PowerStats(0, 0, 0, 0);
        public bool isEnergySafetyOn = false;

        private readonly TaskManager _taskManager;
        private int _maxInstructions = 0;

        public Program()
        {
            _taskManager = new TaskManager(this);
            _taskManager.Schedule(new SyncBlocksTask(this), 5000);
            _taskManager.Schedule(new CapturePowerStatsTask(this), 300, 10);
            _taskManager.Schedule(new PrintStatsTask(this), 300, 20);
            _taskManager.Schedule(new ManageConnectorsTask(this), 5000, 30);
            if (isEnergyBalanceEnabled)
            {
                _taskManager.Schedule(new ManageEnergyBalanceTask(this), 900, 40);
            }

            if (isInventoryManagementEnabled)
            {
                _taskManager.Schedule(new ManageInventoryTask(this), 1000, 50);
            }

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update1)
            {
                _taskManager.Run(1);
            }
            else if (updateSource == UpdateType.Update10) {
                _taskManager.Run(10);
            }
            else if (updateSource == UpdateType.Update100)
            {
                _taskManager.Run(100);
            }

            if (PRINT_DEBUG) {
                _maxInstructions = Math.Max(_maxInstructions, Runtime.CurrentInstructionCount);
                Echo($"Instructions executed: {Runtime.CurrentInstructionCount} (peek {_maxInstructions})");
            }
        }

        public void Troubleshoot(Exception e)
        {
            Log($"\n Ooops, something went wrong. {e.Message}\n{e.StackTrace}");
        }

        public void Log(string message, bool clearLog = false)
        {
            var appendNewLine = !clearLog && Me.GetSurface(0).GetText().Length < 3000;
            Me.GetSurface(0).WriteText($"{message}\n", appendNewLine);
        }
    }
}