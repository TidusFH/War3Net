using System;
using System.Collections.Generic;
using System.Linq;
using War3Net.Build.Script;

namespace WTGMerger
{
    /// <summary>
    /// Converts between War3Net MapTriggers and Intermediate representation
    /// Based on BetterTriggers' approach with predictable ID assignment
    /// </summary>
    public static class IntermediateConverter
    {
        // ID prefixes similar to BetterTriggers
        private const int CATEGORY_ID_BASE = 33550000;
        private const int TRIGGER_ID_BASE = 50330000;
        private const int VARIABLE_ID_BASE = 0;  // Variables start at 0

        private static bool debugMode = false;

        public static void SetDebugMode(bool enabled)
        {
            debugMode = enabled;
        }

        /// <summary>
        /// Disassemble War3Net MapTriggers into intermediate representation
        /// </summary>
        public static IntermediateWTG Disassemble(MapTriggers mapTriggers, string sourceFile)
        {
            Console.WriteLine($"  Reading {mapTriggers.TriggerItems.Count} trigger items...");

            if (debugMode)
            {
                Console.WriteLine($"[DEBUG] Disassembling {sourceFile}...");
                Console.WriteLine($"[DEBUG]   TriggerItems: {mapTriggers.TriggerItems.Count}");
                Console.WriteLine($"[DEBUG]   Variables: {mapTriggers.Variables.Count}");
            }

            var intermediate = new IntermediateWTG
            {
                SourceFile = sourceFile,
                FormatVersion = mapTriggers.FormatVersion,
                SubVersion = mapTriggers.SubVersion
            };

            // Process categories first, building hierarchy
            var categoryNodes = new Dictionary<int, CategoryNode>();

            foreach (var item in mapTriggers.TriggerItems)
            {
                if (item is TriggerCategoryDefinition category && category.Type != TriggerItemType.RootCategory)
                {
                    var node = new CategoryNode(category)
                    {
                        SourceFile = sourceFile
                    };
                    categoryNodes[category.Id] = node;

                    if (debugMode)
                    {
                        Console.WriteLine($"[DEBUG]   Created CategoryNode: '{category.Name}' (ID={category.Id}, ParentId={category.ParentId})");
                    }
                }
            }

            Console.WriteLine($"  Found {categoryNodes.Count} categories");

            if (categoryNodes.Count == 0)
            {
                Console.WriteLine($"  ⚠ WARNING: No categories found in source!");
                Console.WriteLine($"  Checking trigger item types:");
                var typeCounts = mapTriggers.TriggerItems
                    .GroupBy(t => t.GetType().Name)
                    .Select(g => $"{g.Key}: {g.Count()}")
                    .ToArray();
                foreach (var typeCount in typeCounts)
                {
                    Console.WriteLine($"    - {typeCount}");
                }
            }

            // Build category hierarchy using stored ParentId
            // IMPORTANT: In WC3 1.27 format (SubVersion==null), ParentId is NOT saved for categories
            // All categories get ParentId=0 by default, which means "root level"
            bool is127Format = mapTriggers.SubVersion == null;

            foreach (var kvp in categoryNodes)
            {
                var node = kvp.Value;

                // In 1.27 format, ParentId=0 means root level (same as -1)
                // In newer formats, ParentId=0 could be a valid category reference
                bool isRootLevel = node.OriginalParentId == -1 ||
                                  (is127Format && node.OriginalParentId == 0);

                if (isRootLevel)
                {
                    // Top-level category
                    intermediate.Root.AddChild(node);
                    if (debugMode)
                    {
                        Console.WriteLine($"[DEBUG]   Added '{node.Name}' to Root");
                    }
                }
                else if (categoryNodes.ContainsKey(node.OriginalParentId))
                {
                    // Nested category
                    categoryNodes[node.OriginalParentId].AddChild(node);
                    if (debugMode)
                    {
                        Console.WriteLine($"[DEBUG]   Added '{node.Name}' to '{categoryNodes[node.OriginalParentId].Name}'");
                    }
                }
                else
                {
                    // Parent not found, add to root
                    Console.WriteLine($"⚠ Warning: Category '{node.Name}' has invalid ParentId {node.OriginalParentId}, adding to root");
                    intermediate.Root.AddChild(node);
                }
            }

            // Process triggers and add to appropriate parent
            foreach (var item in mapTriggers.TriggerItems)
            {
                if (item is TriggerDefinition trigger)
                {
                    var node = new TriggerItemNode(trigger)
                    {
                        SourceFile = sourceFile
                    };

                    // In 1.27 format, ParentId=0 for triggers means root level too
                    // BUT: Triggers can legitimately reference category ID=0
                    // So we only treat ParentId=0 as root if there's NO category with ID=0
                    bool isRootLevel = trigger.ParentId == -1 ||
                                      (is127Format && trigger.ParentId == 0 && !categoryNodes.ContainsKey(0));

                    if (isRootLevel)
                    {
                        // Top-level trigger
                        intermediate.Root.AddChild(node);
                        if (debugMode)
                        {
                            Console.WriteLine($"[DEBUG]   Added trigger '{node.Name}' to Root");
                        }
                    }
                    else if (categoryNodes.ContainsKey(trigger.ParentId))
                    {
                        // Trigger in category
                        categoryNodes[trigger.ParentId].AddChild(node);
                        if (debugMode)
                        {
                            Console.WriteLine($"[DEBUG]   Added trigger '{node.Name}' to category '{categoryNodes[trigger.ParentId].Name}'");
                        }
                    }
                    else
                    {
                        // Parent not found, add to root
                        Console.WriteLine($"⚠ Warning: Trigger '{node.Name}' has invalid ParentId {trigger.ParentId}, adding to root");
                        intermediate.Root.AddChild(node);
                    }
                }
            }

            // Process variables
            foreach (var variable in mapTriggers.Variables)
            {
                var node = new VariableNode(variable)
                {
                    SourceFile = sourceFile
                };
                intermediate.Variables.Add(node);
            }

            if (debugMode)
            {
                Console.WriteLine($"[DEBUG] Disassembly complete:");
                Console.WriteLine($"[DEBUG]   Categories: {intermediate.GetAllCategories().Count()}");
                Console.WriteLine($"[DEBUG]   Triggers: {intermediate.GetAllTriggers().Count()}");
                Console.WriteLine($"[DEBUG]   Variables: {intermediate.Variables.Count}");
            }

            return intermediate;
        }

