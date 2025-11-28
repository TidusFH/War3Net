using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using War3Net.Build.Script;

namespace WTGMerger
{
    /// <summary>
    /// Exports trigger data to human-readable formats for inspection
    /// </summary>
    public static class TriggerExporter
    {
        /// <summary>
        /// Exports trigger to detailed text format showing all internals
        /// </summary>
        public static string ExportToDetailedText(TriggerDefinition trigger, MapTriggers map, bool showHex = false)
        {
            var sb = new StringBuilder();

            sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║  TRIGGER EXPORT: {trigger.Name,-41} ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════╝");
            sb.AppendLine();

            // Basic properties
            sb.AppendLine("=== BASIC PROPERTIES ===");
            sb.AppendLine($"Name: {trigger.Name}");
            sb.AppendLine($"Description: {trigger.Description ?? "(none)"}");
            sb.AppendLine($"ID: {trigger.Id}");
            sb.AppendLine($"ParentId: {trigger.ParentId}");
            sb.AppendLine($"Type: {trigger.Type}");
            sb.AppendLine($"IsEnabled: {trigger.IsEnabled}");
            sb.AppendLine($"IsInitiallyOn: {trigger.IsInitiallyOn}");
            sb.AppendLine($"IsComment: {trigger.IsComment}");
            sb.AppendLine($"IsCustomTextTrigger: {trigger.IsCustomTextTrigger}");
            sb.AppendLine($"RunOnMapInit: {trigger.RunOnMapInit}");
            sb.AppendLine();

            // Parent category
            sb.AppendLine("=== PARENT CATEGORY ===");
            var parentCategory = map.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Id == trigger.ParentId);

            if (parentCategory != null)
            {
                sb.AppendLine($"✓ Category found: '{parentCategory.Name}' (ID={parentCategory.Id})");
            }
            else if (trigger.ParentId == -1 || trigger.ParentId == 0)
            {
                sb.AppendLine($"✓ Root level (ParentId={trigger.ParentId})");
            }
            else
            {
                sb.AppendLine($"✗ INVALID: ParentId={trigger.ParentId} does not match any category!");
            }
            sb.AppendLine();

            // Functions
            sb.AppendLine("=== FUNCTIONS ===");
            sb.AppendLine($"Total: {trigger.Functions.Count}");
            sb.AppendLine();

            var events = trigger.Functions.Where(f => f.Type == TriggerFunctionType.Event).ToList();
            var conditions = trigger.Functions.Where(f => f.Type == TriggerFunctionType.Condition).ToList();
            var actions = trigger.Functions.Where(f => f.Type == TriggerFunctionType.Action).ToList();

            sb.AppendLine($"EVENTS ({events.Count}):");
            for (int i = 0; i < events.Count; i++)
            {
                ExportFunction(sb, events[i], i, "  ", map, showHex);
            }

            sb.AppendLine();
            sb.AppendLine($"CONDITIONS ({conditions.Count}):");
            for (int i = 0; i < conditions.Count; i++)
            {
                ExportFunction(sb, conditions[i], i, "  ", map, showHex);
            }

            sb.AppendLine();
            sb.AppendLine($"ACTIONS ({actions.Count}):");
            for (int i = 0; i < actions.Count; i++)
            {
                ExportFunction(sb, actions[i], i, "  ", map, showHex);
            }

            // Variable references
            sb.AppendLine();
            sb.AppendLine("=== VARIABLE REFERENCES ===");
            var referencedVars = CollectVariableReferences(trigger);

