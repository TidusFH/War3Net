using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using War3Net.Build;

namespace ObjectMerger.Services
{
    /// <summary>
    /// Reads WTS (string table) files from maps to resolve TRIGSTR_* references
    /// </summary>
    public class StringTableReader
    {
        private readonly Dictionary<int, string> strings = new();

        /// <summary>
        /// Load string table from a map
        /// </summary>
        public static StringTableReader? LoadFromMap(Map map, string mapPath)
        {
            try
            {
                // Try to read war3map.wts from the map archive
                using var archive = War3Net.IO.Mpq.MpqArchive.Open(mapPath, true);

                if (!archive.FileExists("war3map.wts"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  Note: Map has no string table (war3map.wts)");
                    Console.ResetColor();
                    return null;
                }

                var reader = new StringTableReader();

                using var stream = archive.OpenFile("war3map.wts");
                using var streamReader = new StreamReader(stream, Encoding.UTF8);

                reader.Parse(streamReader);

                Console.WriteLine($"  Loaded {reader.strings.Count} strings from WTS");

                return reader;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Warning: Could not load string table: {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }

        /// <summary>
        /// Parse WTS file format
        /// </summary>
        private void Parse(StreamReader reader)
        {
            int? currentKey = null;
            StringBuilder currentValue = new StringBuilder();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null) continue;

                line = line.Trim();

                // String entry starts with "STRING <number>"
                if (line.StartsWith("STRING ", StringComparison.OrdinalIgnoreCase))
                {
                    // Save previous entry if exists
                    if (currentKey.HasValue && currentValue.Length > 0)
                    {
                        strings[currentKey.Value] = currentValue.ToString();
                    }

                    // Parse new entry key
                    var keyStr = line.Substring(7).Trim();
                    if (int.TryParse(keyStr, out int key))
                    {
                        currentKey = key;
                        currentValue.Clear();
                    }
                }
                // Line starting with { starts the value
                else if (line == "{")
                {
                    // Value starts on next line
                    continue;
                }
                // Line with } ends the value
                else if (line == "}")
                {
                    // Value is complete (already in currentValue)
                    continue;
                }
                // Everything else is part of the value
                else if (currentKey.HasValue && line.Length > 0)
                {
                    if (currentValue.Length > 0)
                        currentValue.AppendLine();
                    currentValue.Append(line);
                }
            }

            // Save last entry
            if (currentKey.HasValue && currentValue.Length > 0)
            {
                strings[currentKey.Value] = currentValue.ToString();
            }
        }

        /// <summary>
        /// Resolve a TRIGSTR_* reference to actual text
        /// </summary>
        public string? Resolve(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Check if it's a TRIGSTR reference
            if (value.StartsWith("TRIGSTR_", StringComparison.OrdinalIgnoreCase))
            {
                var keyStr = value.Substring(8);
                if (int.TryParse(keyStr, out int key))
                {
                    if (strings.TryGetValue(key, out string? resolved))
                    {
                        return resolved;
                    }
                }
            }

            return value;
        }

        /// <summary>
        /// Get resolved name with TRIGSTR reference for debugging
        /// </summary>
        public string GetDebugName(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "(no name)";

            var resolved = Resolve(value);

            if (resolved != value && resolved != null)
            {
                // Show both resolved and original
                return $"{resolved} [{value}]";
            }

            return value;
        }
    }
}
