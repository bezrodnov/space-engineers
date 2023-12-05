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
        readonly Program _program;

        public ManageInventoryTask(Program program)
        {
            _program = program;
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
            var containersToAnalyze = new LinkedList<IMyCubeBlock>(_program.CargoContainerBlocks);
            _program.AssemblerBlocks.ForEach(block => containersToAnalyze.AddLast(block));
            _program.RefineryBlocks.ForEach(block => containersToAnalyze.AddLast(block));
            foreach (var connectors in _program._connectorsByTags.Values)
            {
                foreach (var connector in connectors)
                {
                    containersToAnalyze.AddLast(connector);
                }
            }

            _program.Log($"Found {containersToAnalyze.Count} inventories");

            var containersToIgnore = new List<IMyCubeBlock>();
            foreach (var container in containersToAnalyze)
            {
                if (container.DisplayNameText.Contains($"[{Program.INVENTORY_MANAGEMENT_IGNORE_TAG}]"))
                {
                    containersToIgnore.Add(container);
                }
            }

            _program.Log($"Inventories with ignore tag: {containersToIgnore.Count} inventories");
            foreach (var toRemove in containersToIgnore)
            {
                containersToAnalyze.Remove(toRemove);
            }

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


            var chainsWithMoreThanOneElement = new List<List<IMyCubeBlock>>();
            foreach (var chain in chains)
            {
                if (chain.Count > 1)
                {
                    chainsWithMoreThanOneElement.Add(chain);
                }
                else
                {
                    _program.Log($"chain with a single inventory ('{Utils.getBlockName(chain.First())}') can't be optimized");
                }
            }

            _program.Log($"Found {chains.Count} connected container chains\n{chainsWithMoreThanOneElement.Count} of them have/has more than one container");

            foreach (var chain in chainsWithMoreThanOneElement)
            {
                OptimizeInventory(chain);
            }
        }

        private void OptimizeInventory(List<IMyCubeBlock> chain)
        {
            try
            {
                _program.Log($"Found {chain.Count} connected inventories: {String.Join(", ", chain.ConvertAll(Utils.getBlockName))}");
                var cargoInventories = chain.FindAll(b => b is IMyCargoContainer).ConvertAll(b => b.GetInventory());
                if (cargoInventories.Count == 0)
                {
                    // nothing we can help you with, bye bye! xD
                    return;
                }

                _program.Log($"Found {cargoInventories.Count} connected cargo inventories: {String.Join(", ", cargoInventories.ConvertAll(i => i.Owner).ConvertAll(Utils.getBlockName))}");
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
                            _program.   Log($"Moving {itemInfo.Volume} litres of {item.Type} to {Utils.getBlockName(largestCargoInventory.Owner)}");
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
