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
using IngameScript.Tasks;

namespace IngameScript.Tasks
{
    class ManageInventoryTask : Task
    {
        private readonly Program _program;
        private readonly TaskManager _taskManager;
        private readonly LinkedList<IMyCubeBlock> _containers = new LinkedList<IMyCubeBlock>();
        private readonly List<List<IMyCubeBlock>> _chains = new List<List<IMyCubeBlock>>();

        public ManageInventoryTask(Program program)
        {
            _program = program;

            _taskManager = new TaskManager(program);
            _taskManager.Schedule(new GetContainersSubtask(program, _containers), 3);
            _taskManager.Schedule(new GroupContainersInChainsSubtask(program, _containers, _chains), 3, 1);
            _taskManager.Schedule(new OptimizeInventorySubtask(program, _chains), 3, 2);
        }

        string Task.Id
        {
            get
            {
                return "Managing inventory";
            }
        }

        string Task.Name
        {
            get
            {
                return "Managing inventory ";
            }
        }

        void Task.Run()
        {
            _taskManager.Run();
        }
    }

    class GetContainersSubtask : Task
    {
        private readonly Program _program;
        private readonly LinkedList<IMyCubeBlock> _containersToAnalyze;

        public GetContainersSubtask(Program program, LinkedList<IMyCubeBlock> containersToAnalyze)
        {
            _program = program;
            _containersToAnalyze = containersToAnalyze;
        }

        string Task.Id
        {
            get
            {
                return "Getting containers to analyze";
            }
        }

        string Task.Name
        {
            get
            {
                return "Getting containers to analyze";
            }
        }

        void Task.Run()
        {
            _containersToAnalyze.Clear();
            AddContainers(_program.AssemblerBlocks);
            AddContainers(_program.RefineryBlocks);
            foreach (var connectors in _program._connectorsByTags.Values)
            {
                AddContainers(connectors);
            }

            _program.Log($"Found {_containersToAnalyze.Count} inventories");
        }

        private void AddContainers<T>(IEnumerable<T> containers) where T : IMyCubeBlock
        {
            foreach (var container in containers)
            {
                if (!ShouldIgnoreContainer(container))
                {
                    _containersToAnalyze.AddLast(container);
                }
            }
        }

        private bool ShouldIgnoreContainer(IMyCubeBlock container)
        {
            return !container.CubeGrid.IsSameConstructAs(_program.Me.CubeGrid)
                || Utils.getBlockName(container).Contains($"[{Program.INVENTORY_MANAGEMENT_IGNORE_TAG}]");
        }
    }

    class GroupContainersInChainsSubtask : Task
    {
        private readonly Program _program;
        private readonly LinkedList<IMyCubeBlock> _containers;
        private readonly List<List<IMyCubeBlock>> _chains;

        public GroupContainersInChainsSubtask(Program program, LinkedList<IMyCubeBlock> containers, List<List<IMyCubeBlock>> chains)
        {
            _program = program;
            _containers = containers;
            _chains = chains;
        }

        string Task.Id
        {
            get
            {
                return "Groupping containers in chains";
            }
        }

        string Task.Name
        {
            get
            {
                return "Groupping containers in chains";
            }
        }

        void Task.Run()
        {
            var containersToAnalyze = new LinkedList<IMyCubeBlock>(_containers);
            var chains = new List<List<IMyCubeBlock>>();
            List<IMyCubeBlock> currentChain = null;

            while (containersToAnalyze.Count != 0)
            {
                var currentContainer = containersToAnalyze.First.Value;
                containersToAnalyze.RemoveFirst();

                currentChain = new List<IMyCubeBlock>
                {
                    currentContainer
                };
                chains.Add(currentChain);

                var containersToRemove = new List<IMyCubeBlock>();
                foreach (var container in containersToAnalyze)
                {
                    if (currentContainer.GetInventory().IsConnectedTo(container.GetInventory()))
                    {
                        currentChain.Add(container);
                        containersToRemove.Add(container);
                    }
                }
                containersToRemove.ForEach(toRemove => containersToAnalyze.Remove(toRemove));
            }

            _chains.Clear();
            foreach (var chain in chains)
            {
                if (chain.Count > 1)
                {
                    _chains.Add(chain);
                }
                else
                {
                    _program.Log($"chain with a single inventory ('{Utils.getBlockName(chain.First())}') can't be optimized");
                }
            }

            _program.Log($"Found {chains.Count} connected container chains\n{_chains.Count} of them have/has more than one container");
        }
    }

