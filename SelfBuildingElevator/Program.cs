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
        static readonly string STATE_MOVE_AND_WELD = "moving and welding";
        static readonly string STATE_CONNECTING_TOP = "connecting top";
        static readonly string STATE_CONNECTING_BOTTOM = "connecting bottom";
        static readonly string STATE_DISCONNECTING_TOP = "disconnecting top";
        static readonly string STATE_DISCONNECTING_BOTTOM = "disconnecting bottom";
        static readonly string STATE_COLLAPSING = "collapsing";
        static readonly string STATE_MOVING = "moving";

        static readonly Version version = Version.CONNECTOR;

        readonly IMyShipWelder welder;
        readonly IMyCargoContainer container;
        readonly IMyPistonBase piston;
        readonly IMyPistonBase pistonTop;
        readonly IMyPistonBase pistonBottom;
        readonly IMyShipMergeBlock mergeTop;
        readonly IMyShipMergeBlock mergeBottom;
        readonly IMyShipConnector connectorTop;
        readonly IMyShipConnector connectorBottom;

        enum Version { MERGE, CONNECTOR }

        string state = STATE_MOVE_AND_WELD;
        bool isRunning = false;
        bool isMovingUp = true;

        public Program()
        {
            welder = GetBlock<IMyShipWelder>("welder");
            container = GetBlock<IMyCargoContainer>("container");
            piston = GetBlock<IMyPistonBase>("piston");
            pistonTop = GetBlock<IMyPistonBase>("piston top");
            pistonBottom = GetBlock<IMyPistonBase>("piston bottom");
            if (version == Version.CONNECTOR)
            {
                connectorTop = GetBlock<IMyShipConnector>("connector top");
                connectorBottom = GetBlock<IMyShipConnector>("connector bottom");
            }
            else if (version == Version.MERGE)
            {
                mergeTop = GetBlock<IMyShipMergeBlock>("merge top");
                mergeBottom = GetBlock<IMyShipMergeBlock>("merge bottom");
            }

        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if ("start".Equals(argument))
            {
                Start();
            }
            else if ("stop".Equals(argument))
            {
                Stop();
            }

            PrintStatus();
            if (isRunning)
            {
                Run();
            }
        }

        void Start()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            isRunning = true;
        }

        void Stop()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            isRunning = false;
        }

        void Run()
        {
            if (STATE_MOVE_AND_WELD.Equals(state))
            {
                SetProjectorsEnabled(true);
                welder.Enabled = true;
                piston.Velocity = 3f;

                if (piston.CurrentPosition == piston.MaxLimit)
                {
                    piston.Velocity = 0;
                    SetProjectorsEnabled(false);
                    welder.Enabled = false;
                    state = isMovingUp ? STATE_CONNECTING_TOP : STATE_CONNECTING_BOTTOM;
                }
            }
            else if (STATE_CONNECTING_TOP.Equals(state))
            {
                pistonTop.Velocity = 1;
                SetTopConnected(true);

                if (IsTopConnected())
                {
                    pistonTop.Velocity = 0f;
                    state = isMovingUp ? STATE_DISCONNECTING_BOTTOM : STATE_COLLAPSING;
                }
            }
            else if (STATE_DISCONNECTING_BOTTOM.Equals(state))
            {
                SetBottomConnected(false);
                pistonBottom.Velocity = -1f;

                if (pistonBottom.CurrentPosition == 0)
                {
                    pistonBottom.Velocity = 0f;
                    state = isMovingUp ? STATE_COLLAPSING : STATE_MOVING;
                }
            }
            else if (STATE_COLLAPSING.Equals(state))
            {
                SetProjectorsEnabled(false);
                welder.Enabled = false;
                SetBottomConnected(!isMovingUp);
                SetTopConnected(isMovingUp);
                piston.Velocity = -1f;

                if (piston.CurrentPosition == 0)
                {
                    piston.Velocity = 0f;
                    state = isMovingUp ? STATE_CONNECTING_BOTTOM : STATE_CONNECTING_TOP; // TODO: what if just moving?
                }
            }
            else if (STATE_CONNECTING_BOTTOM.Equals(state))
            {
                pistonBottom.Velocity = 1;
                SetBottomConnected(true);

                if (IsBottomConnected())
                {
                    pistonBottom.Velocity = 0f;
                    state = STATE_DISCONNECTING_TOP;
                }
            }
            else if (STATE_DISCONNECTING_TOP.Equals(state))
            {
                SetTopConnected(false);
                pistonTop.Velocity = -1f;

                if (pistonTop.CurrentPosition == 0)
                {
                    pistonTop.Velocity = 0f;
                    state = isMovingUp ? STATE_MOVE_AND_WELD : STATE_COLLAPSING;
                }
            }
        }

        void SetTopConnected(bool connected)
        {
            if (version == Version.CONNECTOR)
            {
                if (connected) connectorTop.Connect();
                else connectorTop.Disconnect();
            }
            else if (version == Version.MERGE)
            {
                mergeTop.Enabled = true;
            }
        }

        void SetBottomConnected(bool connected)
        {
            if (version == Version.CONNECTOR)
            {
                if (connected) connectorBottom.Connect();
                else connectorBottom.Disconnect();
            }
            else if (version == Version.MERGE)
            {
                mergeBottom.Enabled = true;
            }
        }

        bool IsTopConnected()
        {
            if (version == Version.CONNECTOR) return connectorTop.IsConnected;
            return mergeTop.IsConnected;
        }

        bool IsBottomConnected()
        {
            if (version == Version.CONNECTOR) return connectorBottom.IsConnected;
            return mergeBottom.IsConnected;
        }

        void PrintStatus()
        {
            Echo(isRunning ? state : "paused");
        }

        T GetBlock<T>(string blockName) where T : class
        {
            T block = GridTerminalSystem.GetBlockWithName(blockName) as T;
            Echo($"Block '{blockName}'{(block == null ? " not" : "")} found");
            return block;
        }

        void SetProjectorsEnabled(bool enabled)
        {
            var projectors = new List<IMyProjector>();
            GridTerminalSystem.GetBlocksOfType(projectors);
            projectors.ForEach(p => p.Enabled = enabled);
        }
    }
}
