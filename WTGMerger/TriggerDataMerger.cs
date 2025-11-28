using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WTGMerger
{
    /// <summary>
    /// Merges TriggerData from multiple sources including YDWE TOML format.
    /// Supports:
    /// - Standard TriggerData.txt format (War3Net)
    /// - YDWE TOML format (action.txt, call.txt, condition.txt, event.txt)
    /// - dzapi/kkapi/bzapi TriggerData.txt format
    /// </summary>
    public static class TriggerDataMerger
    {
        public class TriggerFunction
        {
            public string Name { get; set; } = "";
            public int GameVersion { get; set; } = 0;
            public List<string> ArgumentTypes { get; set; } = new();
            public string? DisplayName { get; set; }
            public string? Parameters { get; set; }
            public List<string>? Defaults { get; set; }
            public string? Category { get; set; }
            public string? ScriptName { get; set; }
            public string? ReturnType { get; set; } // For TriggerCalls
            public bool CanUseInEvents { get; set; } = false; // For TriggerCalls
        }

        /// <summary>
        /// Parse YDWE TOML-style file (action.txt, call.txt, condition.txt, event.txt)
        /// </summary>
        public static List<TriggerFunction> ParseYdweToml(string filePath)
        {
            var functions = new List<TriggerFunction>();
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);

            TriggerFunction? current = null;
            bool inArgs = false;
            string? currentArgType = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("//") || line.StartsWith("#")) continue;

                // Function header: [FunctionName]
                if (line.StartsWith("[") && line.EndsWith("]") && !line.StartsWith("[["))
                {
                    // Save previous function
                    if (current != null)
                    {
                        functions.Add(current);
                    }

                    current = new TriggerFunction
                    {
                        Name = line.Substring(1, line.Length - 2)
                    };
                    inArgs = false;
                    continue;
                }

                if (current == null) continue;

                // Args section marker: [[.args]]
                if (line == "[[.args]]")
                {
                    inArgs = true;
                    currentArgType = null;
                    continue;
                }

                // Key = value pairs
                var match = Regex.Match(line, @"^(\w+)\s*=\s*(.*)$");
                if (match.Success)
                {
                    var key = match.Groups[1].Value;
                    var value = match.Groups[2].Value.Trim();

                    // Remove quotes if present
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    if (inArgs)
                    {
                        // Argument properties
                        if (key == "type")
                        {
                            current.ArgumentTypes.Add(value);
                        }
                        // Skip default, min, max for args
                    }
                    else
                    {
                        // Function properties
                        switch (key)
                        {
                            case "title":
                                // Add quotes to match TriggerData.txt format
                                current.DisplayName = $"\"{value}\"";
                                break;
                            case "description":
                                // Add quotes to match TriggerData.txt format
                                current.Parameters = $"\"{value}\"";
                                break;
                            case "category":
                                current.Category = value;
                                break;
                            case "script_name":
                                current.ScriptName = value;
                                break;
                            case "returns":
                                current.ReturnType = value;
                                break;
                        }
                    }
                }
            }

            // Don't forget the last function
            if (current != null)
            {
                functions.Add(current);
            }

            return functions;
        }

        /// <summary>
        /// Parse standard TriggerData.txt section
        /// </summary>
        public static Dictionary<string, List<TriggerFunction>> ParseTriggerData(string filePath)
        {
            var sections = new Dictionary<string, List<TriggerFunction>>
            {
                ["TriggerEvents"] = new(),
                ["TriggerConditions"] = new(),
                ["TriggerActions"] = new(),
                ["TriggerCalls"] = new()
            };

            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            string? currentSection = null;
            string? previousSection = null;
            TriggerFunction? currentFunction = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(trimmed)) continue;
                if (trimmed.StartsWith("//")) continue;

                // Section header
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    // Save previous function to PREVIOUS section before switching
                    if (currentFunction != null && previousSection != null)
                    {
                        sections[previousSection].Add(currentFunction);
                        currentFunction = null;
                    }

                    var sectionName = trimmed.Substring(1, trimmed.Length - 2);
                    previousSection = currentSection;
                    currentSection = sections.ContainsKey(sectionName) ? sectionName : null;
                    continue;
                }

                if (currentSection == null) continue;

                var match = Regex.Match(trimmed, @"^([^=]+)=(.*)$");
                if (!match.Success) continue;

                var key = match.Groups[1].Value.Trim();
                var value = match.Groups[2].Value.Trim();

                // Metadata line (starts with _)
                if (key.StartsWith("_"))
                {
                    if (currentFunction != null)
                    {
                        var parts = key.Split('_', 3, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && parts[0] == currentFunction.Name)
                        {
                            switch (parts[1])
                            {
                                case "DisplayName":
                                    currentFunction.DisplayName = value; // Keep as-is with quotes
                                    break;
                                case "Parameters":
                                    currentFunction.Parameters = value; // Keep as-is with quotes
                                    break;
                                case "Defaults":
                                    currentFunction.Defaults = value.Split(',').ToList();
                                    break;
                                case "Category":
                                    currentFunction.Category = value;
                                    break;
                                case "ScriptName":
                                    currentFunction.ScriptName = value;
                                    break;
                            }
                        }
                    }
                    continue;
                }

                // New function definition
                // Save previous function
                if (currentFunction != null)
                {
                    sections[currentSection].Add(currentFunction);
                }

                var values = value.Split(',');
                currentFunction = new TriggerFunction
                {
                    Name = key,
                    GameVersion = int.TryParse(values[0], out var ver) ? ver : 0
                };

                if (currentSection == "TriggerCalls")
                {
                    // TriggerCalls format: Name=version,canUseInEvents,returnType,arg1,arg2,...
                    if (values.Length > 1)
                        currentFunction.CanUseInEvents = values[1] == "1";
                    if (values.Length > 2)
                        currentFunction.ReturnType = values[2];
                    if (values.Length > 3)
                        currentFunction.ArgumentTypes = values[3..].ToList();
                }
                else
                {
                    // Events/Conditions/Actions format: Name=version,arg1,arg2,...
                    if (values.Length > 1)
                    {
                        currentFunction.ArgumentTypes = values[1..].ToList();
                    }
                }
            }

            // Don't forget the last function
            if (currentFunction != null && currentSection != null)
            {
                sections[currentSection].Add(currentFunction);
            }

            return sections;
        }

        /// <summary>
        /// Convert a list of functions to TriggerData.txt format
        /// </summary>
        public static string FunctionsToTriggerDataFormat(List<TriggerFunction> functions, string sectionType)
        {
            var sb = new StringBuilder();

            foreach (var func in functions)
            {
                if (sectionType == "TriggerCalls")
                {
                    // TriggerCalls format: Name=version,canUseInEvents,returnType,arg1,arg2,...
                    var canUse = func.CanUseInEvents ? "1" : "0";
                    var returnType = func.ReturnType ?? "nothing";
                    var args = string.Join(",", func.ArgumentTypes);
                    if (!string.IsNullOrEmpty(args))
                        sb.AppendLine($"{func.Name}={func.GameVersion},{canUse},{returnType},{args}");
                    else
                        sb.AppendLine($"{func.Name}={func.GameVersion},{canUse},{returnType}");
                }
                else
                {
                    // Events/Conditions/Actions format: Name=version,arg1,arg2,...
                    var args = string.Join(",", func.ArgumentTypes);
                    if (!string.IsNullOrEmpty(args))
                        sb.AppendLine($"{func.Name}={func.GameVersion},{args}");
                    else
                    {
                        // Events without arguments need "nothing" as placeholder
                        if (sectionType == "TriggerEvents")
                            sb.AppendLine($"{func.Name}={func.GameVersion},nothing");
                        else
                            sb.AppendLine($"{func.Name}={func.GameVersion}");
                    }
                }

                // Add metadata (preserve original format exactly)
                // Note: DisplayName and Parameters are required for proper parsing
                if (!string.IsNullOrEmpty(func.DisplayName))
                    sb.AppendLine($"_{func.Name}_DisplayName={func.DisplayName}");
                if (!string.IsNullOrEmpty(func.Parameters))
                    sb.AppendLine($"_{func.Name}_Parameters={func.Parameters}");
                
                // Always output Defaults line (even if empty) as parser requires it
                if (func.Defaults != null && func.Defaults.Count > 0)
                    sb.AppendLine($"_{func.Name}_Defaults={string.Join(",", func.Defaults)}");
                else
                    sb.AppendLine($"_{func.Name}_Defaults=");
                
                if (!string.IsNullOrEmpty(func.Category))
                    sb.AppendLine($"_{func.Name}_Category={func.Category}");
                if (!string.IsNullOrEmpty(func.ScriptName))
                    sb.AppendLine($"_{func.Name}_ScriptName={func.ScriptName}");

                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Merge all TriggerData sources from War3 Patches folder
        /// </summary>
        public static void MergeTriggerData(string war3PatchesFolder, string baseTriggerDataPath, string outputPath)
        {
            Console.WriteLine("=== TriggerData Merger ===\n");

            // Parse base TriggerData.txt
            Console.WriteLine($"Reading base TriggerData: {baseTriggerDataPath}");
            var baseSections = ParseTriggerData(baseTriggerDataPath);

            int baseCount = baseSections.Values.Sum(l => l.Count);
            Console.WriteLine($"  Base functions: {baseCount}");

            // Track all function names to avoid duplicates
            var allEvents = new Dictionary<string, TriggerFunction>(StringComparer.OrdinalIgnoreCase);
            var allConditions = new Dictionary<string, TriggerFunction>(StringComparer.OrdinalIgnoreCase);
            var allActions = new Dictionary<string, TriggerFunction>(StringComparer.OrdinalIgnoreCase);
            var allCalls = new Dictionary<string, TriggerFunction>(StringComparer.OrdinalIgnoreCase);

            // Add base functions
            foreach (var f in baseSections["TriggerEvents"]) allEvents[f.Name] = f;
            foreach (var f in baseSections["TriggerConditions"]) allConditions[f.Name] = f;
            foreach (var f in baseSections["TriggerActions"]) allActions[f.Name] = f;
            foreach (var f in baseSections["TriggerCalls"]) allCalls[f.Name] = f;

            // Process YDWE folder
            var ydweFolder = Path.Combine(war3PatchesFolder, "ydwe");
            if (Directory.Exists(ydweFolder))
            {
                Console.WriteLine($"\nProcessing YDWE folder: {ydweFolder}");
                ProcessYdweFolder(ydweFolder, allEvents, allConditions, allActions, allCalls);
            }

            // Process YDTrigger folder
            var ydTriggerFolder = Path.Combine(war3PatchesFolder, "YDTrigger");
            if (Directory.Exists(ydTriggerFolder))
            {
                Console.WriteLine($"\nProcessing YDTrigger folder: {ydTriggerFolder}");
                ProcessYdweFolder(ydTriggerFolder, allEvents, allConditions, allActions, allCalls);
            }

            // Process dzapi folder
            var dzapiFolder = Path.Combine(war3PatchesFolder, "dzapi", "ui");
            if (Directory.Exists(dzapiFolder))
            {
                Console.WriteLine($"\nProcessing dzapi folder: {dzapiFolder}");
                var dzapiTriggerData = Path.Combine(dzapiFolder, "TriggerData.txt");
                if (File.Exists(dzapiTriggerData))
                {
                    ProcessTriggerDataFile(dzapiTriggerData, allEvents, allConditions, allActions, allCalls);
                }
            }

            // Process dzapi2 folder
            var dzapi2Folder = Path.Combine(war3PatchesFolder, "dzapi2");
            if (Directory.Exists(dzapi2Folder))
            {
                Console.WriteLine($"\nProcessing dzapi2 folder: {dzapi2Folder}");
                ProcessYdweFolder(dzapi2Folder, allEvents, allConditions, allActions, allCalls);
            }

            // Process kkapi folder
            var kkapiFolder = Path.Combine(war3PatchesFolder, "kkapi");
            if (Directory.Exists(kkapiFolder))
            {
                Console.WriteLine($"\nProcessing kkapi folder: {kkapiFolder}");
                ProcessYdweFolder(kkapiFolder, allEvents, allConditions, allActions, allCalls);
            }

            // Process bzapi folder
            var bzapiFolder = Path.Combine(war3PatchesFolder, "bzapi");
            if (Directory.Exists(bzapiFolder))
            {
                Console.WriteLine($"\nProcessing bzapi folder: {bzapiFolder}");
                ProcessYdweFolder(bzapiFolder, allEvents, allConditions, allActions, allCalls);
            }

            // Process japi folder
            var japiFolder = Path.Combine(war3PatchesFolder, "japi");
            if (Directory.Exists(japiFolder))
            {
                Console.WriteLine($"\nProcessing japi folder: {japiFolder}");
                ProcessYdweFolder(japiFolder, allEvents, allConditions, allActions, allCalls);
            }

            // Generate merged output
            Console.WriteLine($"\n=== Generating merged TriggerData ===");
            Console.WriteLine($"  Events: {allEvents.Count}");
            Console.WriteLine($"  Conditions: {allConditions.Count}");
            Console.WriteLine($"  Actions: {allActions.Count}");
            Console.WriteLine($"  Calls: {allCalls.Count}");
            Console.WriteLine($"  Total: {allEvents.Count + allConditions.Count + allActions.Count + allCalls.Count}");

            // Read original file and preserve non-function sections
            var originalContent = File.ReadAllText(baseTriggerDataPath, Encoding.UTF8);

            // Build new content
            var sb = new StringBuilder();

            // Copy everything up to [TriggerEvents]
            var eventsStart = originalContent.IndexOf("[TriggerEvents]");
            if (eventsStart > 0)
            {
                sb.Append(originalContent.Substring(0, eventsStart));
            }

            // Add events section
            sb.AppendLine("[TriggerEvents]");
            sb.AppendLine("// Extended with YDWE/KKWE/dzapi functions");
            sb.AppendLine();
            sb.Append(FunctionsToTriggerDataFormat(allEvents.Values.ToList(), "TriggerEvents"));

            // Add conditions section
            sb.AppendLine("[TriggerConditions]");
            sb.AppendLine("// Extended with YDWE/KKWE/dzapi functions");
            sb.AppendLine();
            sb.Append(FunctionsToTriggerDataFormat(allConditions.Values.ToList(), "TriggerConditions"));

            // Add actions section
            sb.AppendLine("[TriggerActions]");
            sb.AppendLine("// Extended with YDWE/KKWE/dzapi functions");
            sb.AppendLine();
            sb.Append(FunctionsToTriggerDataFormat(allActions.Values.ToList(), "TriggerActions"));

            // Add calls section
            sb.AppendLine("[TriggerCalls]");
            sb.AppendLine("// Extended with YDWE/KKWE/dzapi functions");
            sb.AppendLine();
            sb.Append(FunctionsToTriggerDataFormat(allCalls.Values.ToList(), "TriggerCalls"));

            // Add default categories and triggers sections (extract properly, don't include malformed content)
            var defaultCatStart = originalContent.IndexOf("[DefaultTriggerCategories]");
            var defaultTriggersStart = originalContent.IndexOf("[DefaultTriggers]");
            
            if (defaultCatStart > 0 && defaultTriggersStart > defaultCatStart)
            {
                // Extract just [DefaultTriggerCategories] section up to [DefaultTriggers]
                sb.AppendLine("[DefaultTriggerCategories]");
                var catContent = originalContent.Substring(defaultCatStart + "[DefaultTriggerCategories]".Length,
                    defaultTriggersStart - defaultCatStart - "[DefaultTriggerCategories]".Length).Trim();
                
                // Only include lines that look like category definitions (Category## or NumCategories)
                foreach (var line in catContent.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//") || 
                        trimmed.StartsWith("NumCategories") || trimmed.StartsWith("Category"))
                    {
                        sb.AppendLine(trimmed);
                    }
                }
                sb.AppendLine();
                
                // Find end of [DefaultTriggers] section (next [ bracket or end of file)
                var defaultTriggersEnd = originalContent.IndexOf("\n[", defaultTriggersStart + 1);
                if (defaultTriggersEnd < 0) defaultTriggersEnd = originalContent.Length;
                
                sb.AppendLine("[DefaultTriggers]");
                var trigContent = originalContent.Substring(defaultTriggersStart + "[DefaultTriggers]".Length,
                    defaultTriggersEnd - defaultTriggersStart - "[DefaultTriggers]".Length).Trim();
                
                // Only include lines that look like trigger definitions
                foreach (var line in trigContent.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//") ||
                        trimmed.StartsWith("NumTriggers") || trimmed.StartsWith("Trigger"))
                    {
                        sb.AppendLine(trimmed);
                    }
                }
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("[DefaultTriggerCategories]");
                sb.AppendLine();
                sb.AppendLine("[DefaultTriggers]");
                sb.AppendLine();
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            Console.WriteLine($"\n✓ Merged TriggerData saved to: {outputPath}");
        }

        private static void ProcessYdweFolder(string folder,
            Dictionary<string, TriggerFunction> allEvents,
            Dictionary<string, TriggerFunction> allConditions,
            Dictionary<string, TriggerFunction> allActions,
            Dictionary<string, TriggerFunction> allCalls)
        {
            // Process event.txt
            var eventFile = Path.Combine(folder, "event.txt");
            if (File.Exists(eventFile))
            {
                var functions = ParseYdweToml(eventFile);
                Console.WriteLine($"  event.txt: {functions.Count} functions");
                foreach (var f in functions)
                {
                    if (!allEvents.ContainsKey(f.Name))
                        allEvents[f.Name] = f;
                }
            }

            // Process condition.txt
            var conditionFile = Path.Combine(folder, "condition.txt");
            if (File.Exists(conditionFile))
            {
                var functions = ParseYdweToml(conditionFile);
                Console.WriteLine($"  condition.txt: {functions.Count} functions");
                foreach (var f in functions)
                {
                    if (!allConditions.ContainsKey(f.Name))
                        allConditions[f.Name] = f;
                }
            }

            // Process action.txt
            var actionFile = Path.Combine(folder, "action.txt");
            if (File.Exists(actionFile))
            {
                var functions = ParseYdweToml(actionFile);
                Console.WriteLine($"  action.txt: {functions.Count} functions");
                foreach (var f in functions)
                {
                    if (!allActions.ContainsKey(f.Name))
                        allActions[f.Name] = f;
                }
            }

            // Process call.txt
            var callFile = Path.Combine(folder, "call.txt");
            if (File.Exists(callFile))
            {
                var functions = ParseYdweToml(callFile);
                Console.WriteLine($"  call.txt: {functions.Count} functions");
                foreach (var f in functions)
                {
                    if (!allCalls.ContainsKey(f.Name))
                        allCalls[f.Name] = f;
                }
            }

            // Process define.txt (usually contains types, not functions)
            // Skip for now
        }

        private static void ProcessTriggerDataFile(string filePath,
            Dictionary<string, TriggerFunction> allEvents,
            Dictionary<string, TriggerFunction> allConditions,
            Dictionary<string, TriggerFunction> allActions,
            Dictionary<string, TriggerFunction> allCalls)
        {
            Console.WriteLine($"  TriggerData.txt: {filePath}");
            var sections = ParseTriggerData(filePath);

            int count = 0;
            foreach (var f in sections["TriggerEvents"])
            {
                if (!allEvents.ContainsKey(f.Name))
                {
                    allEvents[f.Name] = f;
                    count++;
                }
            }
            foreach (var f in sections["TriggerConditions"])
            {
                if (!allConditions.ContainsKey(f.Name))
                {
                    allConditions[f.Name] = f;
                    count++;
                }
            }
            foreach (var f in sections["TriggerActions"])
            {
                if (!allActions.ContainsKey(f.Name))
                {
                    allActions[f.Name] = f;
                    count++;
                }
            }
            foreach (var f in sections["TriggerCalls"])
            {
                if (!allCalls.ContainsKey(f.Name))
                {
                    allCalls[f.Name] = f;
                    count++;
                }
            }
            Console.WriteLine($"    Added {count} new functions");
        }

    }
}

