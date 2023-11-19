using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
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
        static readonly string TAG = "[m]";
        static readonly string COCKPIT_BLOCK_NAME = "Cockpit " + TAG;
        static readonly string REMOTE_CONTROL_BLOCK_NAME = "RC " + TAG;
        static readonly string LEFT_RIGHT_GROUP_BLOCK_NAME = "Left/Right " + TAG;
        static readonly string Q_E_GROUP_NAME = "Q/E " + TAG;
        static readonly string SPACE_C_GROUP_BLOCK_NAME = "Space/C " + TAG;
        static readonly string MOUSE_UP_DOWN_GROUP_NAME = "Mouse Up/Down " + TAG;
        static readonly string MOUSE_LEFT_RIGHT_GROUP_NAME = "Mouse Left/Right " + TAG;
        static readonly string W_S_GROUP_NAME = "W/S " + TAG;
        static readonly string SOUND_GROUP_NAME = "Sound Block " + TAG;
        static readonly string LCD_BLOCK_NAME = "LCD " + TAG;
        static readonly string LIGHTS_GROUP_NAME = "Lights " + TAG;
        // MANIPULATOR SENSETIVITY
        static readonly float LEFT_RIGHT_SENSITIVITY = 1f;
        static readonly float SPACE_C_SENSITIVITY = 1f;
        static readonly float MOUSE_LEFT_RIGHT_SENSITIVITY = 0.2f;
        static readonly float MOUSE_UP_DOWN_SENSITIVITY = 0.2f;
        static readonly float W_S_SENSITIVITY = 1f;
        static readonly float Q_E_SENSITIVITY = 0.2f;
        // OTHER
        static readonly bool IS_AUTO = true;
        static readonly string SOUND_START = "EXODUS";
        static readonly string SOUND_STOP = "Objective complete";
        // PB COMMANDS
        static readonly string COMMAND_START = "start";
        static readonly string COMMAND_STOP = "stop";

        static readonly string COMMAND_RECORD = "record path";
        static readonly string COMMAND_STOP_RECORDING = "stop recording";
        static readonly string COMMAND_CLEAR_RECORDING = "clear recording";
        static readonly string COMMAND_RETURN = "return";
        static readonly string COMMAND_DEPLOY = "deploy";

        // END OF CONFIG

        readonly IMyCockpit cockpit;
        readonly IMyRemoteControl remoteControl;
        readonly List<IMyEntity> leftRightControlledBlocks;
        readonly List<IMyEntity> qEControlledBlocks;
        readonly List<IMyEntity> spaceCControlledBlocks;
        readonly List<IMyEntity> mouseUpDownControlledBlocks;
        readonly List<IMyEntity> mouseLeftRightControlledBlocks;
        readonly List<IMyEntity> wSControlledBlocks;
        readonly IMySoundBlock soundBlock;
        readonly IMyTextPanel lcdBlock;
        readonly List<IMyLightingBlock> lights;

        // recording related stuff
        enum State { TURNED_ON, TURNED_OFF, RECORDING, DEPLOYING, RETURNING }
        State state = State.TURNED_OFF;

        bool _isSoundDelayed = false;
        int _currentMovePosition = -1;
        List<List<float>> recordedPositions;
        List<IMyEntity> allMovableBlocks;

        public Program()
        {
            cockpit = GetCockpit();
            remoteControl = GetRemoteControl();

            leftRightControlledBlocks = GetBlocksWithGroupName<IMyEntity>(LEFT_RIGHT_GROUP_BLOCK_NAME);
            qEControlledBlocks = GetBlocksWithGroupName<IMyEntity>(Q_E_GROUP_NAME);
            spaceCControlledBlocks = GetBlocksWithGroupName<IMyEntity>(SPACE_C_GROUP_BLOCK_NAME);
            mouseUpDownControlledBlocks = GetBlocksWithGroupName<IMyEntity>(MOUSE_UP_DOWN_GROUP_NAME);
            mouseLeftRightControlledBlocks = GetBlocksWithGroupName<IMyEntity>(MOUSE_LEFT_RIGHT_GROUP_NAME);
            wSControlledBlocks = GetBlocksWithGroupName<IMyEntity>(W_S_GROUP_NAME);
            lights = GetBlocksWithGroupName<IMyLightingBlock>(LIGHTS_GROUP_NAME);

            allMovableBlocks = leftRightControlledBlocks
                .Concat(spaceCControlledBlocks)
                .Concat(mouseUpDownControlledBlocks)
                .Concat(mouseLeftRightControlledBlocks)
                .Concat(wSControlledBlocks)
                .ToList();

            soundBlock = GetSoundBlock();
            lcdBlock = GetLCD();

            if (IS_AUTO)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            ShouldPlaySound();
            if (argument != null)
            {
                ExecuteCommands(argument);
            }

            if (cockpit == null && remoteControl == null)
            {
                Stop("Manipulator script stopped: cockpit or RC ais not found", "Alert 1");
            }

            if (state == State.RECORDING)
            {
                RecordNextPosition();
            }

            if (state == State.RETURNING)
            {
                MoveToRecordedPosition(-1);
                return;
            }
            if (state == State.DEPLOYING)
            {
                MoveToRecordedPosition(1);
                return;
            }

            if (state == State.TURNED_OFF)
            {
                return;
            }

            var pistonExtensionMultiplier = 1 - GetPistonsExtensionRatio() / 2;
            var moveIndicator = GetMoveIndicator();
            if (moveIndicator.HasValue)
            {
                MoveBlocks(leftRightControlledBlocks, moveIndicator.Value.X * LEFT_RIGHT_SENSITIVITY * pistonExtensionMultiplier);
                MoveBlocks(spaceCControlledBlocks, moveIndicator.Value.Y * SPACE_C_SENSITIVITY * pistonExtensionMultiplier);
                MoveBlocks(wSControlledBlocks, -moveIndicator.Value.Z * W_S_SENSITIVITY);
            }
            
            var rotationIndicator = GetRotationIndicator();
            if (rotationIndicator.HasValue)
            {
                MoveBlocks(mouseUpDownControlledBlocks, -rotationIndicator.Value.X * MOUSE_UP_DOWN_SENSITIVITY * pistonExtensionMultiplier);
                MoveBlocks(mouseLeftRightControlledBlocks, rotationIndicator.Value.Y * MOUSE_LEFT_RIGHT_SENSITIVITY * pistonExtensionMultiplier);
            }

            var rollIndicator = GetRollIndicator();
            if (rollIndicator.HasValue)
            {
                MoveBlocks(qEControlledBlocks, rollIndicator.Value * Q_E_SENSITIVITY);
            }
        }

        void ExecuteCommands(string command)
        {
            // TODO: refactor, use command pattern
            if (COMMAND_START.Equals(command) || IS_AUTO && IsUnderControl())
            {
                Start();
            }
            else if (COMMAND_STOP.Equals(command))
            {
                Stop("Manipulator script stopped", SOUND_STOP);
            }

            if (!IsUnderControl() && state == State.TURNED_ON)
            {
                Stop("Manipulator script stopped", SOUND_STOP);
            }

            if (COMMAND_RECORD.Equals(command))
            {
                StartRecording();
            }
            else if (COMMAND_STOP_RECORDING.Equals(command))
            {
                StopRecording();
            }
            else if (COMMAND_RETURN.Equals(command))
            {
                StartReturningToOriginalPosition();
            }
            else if (COMMAND_CLEAR_RECORDING.Equals(command))
            {
                ClearRecording();
            }
            else if (COMMAND_DEPLOY.Equals(command))
            {
                StartDeploying();
            }
        }

        void Start()
        {
            if (state == State.TURNED_ON) return;

            state = State.TURNED_ON;
            Log("Manipulator script started");
            PlaySound(SOUND_START);
            SetEnabled(lights, true);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        void Stop(string message, string sound)
        {
            MoveBlocks(leftRightControlledBlocks, 0);
            MoveBlocks(spaceCControlledBlocks, 0);
            MoveBlocks(wSControlledBlocks, 0);
            MoveBlocks(qEControlledBlocks, 0);
            MoveBlocks(mouseUpDownControlledBlocks, 0);
            MoveBlocks(mouseLeftRightControlledBlocks, 0);
            SetEnabled(lights, false);

            if (state == State.TURNED_OFF) return;

            state = State.TURNED_OFF;
            Log(message);
            PlaySound(sound);

            Runtime.UpdateFrequency = IS_AUTO ? UpdateFrequency.Update100 : UpdateFrequency.None;
        }

        void StartRecording()
        {
            state = State.RECORDING;
            recordedPositions = new List<List<float>>();
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Log("Starting path recording");
        }

        void StopRecording()
        {
            Stop("Path recording stopped", SOUND_STOP);
        }

        void RecordNextPosition()
        {
            recordedPositions.Add(GetCurrentPositions());
        }

        void StartReturningToOriginalPosition()
        {
            if (recordedPositions.Count > 0)
            {
                state = State.RETURNING;
                _currentMovePosition = recordedPositions.Count - 1;
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                Log("Starting return to the original position");
            }
        }



        void StartDeploying()
        {
            if (recordedPositions.Count > 0)
            {
                state = State.DEPLOYING;
                _currentMovePosition = 0;
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                Log("Deploying...");
            }
        }

        void MoveToRecordedPosition(short direction)
        {
            Log("Returning to the original position");
            if (recordedPositions.Count > 0 && _currentMovePosition >= 0 && _currentMovePosition < recordedPositions.Count)
            {
                var nextTargetPositions = recordedPositions[_currentMovePosition];
                Log("current path variables: " + string.Join(" ", nextTargetPositions));

                var allNextTargetPositionsMet = MoveToPositions(nextTargetPositions);
                if (allNextTargetPositionsMet)
                {
                    _currentMovePosition += direction;
                }

                Log("recorded paths left: " + (direction == 1
                    ? recordedPositions.Count - _currentMovePosition
                    : _currentMovePosition + 1
                ));
            }
            else
            {
                Stop("Reached target position", SOUND_STOP);
            }
        }

        void ClearRecording()
        {
            StopRecording();
            recordedPositions.Clear();
            _currentMovePosition = -1;
            Log("Recorded path cleared!");
        }

        void Log(string message, bool append = true)
        {
            if (message == null) return;

            if (lcdBlock != null)
            {
                lcdBlock.WriteText(message + "\n", append);
            }
            if (cockpit != null)
            {
                // TODO: make display choice configurable
                var textSurface = cockpit.GetSurface(0);
                textSurface.ContentType = ContentType.TEXT_AND_IMAGE;
                textSurface.WriteText(message + "\n", append);
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

        Vector3? GetMoveIndicator()
        {
            if (remoteControl != null && remoteControl.IsUnderControl)
            {
                return remoteControl.MoveIndicator;
            }
            if (cockpit != null && cockpit.IsUnderControl)
            {
                return cockpit.MoveIndicator;
            }
            return null;
        }

        Vector2? GetRotationIndicator()
        {
            if (remoteControl != null && remoteControl.IsUnderControl)
            {
                return remoteControl.RotationIndicator;
            }
            if (cockpit != null && cockpit.IsUnderControl)
            {
                return cockpit.RotationIndicator;
            }
            return null;
        }

        float? GetRollIndicator()
        {
            if (remoteControl != null && remoteControl.IsUnderControl)
            {
                return remoteControl.RollIndicator;
            }
            if (cockpit != null && cockpit.IsUnderControl)
            {
                return cockpit.RollIndicator;
            }
            return null;
        }

        IMyCockpit GetCockpit()
        {
            return GridTerminalSystem.GetBlockWithName(COCKPIT_BLOCK_NAME) as IMyCockpit;
        }

        IMyRemoteControl GetRemoteControl()
        {
            return GridTerminalSystem.GetBlockWithName(REMOTE_CONTROL_BLOCK_NAME) as IMyRemoteControl;
        }

        float GetPistonsExtensionRatio()
        {
            var currentExtension = GetExtension(wSControlledBlocks)
                + GetExtension(mouseUpDownControlledBlocks)
                + GetExtension(spaceCControlledBlocks)
                + GetExtension(leftRightControlledBlocks);

            var maxExtension = GetMaxExtension(wSControlledBlocks)
                + GetMaxExtension(mouseUpDownControlledBlocks)
                + GetMaxExtension(spaceCControlledBlocks)
                + GetMaxExtension(leftRightControlledBlocks); ;

            return maxExtension == 0f ? 1 : currentExtension / maxExtension;
        }

        float GetExtension(List<IMyEntity> blocks)
        {
            return blocks.Aggregate(0f, (acc, block) => acc + ((block is IMyPistonBase) ? (block as IMyPistonBase).CurrentPosition : 0f));
        }

        float GetMaxExtension(List<IMyEntity> blocks)
        {
            return blocks.Aggregate(0f, (acc, block) => acc + ((block is IMyPistonBase) ? (block as IMyPistonBase).MaxLimit : 0f));
        }

        List<float> GetCurrentPositions()
        {
            var positions = new List<float>();
            foreach (var block in allMovableBlocks)
            {

                if (block is IMyPistonBase)
                {
                    positions.Add((block as IMyPistonBase).CurrentPosition);
                }
                else if (block is IMyMotorAdvancedStator)
                {
                    positions.Add((block as IMyMotorAdvancedStator).Angle);
                }

            }

            return positions;
        }

        bool MoveToPositions(List<float> positions)
        {
            bool allPositionsMatch = true;
            var index = 0;

            allMovableBlocks.ForEach(block =>
            {
                var targetPosition = positions[index++];
                if (block is IMyPistonBase)
                {
                    var piston = block as IMyPistonBase;
                    var diff = piston.CurrentPosition - targetPosition;
                    if (Math.Abs(diff) < 0.1)
                    {
                        piston.Velocity = 0;
                        return;
                    }

                    allPositionsMatch = false;
                    piston.Velocity = diff > 0 ? -0.5f : 0.5f;
                }
                else if (block is IMyMotorAdvancedStator)
                {
                    var motor = block as IMyMotorAdvancedStator;
                    double diff = motor.Angle - targetPosition;
                    if (Math.Abs(diff) < 0.1)
                    {
                        motor.TargetVelocityRPM = 0;
                        return;
                    }
                    if (diff > Math.PI) diff -= 2 * Math.PI;
                    if (diff < -Math.PI) diff += 2 * Math.PI;

                    allPositionsMatch = false;
                    motor.TargetVelocityRPM = diff > 0 ? -0.5f : 0.5f;
                }
            });

            return allPositionsMatch;
        }

        IMySoundBlock GetSoundBlock()
        {
            return GridTerminalSystem.GetBlockWithName(SOUND_GROUP_NAME) as IMySoundBlock;
        }

        IMyTextPanel GetLCD()
        {
            return GridTerminalSystem.GetBlockWithName(LCD_BLOCK_NAME) as IMyTextPanel;
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

        bool IsUnderControl()
        {
            return (cockpit != null && cockpit.IsUnderControl) || (remoteControl != null && remoteControl.IsUnderControl);
        }

        void MoveBlocks(List<IMyEntity> blocks, float value)
        {
            blocks.ForEach(block =>
            {
                if (block is IMyMotorAdvancedStator)
                {
                    (block as IMyMotorAdvancedStator).TargetVelocityRPM = value;
                }
                else if (block is IMyPistonBase)
                {
                    (block as IMyPistonBase).Velocity = value;
                }
            });
        }

        void SetEnabled<T>(List<T> blocks, bool enabled) where T : IMyFunctionalBlock
        {
            blocks.ForEach(block => block.Enabled = enabled);
        }
    }
}
