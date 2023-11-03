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
using System.Threading.Tasks;
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
        // CONFIG

        // BLOCK NAMES
        static readonly string COCKPIT_BLOCK_NAME = "Cockpit [m]";
        static readonly string REMOTE_CONTROL_BLOCK_NAME = "RC [m]";
        static readonly string ROTOR_GROUP_NAME = "Rotors [m]";
        static readonly string HINGE_LEFT_GROUP_BLOCK_NAME = "Hinges1 [m]";
        static readonly string HINGE_UP_DOWN_GROUP_NAME = "Hinges2 [m]";
        static readonly string PISTONS_GROUP_NAME = "Pistons [m]";
        static readonly string SOUND_GROUP_NAME = "Sound Block [m]";
        // MANIPULATOR SENSETIVITY
        static readonly float ROTOR_LEFT_RIGHT_SENSITIVITY = 1f;
        static readonly float HINGE_UP_DOWN_SENSITIVITY = 1f;
        static readonly float HINGE_MOUSE_SENSITIVITY = 0.2f;
        static readonly float PISTON_SENSITIVITY = 1f;
        // OTHER
        static readonly bool IS_AUTO = false;
        static readonly string SOUND_START = "EXODUS";
        static readonly string SOUND_STOP = "Objective complete";
        // PB COMMANDS
        static readonly string COMMAND_START = "start";
        static readonly string COMMAND_STOP = "stop";
        // END OF CONFIG

        readonly IMyCockpit cockpit;
        readonly IMyRemoteControl remoteControl;
        readonly List<IMyMotorAdvancedStator> rotors;
        readonly List<IMyMotorAdvancedStator> hinges1;
        readonly List<IMyMotorAdvancedStator> hinges2;
        readonly List<IMyPistonBase> pistons;
        readonly IMySoundBlock soundBlock;
        bool _isTurnedOn = false;
        bool _isSoundDelayed = false;

        public Program()
        {
            cockpit = GetCockpit();
            remoteControl = GetRemoteControl();
            rotors = GetRotors();
            hinges1 = GetHinges1();
            hinges2 = GetHinges2();
            pistons = GetPistons();
            soundBlock = GetSoundBlock();

            if (IS_AUTO)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            ShouldPlaySound();

            if (COMMAND_START.Equals(argument) || IS_AUTO && cockpit.IsUnderControl)
            {
                Start();
            }
            else if (COMMAND_STOP.Equals(argument) || !cockpit.IsUnderControl)
            {
                Stop("Manipulator script stopped", SOUND_STOP);
            }

            if (cockpit == null)
            {
                Stop("Manipulator script stopped: cockpit is not found", "Alert 1");
                return;
            }

            if (!_isTurnedOn)
            {
                return;
            }

            var pistonExtensionMultiplier = 1 - GetPistonsExtensionRatio() / 2;
            var moveIndicator = GetMoveIndicator();
            var rotationIndicator = GetRotationIndicator();

            SetTargetRPM(rotors, moveIndicator.X * ROTOR_LEFT_RIGHT_SENSITIVITY * pistonExtensionMultiplier);
            SetTargetRPM(hinges1, moveIndicator.Y * HINGE_UP_DOWN_SENSITIVITY * pistonExtensionMultiplier);
            SetTargetRPM(hinges2, rotationIndicator.X * HINGE_MOUSE_SENSITIVITY * pistonExtensionMultiplier);
            pistons.ForEach(piston => piston.Velocity = -moveIndicator.Z * PISTON_SENSITIVITY);
        }

        void Start()
        {
            if (_isTurnedOn) return;
            _isTurnedOn = true;

            Log("Manipulator script started");
            PlaySound(SOUND_START);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        void Stop(string message, string sound)
        {
            if (!_isTurnedOn) return;
            _isTurnedOn = false;

            Log(message);
            PlaySound(sound);

            Runtime.UpdateFrequency = IS_AUTO ? UpdateFrequency.Update100 : UpdateFrequency.None;
        }

        void Log(string message)
        {
            if (message == null) return;

            if (cockpit != null)
            {
                // TODO: make display choice configurable
                var textSurface = cockpit.GetSurface(0);
                textSurface.ContentType = ContentType.TEXT_AND_IMAGE;
                textSurface.WriteText(message);
            }
            Echo(message);
        }

        void PlaySound(string sound)
        {
            if (soundBlock != null)
            {
                soundBlock.Stop();
                soundBlock.SelectedSound = sound;
                _isSoundDelayed = true;
            }
        }

        void ShouldPlaySound()
        {
            if (soundBlock == null)
            {
                return;
            }

            if (_isSoundDelayed)
            {
                _isSoundDelayed = false;
                soundBlock.Play();
            }
        }

        Vector3 GetMoveIndicator()
        {
            if (remoteControl != null && remoteControl.IsUnderControl)
            {
                return remoteControl.MoveIndicator;
            }
            return cockpit.MoveIndicator;
        }

        Vector2 GetRotationIndicator()
        {
            if (remoteControl != null && remoteControl.IsUnderControl)
            {
                return remoteControl.RotationIndicator;
            }
            return cockpit.RotationIndicator;
        }

        IMyCockpit GetCockpit()
        {
            return GridTerminalSystem.GetBlockWithName(COCKPIT_BLOCK_NAME) as IMyCockpit;
        }

        IMyRemoteControl GetRemoteControl()
        {
            return GridTerminalSystem.GetBlockWithName(REMOTE_CONTROL_BLOCK_NAME) as IMyRemoteControl;
        }

        List<IMyMotorAdvancedStator> GetRotors()
        {
            return GetBlocksWithGroupName<IMyMotorAdvancedStator>(ROTOR_GROUP_NAME);
        }

        List<IMyMotorAdvancedStator> GetHinges1()
        {
            return GetBlocksWithGroupName<IMyMotorAdvancedStator>(HINGE_LEFT_GROUP_BLOCK_NAME);
        }

        List<IMyMotorAdvancedStator> GetHinges2()
        {
            return GetBlocksWithGroupName< IMyMotorAdvancedStator>(HINGE_UP_DOWN_GROUP_NAME);
        }

        List<IMyPistonBase> GetPistons()
        {
            var pistons = new List<IMyPistonBase>();

            var pistonGroup = GridTerminalSystem.GetBlockGroupWithName(PISTONS_GROUP_NAME);
            if (pistonGroup != null)
            {
                pistonGroup.GetBlocksOfType(pistons);
            }
            return pistons;
        }

        float GetPistonsExtensionRatio()
        {
            var currentExtension = 0f;
            var maxExtension = 0f;
            pistons.ForEach(piston =>
            {
                currentExtension += piston.CurrentPosition;
                maxExtension += piston.MaxLimit;
            });

            return maxExtension == 0f ? 1 : currentExtension / maxExtension;
        }

        IMySoundBlock GetSoundBlock()
        {
            return GridTerminalSystem.GetBlockWithName(SOUND_GROUP_NAME) as IMySoundBlock;
        }

        List<T> GetBlocksWithGroupName<T>(string groupName) where T : class
        {
            var blocks = new List<T>();

            var group = GridTerminalSystem.GetBlockGroupWithName(groupName);
            if (group != null)
            {
                group.GetBlocksOfType(blocks);
            }
            return blocks;
        }

        void SetTargetRPM(List<IMyMotorAdvancedStator> blocks, float value)
        {
            blocks.ForEach(block => block.TargetVelocityRPM = value);
        }
    }
}
