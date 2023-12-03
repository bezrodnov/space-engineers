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
    class ManageEnergyBalanceTask : Task
    {
        Program _program;

        public ManageEnergyBalanceTask(Program program)
        {
            this._program = program;
        }

        string Task.Id
        {
            get
            {
                return "Managing Energy Balance";
            }
        }

        string Task.Name
        {
            get
            {
                return "Managing Energy Balance";
            }
        }

        void Task.Run()
        {
            var chargePercentage = _program.powerStats.Stored / (_program.powerStats.Capacity == 0 ? 1 : _program.powerStats.Capacity);
            _program.isEnergySafetyOn = chargePercentage < Program.POWER_RATIO_SAFETY_THRESHOLD;
            var isAtLeastOneSurvivalKitEnabled = false;

            var energyConsumingBlocks = new List<IMyFunctionalBlock>();
            energyConsumingBlocks.AddRange(_program.RefineryBlocks);
            energyConsumingBlocks.AddRange(_program.AssemblerBlocks);
            energyConsumingBlocks.AddRange(_program.GasGeneratorBlocks);

            foreach (var energyConsumingBlock in energyConsumingBlocks)
            {
                if (!energyConsumingBlock.IsFunctional)
                {
                    continue;
                }

                if (!isAtLeastOneSurvivalKitEnabled && energyConsumingBlock.CustomName.Contains("Survival Kit"))
                {
                    isAtLeastOneSurvivalKitEnabled = true;
                    energyConsumingBlock.Enabled = true;
                    continue;
                }

                if (!_program.isEnergySafetyOn && energyConsumingBlock.CustomName.Contains(Program.DISABLE_AUTO_TURN_ON_TAG))
                {
                    continue;
                }

                energyConsumingBlock.Enabled = !_program.isEnergySafetyOn;
            }
        }
    }
}
