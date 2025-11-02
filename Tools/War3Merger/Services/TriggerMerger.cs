// ------------------------------------------------------------------------------
// <copyright file="TriggerMerger.cs" company="Drake53">
// Licensed under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using War3Net.Build.Script;

namespace War3Net.Tools.TriggerMerger.Services
{
    /// <summary>
    /// Service for merging trigger categories between maps.
    /// </summary>
    internal class TriggerMerger
    {
        /// <summary>
        /// Copies specified categories from source to target triggers.
        /// </summary>
        /// <param name="source">Source map triggers.</param>
        /// <param name="target">Target map triggers.</param>
        /// <param name="categoryNames">Names of categories to copy.</param>
        /// <param name="overwrite">Whether to overwrite existing categories.</param>
        /// <returns>Result of the merge operation.</returns>
        public MergeResult CopyCategories(
            MapTriggers source,
            MapTriggers target,
            List<string> categoryNames,
            bool overwrite)
        {
            var result = new MergeResult
            {
                Success = false,
                ModifiedTriggers = target,
                CopiedCategories = new List<CopiedCategoryInfo>(),
                SkippedCategories = new List<string>(),
                NotFoundCategories = new List<string>(),
            };

            if (source.TriggerItems == null || !source.TriggerItems.Any())
            {
                result.ErrorMessage = "Source map has no triggers.";
                return result;
            }

            // Ensure TriggerItems is not null
            if (target.TriggerItems == null)
            {
                throw new InvalidOperationException("Target MapTriggers.TriggerItems is null. Cannot modify triggers.");
            }

            // Find all categories in source
            var sourceCategories = source.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .ToList();

            // Find all categories in target
            var targetCategories = target.TriggerItems?
                .OfType<TriggerCategoryDefinition>()
                .ToList() ?? new List<TriggerCategoryDefinition>();

            foreach (var categoryName in categoryNames)
            {
                var sourceCategory = sourceCategories
                    .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

                if (sourceCategory == null)
                {
                    result.NotFoundCategories.Add(categoryName);
                    continue;
                }

                var existingCategory = targetCategories
                    .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

                if (existingCategory != null && !overwrite)
                {
                    result.SkippedCategories.Add(categoryName);
                    continue;
                }

                // Get all triggers that belong to this category
                var categoryTriggers = GetTriggersInCategory(source, sourceCategory);

                if (existingCategory != null)
                {
                    // Remove existing category and its triggers
                    RemoveCategory(target, existingCategory);
                }

                // Copy the category and its triggers
                CopyCategory(source, target, sourceCategory, categoryTriggers);

                result.CopiedCategories.Add(new CopiedCategoryInfo
                {
                    CategoryName = categoryName,
                    TriggerCount = categoryTriggers.Count,
                    WasOverwritten = existingCategory != null,
                });
            }

            result.Success = result.CopiedCategories.Any() || (!result.NotFoundCategories.Any() && !categoryNames.Any());

            if (result.NotFoundCategories.Any() && !result.CopiedCategories.Any())
            {
                result.ErrorMessage = "None of the specified categories were found in the source map.";
            }

            return result;
        }

        private List<TriggerDefinition> GetTriggersInCategory(MapTriggers triggers, TriggerCategoryDefinition category)
        {
            var categoryTriggers = new List<TriggerDefinition>();

            if (triggers.TriggerItems == null)
            {
                return categoryTriggers;
            }

            // Find the index of the category
            var categoryIndex = triggers.TriggerItems.IndexOf(category);
            if (categoryIndex == -1)
            {
                return categoryTriggers;
            }

            // Get all triggers that come after this category until the next category or end
            for (var i = categoryIndex + 1; i < triggers.TriggerItems.Count; i++)
            {
                var item = triggers.TriggerItems[i];

                // Stop if we hit another category (categories at the same level)
                if (item is TriggerCategoryDefinition)
                {
                    break;
                }

                if (item is TriggerDefinition trigger)
                {
                    categoryTriggers.Add(trigger);
                }
            }

            return categoryTriggers;
        }

