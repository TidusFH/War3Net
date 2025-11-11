using System;
using System.Collections.Generic;
using System.Linq;

namespace WTGMerger
{
    /// <summary>
    /// Merges two intermediate WTG representations with conflict detection
    /// </summary>
    public static class IntermediateMerger
    {
        private static bool debugMode = false;

        public static void SetDebugMode(bool enabled)
        {
            debugMode = enabled;
        }

        /// <summary>
        /// Merge source into target, returning the merged result and any conflicts
        /// </summary>
        public static (IntermediateWTG merged, List<MergeConflict> conflicts) Merge(
            IntermediateWTG source,
            IntermediateWTG target)
        {
            if (debugMode)
            {
                Console.WriteLine($"[DEBUG] Merging:");
                Console.WriteLine($"[DEBUG]   Source: {source.SourceFile}");
                Console.WriteLine($"[DEBUG]     Categories: {source.GetAllCategories().Count()}");
                Console.WriteLine($"[DEBUG]     Triggers: {source.GetAllTriggers().Count()}");
                Console.WriteLine($"[DEBUG]     Variables: {source.Variables.Count}");
                Console.WriteLine($"[DEBUG]   Target: {target.SourceFile}");
                Console.WriteLine($"[DEBUG]     Categories: {target.GetAllCategories().Count()}");
                Console.WriteLine($"[DEBUG]     Triggers: {target.GetAllTriggers().Count()}");
                Console.WriteLine($"[DEBUG]     Variables: {target.Variables.Count}");
            }

            var conflicts = new List<MergeConflict>();

            // Start with target as base
            var merged = new IntermediateWTG
            {
                SourceFile = $"Merged({source.SourceFile} + {target.SourceFile})",
                FormatVersion = target.FormatVersion  // Use target's format version
            };

            // Copy all target content first
            CopyHierarchy(target.Root, merged.Root, conflicts);

            // Copy target variables
            foreach (var variable in target.Variables)
            {
                merged.Variables.Add(variable);
            }

            // Track what we've added from target
            var categoryNames = new HashSet<string>(merged.GetAllCategories().Select(c => c.Name));
            var triggerNames = new HashSet<string>(merged.GetAllTriggers().Select(t => t.Name));
            var variableNames = new HashSet<string>(merged.Variables.Select(v => v.Name));

            if (debugMode)
            {
                Console.WriteLine($"[DEBUG] Target content copied. Now merging source...");
            }

            // Merge source content
            int categoriesAdded = 0;
            int triggersAdded = 0;
            int categoriesSkipped = 0;
            int triggersSkipped = 0;

            // Merge categories and triggers from source
            MergeNode(source.Root, merged.Root, categoryNames, triggerNames, conflicts,
                      ref categoriesAdded, ref triggersAdded, ref categoriesSkipped, ref triggersSkipped);

            // Merge variables from source
            int variablesAdded = 0;
            int variablesSkipped = 0;

            foreach (var sourceVar in source.Variables)
            {
                if (!variableNames.Contains(sourceVar.Name))
                {
                    merged.Variables.Add(sourceVar);
                    variableNames.Add(sourceVar.Name);
                    variablesAdded++;

                    if (debugMode)
                    {
                        Console.WriteLine($"[DEBUG]   Added variable '{sourceVar.Name}' from source");
                    }
                }
                else
                {
                    variablesSkipped++;
                    conflicts.Add(new MergeConflict
                    {
                        Type = MergeConflict.ConflictType.DuplicateVariableName,
                        Name = sourceVar.Name,
                        SourcePath = source.SourceFile,
                        TargetPath = target.SourceFile,
                        Message = $"Variable '{sourceVar.Name}' already exists in target"
                    });

                    if (debugMode)
                    {
                        Console.WriteLine($"[DEBUG]   Skipped variable '{sourceVar.Name}' (already exists)");
                    }
                }
            }

            // Summary
            Console.WriteLine($"\n=== Merge Summary ===");
            Console.WriteLine($"Categories added: {categoriesAdded}");
            Console.WriteLine($"Triggers added: {triggersAdded}");
            Console.WriteLine($"Variables added: {variablesAdded}");

            if (categoriesSkipped > 0 || triggersSkipped > 0 || variablesSkipped > 0)
            {
                Console.WriteLine($"\nSkipped (already exist):");
                if (categoriesSkipped > 0) Console.WriteLine($"  Categories: {categoriesSkipped}");
                if (triggersSkipped > 0) Console.WriteLine($"  Triggers: {triggersSkipped}");
                if (variablesSkipped > 0) Console.WriteLine($"  Variables: {variablesSkipped}");
            }

            if (conflicts.Count > 0)
            {
                Console.WriteLine($"\n⚠ {conflicts.Count} conflict(s) detected:");
                foreach (var conflict in conflicts)
                {
                    Console.WriteLine($"  - {conflict.Type}: {conflict.Message}");
                }
            }

            Console.WriteLine($"\n=== Merged Result ===");
            Console.WriteLine($"Total categories: {merged.GetAllCategories().Count()}");
            Console.WriteLine($"Total triggers: {merged.GetAllTriggers().Count()}");
            Console.WriteLine($"Total variables: {merged.Variables.Count}");

            return (merged, conflicts);
        }

