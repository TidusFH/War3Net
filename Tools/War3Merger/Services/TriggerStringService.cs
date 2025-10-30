// ------------------------------------------------------------------------------
// <copyright file="TriggerStringService.cs" company="Drake53">
// Licensed under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using War3Net.Build.Extensions;
using War3Net.Build.Script;
using War3Net.IO.Mpq;

namespace War3Net.Tools.TriggerMerger.Services
{
    /// <summary>
    /// Service for managing trigger strings (.wts files) when merging triggers.
    /// </summary>
    internal class TriggerStringService
    {
        private static readonly Regex TriggerStringPattern = new Regex(@"TRIGSTR_(\d+)", RegexOptions.Compiled);

        /// <summary>
        /// Scans triggers for TRIGSTR_ references and returns the set of required string IDs.
        /// </summary>
        public HashSet<uint> GetRequiredStringIds(List<TriggerDefinition> triggers)
        {
            var stringIds = new HashSet<uint>();

            foreach (var trigger in triggers)
            {
                // Scan trigger name
                ExtractStringIds(trigger.Name, stringIds);

                // Scan trigger description
                if (!string.IsNullOrEmpty(trigger.Description))
                {
                    ExtractStringIds(trigger.Description, stringIds);
                }

                // Scan all trigger functions (events, conditions, actions)
                if (trigger.Functions != null)
                {
                    foreach (var function in trigger.Functions)
                    {
                        ScanFunction(function, stringIds);
                    }
                }
            }

            return stringIds;
        }

        /// <summary>
        /// Reads trigger strings from a map file.
        /// </summary>
        public TriggerStrings? ReadTriggerStrings(string mapPath)
        {
            try
            {
                using var archive = MpqArchive.Open(mapPath, loadListFile: true);
                var wtsFileName = TriggerStrings.MapFileName; // "war3map.wts"

                if (!archive.FileExists(wtsFileName))
                {
                    Console.WriteLine($"  Note: {Path.GetFileName(mapPath)} does not have a {wtsFileName} file");
                    return null;
                }

                using var wtsStream = archive.OpenFile(wtsFileName);
                using var reader = new StreamReader(wtsStream);
                return reader.ReadTriggerStrings();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Warning: Could not read trigger strings: {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }

        /// <summary>
        /// Merges required strings from source into target trigger strings.
        /// </summary>
        public TriggerStrings MergeTriggerStrings(
            TriggerStrings? sourceTriggerStrings,
            TriggerStrings? targetTriggerStrings,
            HashSet<uint> requiredStringIds)
        {
            // If target doesn't have .wts, create new one
            var result = targetTriggerStrings ?? new TriggerStrings();

            if (sourceTriggerStrings == null || !requiredStringIds.Any())
            {
                return result;
            }

            // Build a dictionary of existing target strings for quick lookup
            var existingKeys = new HashSet<uint>(result.Strings.Select(s => s.Key));

            // Add required strings from source that don't exist in target
            foreach (var sourceString in sourceTriggerStrings.Strings)
            {
                if (requiredStringIds.Contains(sourceString.Key) && !existingKeys.Contains(sourceString.Key))
                {
                    result.Strings.Add(new TriggerString
                    {
                        Key = sourceString.Key,
                        KeyPrecision = sourceString.KeyPrecision,
                        Value = sourceString.Value,
                        Comment = sourceString.Comment,
                        EmptyLineCount = sourceString.EmptyLineCount,
                    });

                    Console.WriteLine($"    Added TRIGSTR_{sourceString.Key:D3}: \"{sourceString.Value?.Substring(0, Math.Min(50, sourceString.Value?.Length ?? 0))}...\"");
                }
            }

            return result;
        }

        /// <summary>
        /// Writes trigger strings to a map file.
        /// </summary>
        public void WriteTriggerStrings(string mapPath, string outputPath, TriggerStrings triggerStrings)
        {
            try
            {
                // Open the original archive
                using var originalArchive = MpqArchive.Open(mapPath, loadListFile: true);

                // Create a custom builder
                var builder = new CustomMpqArchiveBuilder(originalArchive);

                var wtsFileName = TriggerStrings.MapFileName;

                // Serialize trigger strings to text
                using var stringStream = new MemoryStream();
                using var writer = new StreamWriter(stringStream);
                writer.WriteTriggerStrings(triggerStrings);
                writer.Flush();

                var stringData = stringStream.ToArray();
                Console.WriteLine($"    Serialized {triggerStrings.Strings.Count} trigger strings ({stringData.Length} bytes)");

                // Add the .wts file to the builder
                using var newStringStream = new MemoryStream(stringData);
                newStringStream.Position = 0;
                var mpqFile = MpqFile.New(newStringStream, wtsFileName, leaveOpen: true);
                builder.AddFile(mpqFile);

                // Save with pre-archive data
                // Cast to MpqArchiveBuilder to use extension method
                ((MpqArchiveBuilder)builder).SaveWithPreArchiveData(outputPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to write trigger strings: {ex.Message}", ex);
            }
        }

        private void ExtractStringIds(string text, HashSet<uint> stringIds)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var matches = TriggerStringPattern.Matches(text);
            foreach (Match match in matches)
            {
                if (uint.TryParse(match.Groups[1].Value, out var id))
                {
                    stringIds.Add(id);
                }
            }
        }

        private void ScanFunction(TriggerFunction function, HashSet<uint> stringIds)
        {
            // Scan function name
            if (!string.IsNullOrEmpty(function.Name))
            {
                ExtractStringIds(function.Name, stringIds);
            }

            // Scan parameters
            if (function.Parameters != null)
            {
                foreach (var param in function.Parameters)
                {
                    ScanParameter(param, stringIds);
                }
            }

            // Scan child functions recursively
            if (function.ChildFunctions != null)
            {
                foreach (var childFunc in function.ChildFunctions)
                {
                    ScanFunction(childFunc, stringIds);
                }
            }
        }

        private void ScanParameter(TriggerFunctionParameter param, HashSet<uint> stringIds)
        {
            // Scan parameter value
            if (!string.IsNullOrEmpty(param.Value))
            {
                ExtractStringIds(param.Value, stringIds);
            }

            // Scan nested function if present
            if (param.Function != null)
            {
                ScanFunction(param.Function, stringIds);
            }

            // Scan array indexer recursively
            if (param.ArrayIndexer != null)
            {
                ScanParameter(param.ArrayIndexer, stringIds);
            }
        }
    }
}
