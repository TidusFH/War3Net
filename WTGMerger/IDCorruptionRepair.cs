using System;
using System.Collections.Generic;
using System.Linq;
using War3Net.Build.Script;

namespace WTGMerger
{
    /// <summary>
    /// Detects and repairs corrupted IDs in trigger data
    /// Handles corruption from tools like BetterTriggers
    /// </summary>
    public static class IDCorruptionRepair
    {
        /// <summary>
        /// Detects if a category ID is corrupted
        /// </summary>
        public static bool IsCorruptedCategoryID(int id)
        {
            // Corrupted IDs are typically > 1000
            // Normal category IDs are 0-100 range
            // The specific corruption pattern we saw: 0x2000001, 0x200000E, etc.
            return id > 1000 || (id > 0 && (id & 0x02000000) != 0);
        }

        /// <summary>
        /// Detects if all triggers share the same ID (corruption pattern)
        /// </summary>
        public static bool HasDuplicateTriggerIDs(MapTriggers triggers)
        {
            var triggerIds = triggers.TriggerItems.OfType<TriggerDefinition>()
                .Select(t => t.Id)
                .Distinct()
                .ToList();

            // If all triggers share same ID, that's corruption
            return triggerIds.Count == 1;
        }

        /// <summary>
        /// Repairs a corrupted MapTriggers by reassigning all IDs sequentially
        /// </summary>
        public static void RepairCorruptedIDs(MapTriggers triggers)
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           REPAIRING CORRUPTED IDs                        ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

            bool is127Format = triggers.SubVersion == null;
            int rootParentId = is127Format ? 0 : -1;

            // Create ID mappings
            var oldCategoryIdToNew = new Dictionary<int, int>();
            var categoryNameToNewId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Step 1: Reassign category IDs sequentially
            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>()
                .Where(c => c.Type != TriggerItemType.RootCategory)
                .ToList();

            Console.WriteLine($"\n=== Repairing {categories.Count} categories ===");

            for (int i = 0; i < categories.Count; i++)
            {
                int oldId = categories[i].Id;
                int newId = i;

                oldCategoryIdToNew[oldId] = newId;
                categoryNameToNewId[categories[i].Name] = newId;

                if (IsCorruptedCategoryID(oldId))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  Fixing '{categories[i].Name}': ID {oldId} (0x{oldId:X}) → {newId}");
                    Console.ResetColor();
                }

                categories[i].Id = newId;

                // Fix ParentId for categories
                if (categories[i].ParentId != rootParentId)
                {
                    if (IsCorruptedCategoryID(categories[i].ParentId))
                    {
                        // Corrupted parent - set to root
                        categories[i].ParentId = rootParentId;
                    }
                    else if (oldCategoryIdToNew.ContainsKey(categories[i].ParentId))
                    {
                        categories[i].ParentId = oldCategoryIdToNew[categories[i].ParentId];
                    }
                }
            }

            // Step 2: Reassign trigger IDs sequentially
            var triggerDefs = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();

            Console.WriteLine($"\n=== Repairing {triggerDefs.Count} triggers ===");

            int triggerIdStart = categories.Count;
            bool allSameId = HasDuplicateTriggerIDs(triggers);

            for (int i = 0; i < triggerDefs.Count; i++)
            {
                int oldId = triggerDefs[i].Id;
                int newId = triggerIdStart + i;

                if (allSameId || oldId == 0)
                {
                    if (i < 5 || i >= triggerDefs.Count - 2)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  Fixing '{triggerDefs[i].Name}': ID {oldId} → {newId}");
                        Console.ResetColor();
                    }
                    else if (i == 5)
                    {
                        Console.WriteLine($"  ... {triggerDefs.Count - 7} more triggers ...");
                    }
                }

                triggerDefs[i].Id = newId;

                // Fix ParentId for triggers
                int oldParentId = triggerDefs[i].ParentId;

