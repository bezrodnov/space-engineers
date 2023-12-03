using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript.Tasks
{
    internal class ManageConnectorsTask : Task
    {
        private Program _program;

        public ManageConnectorsTask(Program program)
        {
            _program = program;
        }

        string Task.Id
        {
            get
            {
                return "Managing Connectors";
            }
        }

        string Task.Name
        {
            get
            {
                return "Managing Connectors";
            }
        }

        void Task.Run()
        {
            foreach (var ConnectorTag in _program._connectorsByTags.Keys)
            {
                if (ConnectorTag != null && ConnectorTag.Length > 0)
                {
                    foreach (var Connector in _program._connectorsByTags[ConnectorTag])
                    {
                        if (Connector.IsSameConstructAs(_program.Me))
                        {
                            _program.Echo($"Connector {ConnectorTag} detected");
                            if (_program._textPannelsByTags.ContainsKey(ConnectorTag))
                            {
                                var TextPanel = _program._textPannelsByTags[ConnectorTag].First();
                                if (TextPanel != null)
                                {
                                    var IsConnected = Connector.Status == MyShipConnectorStatus.Connected;
                                    TextPanel.WriteText($"{ConnectorTag} {(IsConnected ? "подключен" : "отключен")}\n");

                                    if (IsConnected && _program.BatteriesByTags.ContainsKey(ConnectorTag))
                                    {
                                        var Batteries = _program.BatteriesByTags[ConnectorTag];
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
                                _program.Echo($"But it does not have any text panels associated to it (with the same tag)");
                            }
                        }
                    }
                }
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
    }
}
