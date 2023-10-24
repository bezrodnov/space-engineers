using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using VRageRender;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // CONFIG SECTION
        bool ignoreIce = true;
        // -------------------

        const string COMMAND_PLAY = "play";
        const string COMMAND_PAUSE = "pause";
        const string COMMAND_REWIND = "rewind";

        const float BLOCK_SIZE_LARGE = 2.5f;
        const float BLOCK_SIZE_SMALL = 0.5f;

        // got this value from BuildInfo mod
        const float ShipDrill_VoxelVisualAdd = 0.6f; // based on visual tests
        // taken from the world config 3039009361\Data\CubeBlocks\Mod_CubeBlocks_Tools.sbc
        const float ShipDrill_CutOutRadius = 1.9f;
        const float ShipDrill_CutOutOffset = 2.8f;

        readonly DrillingRoverGrid drillingRoverGrid;

        short tick = 0;
        char[] tickChars = new char[] { '/', '-', '\\' };

        Instruction instruction;
        bool isPaused;

        enum State
        {
            ReadyToMove, AdjustingManipulator, LookingForOre, CollectingOre
        }
        enum Direction
        {
            Forwards, Backwards
        }
        struct Instruction
        {
            public Direction Direction { get; }

            public State State { get; }

            public UpdateFrequency? UpdateFrequency { get; }

            public Instruction(Direction direction, State state) : this(direction, state, null) { }

            public Instruction(Direction direction, State state, UpdateFrequency? updateFrequency)
            {
                Direction = direction;
                State = state;
                UpdateFrequency = updateFrequency;
            }
        }

        delegate void CommandHandler();
        readonly Dictionary<string, CommandHandler> commandHandlers;

        interface IStateProcessor
        {
            Instruction Process(Direction direction);

            void Pause();

            void Reset();
        }

        readonly Dictionary<State, IStateProcessor> stateProcessors;

        public Program()
        {
            drillingRoverGrid = new DrillingRoverGrid(GridTerminalSystem);

            Logger.Echo = (string message) => Echo(message);
            Logger.GridTerminalSystem = GridTerminalSystem;

            commandHandlers = new Dictionary<string, CommandHandler>()
            {
                { COMMAND_PLAY, Play },
                { COMMAND_PAUSE, Pause},
                { COMMAND_REWIND, Rewind },
            };


            var LookingForOreStateProcessor = new LookingForOreStateProcessor(drillingRoverGrid, ignoreIce);

            var CollectingOreStateProcessor = new CollectingOreStateProcessor(drillingRoverGrid);
            LookingForOreStateProcessor.OreEdgesDetectedEvent += CollectingOreStateProcessor.OnOreEdgesDetected;

            stateProcessors = new Dictionary<State, IStateProcessor>()
            {
                { State.ReadyToMove, new ReadyToMoveStateProcessor() },
                { State.AdjustingManipulator, new AdjustManipulatorStateProcessor(drillingRoverGrid) },
                { State.LookingForOre, LookingForOreStateProcessor },
                { State.CollectingOre, CollectingOreStateProcessor }
            };

            ResetState();
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Terminal || updateSource == UpdateType.Trigger)
            {
                CommandHandler CommandHandler;
                commandHandlers.TryGetValue(argument, out CommandHandler);
                if (CommandHandler == null)
                {
                    Logger.Log("Command should be either 'play', 'pause' or 'stop'");
                    return;
                }
                CommandHandler();
            }

            HandleTick();
        }

        class ReadyToMoveStateProcessor : IStateProcessor
        {
            public Instruction Process(Direction direction)
            {
                if (direction == Direction.Forwards) { return new Instruction(Direction.Forwards, State.AdjustingManipulator, UpdateFrequency.Update100); }
                return new Instruction(Direction.Backwards, State.ReadyToMove, UpdateFrequency.None);
            }

            public void Pause() { }

            public void Reset() { }
        }

        class AdjustManipulatorStateProcessor : IStateProcessor
        {
            DrillingRoverGrid drillingRoverGrid;

            public AdjustManipulatorStateProcessor(DrillingRoverGrid drillingRoverGrid)
            {
                this.drillingRoverGrid = drillingRoverGrid;
            }

            public Instruction Process(Direction direction)
            {
                return AdjustManipulator(direction, false);
            }

            public void Pause()
            {
                AdjustManipulator(Direction.Forwards, true);
            }

            public void Reset() { }

            private Instruction AdjustManipulator(Direction direction, bool isPaused)
            {
                Utils.SetVelocity(drillingRoverGrid.GetManipulatorHinges(), isPaused ? 0 : direction == Direction.Forwards ? -0.25f : 0.25f);
                Utils.SetVelocity(drillingRoverGrid.GetHingeDrillTop(), isPaused ? 0 : direction == Direction.Forwards ? 0.25f : -0.25f);
                Utils.SetVelocity(drillingRoverGrid.GetManipulatorPiston(), isPaused ? 0 : direction == Direction.Forwards ? 1.5f : -3f);

                if (direction == Direction.Forwards && IsManipulatorInstalled()) { return new Instruction(Direction.Forwards, State.LookingForOre, UpdateFrequency.Update100); }
                if (direction == Direction.Backwards && IsManipulatorDeinstalled()) { return new Instruction(Direction.Backwards, State.ReadyToMove, UpdateFrequency.Once); }
                return new Instruction(direction, State.AdjustingManipulator, UpdateFrequency.Update100);
            }

            private bool IsManipulatorInstalled()
            {
                if (!Utils.AreCloseTo(drillingRoverGrid.GetManipulatorHinges(), 0))
                {
                    return false;
                }

                if (!Utils.IsFullyExtended(drillingRoverGrid.GetManipulatorPiston()))
                {
                    return false;
                }

                if (!Utils.IsCloseTo(drillingRoverGrid.GetHingeDrillTop(), Math.PI / 2))
                {
                    return false;
                }

                return true;
            }

            private bool IsManipulatorDeinstalled()
            {
                if (!Utils.AreCloseTo(drillingRoverGrid.GetManipulatorHinges(), Math.PI / 2))
                {
                    return false;
                }

                if (!Utils.IsCloseTo(drillingRoverGrid.GetManipulatorPiston(), 0))
                {
                    return false;
                }

                if (!Utils.IsCloseTo(drillingRoverGrid.GetHingeDrillTop(), 0))
                {
                    return false;
                }

                return true;
            }
        }

        class LookingForOreStateProcessor : IStateProcessor
        {
            private float? oreTopOffset = null;
            private float? oreBottomOffset = null;
            private double oreAmmount = 0;
            private bool ignoreIce;

            private DrillingRoverGrid drillingRoverGrid;

            public class OreEdgesDetectedEventArgs
            {
                public float TopOffset { get; }
                public float BottomOffset { get; }

                public OreEdgesDetectedEventArgs(float topOffset, float bottomOffset)
                {
                    TopOffset = topOffset;
                    BottomOffset = bottomOffset;
                }
            }

            public delegate void OreEdgesDetected(object sender, OreEdgesDetectedEventArgs e);
            public event OreEdgesDetected OreEdgesDetectedEvent;

            public LookingForOreStateProcessor(DrillingRoverGrid drillingRoverGrid, bool ignoreIce)
            {
                this.drillingRoverGrid = drillingRoverGrid;
                this.ignoreIce = ignoreIce;

                Reset();
            }


            public Instruction Process(Direction direction)
            {
                return MoveDrill(direction);
            }

            public void Pause()
            {
                StopDrilling();
            }

            public void Reset()
            {
                oreAmmount = drillingRoverGrid.GetOreAmmount(ignoreIce);
                oreTopOffset = null;
                oreBottomOffset = null;
            }

            private Instruction MoveDrill(Direction direction)
            {
                drillingRoverGrid.GetDrill().Enabled = direction == Direction.Forwards;

                var TopPistons = drillingRoverGrid.GetTopPistons();
                Utils.SetVelocity(TopPistons, (direction == Direction.Forwards ? 0.1f : -1f) / TopPistons.Count);

                /*pistonDrillBottom.Enabled = true;
                pistonDrillBottom.Velocity = 0.1f;*/

                if (direction == Direction.Forwards)
                {
                    var CurrentOffset = Utils.GetPistonsTotalOffset(drillingRoverGrid.GetTopPistons());
                    Logger.Log($"Current depth: {CurrentOffset} meters");
                    if (IsMiningOre())
                    {
                        if (!oreTopOffset.HasValue)
                        {
                            oreTopOffset = CurrentOffset;
                        }
                        Logger.Log($"Ore detected at {oreTopOffset.Value} meters");
                    }
                    else if (oreTopOffset.HasValue)
                    {
                        oreBottomOffset = CurrentOffset;
                        OreEdgesDetectedEvent.Invoke(this, new OreEdgesDetectedEventArgs(oreTopOffset.Value, oreBottomOffset.Value));
                        Logger.Log($"Ore bottom edge detected at {oreBottomOffset.Value} meters");
                        return new Instruction(Direction.Forwards, State.CollectingOre, UpdateFrequency.Once);
                    }

                    if (IsDrillFullyExtended())
                    {
                        if (oreTopOffset.HasValue)
                        {
                            OreEdgesDetectedEvent.Invoke(this, new OreEdgesDetectedEventArgs(oreTopOffset.Value, CurrentOffset));
                            return new Instruction(Direction.Forwards, State.CollectingOre, UpdateFrequency.Once);
                        }
                        return new Instruction(Direction.Backwards, State.LookingForOre, UpdateFrequency.Update100);
                    }
                }

                if (direction == Direction.Backwards && IsReadyForManipulatorDeinstall()) { return new Instruction(Direction.Backwards, State.AdjustingManipulator, UpdateFrequency.Once); }
                return new Instruction(direction, State.LookingForOre, UpdateFrequency.Update100);
            }

            private void StopDrilling()
            {
                drillingRoverGrid.GetDrill().Enabled = false;
                Utils.SetVelocity(drillingRoverGrid.GetTopPistons(), 0);
                Utils.SetVelocity(drillingRoverGrid.GetBottomPiston(), 0);
            }


            private bool IsDrillFullyExtended()
            {
                if (!Utils.AreFullyExtended(drillingRoverGrid.GetTopPistons())) { return false; }
                if (!Utils.IsFullyExtended(drillingRoverGrid.GetBottomPiston())) { /* // TODO: enable later // return false; */ }
                return true;
            }

            private bool IsReadyForManipulatorDeinstall()
            {
                if (!Utils.AreFullyCollapsed(drillingRoverGrid.GetTopPistons())) { return false; }
                if (!Utils.IsFullyCollapsed(drillingRoverGrid.GetBottomPiston())) { return false; }
                if (!Utils.IsCloseTo(drillingRoverGrid.GetHingeDrillBottom(), 0)) { return false; }
                return true;
            }

            private bool IsMiningOre()
            {
                var OreAmmount = drillingRoverGrid.GetOreAmmount(ignoreIce);
                var IsMiningOre = OreAmmount > oreAmmount;
                oreAmmount = OreAmmount;
                return IsMiningOre;
            }
        }

        class CollectingOreStateProcessor : IStateProcessor
        {
            private float oreTopOffset;
            private float oreBottomOffset;
            private DrillingRoverGrid drillingRoverGrid;

            private short maxAngle = 3;
            private double hingeToDrillDistance;

            public CollectingOreStateProcessor(DrillingRoverGrid drillingRoverGrid)
            {
                this.drillingRoverGrid = drillingRoverGrid;

                Reset();
            }

            public void OnOreEdgesDetected(object sender, LookingForOreStateProcessor.OreEdgesDetectedEventArgs e)
            {
                this.oreTopOffset = e.TopOffset;
                this.oreBottomOffset = e.BottomOffset;
            }

            public void Pause()
            {
                drillingRoverGrid.GetRotor().Enabled = false;
                drillingRoverGrid.GetDrill().Enabled = false;
                drillingRoverGrid.GetBottomPiston().Enabled = false;
            }

            public Instruction Process(Direction direction)
            {
                if (direction == Direction.Backwards)
                {
                    drillingRoverGrid.GetDrill().Enabled = false;
                    Utils.SetVelocity(drillingRoverGrid.GetRotor(), 0);
                    Utils.SetVelocity(drillingRoverGrid.GetTopPistons(), 0);
                    Utils.SetVelocity(drillingRoverGrid.GetBottomPiston(), -1);
                    Utils.SetVelocity(drillingRoverGrid.GetHingeDrillBottom(), -1);

                    if (IsReadyToExtract()) { return new Instruction(Direction.Backwards, State.LookingForOre, UpdateFrequency.Once); }
                }
                else
                {
                    drillingRoverGrid.GetDrill().Enabled = true;
                    Utils.SetVelocity(drillingRoverGrid.GetRotor(), 0.3f); // TODO: calculate speed
                    Utils.SetVelocity(drillingRoverGrid.GetBottomPiston(), 0.01f);
                    var Hinge = drillingRoverGrid.GetHingeDrillBottom();
                    Hinge.UpperLimitDeg = maxAngle;
                    Utils.SetVelocity(Hinge, 1);

                    var TopPistons = drillingRoverGrid.GetTopPistons();
                    Utils.SetVelocity(TopPistons, -0.05f);

                    if (IsDoneDrilling()) { return new Instruction(Direction.Backwards, State.CollectingOre, UpdateFrequency.Update100); }
                }

                return new Instruction(direction, State.CollectingOre, UpdateFrequency.Update100);
            }

            public void Reset()
            {
                var Drill = drillingRoverGrid.GetDrill();
                //var MiningRadius = ShipDrill_VoxelVisualAdd + ShipDrill_CutOutRadius;
                //Log($"Drill mining radius: {MiningRadius} meters");
                var DrillLength = 3 * BLOCK_SIZE_LARGE;
                hingeToDrillDistance = Vector3D.Distance(drillingRoverGrid.GetHingeDrillTop().WorldMatrix.Translation, Drill.WorldMatrix.Translation) + DrillLength;
                Logger.Log($"distance between hinge top and drill bottom is\n  {hingeToDrillDistance} meters");
            }

            private bool IsReadyToExtract()
            {
                if (!Utils.IsFullyCollapsed(drillingRoverGrid.GetBottomPiston())) { return false; }
                if (!Utils.IsCloseTo(drillingRoverGrid.GetHingeDrillBottom(), 0)) { return false; }
                return true;
            }

            private bool IsDoneDrilling()
            {
                var CurrentOffset = Utils.GetPistonsTotalOffset(drillingRoverGrid.GetTopPistons());
                Logger.Log($"Current depth: {CurrentOffset} meters");
                Logger.Log($"Ore edges: [{oreTopOffset}, {oreBottomOffset}] meters");
                if (CurrentOffset > oreTopOffset) { return false; }
                return true;
            }
        }

        private void ResetState()
        {
            Pause();
            instruction = new Instruction(Direction.Forwards, State.ReadyToMove);
        }

        private void Play()
        {
            instruction = new Instruction(Direction.Forwards, instruction.State, UpdateFrequency.Once);
            isPaused = false;
        }

        private void Pause()
        {
            isPaused = true;
        }

        private void Rewind()
        {
            instruction = new Instruction(Direction.Backwards, instruction.State, UpdateFrequency.Once);
            isPaused = false;
        }

        private void HandleTick()
        {
            tick++;
            PrintInfo();

            IStateProcessor StateProcessor;
            stateProcessors.TryGetValue(instruction.State, out StateProcessor);
            if (StateProcessor != null)
            {
                if (isPaused)
                {
                    StateProcessor.Pause();
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    return;
                }

                var NextInstruction = StateProcessor.Process(instruction.Direction);
                if (instruction.State != NextInstruction.State) { StateProcessor.Reset(); }
                instruction = NextInstruction;
                if (instruction.UpdateFrequency.HasValue)
                {
                    Runtime.UpdateFrequency = instruction.UpdateFrequency.Value;
                }
            }
        }

        private void PrintInfo()
        {
            Logger.Log(tickChars[tick % tickChars.Length].ToString(), false);
            PrintState(instruction.State);
            PrintDirection(instruction.Direction);
        }

        private void PrintState(State state)
        {
            switch (state)
            {
                case State.ReadyToMove:
                    PrintState("Ready to move");
                    break;
                case State.LookingForOre:
                    PrintState("Drilling");
                    break;
                case State.AdjustingManipulator:
                    PrintState("Adjusting Manipulator");
                    break;
                case State.CollectingOre:
                    PrintState("Collecting Ore");
                    break;
            }
        }

        private void PrintState(string state)
        {
            Logger.Log("State: " + state);
        }

        private void PrintDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Forwards:
                    PrintDirection("Forwards");
                    break;
                case Direction.Backwards:
                    PrintDirection("Backwards");
                    break;
            }
        }

        private void PrintDirection(string direction)
        {
            Logger.Log("Direction: " + direction);
        }
    }
}