        /// <summary>
        /// Recursively copy hierarchy from source to destination
        /// </summary>
        private static void CopyHierarchy(HierarchyNode source, HierarchyNode dest, List<MergeConflict> conflicts)
        {
            foreach (var child in source.Children)
            {
                HierarchyNode newNode = null;

                if (child is CategoryNode category)
                {
                    newNode = new CategoryNode(new War3Net.Build.Script.TriggerCategoryDefinition
                    {
                        Name = category.Name,
                        Id = category.OriginalId,
                        IsComment = category.IsComment,
                        IsExpanded = category.IsExpanded
                    })
                    {
                        SourceFile = category.SourceFile
                    };
                }
                else if (child is TriggerItemNode trigger)
                {
                    newNode = new TriggerItemNode(trigger.OriginalTrigger)
                    {
                        SourceFile = trigger.SourceFile
                    };
                }

                if (newNode != null)
                {
                    dest.AddChild(newNode);

                    // Recursively copy children
                    if (child.Children.Count > 0)
                    {
                        CopyHierarchy(child, newNode, conflicts);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively merge nodes from source into destination
        /// </summary>
        private static void MergeNode(
            HierarchyNode sourceNode,
            HierarchyNode destNode,
            HashSet<string> categoryNames,
            HashSet<string> triggerNames,
            List<MergeConflict> conflicts,
            ref int categoriesAdded,
            ref int triggersAdded,
            ref int categoriesSkipped,
            ref int triggersSkipped)
        {
            foreach (var child in sourceNode.Children)
            {
                if (child is CategoryNode sourceCategory)
                {
                    // Check if category already exists
                    if (categoryNames.Contains(sourceCategory.Name))
                    {
                        categoriesSkipped++;
                        conflicts.Add(new MergeConflict
                        {
                            Type = MergeConflict.ConflictType.DuplicateCategoryName,
                            Name = sourceCategory.Name,
                            SourcePath = sourceCategory.SourceFile,
                            TargetPath = destNode.SourceFile,
                            Message = $"Category '{sourceCategory.Name}' already exists in target"
                        });

                        if (debugMode)
                        {
                            Console.WriteLine($"[DEBUG]   Skipped category '{sourceCategory.Name}' (already exists)");
                        }

                        // Still merge children into existing category
                        var existingCategory = FindCategoryInNode(destNode, sourceCategory.Name);
                        if (existingCategory != null)
                        {
                            MergeNode(sourceCategory, existingCategory, categoryNames, triggerNames, conflicts,
                                     ref categoriesAdded, ref triggersAdded, ref categoriesSkipped, ref triggersSkipped);
                        }
                        continue;
                    }

                    // Add new category
                    var newCategory = new CategoryNode(new War3Net.Build.Script.TriggerCategoryDefinition
                    {
                        Name = sourceCategory.Name,
                        Id = sourceCategory.OriginalId,
                        IsComment = sourceCategory.IsComment,
                        IsExpanded = sourceCategory.IsExpanded
                    })
                    {
                        SourceFile = sourceCategory.SourceFile
                    };

                    destNode.AddChild(newCategory);
                    categoryNames.Add(newCategory.Name);
                    categoriesAdded++;

                    if (debugMode)
                    {
                        Console.WriteLine($"[DEBUG]   Added category '{newCategory.Name}' from source");
                    }

                    // Recursively merge children
                    MergeNode(sourceCategory, newCategory, categoryNames, triggerNames, conflicts,
                             ref categoriesAdded, ref triggersAdded, ref categoriesSkipped, ref triggersSkipped);
                }
                else if (child is TriggerItemNode sourceTrigger)
                {
                    // Check if trigger already exists
                    if (triggerNames.Contains(sourceTrigger.Name))
                    {
                        triggersSkipped++;
                        conflicts.Add(new MergeConflict
                        {
                            Type = MergeConflict.ConflictType.DuplicateTriggerName,
                            Name = sourceTrigger.Name,
                            SourcePath = sourceTrigger.SourceFile,
                            TargetPath = destNode.SourceFile,
                            Message = $"Trigger '{sourceTrigger.Name}' already exists in target"
                        });

                        if (debugMode)
                        {
                            Console.WriteLine($"[DEBUG]   Skipped trigger '{sourceTrigger.Name}' (already exists)");
                        }
                        continue;
                    }

                    // Add new trigger
                    var newTrigger = new TriggerItemNode(sourceTrigger.OriginalTrigger)
                    {
                        SourceFile = sourceTrigger.SourceFile
                    };

                    destNode.AddChild(newTrigger);
                    triggerNames.Add(newTrigger.Name);
                    triggersAdded++;

                    if (debugMode)
                    {
                        Console.WriteLine($"[DEBUG]   Added trigger '{newTrigger.Name}' from source");
                    }
                }
            }
        }

        /// <summary>
        /// Find a category by name in a node's children (recursively)
        /// </summary>
        private static CategoryNode FindCategoryInNode(HierarchyNode node, string name)
        {
            foreach (var child in node.Children)
            {
                if (child is CategoryNode category && category.Name == name)
                    return category;

                var result = FindCategoryInNode(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
