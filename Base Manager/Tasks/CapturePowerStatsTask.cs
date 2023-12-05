using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript.Tasks
{
    class CapturePowerStatsTask : Task
    {
        readonly Program _program;

        public CapturePowerStatsTask(Program program)
        {
            _program = program;
        }

        string Task.Id
        {
            get
            {
                return "Capture Power Stats";
            }
        }

        string Task.Name
        {
            get
            {
                return "Capture Power Stats";
            }
        }

        void Task.Run()
        {
            _program.powerStats.Capacity = 0.0;
            _program.powerStats.Stored = 0.0;
            _program.powerStats.Production = 0.0;
            _program.powerStats.Consumption = 0.0;

            foreach (var Batteries in _program.BatteriesByTags.Values)
            {
                foreach (var Battery in Batteries)
                {
                    if (Battery.IsWorking)
                    {
                        _program.powerStats.Production += Battery.CurrentInput;
                        _program.powerStats.Consumption += Battery.CurrentOutput;
                        _program.powerStats.Capacity += Battery.MaxStoredPower;
                        _program.powerStats.Stored += Battery.CurrentStoredPower;
                    }
                }
            }
        }
    }
}