    class OptimizeInventorySubtask : Task
    {
        private readonly Program _program;
        private readonly List<List<IMyCubeBlock>> _chains = new List<List<IMyCubeBlock>>();

        public OptimizeInventorySubtask(Program program, List<List<IMyCubeBlock>> chains)
        {
            _program = program;
            _chains = chains;
        }

        string Task.Id
        {
            get
            {
                return "Optimizing inventory";
            }
        }

        string Task.Name
        {
            get
            {
                return "Optimizing inventory";
            }
        }

        void Task.Run()
        {
            foreach (var chain in _chains)
            {
                OptimizeInventory(chain);
            }
        }

        private void OptimizeInventory(List<IMyCubeBlock> chain)
        {
            try
            {
                _program.Log($"Found {chain.Count} connected inventories: {string.Join(", ", chain.ConvertAll(Utils.getBlockName))}");
                var cargoInventories = chain.FindAll(b => b is IMyCargoContainer).ConvertAll(b => b.GetInventory());
                if (cargoInventories.Count == 0)
                {
                    // nothing we can help you with, bye bye! xD
                    return;
                }

                _program.Log($"Found {cargoInventories.Count} connected cargo inventories: {string.Join(", ", cargoInventories.ConvertAll(i => i.Owner).ConvertAll(Utils.getBlockName))}");
                cargoInventories.Sort(Comparer<IMyInventory>.Create((a, b) => (int)(b.MaxVolume - a.MaxVolume)));

                foreach (var inventory in cargoInventories)
                {
                    _program.Log($"'{Utils.getBlockName(inventory.Owner)}' has volume {inventory.CurrentVolume}/{inventory.MaxVolume}");
                }

                // step#1 move all non ore and ingots items to other containers
                var largestCargoInventory = cargoInventories.First();
                _program.Log($"Moving all non ore and ingots from '{Utils.getBlockName(largestCargoInventory.Owner)}' to other containres");
                if (cargoInventories.Count > 1)
                {
                    var inventoriesToMoveOtherItemsTo = cargoInventories.FindAll(inventory => inventory != largestCargoInventory && inventory.CurrentVolume < inventory.MaxVolume);
                    if (inventoriesToMoveOtherItemsTo.Count > 0)
                    {
                        foreach (var block in chain)
                        {
                            var inventory = block.GetInventory();
                            if (inventoriesToMoveOtherItemsTo.Contains(inventory))
                            {
                                continue;
                            }

                            var items = new List<MyInventoryItem>();
                            inventory.GetItems(items);
                            foreach (var item in items)
                            {
                                var itemInfo = item.Type.GetItemInfo();
                                if (itemInfo.IsOre || itemInfo.IsIngot)
                                {
                                    continue;
                                }

                                foreach (var inventoryToMoveTo in inventoriesToMoveOtherItemsTo)
                                {
                                    if (inventory.CanTransferItemTo(inventoryToMoveTo, item.Type))
                                    {
                                        _program.Log($"Moving {itemInfo.Volume} litres of {item.Type} to {Utils.getBlockName(inventoryToMoveTo.Owner)}");
                                        if (inventory.TransferItemTo(inventoryToMoveTo, item))
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // step#2 move to the largest container all possible ores and ingots from other containers
                // except refineries and stone from survival kit
                _program.Log($"Moving all ore and ingots to '{Utils.getBlockName(largestCargoInventory.Owner)}'");
                foreach (var block in chain)
                {
                    if (largestCargoInventory.CurrentVolume == largestCargoInventory.MaxVolume)
                    {
                        break;
                    }

                    var inventory = block is IMyProductionBlock ? ((IMyProductionBlock)block).OutputInventory : block.GetInventory();
                    if (inventory == largestCargoInventory)
                    {
                        continue;
                    }

                    var isSurvivalKit = Utils.getBlockName(block).Contains("Survival Kit");

                    var items = new List<MyInventoryItem>();
                    inventory.GetItems(items);

                    foreach (var item in items)
                    {
                        if (isSurvivalKit && item.Type.ToString().Contains("Ore/Stone"))
                        {
                            // leave the stone in survival kit
                            continue;
                        }
                        var itemInfo = item.Type.GetItemInfo();
                        if ((itemInfo.IsOre || itemInfo.IsIngot) && inventory.CanTransferItemTo(largestCargoInventory, item.Type))
                        {
                            _program.Log($"Moving {itemInfo.Volume} litres of {item.Type} to {Utils.getBlockName(largestCargoInventory.Owner)}");
                            inventory.TransferItemTo(largestCargoInventory, item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _program.Troubleshoot(e);
            }
        }
    }
}
