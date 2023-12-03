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

namespace IngameScript.Tasks
{
    class SyncBlocksTask : Task
    {
        private Program _program;
        private IMyGridTerminalSystem GridTerminalSystem
        {
            get
            {
                return _program.GridTerminalSystem;
            }
        }
        public SyncBlocksTask(Program program)
        {
            this._program = program;
        }

        string Task.Id
        {
            get
            {
                return "Syncing Blocks";
            }
        }

        string Task.Name
        {
            get
            {
                return "Syncing Blocks";
            }
        }

        void Task.Run()
        {
            var Connectors = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType(Connectors);
            _program._connectorsByTags = groupByTags(Connectors);

            var TextPannels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(TextPannels);
            _program._textPannelsByTags = groupByTags(TextPannels);

            var Cockpits = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType(Cockpits);
            _program.CockpitsByTags = groupByTags(Cockpits);

            _program.CargoContainerBlocks = new List<IMyCargoContainer>();
            GridTerminalSystem.GetBlocksOfType(_program.CargoContainerBlocks);

            var Batteries = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType(Batteries);
            _program.BatteriesByTags = groupByTags(Batteries);

            _program.AssemblerBlocks = new List<IMyAssembler>();
            GridTerminalSystem.GetBlocksOfType(_program.AssemblerBlocks);

            _program.GasGeneratorBlocks = new List<IMyGasGenerator>();
            GridTerminalSystem.GetBlocksOfType(_program.GasGeneratorBlocks);

            _program.RefineryBlocks = new List<IMyRefinery>();
            GridTerminalSystem.GetBlocksOfType(_program.RefineryBlocks);
        }

        private static Dictionary<string, HashSet<T>> groupByTags<T>(List<T> Blocks) where T : IMyTerminalBlock
        {
            var result = new Dictionary<string, HashSet<T>>();

            Blocks.ForEach(Block =>
            {
                var tags = getTags(Block);
                tags.ForEach(tag =>
                {
                    if (!result.ContainsKey(tag))
                    {
                        result.Add(tag, new HashSet<T>());
                    }
                    result[tag].Add(Block);
                });

                var joinedTag = String.Join("", tags);
                if (!result.ContainsKey(joinedTag))
                {
                    result.Add(joinedTag, new HashSet<T>());
                }
                result[joinedTag].Add(Block);
            });

            return result;
        }

        private static List<string> getTags(IMyTerminalBlock Block)
        {
            var blockTagRegex = new System.Text.RegularExpressions.Regex(@"(?<=\[).+?(?=\])");
            return blockTagRegex
                .Matches(Block.CustomName)
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(match => match.Value).ToList();
        }
    }
}
