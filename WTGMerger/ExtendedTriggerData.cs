using System;
using System.IO;
using System.Linq;
using System.Reflection;
using War3Net.Build.Script;

namespace WTGMerger
{
    /// <summary>
    /// Helper class to load extended TriggerData with YDWE/KKWE/dzapi support.
    /// Uses reflection to access the internal TriggerData constructor.
    /// </summary>
    public static class ExtendedTriggerData
    {
        private static TriggerData? _extendedData;
        private static bool _initialized;
        private static string? _loadedFrom;

        /// <summary>
        /// Gets the extended TriggerData if loaded, otherwise returns TriggerData.Default
        /// </summary>
        public static TriggerData Instance => _extendedData ?? TriggerData.Default;

        /// <summary>
        /// Returns true if extended TriggerData has been loaded
        /// </summary>
        public static bool IsExtended => _extendedData != null;

        /// <summary>
        /// Gets the path from which extended TriggerData was loaded
        /// </summary>
        public static string? LoadedFrom => _loadedFrom;

        /// <summary>
        /// Initializes extended TriggerData from a file.
        /// </summary>
        /// <param name="triggerDataPath">Path to extended TriggerData.txt file</param>
        /// <returns>True if successfully loaded, false otherwise</returns>
        public static bool Initialize(string triggerDataPath)
        {
            if (_initialized && _extendedData != null)
            {
                Console.WriteLine($"Extended TriggerData already loaded from: {_loadedFrom}");
                return true;
            }

            if (!File.Exists(triggerDataPath))
            {
                Console.WriteLine($"Extended TriggerData file not found: {triggerDataPath}");
                return false;
            }

            try
            {
                Console.WriteLine($"Loading extended TriggerData from: {triggerDataPath}");

                var content = File.ReadAllText(triggerDataPath);
                using var reader = new StringReader(content);

                // Use reflection to call internal constructor
                var constructorInfo = typeof(TriggerData).GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    new[] { typeof(StringReader) },
                    null);

                if (constructorInfo == null)
                {
                    Console.WriteLine("ERROR: Could not find TriggerData constructor");
                    return false;
                }

                _extendedData = (TriggerData)constructorInfo.Invoke(new object[] { reader });
                _loadedFrom = triggerDataPath;
                _initialized = true;

                // Count functions
                int eventCount = _extendedData.TriggerEvents?.Count ?? 0;
                int condCount = _extendedData.TriggerConditions?.Count ?? 0;
                int actionCount = _extendedData.TriggerActions?.Count ?? 0;
                int callCount = _extendedData.TriggerCalls?.Count ?? 0;

                Console.WriteLine($"✓ Loaded extended TriggerData:");
                Console.WriteLine($"  Events: {eventCount}");
                Console.WriteLine($"  Conditions: {condCount}");
                Console.WriteLine($"  Actions: {actionCount}");
                Console.WriteLine($"  Calls: {callCount}");
                Console.WriteLine($"  Total: {eventCount + condCount + actionCount + callCount}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR loading extended TriggerData: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"  Inner: {ex.InnerException.Message}");
                    if (ex.InnerException.StackTrace != null)
                    {
                        // Extract the relevant part of the stack trace
                        var stackLines = ex.InnerException.StackTrace.Split('\n');
                        foreach (var line in stackLines.Take(5))
                        {
                            if (line.Contains("TriggerData") || line.Contains("Read"))
                            {
                                Console.WriteLine($"  Stack: {line.Trim()}");
                            }
                        }
                    }
                }
                
                // Try to identify the problematic line
                Console.WriteLine("\nTrying to identify problematic content...");
                TryIdentifyProblem(triggerDataPath);
                
                return false;
            }
        }

        /// <summary>
        /// Auto-initialize by looking for extended TriggerData in common locations.
        /// If not found, automatically generates it from War3 Patches folder.
        /// </summary>
        public static bool AutoInitialize()
        {
            if (_initialized && _extendedData != null)
            {
                return true;
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var searchPaths = new[]
            {
                Path.Combine(baseDir, "ExtendedTriggerData.txt"),
                "ExtendedTriggerData.txt",
                "../ExtendedTriggerData.txt",
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    Console.WriteLine($"Found extended TriggerData at: {path}");
                    return Initialize(path);
                }
            }

            // Not found - try to auto-generate it
            Console.WriteLine("ExtendedTriggerData.txt not found. Attempting to generate...");
            return AutoGenerate();
        }

        /// <summary>
        /// Automatically generate ExtendedTriggerData.txt from War3 Patches
        /// </summary>
        private static bool AutoGenerate()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Find War3 Patches folder
            var war3PatchesPaths = new[]
            {
                Path.Combine(baseDir, "War3 Patches"),
                "War3 Patches",
                "../War3 Patches",
                "../../War3 Patches",
            };
            
            string? war3PatchesFolder = null;
            foreach (var path in war3PatchesPaths)
            {
                if (Directory.Exists(path))
                {
                    war3PatchesFolder = Path.GetFullPath(path);
                    break;
                }
            }
            
