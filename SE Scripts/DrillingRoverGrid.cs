using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ModAPI.Ingame;
namespace IngameScript
{
    internal class DrillingRoverGrid
    {
        const string HINGES_INSTALL_GROUP = "Hinges Deploy";
        const string HINGE_DRILL_TOP = "Hinge Drill Top";
        const string HINGE_DRILL_BOTTOM = "Hinge Drill Bottom";
        const string PISTON_MANIPULATOR = "Piston 3";
        const string PISTONS_DRILL_TOP_GROUP = "Pistons Drill Top";
        const string PISTONS_DRILL_BOTTOM = "Piston Drill Bottom";
        const string ROTOR = "Drill Rotor";

        IMyGridTerminalSystem gridTerminalSystem;

        public DrillingRoverGrid(IMyGridTerminalSystem gridTerminalSystem)
        {
            this.gridTerminalSystem = gridTerminalSystem;
        }

        public List<IMyMotorAdvancedStator> GetManipulatorHinges()
        {
            var DeployHingesGroup = gridTerminalSystem.GetBlockGroupWithName(HINGES_INSTALL_GROUP);
            var DeployHinges = new List<IMyMotorAdvancedStator>();
            DeployHingesGroup.GetBlocksOfType(DeployHinges);

            if (DeployHinges.Count == 0)
            {
                throw new StateNotReadyException("Deploy hinges not found");
            }

            foreach (var Hinge in DeployHinges)
            {
                if (!Hinge.IsFunctional)
                {
                    throw new StateNotReadyException($"Hinge {Hinge.CustomName} is not functional");
                }
            }

            return DeployHinges;
        }

        public IMyPistonBase GetManipulatorPiston()
        {
            return gridTerminalSystem.GetBlockWithName(PISTON_MANIPULATOR) as IMyPistonBase;
        }

        public IMyShipDrill GetDrill()
        {
            var Drills = new List<IMyShipDrill>();
            gridTerminalSystem.GetBlocksOfType(Drills);
            var Drill = Drills.First();
            if (Drill == null) { throw new StateNotReadyException("Drill not found"); }
            if (!Drill.IsFunctional) { throw new StateNotReadyException("Drill is not functional"); }
            return Drill;
        }

        public IMyMotorAdvancedStator GetHingeDrillTop()
        {
            return GetBlock<IMyMotorAdvancedStator>(HINGE_DRILL_TOP, "Drill hinge top");
        }

        public IMyMotorAdvancedStator GetHingeDrillBottom()
        {
            return GetBlock<IMyMotorAdvancedStator>(HINGE_DRILL_BOTTOM, "Drill hinge bottom");
        }

        public List<IMyPistonBase> GetTopPistons()
        {
            var TopPistonsGroup = gridTerminalSystem.GetBlockGroupWithName(PISTONS_DRILL_TOP_GROUP);
            var TopPistons = new List<IMyPistonBase>();
            TopPistonsGroup.GetBlocksOfType(TopPistons);
            if (TopPistons.Count == 0) { throw new StateNotReadyException("Pistons on top of the drill not found"); }
            foreach (var Piston in TopPistons)
            {
                if (!Piston.IsFunctional) { throw new StateNotReadyException($"{Piston.Name} is not functional"); }
            }

            return TopPistons;
        }

        public IMyPistonBase GetBottomPiston()
        {
            return GetBlock<IMyPistonBase>(PISTONS_DRILL_BOTTOM, "Bottom drill piston");
        }

        public IMyMotorAdvancedStator GetRotor()
        {
            return GetBlock<IMyMotorAdvancedStator>(ROTOR, "Drill rotor");
        }

        private T GetBlock<T>(string name, string displayName) where T : IMyTerminalBlock
        {
            var Block = gridTerminalSystem.GetBlockWithName(name);

            if (Block == null || !(Block is T)) { throw new StateNotReadyException($"{displayName} not found by name '{name}'"); }
            if (!Block.IsFunctional) { throw new StateNotReadyException($"{name} is not functional"); }
            return (T) Block;
        }

        public double GetOreAmmount(bool ignoreIce)
        {
            var entities = new List<IMyEntity>();
            gridTerminalSystem.GetBlocksOfType(entities, block => block.HasInventory);

            var OreAmmount = 0.0;
            foreach (var entity in entities)
            {
                var Items = new List<MyInventoryItem>();
                entity.GetInventory().GetItems(Items);
                foreach (var Item in Items)
                {
                    if (Item.Type.GetItemInfo().IsOre)
                    {
                        var OreType = Item.Type.ToString();
                        var isStone = OreType.ToUpper().IndexOf("STONE") >= 0;
                        if (isStone) { continue; }

                        var IsIce = OreType.EndsWith("Ore/Ice");
                        if (IsIce && ignoreIce) { continue; }

                        OreAmmount += Item.Amount.ToIntSafe();
                    }
                }
            }

            Logger.Log($"Ore amount: {OreAmmount}");
            return OreAmmount;
        }

    }
}
