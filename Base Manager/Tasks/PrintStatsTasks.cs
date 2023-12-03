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
    class PrintStatsTask : Task
    {
        private Program _program;

        public PrintStatsTask(Program program)
        {
            _program = program;
        }

        string Task.Id
        {
            get
            {
                return "Printing Stats";
            }
        }

        string Task.Name
        {
            get
            {
                return "Printing Stats";
            }
        }

        void Task.Run()
        {
            var InfoByTag = new Dictionary<string, string>()
            {
                { Program.POWER_STATS_DISPLAYS_TAG, GetEnergyStatus() },
                { Program.ORE_STATS_DISPLAYS_TAG, GetOreStatus() },
                { Program.COMPONENT_STATS_DISPLAYS_TAG, GetComponentStatus() },
            };

            var IsInfoPrinted = new Dictionary<string, bool>();

            var ClearedDisplays = new HashSet<IMyEntity>();

            foreach (var Tag in InfoByTag.Keys)
            {
                var Stats = InfoByTag[Tag];
                if (_program._textPannelsByTags.ContainsKey(Tag))
                {
                    foreach (var textPanel in _program._textPannelsByTags[Tag])
                    {
                        IsInfoPrinted[Tag] = true;

                        if (!ClearedDisplays.Contains(textPanel))
                        {
                            textPanel.WriteText("");
                            ClearedDisplays.Add(textPanel);
                        }
                        textPanel.WriteText(Stats, true);
                    }
                }

                if (_program.CockpitsByTags.ContainsKey(Tag))
                {
                    foreach (var cockpit in _program.CockpitsByTags[Tag])
                    {
                        var CenterPanel = cockpit.GetSurface(1);
                        if (!ClearedDisplays.Contains(cockpit))
                        {
                            CenterPanel.WriteText("");
                            ClearedDisplays.Add(cockpit);
                        }
                        CenterPanel.WriteText(Stats, true);
                        CenterPanel.FontSize = 0.58f;
                        CenterPanel.Font = "Monospace";
                        CenterPanel.ContentType = ContentType.TEXT_AND_IMAGE;
                    }
                }

                if (!IsInfoPrinted.ContainsKey(Tag) || !IsInfoPrinted[Tag])
                {
                    _program.Echo($"No text panels or cockpits found to print Info. Expected tag is: [{Tag}]");
                }
            }
        }

        private String GetEnergyStatus()
        {
            var powerStats = _program.powerStats;
            var EnergyBalance = powerStats.Production - powerStats.Consumption;
            var EstimatedTime = EnergyBalance < 0 ? powerStats.Stored / EnergyBalance : (powerStats.Capacity - powerStats.Stored) / EnergyBalance;
            var EstimatedTimeInSeconds = EstimatedTime * 3600;
            var EstimatedTimeString = Math.Abs(EstimatedTimeInSeconds) > 1 ? String.Format("Батареи {0} через {1}",
                EstimatedTimeInSeconds > 0 ? "зарядятся" : "разрядятся",
                Utils.FormatTime(Math.Abs(EstimatedTimeInSeconds))
             ) : "";
            var EnergyPercentage = powerStats.Stored / powerStats.Capacity;

            var sb = new StringBuilder();
            sb.AppendLine($"Энергия: {Utils.FormatNumber(powerStats.Stored)}/{Utils.FormatNumber(powerStats.Capacity)} MWh ({EnergyPercentage.ToString("P", System.Globalization.CultureInfo.InvariantCulture)})");
            sb.AppendLine($"Производство/Потребление: {Utils.FormatNumber(powerStats.Production)}/{Utils.FormatNumber(powerStats.Consumption)} MW");
            sb.AppendLine(EstimatedTimeString);
            sb.AppendLine(String.Format("Режим сбережения {0}", _program.isEnergySafetyOn ? "включен" : "выключен"));

            return sb.ToString();
        }

        private String GetOreStatus()
        {
            var OreAmountByType = new Dictionary<String, float>();
            var IngotAmountByType = new Dictionary<String, float>();

            var BlocksWithItems = new List<IMyTerminalBlock>();
            BlocksWithItems.AddRange(_program.RefineryBlocks);
            BlocksWithItems.AddRange(_program.CargoContainerBlocks);
            BlocksWithItems.AddRange(_program.AssemblerBlocks);

            foreach (var Container in BlocksWithItems)
            {
                var Items = new List<MyInventoryItem>();
                Container.GetInventory().GetItems(Items);
                if (Container is IMyProductionBlock)
                {
                    var OutputItems = new List<MyInventoryItem>();
                    ((IMyProductionBlock)Container).OutputInventory.GetItems(OutputItems);
                    Items.AddRange(OutputItems);
                }

                foreach (var Item in Items)
                {
                    if (Item.Type.GetItemInfo().IsOre)
                    {
                        var oreType = Item.Type.ToString().Substring(Item.Type.ToString().IndexOf("Ore/") + 4);
                        if (!OreAmountByType.ContainsKey(oreType))
                        {
                            OreAmountByType.Add(oreType, 0);
                        }
                        OreAmountByType[oreType] += Item.Amount.ToIntSafe();
                    }
                    else if (Item.Type.GetItemInfo().IsIngot)
                    {
                        var ingotType = Item.Type.ToString().Substring(Item.Type.ToString().IndexOf("Ingot/") + 6);
                        if (!IngotAmountByType.ContainsKey(ingotType))
                        {
                            IngotAmountByType.Add(ingotType, 0);
                        }
                        IngotAmountByType[ingotType] += Item.Amount.ToIntSafe();
                    }
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("╔════════════════════╦════════════════════╗");
            sb.AppendLine(String.Format("║ {0,-19}║ {1, -19}║", "Руда", "Слитки"));
            sb.AppendLine("╠════════════════════╬════════════════════╣");

            var length = Math.Max(OreAmountByType.Count, IngotAmountByType.Count);

            for (var i = 0; i < length; i++)
            {
                var ore = i < OreAmountByType.Count ? Utils.FormatResourceAmount(OreAmountByType.ElementAt(i).Key, OreAmountByType.ElementAt(i).Value) : "";
                var ingot = i < IngotAmountByType.Count ? Utils.FormatResourceAmount(IngotAmountByType.ElementAt(i).Key, IngotAmountByType.ElementAt(i).Value) : "";
                sb.AppendLine(String.Format("║ {0,-19}║ {1,-19}║", ore, ingot));
            }
            sb.AppendLine("╚════════════════════╩════════════════════╝");

            return sb.ToString();
        }

        private String GetComponentStatus()
        {
            var ComponentAmountByType = new Dictionary<String, float>();

            var BlocksWithItems = new List<IMyTerminalBlock>();
            BlocksWithItems.AddRange(_program.RefineryBlocks);
            BlocksWithItems.AddRange(_program.CargoContainerBlocks);
            BlocksWithItems.AddRange(_program.AssemblerBlocks);

            foreach (var Container in BlocksWithItems)
            {
                var Items = new List<MyInventoryItem>();
                Container.GetInventory().GetItems(Items);
                if (Container is IMyProductionBlock)
                {
                    var OutputItems = new List<MyInventoryItem>();
                    ((IMyProductionBlock)Container).OutputInventory.GetItems(OutputItems);
                    Items.AddRange(OutputItems);
                }

                foreach (var Item in Items)
                {
                    if (Item.Type.GetItemInfo().IsComponent)
                    {
                        var componentType = Item.Type.ToString().Substring(Item.Type.ToString().IndexOf("Component/") + "Component/".Length);
                        if (!ComponentAmountByType.ContainsKey(componentType))
                        {
                            ComponentAmountByType.Add(componentType, 0);
                        }
                        ComponentAmountByType[componentType] += Item.Amount.ToIntSafe();
                    }
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("╔═════════════════════════════════════════╗");
            sb.AppendLine(String.Format("║ {0,-40}║", "Компоненты"));
            sb.AppendLine("╠═════════════════════════════════════════╣");

            for (var i = 0; i < ComponentAmountByType.Count; i++)
            {
                var componentInfo = Utils.FormatResourceAmount(ComponentAmountByType.ElementAt(i).Key, ComponentAmountByType.ElementAt(i).Value);
                sb.AppendLine(String.Format("║ {0,-40}║", componentInfo));
            }
            sb.AppendLine("╚═════════════════════════════════════════╝");

            return sb.ToString();
        }
    }
}
