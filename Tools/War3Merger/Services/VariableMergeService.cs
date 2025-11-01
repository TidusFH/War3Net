// ------------------------------------------------------------------------------
// <copyright file="VariableMergeService.cs" company="Drake53">
// Licensed under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using War3Net.Build.Script;

namespace War3Net.Tools.TriggerMerger.Services
{
    /// <summary>
    /// Service for identifying and merging global variables referenced by triggers.
    /// </summary>
    internal class VariableMergeService
    {
        // Pattern to match variable references like "udg_VariableName" or just "VariableName" in functions
        private static readonly Regex VariablePattern = new Regex(@"\budg_(\w+)\b|\b([A-Z]\w+)\b", RegexOptions.Compiled);

        /// <summary>
        /// Scans triggers for variable references and returns the set of required variable names.
        /// </summary>
        public HashSet<string> GetReferencedVariables(List<TriggerDefinition> triggers, List<VariableDefinition> sourceVariables)
        {
            var referencedVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sourceVarNames = new HashSet<string>(sourceVariables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);

            foreach (var trigger in triggers)
            {
                // Scan all trigger functions
                if (trigger.Functions != null)
                {
                    foreach (var function in trigger.Functions)
                    {
                        ScanFunction(function, referencedVars, sourceVarNames);
                    }
                }
            }

            return referencedVars;
        }

        /// <summary>
        /// Merges required variables from source into target.
        /// </summary>
        public int MergeVariables(
            MapTriggers source,
            MapTriggers target,
            HashSet<string> requiredVariableNames)
        {
            if (source.Variables == null || !requiredVariableNames.Any())
            {
                return 0;
            }

            // Build a dictionary of existing target variables for quick lookup
            var existingVarNames = new HashSet<string>(
                target.Variables?.Select(v => v.Name) ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            int addedCount = 0;

            // Add required variables from source that don't exist in target
            foreach (var sourceVar in source.Variables)
            {
                if (requiredVariableNames.Contains(sourceVar.Name) && !existingVarNames.Contains(sourceVar.Name))
                {
                    var newVar = new VariableDefinition
                    {
                        Name = sourceVar.Name,
                        Type = sourceVar.Type,
                        IsArray = sourceVar.IsArray,
                        ArraySize = sourceVar.ArraySize,
                        IsInitialized = sourceVar.IsInitialized,
                        InitialValue = sourceVar.InitialValue,
                    };

                    target.Variables.Add(newVar);
                    addedCount++;

                    Console.WriteLine($"    Added variable: {sourceVar.Name} ({sourceVar.Type}{(sourceVar.IsArray ? $"[{sourceVar.ArraySize}]" : "")})");
                }
            }

            return addedCount;
        }

        private void ScanFunction(TriggerFunction function, HashSet<string> referencedVars, HashSet<string> sourceVarNames)
        {
            // Scan function name
            if (!string.IsNullOrEmpty(function.Name))
            {
                ExtractVariableNames(function.Name, referencedVars, sourceVarNames);
            }

            // Scan parameters
            if (function.Parameters != null)
            {
                foreach (var param in function.Parameters)
                {
                    ScanParameter(param, referencedVars, sourceVarNames);
                }
            }

            // Scan child functions recursively
            if (function.ChildFunctions != null)
            {
                foreach (var childFunc in function.ChildFunctions)
                {
                    ScanFunction(childFunc, referencedVars, sourceVarNames);
                }
            }
        }

        private void ScanParameter(TriggerFunctionParameter param, HashSet<string> referencedVars, HashSet<string> sourceVarNames)
        {
            // Scan parameter value
            if (!string.IsNullOrEmpty(param.Value))
            {
                ExtractVariableNames(param.Value, referencedVars, sourceVarNames);
            }

            // Scan nested function if present
            if (param.Function != null)
            {
                ScanFunction(param.Function, referencedVars, sourceVarNames);
            }

            // Scan array indexer recursively
            if (param.ArrayIndexer != null)
            {
                ScanParameter(param.ArrayIndexer, referencedVars, sourceVarNames);
            }
        }

        private void ExtractVariableNames(string text, HashSet<string> referencedVars, HashSet<string> sourceVarNames)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var matches = VariablePattern.Matches(text);
            foreach (Match match in matches)
            {
                // Group 1: udg_VariableName
                if (match.Groups[1].Success)
                {
                    var varName = match.Groups[1].Value;
                    if (sourceVarNames.Contains(varName))
                    {
                        referencedVars.Add(varName);
                    }
                }
                // Group 2: VariableName (without udg_ prefix)
                else if (match.Groups[2].Success)
                {
                    var varName = match.Groups[2].Value;
                    if (sourceVarNames.Contains(varName))
                    {
                        referencedVars.Add(varName);
                    }
                }
            }
        }
    }
}
