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
        // CONFIG
        static readonly string PISTON_TOP_BLOCK_GROUP_NAME = "Pistons Top";
        static readonly string PISTON_BOTTOM_BLOCK_GROUP_NAME = "Pistons Bottom";
        static readonly string PISTON_VERTICAL_BLOCK_GROUP_NAME = "Pistons V";
        static readonly string MAGNET_TOP_BLOCK_GROUP_NAME = "Magnetic Plates Top";
        static readonly string MAGNET_BOTTOM_BLOCK_GROUP_NAME = "Magnetic Plates Bottom";
        static readonly float HORIZONTAL_PISTON_VELOCITY = 5;
        static readonly float VERTICAL_PISTON_VELOCITY = 5;
        static readonly float PISTON_STABLE_EXTENSION = 1.8f;
        // END OF CONFIG

        private readonly List<IMyPistonBase> _topPistons;
        private readonly List<IMyPistonBase> _bottomPistons;
        private readonly List<IMyPistonBase> _verticalPistons;
        private readonly List<IMyLandingGear> _topMagnets;
        private readonly List<IMyLandingGear> _bottomMagnets;


        static readonly string STATE_CONNECTING_TOP = "CONNECTING_TOP";
        static readonly string STATE_DISCONNECTING_BOTTOM = "DISCONNECTING_BOTTOM";
        static readonly string STATE_COLLAPSING_VERTICAL_PISTON = "COLLAPSING_VERTICAL_PISTON";
        static readonly string STATE_CONNECTING_BOTTOM = "CONNECTING_BOTTOM";
        static readonly string STATE_DISCONNECTING_TOP = "DISCONNECTING_TOP";
        static readonly string STATE_EXPANDING_VERTICAL_PISTON = "EXPANDING_VERTICAL_PISTON";

        private bool isRunning = false;
        private string state = STATE_CONNECTING_TOP;
        private float previousVerticalPistonPos = 0f;
        private short sameValueTicks = 0;

        public Program()
        {
            _topPistons = GetBlocksInGroup<IMyPistonBase>(PISTON_TOP_BLOCK_GROUP_NAME);
            _bottomPistons = GetBlocksInGroup<IMyPistonBase>(PISTON_BOTTOM_BLOCK_GROUP_NAME);
            _verticalPistons = GetBlocksInGroup<IMyPistonBase>(PISTON_VERTICAL_BLOCK_GROUP_NAME);
            _topMagnets = GetBlocksInGroup<IMyLandingGear>(MAGNET_TOP_BLOCK_GROUP_NAME);
            _bottomMagnets = GetBlocksInGroup<IMyLandingGear>(MAGNET_BOTTOM_BLOCK_GROUP_NAME);
            Echo("blocks initialized");
            _topMagnets.ForEach(magnet => magnet.AutoLock = false);
            _bottomMagnets.ForEach(magnet => magnet.AutoLock = false);
            Echo("magnets set up");

            _topPistons.ForEach(piston => piston.MaxLimit = PISTON_STABLE_EXTENSION);
            _bottomPistons.ForEach(piston => piston.MaxLimit = PISTON_STABLE_EXTENSION);
            Echo("pistons set up");
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if ("start".Equals(argument))
            {
                isRunning = true;
                Runtime.UpdateFrequency = UpdateFrequency.Update1;
            }
            else if ("stop".Equals(argument))
            {
                isRunning = false;
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }

            if (isRunning)
            {
                Run();
            }
        }

        private void Run()
        {
            Echo(state.ToString());

            if (state.Equals(STATE_CONNECTING_TOP))
            {
                SetVelocity(_topPistons, HORIZONTAL_PISTON_VELOCITY);
                if (IsAtPosition(_topPistons, PISTON_STABLE_EXTENSION))
                {
                    SetLocked(_topMagnets, true);
                    SetVelocity(_topPistons, 0);

                    if (IsAllLocked(_topMagnets))
                    {
                        state = STATE_DISCONNECTING_BOTTOM;
                    }
                }
            }
            else if (state.Equals(STATE_DISCONNECTING_BOTTOM))
            {
                SetLocked(_bottomMagnets, false);
                SetVelocity(_bottomPistons, -HORIZONTAL_PISTON_VELOCITY);
                if (IsAtPosition(_bottomPistons, 0))
                {
                    SetVelocity(_bottomPistons, 0);
                    state = STATE_COLLAPSING_VERTICAL_PISTON;
                }
            }
            else if (state.Equals(STATE_COLLAPSING_VERTICAL_PISTON))
            {
                SetVelocity(_verticalPistons, CalculateVelocity(true));
                if (IsAtPosition(_verticalPistons, 0))
                {
                    SetVelocity(_verticalPistons, 0);
                    state = STATE_CONNECTING_BOTTOM;
                }
            }
            else if (state.Equals(STATE_CONNECTING_BOTTOM))
            {
                SetVelocity(_bottomPistons, HORIZONTAL_PISTON_VELOCITY);
                if (IsAtPosition(_bottomPistons, PISTON_STABLE_EXTENSION))
                {
                    SetLocked(_bottomMagnets, true);
                    SetVelocity(_bottomPistons, 0);
                    if (IsAllLocked(_bottomMagnets))
                    {
                        state = STATE_DISCONNECTING_TOP;
                    }
                }
            }
            else if (state.Equals(STATE_DISCONNECTING_TOP))
            {
                SetLocked(_topMagnets, false);
                SetVelocity(_topPistons, -HORIZONTAL_PISTON_VELOCITY);
                if (IsAtPosition(_topPistons, 0))
                {
                    SetVelocity(_topPistons, 0);
                    state = STATE_EXPANDING_VERTICAL_PISTON;
                }
            }
            else if (state.Equals(STATE_EXPANDING_VERTICAL_PISTON))
            {
                SetVelocity(_verticalPistons, CalculateVelocity(false));
                if (IsAtPosition(_verticalPistons, _verticalPistons[0].MaxLimit))
                {
                    SetVelocity(_verticalPistons, 0);
                    state = STATE_CONNECTING_TOP;
                }
            }
        }

        private List<T> GetBlocksInGroup<T>(string groupName) where T : class
        {
            var blocks = new List<T>();

            var Group = GridTerminalSystem.GetBlockGroupWithName(groupName);
            Group.GetBlocksOfType(blocks);

            return blocks;
        }

        private void SetVelocity(List<IMyPistonBase> pistons, float velocity)
        {
            pistons.ForEach(piston => piston.Velocity = velocity);
        }

        private bool IsAtPosition(List<IMyPistonBase> pistons, float position)
        {
            return pistons.All(piston => Math.Abs(piston.CurrentPosition - position) < 0.1);
        }

        private bool IsAllLocked(List<IMyLandingGear> magnets)
        {
            return magnets.All(magnet => magnet.IsLocked);
        }

        private void SetLocked(List<IMyLandingGear> magnets, bool isLocked)
        {
            magnets.ForEach(magnet =>
            {
                if (isLocked)
                {
                    magnet.Lock();
                }
                else
                {
                    magnet.Unlock();
                }
            });
        }

        private float CalculateVelocity(bool isCollapsing)
        {
            var currentPos = _verticalPistons[0].CurrentPosition;
            if (Math.Abs(previousVerticalPistonPos - currentPos) < 0.01 && sameValueTicks < 2)
            {
                sameValueTicks++;
                return isCollapsing ? 0.01f : -0.01f;
            }
            
            sameValueTicks = 0;
            previousVerticalPistonPos = currentPos;
            return isCollapsing ? -VERTICAL_PISTON_VELOCITY : VERTICAL_PISTON_VELOCITY;
        }
    }


}
