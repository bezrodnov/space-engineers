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

    partial class Utils
    {
        public static Dictionary<string, List<T>> groupByTags<T>(List<T> Blocks) where T : IMyTerminalBlock
        {
            var result = new Dictionary<string, List<T>>();

            Blocks.ForEach(Block =>
            {
                var tags = getTags(Block);
                var joinedTag = String.Join("", tags);
                if (!result.ContainsKey(joinedTag))
                {
                    result.Add(joinedTag, new List<T>());
                }
                result[joinedTag].Add(Block);
            });

            return result;
        }

        public static List<string> getTags(IMyTerminalBlock Block)
        {
            var blockTagRegex = new System.Text.RegularExpressions.Regex(@"(?<=\[).+?(?=\])");
            return blockTagRegex
                .Matches(Block.CustomName)
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(match => match.Value.ToLower()).ToList();
        }

        public static bool IsFullyExtended(IMyPistonBase piston)
        {
            return IsCloseTo(piston, piston.MaxLimit);
        }

        public static bool IsFullyCollapsed(IMyPistonBase piston)
        {
            return IsCloseTo(piston, piston.MinLimit);
        }

        public static bool AreFullyExtended(List<IMyPistonBase> pistons)
        {
            return pistons.Find(Piston => !IsFullyExtended(Piston)) == null;
        }

        public static bool AreFullyCollapsed(List<IMyPistonBase> pistons)
        {
            return pistons.Find(Piston => !IsFullyCollapsed(Piston)) == null;
        }

        public static bool IsCloseTo(IMyMotorAdvancedStator hinge, double value)
        {
            return Math.Abs(hinge.Angle - value) < 0.01f;
        }

        public static bool IsCloseTo(IMyPistonBase piston, double value)
        {
            return Math.Abs(piston.CurrentPosition - value) < 0.01f;
        }

        public static bool AreCloseTo(List<IMyMotorAdvancedStator> hinges, double value)
        {
            return hinges.Find(Hinge => !IsCloseTo(Hinge, value)) == null;
        }

        public static bool AreAllLocked(List<IMyLandingGear> MagnetPlates)
        {
            return MagnetPlates.Find(Plate => !Plate.IsLocked) == null;
        }

        public static void SetEnabled<T>(List<T> blocks, bool IsEnabled) where T : IMyFunctionalBlock
        {
            blocks.ForEach(Block => Block.Enabled = IsEnabled);
        }

        public static void SetVelocity(IMyPistonBase Piston, float Velocity)
        {
            Piston.Enabled = Velocity != 0;
            Piston.Velocity = Velocity;
        }

        public static void SetVelocity(List<IMyPistonBase> Pistons, float Velocity)
        {
            Pistons.ForEach(Piston => SetVelocity(Piston, Velocity));
        }

        public static float GetPistonsTotalOffset(List<IMyPistonBase> pistons)
        {
            return pistons.Aggregate(0f, (totalOffset, piston) => totalOffset + piston.CurrentPosition);
        }

        public static void SetVelocity(IMyMotorAdvancedStator Hinge, float VelocityRad)
        {

            Hinge.Enabled = VelocityRad != 0;
            Hinge.TargetVelocityRad = VelocityRad;
        }

        public static void SetVelocity(List<IMyMotorAdvancedStator> Hinges, float VelocityRad)
        {
            Hinges.ForEach(Hinge => SetVelocity(Hinge, VelocityRad));
        }

        public static void UnlockAll(List<IMyLandingGear> MagnetPlates)
        {
            MagnetPlates.ForEach(MagnetPlate => MagnetPlate.Unlock());
        }

        public static string GetBatteryChargeModeText(IMyBatteryBlock Battery)
        {
            switch (Battery.ChargeMode)
            {
                case ChargeMode.Auto: return "auto";
                case ChargeMode.Discharge: return "discharge";
                case ChargeMode.Recharge: return "recharge";
                default: return "unknown";
            }
        }
    }


}