using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
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
        public static Dictionary<string, List<T>> GroupByTags<T>(List<T> blocks) where T : IMyTerminalBlock
        {
            var result = new Dictionary<string, List<T>>();

            blocks.ForEach(block =>
            {
                var tags = GetTags(block);
                var joinedTag = string.Join("", tags);
                if (!result.ContainsKey(joinedTag))
                {
                    result.Add(joinedTag, new List<T>());
                }
                result[joinedTag].Add(block);
            });

            return result;
        }

        public static List<string> GetTags(IMyTerminalBlock block)
        {
            var blockTagRegex = new System.Text.RegularExpressions.Regex(@"(?<=\[).+?(?=\])");
            return blockTagRegex
                .Matches(block.CustomName)
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
            return pistons.Find(piston => !IsFullyExtended(piston)) == null;
        }

        public static bool AreFullyCollapsed(List<IMyPistonBase> pistons)
        {
            return pistons.Find(piston => !IsFullyCollapsed(piston)) == null;
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
            return hinges.Find(hinge => !IsCloseTo(hinge, value)) == null;
        }

        public static bool AreAllLocked(List<IMyLandingGear> magnetPlates)
        {
            return magnetPlates.Find(plate => !plate.IsLocked) == null;
        }

        public static void SetEnabled<T>(List<T> blocks, bool isEnabled) where T : IMyFunctionalBlock
        {
            blocks.ForEach(block => block.Enabled = isEnabled);
        }

        public static void SetVelocity(IMyPistonBase piston, float velocity)
        {
            piston.Enabled = velocity != 0;
            piston.Velocity = velocity;
        }

        public static void SetVelocity(List<IMyPistonBase> pistons, float velocity)
        {
            pistons.ForEach(Piston => SetVelocity(Piston, velocity));
        }

        public static float GetPistonsTotalOffset(List<IMyPistonBase> pistons)
        {
            return pistons.Aggregate(0f, (totalOffset, piston) => totalOffset + piston.CurrentPosition);
        }

        public static void SetVelocity(IMyMotorAdvancedStator hinge, float velocityRad)
        {

            hinge.Enabled = velocityRad != 0;
            hinge.TargetVelocityRad = velocityRad;
        }

        public static void SetVelocity(List<IMyMotorAdvancedStator> hinges, float velocityRad)
        {
            hinges.ForEach(hinge => SetVelocity(hinge, velocityRad));
        }

        public static void UnlockAll(List<IMyLandingGear> magnetPlates)
        {
            magnetPlates.ForEach(magnetPlate => magnetPlate.Unlock());
        }

        public static string GetBatteryChargeModeText(IMyBatteryBlock battery)
        {
            switch (battery.ChargeMode)
            {
                case ChargeMode.Auto: return "auto";
                case ChargeMode.Discharge: return "discharge";
                case ChargeMode.Recharge: return "recharge";
                default: return "unknown";
            }
        }

        public static T GetBlock<T>(IMyGridTerminalSystem GridTerminalSystem, string name, string displayName) where T : IMyTerminalBlock
        {
            var Block = GridTerminalSystem.GetBlockWithName(name);

            if (Block == null || !(Block is T)) { throw new Exception($"{displayName} not found by name '{name}'"); }
            if (!Block.IsFunctional) { throw new Exception($"{name} is not functional"); }
            return (T)Block;
        }
    }


}