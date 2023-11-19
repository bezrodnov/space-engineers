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
        public struct PowerStats
        {
            public PowerStats(double capacity, double production, double consumption, double stored)
            {
                Capacity = capacity;
                Production = production;
                Consumption = consumption;
                Stored = stored;
            }

            public double Capacity { get; set; }
            public double Production { get; set; }
            public double Consumption { get; set; }
            public double Stored { get; set; }
        }

        private static float POWER_RATIO_SAFETY_THRESHOLD = 0.9f;
        private static short SYNC_BLOCKS_EVERY_X_TICKS = 50;
        private static string DISABLE_AUTO_TURN_ON_TAG = "disable_auto_turn_on";

        private static string POWER_STATS_DISPLAYS_TAG = "power_stats";
        private static string ORE_STATS_DISPLAYS_TAG = "ore_stats";
        private static string COMPONENT_STATS_DISPLAYS_TAG = "component_stats";

        private Dictionary<string, HashSet<IMyShipConnector>> _connectorsByTags;
        private Dictionary<string, HashSet<IMyTextPanel>> _textPannelsByTags;
        private Dictionary<string, HashSet<IMyCockpit>> CockpitsByTags;

        private Dictionary<string, HashSet<IMyBatteryBlock>> BatteriesByTags;
        private List<IMyCargoContainer> CargoContainerBlocks;
        private List<IMyCargoContainer> CurrentBaseCargoContainerBlocks;
        private List<IMyAssembler> AssemblerBlocks;
        private List<IMyRefinery> RefineryBlocks;
        private bool isEnergyBalanceEnabled = true;

        private short ticksSinceLastSync = 0;
        private PowerStats powerStats = new PowerStats(0, 0, 0, 0);
        private bool isEnergySafetyOn = false;
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            SyncBlocks(true);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            SyncBlocks();

            CapturePowerStats();
            ManageConnectors();
            ManageEnergyBalance();
            PrintStats();
        }

        private void SyncBlocks(bool force = false)
        {
            if (force || ticksSinceLastSync >= SYNC_BLOCKS_EVERY_X_TICKS)
            {
                var Connectors = new List<IMyShipConnector>();
                GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(Connectors);
                _connectorsByTags = groupByTags(Connectors);

                var TextPannels = new List<IMyTextPanel>();
                GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(TextPannels);
                _textPannelsByTags = groupByTags(TextPannels);

                var Cockpits = new List<IMyCockpit>();
                GridTerminalSystem.GetBlocksOfType<IMyCockpit>(Cockpits);
                CockpitsByTags = groupByTags(Cockpits);

                CargoContainerBlocks = new List<IMyCargoContainer>();
                GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(CargoContainerBlocks);

                var Batteries = new List<IMyBatteryBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(Batteries);
                BatteriesByTags = groupByTags(Batteries);

                AssemblerBlocks = new List<IMyAssembler>();
                GridTerminalSystem.GetBlocksOfType<IMyAssembler>(AssemblerBlocks);

                RefineryBlocks = new List<IMyRefinery>();
                GridTerminalSystem.GetBlocksOfType<IMyRefinery>(RefineryBlocks);

                ticksSinceLastSync = 0;
            }
            ticksSinceLastSync++;
        }

        private void CapturePowerStats()
        {
            var capacity = 0.0;
            var stored = 0.0;
            var production = 0.0;
            var consumption = 0.0;

            foreach (var Batteries in BatteriesByTags.Values)
            {
                foreach (var Battery in Batteries)
                {
                    if (Battery.IsWorking)
                    {
                        production += Battery.CurrentInput;
                        consumption += Battery.CurrentOutput;
                        capacity += Battery.MaxStoredPower;
                        stored += Battery.CurrentStoredPower;
                    }
                }
            }

            powerStats = new PowerStats(capacity, production, consumption, stored);
        }

        private void PrintStats()
        {
            var InfoByTag = new Dictionary<string, string>()
            {
                { POWER_STATS_DISPLAYS_TAG, GetEnergyStatus() },
                { ORE_STATS_DISPLAYS_TAG, GetOreStatus() },
                { COMPONENT_STATS_DISPLAYS_TAG, GetComponentStatus() },
            };

            var ClearedDisplays = new HashSet<IMyEntity>();

            foreach (var Tag in InfoByTag.Keys)
            {
                var Stats = InfoByTag[Tag];
                if (_textPannelsByTags.ContainsKey(Tag))
                {
                    foreach (var textPanel in _textPannelsByTags[Tag])
                    {
                        if (!ClearedDisplays.Contains(textPanel))
                        {
                            textPanel.WriteText("");
                            ClearedDisplays.Add(textPanel);
                        }
                        textPanel.WriteText(Stats, true);
                    }
                }
                else
                {
                    Echo($"No text panels found to print Info. Expected tag is: [{Tag}]");
                }

                if (CockpitsByTags.ContainsKey(Tag))
                {
                    foreach (var cockpit in CockpitsByTags[Tag])
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
                else
                {
                    Echo($"No cockpits found to print Info. Expected tag is: [{Tag}]");
                }
            }
        }

        private String GetEnergyStatus()
        {
            var EnergyBalance = powerStats.Production - powerStats.Consumption;
            var EstimatedTime = EnergyBalance < 0 ? powerStats.Stored / EnergyBalance : (powerStats.Capacity - powerStats.Stored) / EnergyBalance;
            var EstimatedTimeInSeconds = EstimatedTime * 3600;
            var EstimatedTimeString = Math.Abs(EstimatedTimeInSeconds) > 1 ? String.Format("Батареи {0} через {1}",
                EstimatedTimeInSeconds > 0 ? "зарядятся" : "разрядятся",
                FormatTime(Math.Abs(EstimatedTimeInSeconds))
             ) : "";
            var EnergyPercentage = powerStats.Stored / powerStats.Capacity;

            var sb = new StringBuilder();
            sb.AppendLine($"Энергия: {FormatNumber(powerStats.Stored)}/{FormatNumber(powerStats.Capacity)} MWh ({EnergyPercentage.ToString("P", System.Globalization.CultureInfo.InvariantCulture)})");
            sb.AppendLine($"Производство/Потребление: {FormatNumber(powerStats.Production)}/{FormatNumber(powerStats.Consumption)} MW");
            sb.AppendLine(EstimatedTimeString);
            sb.AppendLine(String.Format("Режим сбережения {0}", isEnergySafetyOn ? "включен" : "выключен"));

            return sb.ToString();
        }

        private String GetOreStatus()
        {
            var OreAmountByType = new Dictionary<String, float>();
            var IngotAmountByType = new Dictionary<String, float>();

            var BlocksWithItems = new List<IMyTerminalBlock>();
            BlocksWithItems.AddRange(RefineryBlocks);
            BlocksWithItems.AddRange(CargoContainerBlocks);
            BlocksWithItems.AddRange(AssemblerBlocks);

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
                var ore = i < OreAmountByType.Count ? FormatResourceAmount(OreAmountByType.ElementAt(i).Key, OreAmountByType.ElementAt(i).Value) : "";
                var ingot = i < IngotAmountByType.Count ? FormatResourceAmount(IngotAmountByType.ElementAt(i).Key, IngotAmountByType.ElementAt(i).Value) : "";
                sb.AppendLine(String.Format("║ {0,-19}║ {1,-19}║", ore, ingot));
            }
            sb.AppendLine("╚════════════════════╩════════════════════╝");

            return sb.ToString();
        }

        private String GetComponentStatus()
        {
            var ComponentAmountByType = new Dictionary<String, float>();

            var BlocksWithItems = new List<IMyTerminalBlock>();
            BlocksWithItems.AddRange(RefineryBlocks);
            BlocksWithItems.AddRange(CargoContainerBlocks);
            BlocksWithItems.AddRange(AssemblerBlocks);

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
                var componentInfo = FormatResourceAmount(ComponentAmountByType.ElementAt(i).Key, ComponentAmountByType.ElementAt(i).Value);
                sb.AppendLine(String.Format("║ {0,-40}║", componentInfo));
            }
            sb.AppendLine("╚═════════════════════════════════════════╝");

            return sb.ToString();
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

        private void ManageConnectors()
        {
            foreach (var ConnectorTag in _connectorsByTags.Keys)
            {
                if (ConnectorTag != null && ConnectorTag.Length > 0)
                {
                    foreach (var Connector in _connectorsByTags[ConnectorTag])
                    {
                        if (Connector.IsSameConstructAs(Me))
                        {
                            Echo($"Connector {ConnectorTag} detected");
                            if (_textPannelsByTags.ContainsKey(ConnectorTag))
                            {
                                var TextPanel = _textPannelsByTags[ConnectorTag].First();
                                if (TextPanel != null)
                                {
                                    var IsConnected = Connector.Status == MyShipConnectorStatus.Connected;
                                    TextPanel.WriteText($"{ConnectorTag} {(IsConnected ? "подключен" : "отключен")}\n");

                                    if (IsConnected && BatteriesByTags.ContainsKey(ConnectorTag))
                                    {
                                        var Batteries = BatteriesByTags[ConnectorTag];
                                        if (Batteries.Count > 0)
                                        {
                                            foreach (var Battery in Batteries)
                                            {
                                                if (Battery.ChargeMode == ChargeMode.Recharge && Battery.CurrentStoredPower == Battery.MaxStoredPower)
                                                {
                                                    Battery.ChargeMode = ChargeMode.Auto;
                                                }
                                            }

                                            var BatteryModes = Batteries
                                                .Select(getBatteryChargeModeText)
                                                .Aggregate(new Dictionary<string, short>(), (dict, PowerMode) =>
                                                {
                                                    if (!dict.ContainsKey(PowerMode))
                                                    {
                                                        dict.Add(PowerMode, 0);
                                                    }
                                                    dict[PowerMode]++;
                                                    return dict;
                                                }).Aggregate(new List<string>(), (list, PowerModeWithCount) =>
                                                {
                                                    list.Add(PowerModeWithCount.Value > 1
                                                        ? $"{PowerModeWithCount.Key} ({PowerModeWithCount.Value}x)"
                                                        : PowerModeWithCount.Key
                                                    );
                                                    return list;
                                                });

                                            TextPanel.WriteText($"Режим батарей: ", true);
                                            TextPanel.WriteText(String.Join(", ", BatteryModes), true);

                                            var StoredPower = Batteries.Aggregate(0f, (Power, Battery) => Power + Battery.CurrentStoredPower);
                                            var MaxPower = Batteries.Aggregate(0f, (Power, Battery) => Power + Battery.MaxStoredPower);
                                            TextPanel.WriteText($"\nБатареи заряжены на {Math.Round(StoredPower * 100 / MaxPower, 2)}%", true);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Echo($"But it does not have any text panels associated to it (with the same tag)");
                            }
                        }
                    }
                }
            }
        }

        private void ManageEnergyBalance()
        {
            if (!isEnergyBalanceEnabled)
            {
                return;
            }

            var chargePercentage = powerStats.Stored / (powerStats.Capacity == 0 ? 1 : powerStats.Capacity);
            isEnergySafetyOn = chargePercentage < POWER_RATIO_SAFETY_THRESHOLD;
            var isAtLeastOneSurvivalKitEnabled = false;

            var ProductionBlocks = new List<IMyProductionBlock>();
            ProductionBlocks.AddRange(RefineryBlocks);
            ProductionBlocks.AddRange(AssemblerBlocks);
            foreach (var ProductionBlock in ProductionBlocks)
            {
                if (!ProductionBlock.IsFunctional)
                {
                    continue;
                }

                if (!isAtLeastOneSurvivalKitEnabled && ProductionBlock.CustomName.Contains("Survival Kit"))
                {
                    isAtLeastOneSurvivalKitEnabled = true;
                    ProductionBlock.Enabled = true;
                    continue;
                }

                if (!isEnergySafetyOn && ProductionBlock.CustomName.Contains(DISABLE_AUTO_TURN_ON_TAG))
                {
                    continue;
                }

                ProductionBlock.Enabled = !isEnergySafetyOn;
            }
        }

        private static string getBatteryChargeModeText(IMyBatteryBlock Battery)
        {
            switch (Battery.ChargeMode)
            {
                case ChargeMode.Auto: return "Авто";
                case ChargeMode.Discharge: return "Разрядка";
                case ChargeMode.Recharge: return "Зарядка";
                default: return "х/з";
            }
        }

        private static String FormatNumber(double value)
        {
            return String.Format("{0:0.00}", value);
        }

        private static string FormatResourceAmount(string resourceType, float amount)
        {
            return String.Format("{0,-11}{1,7} ", I18N(resourceType), FormatNumberNice(amount, 3));
        }

        private static string FormatTime(double seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);
            var parts = new List<String>();
            if (timeSpan.Hours != 0)
            {
                parts.Add($"{timeSpan.Hours} ч");
            }
            if (timeSpan.Minutes != 0)
            {
                parts.Add($"{timeSpan.Minutes} м");
            }
            if (timeSpan.Seconds != 0)
            {
                parts.Add($"{timeSpan.Seconds} сек");
            }

            return String.Join(", ", parts);
        }

        private static Dictionary<String, String> I18NMapping = new Dictionary<String, String>() {
           { "Stone", "Камень" },
           { "Silicon", "Кремний" },
           { "Iron", "Железо" },
           { "Ice", "Лёд" },
           { "Nickel", "Никель" },
           { "Silver", "Серебро" },
           { "Gold", "Золото" },
           { "Platinum", "Платина" },
           { "Cobalt", "Кобальт" },
           { "Uranium", "Уран" },
           { "Scrap", "Скрап" },
           { "Magnesium", "Магний" },
        };

        private static string I18N(String text)
        {
            return I18NMapping.ContainsKey(text) ? I18NMapping[text] : text;
        }

        private static string[] prefixes = { "f", "a", "p", "n", "μ", "m", string.Empty, "k", "M", "G", "T", "P", "E" };
        private static string FormatNumberNice(double x, int significant_digits)
        {
            //Check for special numbers and non-numbers
            if (double.IsInfinity(x) || double.IsNaN(x) || x == 0 || significant_digits <= 0)
            {
                return x.ToString();
            }
            // extract sign so we deal with positive numbers only
            int sign = Math.Sign(x);
            x = Math.Abs(x);
            // get scientific exponent, 10^3, 10^6, ...
            int sci = x == 0 ? 0 : (int)Math.Floor(Math.Log(x, 10) / 3) * 3;
            // scale number to exponent found
            x = x * Math.Pow(10, -sci);
            // find number of digits to the left of the decimal
            int dg = x == 0 ? 0 : (int)Math.Floor(Math.Log(x, 10)) + 1;
            // adjust decimals to display
            int decimals = Math.Min(significant_digits - dg, 15);
            // format for the decimals
            string fmt = new string('0', decimals);
            if (sci == 0)
            {
                //no exponent
                return string.Format("{0}{1:0." + fmt + "}",
                    sign < 0 ? "-" : string.Empty,
                    Math.Round(x, decimals));
            }
            // find index for prefix. every 3 of sci is a new index
            int index = sci / 3 + 6;
            if (index >= 0 && index < prefixes.Length)
            {
                // with prefix
                return string.Format("{0}{1:0." + fmt + "}{2}",
                    sign < 0 ? "-" : string.Empty,
                    Math.Round(x, decimals),
                    prefixes[index]);
            }
            // with 10^exp format
            return string.Format("{0}{1:0." + fmt + "}·10^{2}",
                sign < 0 ? "-" : string.Empty,
                Math.Round(x, decimals),
                sci);
        }
    }


}