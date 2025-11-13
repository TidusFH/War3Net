using System;
using System.Collections.Generic;
using System.Linq;
using War3Net.Build.Script;

namespace WTGMerger
{
    /// <summary>
    /// Deep validation of triggers to identify exactly what's invalid
    /// </summary>
    public static class TriggerValidator
    {
        public static void ValidateTrigger(TriggerDefinition trigger, MapTriggers map, bool verbose = true)
        {
            var issues = new List<string>();
            var warnings = new List<string>();

            Console.WriteLine($"\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  VALIDATING TRIGGER: {trigger.Name,-38} ║");
            Console.WriteLine($"╚══════════════════════════════════════════════════════════╝");

            // 1. Basic properties
            Console.WriteLine($"\n=== Basic Properties ===");
            Console.WriteLine($"ID: {trigger.Id}");
            Console.WriteLine($"ParentId: {trigger.ParentId}");
            Console.WriteLine($"IsEnabled: {trigger.IsEnabled}");
            Console.WriteLine($"IsCustomTextTrigger: {trigger.IsCustomTextTrigger}");
            Console.WriteLine($"IsComment: {trigger.IsComment}");
            Console.WriteLine($"Functions: {trigger.Functions.Count}");

            // 2. Validate ParentId
            Console.WriteLine($"\n=== Parent Category Validation ===");
            if (trigger.ParentId < 0 && trigger.ParentId != -1)
            {
                issues.Add($"Invalid ParentId: {trigger.ParentId} (should be >= 0 or -1 for root)");
            }
            else
            {
                var parentCategory = map.TriggerItems
                    .OfType<TriggerCategoryDefinition>()
                    .FirstOrDefault(c => c.Id == trigger.ParentId);

                if (trigger.ParentId >= 0 && parentCategory == null)
                {
                    issues.Add($"ParentId {trigger.ParentId} doesn't match any category");
                }
                else if (parentCategory != null)
                {
                    Console.WriteLine($"✓ Parent category found: '{parentCategory.Name}'");
                }
                else
                {
                    Console.WriteLine($"✓ Root-level trigger (ParentId={trigger.ParentId})");
                }
            }

            // 3. Validate functions
            Console.WriteLine($"\n=== Function Validation ===");
            int eventCount = 0, conditionCount = 0, actionCount = 0;

            foreach (var func in trigger.Functions)
            {
                switch (func.Type)
                {
                    case TriggerFunctionType.Event:
                        eventCount++;
                        break;
                    case TriggerFunctionType.Condition:
                        conditionCount++;
                        break;
                    case TriggerFunctionType.Action:
                        actionCount++;
                        break;
                }
            }

            Console.WriteLine($"Events: {eventCount}");
            Console.WriteLine($"Conditions: {conditionCount}");
            Console.WriteLine($"Actions: {actionCount}");

            if (eventCount == 0 && !trigger.IsComment)
            {
                warnings.Add("Trigger has no events (might be intentional)");
            }

            // 4. Deep function validation
            Console.WriteLine($"\n=== Deep Function Analysis ===");
            for (int i = 0; i < trigger.Functions.Count; i++)
            {
                var func = trigger.Functions[i];
                ValidateFunction(func, map, i, issues, warnings, verbose);
            }

            // 5. Variable references
            Console.WriteLine($"\n=== Variable Reference Validation ===");
            var referencedVars = new HashSet<string>();
            CollectVariableReferences(trigger, referencedVars);

            Console.WriteLine($"Variables referenced: {referencedVars.Count}");

            var mapVarNames = new HashSet<string>(map.Variables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);

            foreach (var varName in referencedVars)
            {
                if (!mapVarNames.Contains(varName))
                {
                    issues.Add($"Variable '{varName}' referenced but not found in map");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ '{varName}' - NOT FOUND");
                    Console.ResetColor();
                }
                else if (verbose)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ '{varName}'");
                    Console.ResetColor();
                }
            }

            // 6. Summary
            Console.WriteLine($"\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  VALIDATION SUMMARY                                      ║");
            Console.WriteLine($"╚══════════════════════════════════════════════════════════╝");

            if (issues.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ No critical issues found!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ {issues.Count} CRITICAL ISSUE(S) FOUND:");
                foreach (var issue in issues)
                {
                    Console.WriteLine($"  • {issue}");
                }
                Console.ResetColor();
            }