            if (war3PatchesFolder == null)
            {
                Console.WriteLine("  War3 Patches folder not found. Using default TriggerData.");
                _initialized = true;
                return false;
            }
            
            // Find base TriggerData.txt
            var triggerDataPaths = new[]
            {
                Path.Combine(baseDir, "TriggerData.txt"),
                "TriggerData.txt",
                "../TriggerData.txt",
                "src/War3Net.Build.Core/Resources/TriggerData.txt",
                "../src/War3Net.Build.Core/Resources/TriggerData.txt",
            };
            
            string? baseTriggerData = null;
            foreach (var path in triggerDataPaths)
            {
                if (File.Exists(path))
                {
                    baseTriggerData = Path.GetFullPath(path);
                    break;
                }
            }
            
            if (baseTriggerData == null)
            {
                Console.WriteLine("  Base TriggerData.txt not found. Using default TriggerData.");
                _initialized = true;
                return false;
            }
            
            // Generate to executable directory
            var outputPath = Path.Combine(baseDir, "ExtendedTriggerData.txt");
            
            Console.WriteLine($"  War3 Patches: {war3PatchesFolder}");
            Console.WriteLine($"  Base TriggerData: {baseTriggerData}");
            Console.WriteLine($"  Output: {outputPath}");
            Console.WriteLine();
            
            try
            {
                TriggerDataMerger.MergeTriggerData(war3PatchesFolder, baseTriggerData, outputPath);
                return Initialize(outputPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR generating: {ex.Message}");
                _initialized = true;
                return false;
            }
        }

        /// <summary>
        /// Generate extended TriggerData by merging all sources
        /// </summary>
        public static bool GenerateAndInitialize(string war3PatchesFolder, string baseTriggerDataPath, string outputPath)
        {
            try
            {
                Console.WriteLine("\n=== Generating Extended TriggerData ===\n");
                TriggerDataMerger.MergeTriggerData(war3PatchesFolder, baseTriggerDataPath, outputPath);
                return Initialize(outputPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR generating extended TriggerData: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if a function exists in the TriggerData
        /// </summary>
        public static bool HasFunction(string functionName, TriggerFunctionType type)
        {
            var data = Instance;
            return type switch
            {
                TriggerFunctionType.Event => data.TriggerEvents?.ContainsKey(functionName) ?? false,
                TriggerFunctionType.Condition => data.TriggerConditions?.ContainsKey(functionName) ?? false,
                TriggerFunctionType.Action => data.TriggerActions?.ContainsKey(functionName) ?? false,
                TriggerFunctionType.Call => data.TriggerCalls?.ContainsKey(functionName) ?? false,
                _ => false
            };
        }

        /// <summary>
        /// Get argument count for a function
        /// </summary>
        public static int GetArgumentCount(string functionName, TriggerFunctionType type)
        {
            var data = Instance;
            try
            {
                return type switch
                {
                    TriggerFunctionType.Event => data.TriggerEvents[functionName].ArgumentTypes.Length,
                    TriggerFunctionType.Condition => data.TriggerConditions[functionName].ArgumentTypes.Length,
                    TriggerFunctionType.Action => data.TriggerActions[functionName].ArgumentTypes.Length,
                    TriggerFunctionType.Call => data.TriggerCalls[functionName].ArgumentTypes.Length,
                    _ => 0
                };
            }
            catch
            {
                return -1; // Function not found
            }
        }

        /// <summary>
        /// Reset to allow re-initialization
        /// </summary>
        public static void Reset()
        {
            _extendedData = null;
            _initialized = false;
            _loadedFrom = null;
        }

        /// <summary>
        /// Try to identify which section/function is causing parsing problems
        /// </summary>
        private static void TryIdentifyProblem(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                string? currentSection = null;
                string? lastFunction = null;
                int lastFunctionLine = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("//")) continue;

                    // Section header
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Substring(1, line.Length - 2);
                        continue;
                    }

                    // Function definition (not metadata)
                    if (!line.StartsWith("_") && line.Contains("="))
                    {
                        var parts = line.Split('=', 2);
                        lastFunction = parts[0];
                        lastFunctionLine = i + 1;

                        // Check for potential issues
                        if (currentSection == "TriggerEvents" || currentSection == "TriggerConditions" ||
                            currentSection == "TriggerActions" || currentSection == "TriggerCalls")
                        {
                            var values = parts[1].Split(',');
                            
                            // Check if it's a valid function definition
                            if (currentSection == "TriggerCalls" && values.Length < 3)
                            {
                                Console.WriteLine($"  PROBLEM at line {i + 1}: TriggerCall '{lastFunction}' has fewer than 3 values");
                                Console.WriteLine($"    Line: {line}");
                            }
                            
                            if (!int.TryParse(values[0], out _))
                            {
                                Console.WriteLine($"  PROBLEM at line {i + 1}: First value is not an integer for '{lastFunction}'");
                                Console.WriteLine($"    Line: {line}");
                            }
                        }
                    }
                }

                Console.WriteLine($"  Last function parsed: {lastFunction} (line {lastFunctionLine}) in section [{currentSection}]");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Could not analyze file: {ex.Message}");
            }
        }
    }
}