                if (oldParentId != rootParentId)
                {
                    if (IsCorruptedCategoryID(oldParentId))
                    {
                        // Try to find category by looking at file position
                        // In 1.27 format, triggers appear after their parent category
                        int triggerIndex = triggers.TriggerItems.IndexOf(triggerDefs[i]);

                        // Search backwards for the nearest category
                        TriggerCategoryDefinition nearestCategory = null;
                        for (int j = triggerIndex - 1; j >= 0; j--)
                        {
                            if (triggers.TriggerItems[j] is TriggerCategoryDefinition cat && cat.Type != TriggerItemType.RootCategory)
                            {
                                nearestCategory = cat;
                                break;
                            }
                        }

                        if (nearestCategory != null)
                        {
                            triggerDefs[i].ParentId = nearestCategory.Id;
                        }
                        else
                        {
                            // No category found - set to root
                            triggerDefs[i].ParentId = rootParentId;
                        }
                    }
                    else if (oldCategoryIdToNew.ContainsKey(oldParentId))
                    {
                        triggerDefs[i].ParentId = oldCategoryIdToNew[oldParentId];
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ Repaired {categories.Count} categories and {triggerDefs.Count} triggers");
            Console.WriteLine($"  Category IDs: 0 to {categories.Count - 1}");
            Console.WriteLine($"  Trigger IDs: {triggerIdStart} to {triggerIdStart + triggerDefs.Count - 1}");
            Console.ResetColor();
        }

        /// <summary>
        /// Creates a copy of a trigger with corruption-aware ID handling
        /// Matches parent category by NAME, not corrupted ID
        /// </summary>
        public static TriggerDefinition CopyTriggerWithIDRepair(
            TriggerDefinition source,
            MapTriggers sourceMap,
            MapTriggers targetMap,
            int newTriggerId,
            string destinationCategoryName)
        {
            // Find the destination category by NAME
            var destCategory = targetMap.TriggerItems.OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(destinationCategoryName, StringComparer.OrdinalIgnoreCase));

            if (destCategory == null)
            {
                throw new InvalidOperationException($"Destination category '{destinationCategoryName}' not found in target");
            }

            // Create the copy with clean IDs
            var copy = new TriggerDefinition(source.Type)
            {
                Id = newTriggerId,
                ParentId = destCategory.Id,  // Use target's category ID, not source's corrupted ID
                Name = source.Name,
                Description = source.Description,
                IsComment = source.IsComment,
                IsEnabled = source.IsEnabled,
                IsCustomTextTrigger = source.IsCustomTextTrigger,
                IsInitiallyOn = source.IsInitiallyOn,
                RunOnMapInit = source.RunOnMapInit
            };

            // Deep copy all functions
            foreach (var function in source.Functions)
            {
                copy.Functions.Add(CopyTriggerFunction(function));
            }

            return copy;
        }

        private static TriggerFunction CopyTriggerFunction(TriggerFunction source)
        {
            var copy = new TriggerFunction
            {
                Type = source.Type,
                Branch = source.Branch,
                Name = source.Name,
                IsEnabled = source.IsEnabled
            };

            foreach (var param in source.Parameters)
            {
                copy.Parameters.Add(CopyTriggerFunctionParameter(param));
            }

            foreach (var childFunc in source.ChildFunctions)
            {
                copy.ChildFunctions.Add(CopyTriggerFunction(childFunc));
            }

            return copy;
        }

        private static TriggerFunctionParameter CopyTriggerFunctionParameter(TriggerFunctionParameter source)
        {
            var copy = new TriggerFunctionParameter
            {
                Type = source.Type,
                Value = source.Value
            };

            if (source.Function != null)
            {
                copy.Function = CopyTriggerFunction(source.Function);
            }

            if (source.ArrayIndexer != null)
            {
                copy.ArrayIndexer = CopyTriggerFunctionParameter(source.ArrayIndexer);
            }

            return copy;
        }

        /// <summary>
        /// Finds the category name for a trigger, even with corrupted IDs
        /// Uses file position to infer category in 1.27 format
        /// </summary>
        public static string FindCategoryNameForTrigger(TriggerDefinition trigger, MapTriggers map)
        {
            int triggerIndex = map.TriggerItems.IndexOf(trigger);

            // Search backwards for the nearest category
            for (int i = triggerIndex - 1; i >= 0; i--)
            {
                if (map.TriggerItems[i] is TriggerCategoryDefinition cat && cat.Type != TriggerItemType.RootCategory)
                {
                    return cat.Name;
                }
            }

            return "Root";
        }
    }
}