        private void RemoveCategory(MapTriggers triggers, TriggerCategoryDefinition category)
        {
            if (triggers.TriggerItems == null)
            {
                return;
            }

            // Get all triggers in this category
            var triggersToRemove = GetTriggersInCategory(triggers, category);

            // Remove the category
            triggers.TriggerItems.Remove(category);

            // Remove all its triggers
            foreach (var trigger in triggersToRemove)
            {
                triggers.TriggerItems.Remove(trigger);
            }
        }

        private void CopyCategory(
            MapTriggers source,
            MapTriggers target,
            TriggerCategoryDefinition sourceCategory,
            List<TriggerDefinition> categoryTriggers)
        {
            // Ensure TriggerItems is not null
            if (target.TriggerItems == null)
            {
                throw new InvalidOperationException("Target MapTriggers.TriggerItems is null. Cannot modify triggers.");
            }

            // Create a new category with a new ID
            var newCategory = new TriggerCategoryDefinition
            {
                Name = sourceCategory.Name,
                IsComment = sourceCategory.IsComment,
                IsExpanded = sourceCategory.IsExpanded,
            };

            // Add the category to the target
            target.TriggerItems!.Add(newCategory);

            // Copy all triggers in the category
            foreach (var trigger in categoryTriggers)
            {
                var newTrigger = CopyTrigger(trigger);
                target.TriggerItems!.Add(newTrigger);
            }
        }

        private TriggerDefinition CopyTrigger(TriggerDefinition source)
        {
            var copy = new TriggerDefinition
            {
                Name = source.Name,
                Description = source.Description,
                IsEnabled = source.IsEnabled,
                IsComment = source.IsComment,
                IsCustomTextTrigger = source.IsCustomTextTrigger,
                IsInitiallyOn = source.IsInitiallyOn,
                RunOnMapInit = source.RunOnMapInit,
            };

            // Copy all functions (events, conditions, actions)
            if (source.Functions != null && source.Functions.Count > 0)
            {
                foreach (var func in source.Functions)
                {
                    copy.Functions.Add(CopyTriggerFunction(func));
                }
            }

            return copy;
        }

        private TriggerFunction CopyTriggerFunction(TriggerFunction source)
        {
            var copy = new TriggerFunction
            {
                Type = source.Type,
                Branch = source.Branch,
                Name = source.Name,
                IsEnabled = source.IsEnabled,
            };

            // Copy all parameters
            if (source.Parameters != null && source.Parameters.Count > 0)
            {
                foreach (var param in source.Parameters)
                {
                    copy.Parameters.Add(CopyTriggerFunctionParameter(param));
                }
            }

            // Copy all child functions (for if-then-else blocks, etc.)
            if (source.ChildFunctions != null && source.ChildFunctions.Count > 0)
            {
                foreach (var childFunc in source.ChildFunctions)
                {
                    copy.ChildFunctions.Add(CopyTriggerFunction(childFunc));
                }
            }

            return copy;
        }

        private TriggerFunctionParameter CopyTriggerFunctionParameter(TriggerFunctionParameter source)
        {
            var copy = new TriggerFunctionParameter
            {
                Type = source.Type,
                Value = source.Value,
            };

            // Handle nested function (for function calls within parameters)
            if (source.Function != null)
            {
                copy.Function = CopyTriggerFunction(source.Function);
            }

            // Handle array indexer (for array variable access)
            if (source.ArrayIndexer != null)
            {
                copy.ArrayIndexer = CopyTriggerFunctionParameter(source.ArrayIndexer);
            }

            return copy;
        }
    }

    /// <summary>
    /// Result of a merge operation.
    /// </summary>
    internal class MergeResult
    {
        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }

        public MapTriggers ModifiedTriggers { get; set; } = null!;

        public List<CopiedCategoryInfo> CopiedCategories { get; set; } = new();

        public List<string> SkippedCategories { get; set; } = new();

        public List<string> NotFoundCategories { get; set; } = new();
    }

    /// <summary>
    /// Information about a copied category.
    /// </summary>
    internal class CopiedCategoryInfo
    {
        public string CategoryName { get; set; } = string.Empty;

        public int TriggerCount { get; set; }

        public bool WasOverwritten { get; set; }
    }
}