            if (referencedVars.Count == 0)
            {
                sb.AppendLine("(no variables referenced)");
            }
            else
            {
                var mapVarNames = new HashSet<string>(map.Variables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);

                foreach (var varName in referencedVars.OrderBy(v => v))
                {
                    var exists = mapVarNames.Contains(varName);
                    var marker = exists ? "✓" : "✗";
                    var status = exists ? "EXISTS" : "MISSING";

                    sb.AppendLine($"{marker} {varName,-30} [{status}]");

                    if (exists)
                    {
                        var varDef = map.Variables.FirstOrDefault(v =>
                            v.Name.Equals(varName, StringComparison.OrdinalIgnoreCase));
                        if (varDef != null)
                        {
                            sb.AppendLine($"   Type: {varDef.Type}, Array: {varDef.IsArray}, Init: {varDef.IsInitialized}");
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private static void ExportFunction(StringBuilder sb, TriggerFunction func, int index, string indent, MapTriggers map, bool showHex)
        {
            sb.AppendLine($"{indent}[{index}] {func.Name}");
            sb.AppendLine($"{indent}    Type: {func.Type}");
            sb.AppendLine($"{indent}    Enabled: {func.IsEnabled}");
            sb.AppendLine($"{indent}    Branch: {func.Branch}");

            if (showHex && !string.IsNullOrEmpty(func.Name))
            {
                var bytes = Encoding.UTF8.GetBytes(func.Name);
                sb.AppendLine($"{indent}    Name (hex): {BitConverter.ToString(bytes).Replace("-", " ")}");
            }

            if (func.Parameters.Count > 0)
            {
                sb.AppendLine($"{indent}    Parameters ({func.Parameters.Count}):");
                for (int p = 0; p < func.Parameters.Count; p++)
                {
                    ExportParameter(sb, func.Parameters[p], p, indent + "      ", map, showHex);
                }
            }

            if (func.ChildFunctions.Count > 0)
            {
                sb.AppendLine($"{indent}    Child Functions ({func.ChildFunctions.Count}):");
                for (int c = 0; c < func.ChildFunctions.Count; c++)
                {
                    ExportFunction(sb, func.ChildFunctions[c], c, indent + "      ", map, showHex);
                }
            }
        }

        private static void ExportParameter(StringBuilder sb, TriggerFunctionParameter param, int index, string indent, MapTriggers map, bool showHex)
        {
            sb.Append($"{indent}[{index}] Type={param.Type}");

            if (!string.IsNullOrEmpty(param.Value))
            {
                sb.Append($", Value=\"{param.Value}\"");

                if (param.Type == TriggerFunctionParameterType.Variable)
                {
                    var varExists = map.Variables.Any(v => v.Name.Equals(param.Value, StringComparison.OrdinalIgnoreCase));
                    sb.Append(varExists ? " ✓" : " ✗MISSING");
                }

                if (showHex)
                {
                    var bytes = Encoding.UTF8.GetBytes(param.Value);
                    sb.Append($" (hex: {BitConverter.ToString(bytes).Replace("-", " ")})");
                }
            }

            sb.AppendLine();

            if (param.Function != null)
            {
                sb.AppendLine($"{indent}   → Nested Function:");
                ExportFunction(sb, param.Function, -1, indent + "      ", map, showHex);
            }

            if (param.ArrayIndexer != null)
            {
                sb.AppendLine($"{indent}   → Array Indexer:");
                ExportParameter(sb, param.ArrayIndexer, -1, indent + "      ", map, showHex);
            }
        }

        private static HashSet<string> CollectVariableReferences(TriggerDefinition trigger)
        {
            var vars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var func in trigger.Functions)
            {
                CollectVariablesFromFunction(func, vars);
            }

            return vars;
        }

        private static void CollectVariablesFromFunction(TriggerFunction func, HashSet<string> vars)
        {
            foreach (var param in func.Parameters)
            {
                if (param.Type == TriggerFunctionParameterType.Variable && !string.IsNullOrWhiteSpace(param.Value))
                {
                    vars.Add(param.Value);
                }

                if (param.Function != null)
                {
                    CollectVariablesFromFunction(param.Function, vars);
                }

                if (param.ArrayIndexer != null)
                {
                    CollectVariablesFromParameter(param.ArrayIndexer, vars);
                }
            }

            foreach (var child in func.ChildFunctions)
            {
                CollectVariablesFromFunction(child, vars);
            }
        }

        private static void CollectVariablesFromParameter(TriggerFunctionParameter param, HashSet<string> vars)
        {
            if (param.Type == TriggerFunctionParameterType.Variable && !string.IsNullOrWhiteSpace(param.Value))
            {
                vars.Add(param.Value);
            }

            if (param.Function != null)
            {
                CollectVariablesFromFunction(param.Function, vars);
            }

            if (param.ArrayIndexer != null)
            {
                CollectVariablesFromParameter(param.ArrayIndexer, vars);
            }
        }

        /// <summary>
        /// Exports trigger to simplified pseudo-code format
        /// </summary>
        public static string ExportToPseudoCode(TriggerDefinition trigger)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"TRIGGER: {trigger.Name}");
            if (!string.IsNullOrWhiteSpace(trigger.Description))
            {
                sb.AppendLine($"// {trigger.Description}");
            }
            sb.AppendLine();

            var events = trigger.Functions.Where(f => f.Type == TriggerFunctionType.Event).ToList();
            var conditions = trigger.Functions.Where(f => f.Type == TriggerFunctionType.Condition).ToList();
            var actions = trigger.Functions.Where(f => f.Type == TriggerFunctionType.Action).ToList();

            sb.AppendLine("EVENTS:");
            foreach (var evt in events)
            {
                sb.AppendLine($"  {FunctionToPseudoCode(evt)}");
            }

            sb.AppendLine();
            sb.AppendLine("CONDITIONS:");
            foreach (var cond in conditions)
            {
                sb.AppendLine($"  {FunctionToPseudoCode(cond)}");
            }

            sb.AppendLine();
            sb.AppendLine("ACTIONS:");
            foreach (var action in actions)
            {
                sb.AppendLine($"  {FunctionToPseudoCode(action)}");
            }

            return sb.ToString();
        }

        private static string FunctionToPseudoCode(TriggerFunction func)
        {
            var sb = new StringBuilder();
            sb.Append(func.Name);

            if (func.Parameters.Count > 0)
            {
                sb.Append("(");
                for (int i = 0; i < func.Parameters.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(ParameterToPseudoCode(func.Parameters[i]));
                }
                sb.Append(")");
            }

            return sb.ToString();
        }

        private static string ParameterToPseudoCode(TriggerFunctionParameter param)
        {
            if (param.Function != null)
            {
                return FunctionToPseudoCode(param.Function);
            }

            if (!string.IsNullOrWhiteSpace(param.Value))
            {
                if (param.Type == TriggerFunctionParameterType.Variable)
                {
                    return $"${param.Value}";
                }
                return param.Value;
            }

            return $"<{param.Type}>";
        }

        /// <summary>
        /// Checks for common corruption patterns in trigger data
        /// </summary>
        public static List<string> DetectCorruption(TriggerDefinition trigger, MapTriggers map)
        {
            var issues = new List<string>();

            // Check 1: Empty or null name
            if (string.IsNullOrWhiteSpace(trigger.Name))
            {
                issues.Add("CORRUPT: Trigger has empty name");
            }

            // Check 2: Invalid ParentId
            if (trigger.ParentId < -1)
            {
                issues.Add($"CORRUPT: Invalid ParentId={trigger.ParentId} (should be >= -1)");
            }

            // Check 3: Extremely high ParentId that doesn't exist
            if (trigger.ParentId > 1000)
            {
                issues.Add($"SUSPICIOUS: Very high ParentId={trigger.ParentId} (might be corrupted)");
            }

            // Check 4: Empty functions
            foreach (var func in trigger.Functions)
            {
                if (string.IsNullOrWhiteSpace(func.Name))
                {
                    issues.Add($"CORRUPT: Function has empty name (Type={func.Type})");
                }

                // Check for null bytes in function name
                if (func.Name != null && func.Name.Contains('\0'))
                {
                    issues.Add($"CORRUPT: Function name contains null bytes: '{func.Name.Replace("\0", "\\0")}'");
                }

                // Check parameters
                foreach (var param in func.Parameters)
                {
                    if (param.Value != null && param.Value.Contains('\0'))
                    {
                        issues.Add($"CORRUPT: Parameter value contains null bytes in function '{func.Name}'");
                    }

                    // Check for missing required values
                    if (param.Type == TriggerFunctionParameterType.Variable && string.IsNullOrWhiteSpace(param.Value))
                    {
                        issues.Add($"CORRUPT: Variable parameter has empty value in function '{func.Name}'");
                    }
                }
            }

            // Check 5: Missing variables
            var referencedVars = CollectVariableReferences(trigger);
            var mapVarNames = new HashSet<string>(map.Variables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);

            foreach (var varName in referencedVars)
            {
                if (!mapVarNames.Contains(varName))
                {
                    issues.Add($"MISSING VARIABLE: '{varName}' referenced but not in map");
                }
            }

            // Check 6: Circular references in nested functions (prevent infinite loops)
            try
            {
                CheckCircularReferences(trigger);
            }
            catch (Exception ex)
            {
                issues.Add($"CORRUPT: Possible circular reference in nested functions: {ex.Message}");
            }

            return issues;
        }

        private static void CheckCircularReferences(TriggerDefinition trigger, int maxDepth = 100)
        {
            foreach (var func in trigger.Functions)
            {
                CheckFunctionDepth(func, 0, maxDepth);
            }
        }

        private static void CheckFunctionDepth(TriggerFunction func, int currentDepth, int maxDepth)
        {
            if (currentDepth > maxDepth)
            {
                throw new InvalidOperationException($"Function nesting exceeds {maxDepth} levels - possible circular reference");
            }

            foreach (var param in func.Parameters)
            {
                if (param.Function != null)
                {
                    CheckFunctionDepth(param.Function, currentDepth + 1, maxDepth);
                }

                if (param.ArrayIndexer != null)
                {
                    CheckParameterDepth(param.ArrayIndexer, currentDepth + 1, maxDepth);
                }
            }

            foreach (var child in func.ChildFunctions)
            {
                CheckFunctionDepth(child, currentDepth + 1, maxDepth);
            }
        }

        private static void CheckParameterDepth(TriggerFunctionParameter param, int currentDepth, int maxDepth)
        {
            if (currentDepth > maxDepth)
            {
                throw new InvalidOperationException($"Parameter nesting exceeds {maxDepth} levels - possible circular reference");
            }

            if (param.Function != null)
            {
                CheckFunctionDepth(param.Function, currentDepth + 1, maxDepth);
            }

            if (param.ArrayIndexer != null)
            {
                CheckParameterDepth(param.ArrayIndexer, currentDepth + 1, maxDepth);
            }
        }
    }
}
