using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
        static readonly string LCD_PANEL_NAME = "LCD [jump power]";
        static readonly bool PRINT_ALL_JUMP_DRIVES = false;
        static readonly bool PRINT_NOT_CHARGED_JUMP_DRIVES = true;

        private IMyTextPanel lcd = null;

        public Program()
        {
            var block = GridTerminalSystem.GetBlockWithName(LCD_PANEL_NAME);
            if (block != null && block is IMyTextPanel)
            {
                lcd = block as IMyTextPanel;
                lcd.ContentType = ContentType.TEXT_AND_IMAGE;
                lcd.FontSize = 1;
            }

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (lcd != null)
            {
                lcd.WriteText("", false); // clear LCD
            }

            var jumpDrives = new List<IMyJumpDrive>();
            GridTerminalSystem.GetBlocksOfType(jumpDrives);
            jumpDrives.Sort((a, b) => a.CustomName.CompareTo(b.CustomName));
            if (PRINT_ALL_JUMP_DRIVES)
            {
                PrintNames("Все найденные прыжковые двигатели: ", jumpDrives);
            }

            if (jumpDrives.Count == 0)
            {
                return;
            }

            var notFullyChargedDrives = jumpDrives.FindAll(jumpDrive => !IsFullyCharged(jumpDrive));
            if (notFullyChargedDrives.Count == 0)
            {
                return;
            }
            
            var chargingJumpDrive = notFullyChargedDrives.Find(jumpDrive => jumpDrive.Recharge);
            if (chargingJumpDrive == null)
            {
                chargingJumpDrive = notFullyChargedDrives[0];
                chargingJumpDrive.Recharge = true;
            }
            PrintNames("Сейчас заряжается: ", new List<IMyJumpDrive>() { chargingJumpDrive });
            
            notFullyChargedDrives.ForEach(jumpDrive =>
            {
                if (jumpDrive != chargingJumpDrive)
                {
                    jumpDrive.Recharge = false;
                }
            });

            if (PRINT_NOT_CHARGED_JUMP_DRIVES)
            {
                PrintNames("Не до конца заряженные двигатели: ", notFullyChargedDrives);
            }
        }

        private void PrintNames<T>(string title, List<T> blocks, bool append = true) where T : IMyTerminalBlock
        {
            Print(title + "\n  - " + String.Join("\n  - ", blocks.Select(GetBlockName)), append);
        }

        private string GetBlockName<T>(T block) where T : IMyTerminalBlock
        {
            return block.CustomName;
        }

        private bool IsFullyCharged(IMyJumpDrive jumpDrive)
        {
            return Math.Abs(jumpDrive.CurrentStoredPower - jumpDrive.MaxStoredPower) < 0.001f;
        }

        private void Print(string text, bool append = true)
        {
            if (lcd != null)
            {
                lcd.WriteText(text + "\n", append);
            }
            else
            {
                Echo(text);
            }
        }
    }
}