        /// <summary>
        /// Rebuild War3Net MapTriggers from intermediate representation with predictable IDs
        /// </summary>
        public static MapTriggers Rebuild(IntermediateWTG intermediate)
        {
            if (debugMode)
            {
                Console.WriteLine($"[DEBUG] Rebuilding MapTriggers from intermediate...");
            }

            var mapTriggers = new MapTriggers(intermediate.FormatVersion, intermediate.SubVersion);

            // Assign predictable IDs to categories
            var allCategories = intermediate.GetAllCategories().ToList();
            var categoryIdMap = new Dictionary<CategoryNode, int>();

            for (int i = 0; i < allCategories.Count; i++)
            {
                int newId = CATEGORY_ID_BASE + i;
                categoryIdMap[allCategories[i]] = newId;

                if (debugMode)
                {
                    Console.WriteLine($"[DEBUG]   Category '{allCategories[i].Name}': ID {allCategories[i].OriginalId} → {newId}");
                }
            }

            // Assign predictable IDs to triggers
            var allTriggers = intermediate.GetAllTriggers().ToList();
            var triggerIdMap = new Dictionary<TriggerItemNode, int>();

            for (int i = 0; i < allTriggers.Count; i++)
            {
                int newId = TRIGGER_ID_BASE + i;
                triggerIdMap[allTriggers[i]] = newId;

                if (debugMode)
                {
                    Console.WriteLine($"[DEBUG]   Trigger '{allTriggers[i].Name}': ID {allTriggers[i].OriginalId} → {newId}");
                }
            }

            // Helper function to get new ParentId
            int GetNewParentId(HierarchyNode node)
            {
                if (node.Parent == null || node.Parent is RootNode)
                    return -1;

                if (node.Parent is CategoryNode categoryParent)
                    return categoryIdMap[categoryParent];

                return -1;  // Shouldn't happen
            }

            // Helper function to recursively add items in hierarchical order
            void AddNodeHierarchically(HierarchyNode node)
            {
                if (node is CategoryNode categoryNode)
                {
                    var newCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
                    {
                        Id = categoryIdMap[categoryNode],
                        ParentId = GetNewParentId(categoryNode),
                        Name = categoryNode.Name,
                        IsComment = categoryNode.IsComment,
                        IsExpanded = categoryNode.IsExpanded
                    };

                    mapTriggers.TriggerItems.Add(newCategory);

                    if (debugMode)
                    {
                        Console.WriteLine($"[DEBUG]   Rebuilt category '{newCategory.Name}' (ID={newCategory.Id}, ParentId={newCategory.ParentId})");
                    }

                    // Recursively add children (subcategories and triggers) immediately after this category
                    foreach (var child in categoryNode.Children)
                    {
                        AddNodeHierarchically(child);
                    }
                }
                else if (node is TriggerItemNode triggerNode)
                {
                    // Clone the original trigger
                    var original = triggerNode.OriginalTrigger;
                    var newTrigger = new TriggerDefinition(original.Type)
                    {
                        Id = triggerIdMap[triggerNode],
                        ParentId = GetNewParentId(triggerNode),
                        Name = original.Name,
                        Description = original.Description,
                        IsEnabled = original.IsEnabled,
                        IsCustomTextTrigger = original.IsCustomTextTrigger,
                        RunOnMapInit = original.RunOnMapInit,
                        IsComment = original.IsComment
                    };

                    // Copy functions (events, conditions, actions)
                    foreach (var func in original.Functions)
                    {
                        newTrigger.Functions.Add(func);
                    }

                    mapTriggers.TriggerItems.Add(newTrigger);

                    if (debugMode)
                    {
                        Console.WriteLine($"[DEBUG]   Rebuilt trigger '{newTrigger.Name}' (ID={newTrigger.Id}, ParentId={newTrigger.ParentId})");
                    }
                }
            }

            // Add all items in hierarchical depth-first order
            // This ensures categories appear before their children in TriggerItems
            foreach (var rootChild in intermediate.Root.Children)
            {
                AddNodeHierarchically(rootChild);
            }

            // Rebuild variables with new IDs
            for (int i = 0; i < intermediate.Variables.Count; i++)
            {
                var variableNode = intermediate.Variables[i];
                var original = variableNode.OriginalVariable;

                // Clone the variable
                var newVariable = new VariableDefinition
                {
                    Id = VARIABLE_ID_BASE + i,
                    Name = original.Name,
                    Type = original.Type,
                    Unk = original.Unk,
                    IsArray = original.IsArray,
                    ArraySize = original.ArraySize,
                    IsInitialized = original.IsInitialized,
                    InitialValue = original.InitialValue
                };

                mapTriggers.Variables.Add(newVariable);

                if (debugMode)
                {
                    Console.WriteLine($"[DEBUG]   Rebuilt variable '{newVariable.Name}' (ID={newVariable.Id})");
                }
            }

            if (debugMode)
            {
                Console.WriteLine($"[DEBUG] Rebuild complete:");
                Console.WriteLine($"[DEBUG]   TriggerItems: {mapTriggers.TriggerItems.Count}");
                Console.WriteLine($"[DEBUG]   Variables: {mapTriggers.Variables.Count}");
            }

            return mapTriggers;
        }
    }
}