            if (warnings.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n⚠ {warnings.Count} WARNING(S):");
                foreach (var warning in warnings)
                {
                    Console.WriteLine($"  • {warning}");
                }
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        private static void ValidateFunction(TriggerFunction func, MapTriggers map, int index, List<string> issues, List<string> warnings, bool verbose)
        {
            if (verbose)
            {
                Console.WriteLine($"\n  [{index}] {func.Type}: {func.Name}");
                Console.WriteLine($"      Enabled: {func.IsEnabled}");
                Console.WriteLine($"      Parameters: {func.Parameters.Count}");
                Console.WriteLine($"      Child Functions: {func.ChildFunctions.Count}");
            }

            // Check if function name is empty
            if (string.IsNullOrWhiteSpace(func.Name))
            {
                issues.Add($"Function [{index}] has empty name");
            }

            // Validate parameters
            for (int p = 0; p < func.Parameters.Count; p++)
            {
                var param = func.Parameters[p];
                ValidateParameter(param, map, $"[{index}].Param[{p}]", issues, warnings, verbose);
            }

            // Validate child functions recursively
            foreach (var child in func.ChildFunctions)
            {
                ValidateFunction(child, map, index, issues, warnings, false);
            }
        }

        private static void ValidateParameter(TriggerFunctionParameter param, MapTriggers map, string path, List<string> issues, List<string> warnings, bool verbose)
        {
            if (verbose)
            {
                Console.WriteLine($"        {path}: Type={param.Type}, Value='{param.Value}'");
            }

            // Check for null/invalid values where they shouldn't be
            if (param.Type == TriggerFunctionParameterType.Variable)
            {
                if (string.IsNullOrWhiteSpace(param.Value))
                {
                    issues.Add($"{path}: Variable parameter has empty value");
                }
            }

            // Validate nested function
            if (param.Function != null)
            {
                ValidateFunction(param.Function, map, -1, issues, warnings, false);
            }

            // Validate array indexer
            if (param.ArrayIndexer != null)
            {
                ValidateParameter(param.ArrayIndexer, map, $"{path}.ArrayIndexer", issues, warnings, false);
            }
        }

        private static void CollectVariableReferences(TriggerDefinition trigger, HashSet<string> variables)
        {
            foreach (var func in trigger.Functions)
            {
                CollectVariableReferencesFromFunction(func, variables);
            }
        }

        private static void CollectVariableReferencesFromFunction(TriggerFunction func, HashSet<string> variables)
        {
            foreach (var param in func.Parameters)
            {
                if (param.Type == TriggerFunctionParameterType.Variable && !string.IsNullOrWhiteSpace(param.Value))
                {
                    variables.Add(param.Value);
                }

                if (param.Function != null)
                {
                    CollectVariableReferencesFromFunction(param.Function, variables);
                }

                if (param.ArrayIndexer != null)
                {
                    CollectVariableReferencesFromParameter(param.ArrayIndexer, variables);
                }
            }

            foreach (var child in func.ChildFunctions)
            {
                CollectVariableReferencesFromFunction(child, variables);
            }
        }

        private static void CollectVariableReferencesFromParameter(TriggerFunctionParameter param, HashSet<string> variables)
        {
            if (param.Type == TriggerFunctionParameterType.Variable && !string.IsNullOrWhiteSpace(param.Value))
            {
                variables.Add(param.Value);
            }

            if (param.Function != null)
            {
                CollectVariableReferencesFromFunction(param.Function, variables);
            }

            if (param.ArrayIndexer != null)
            {
                CollectVariableReferencesFromParameter(param.ArrayIndexer, variables);
            }
        }

        /// <summary>
        /// Compare two triggers to find differences
        /// </summary>
        public static void CompareTriggers(TriggerDefinition trigger1, TriggerDefinition trigger2, string name1 = "Trigger 1", string name2 = "Trigger 2")
        {
            Console.WriteLine($"\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  COMPARING TRIGGERS                                      ║");
            Console.WriteLine($"╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine($"{name1}: {trigger1.Name}");
            Console.WriteLine($"{name2}: {trigger2.Name}");

            Console.WriteLine($"\n=== Property Comparison ===");
            CompareProperty("Name", trigger1.Name, trigger2.Name);
            CompareProperty("IsEnabled", trigger1.IsEnabled, trigger2.IsEnabled);
            CompareProperty("IsCustomTextTrigger", trigger1.IsCustomTextTrigger, trigger2.IsCustomTextTrigger);
            CompareProperty("IsComment", trigger1.IsComment, trigger2.IsComment);
            CompareProperty("IsInitiallyOn", trigger1.IsInitiallyOn, trigger2.IsInitiallyOn);
            CompareProperty("RunOnMapInit", trigger1.RunOnMapInit, trigger2.RunOnMapInit);
            CompareProperty("Functions.Count", trigger1.Functions.Count, trigger2.Functions.Count);

            Console.WriteLine($"\n=== Function Comparison ===");
            int minFunctions = Math.Min(trigger1.Functions.Count, trigger2.Functions.Count);
            for (int i = 0; i < minFunctions; i++)
            {
                var f1 = trigger1.Functions[i];
                var f2 = trigger2.Functions[i];

                if (f1.Name != f2.Name || f1.Type != f2.Type || f1.Parameters.Count != f2.Parameters.Count)
                {
                    Console.WriteLine($"\n  Function [{i}]:");
                    CompareProperty("  Name", f1.Name, f2.Name);
                    CompareProperty("  Type", f1.Type, f2.Type);
                    CompareProperty("  Parameters.Count", f1.Parameters.Count, f2.Parameters.Count);
                }
            }
        }

        private static void CompareProperty<T>(string name, T value1, T value2)
        {
            if (!EqualityComparer<T>.Default.Equals(value1, value2))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  {name}:");
                Console.WriteLine($"    [1]: {value1}");
                Console.WriteLine($"    [2]: {value2}");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"  {name}: {value1} ✓");
            }
        }
    }
}
