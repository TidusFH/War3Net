using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using War3Net.Build.Script;
using War3Net.IO.Mpq;

namespace War3TriggerMerger
{
    /// <summary>
    /// Warcraft 3 1.27 Trigger Merger
    /// Designed specifically for OLD FORMAT (SubVersion=null) with position-based category IDs
    /// </summary>
    class Program
    {
        private static bool _debugMode = false;
        private static MapTriggers? _source;
        private static MapTriggers? _target;
        private static string _sourcePath = "";
        private static string _targetPath = "";
        private static string _outputPath = "";
        private static bool _hasChanges = false;

        static void Main(string[] args)
        {
            try
            {
                PrintHeader();
                InitializePaths(args);
                LoadMaps();
                ValidateOldFormat();
                EnsureProperStructure();
                InteractiveMenu();
            }
            catch (Exception ex)
            {
                PrintError($"Fatal error: {ex.Message}");
                if (_debugMode)
                {
                    Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
                }
                Environment.Exit(1);
            }
        }

        #region Initialization

        static void PrintHeader()
        {
            Console.WriteLine("================================================================");
            Console.WriteLine("  WARCRAFT 3 1.27 TRIGGER MERGER");
            Console.WriteLine("  Old Format (SubVersion=null) - Position-Based Category IDs");
            Console.WriteLine("================================================================\n");
        }

        static void InitializePaths(string[] args)
        {
            if (args.Length >= 2)
            {
                _sourcePath = args[0];
                _targetPath = args[1];
                _outputPath = args.Length > 2 ? args[2] : GenerateOutputPath(_targetPath);
            }
            else
            {
                _sourcePath = DetectMapFile("../Source");
                _targetPath = DetectMapFile("../Target");
                _outputPath = GenerateOutputPath(_targetPath);
            }

            Console.WriteLine("Map files:");
            Console.WriteLine($"  Source: {Path.GetFileName(_sourcePath)}");
            Console.WriteLine($"  Target: {Path.GetFileName(_targetPath)}");
            Console.WriteLine($"  Output: {Path.GetFileName(_outputPath)}\n");
        }

        static void LoadMaps()
        {
            Console.WriteLine("Loading maps...");

            _source = LoadMapTriggers(_sourcePath);
            Console.WriteLine($"+ Source: {CountCategories(_source)} categories, {CountTriggers(_source)} triggers, {_source.Variables.Count} variables");

            _target = LoadMapTriggers(_targetPath);
            Console.WriteLine($"+ Target: {CountCategories(_target)} categories, {CountTriggers(_target)} triggers, {_target.Variables.Count} variables");

            Console.WriteLine();
        }

        static void ValidateOldFormat()
        {
            if (_source!.SubVersion != null)
            {
                PrintWarning($"Source map is ENHANCED format (SubVersion={_source.SubVersion})");
                PrintWarning("This tool is designed for OLD format. Proceed with caution.");
            }
            else
            {
                PrintSuccess("Source map is OLD FORMAT (SubVersion=null) - Compatible!");
            }

            if (_target!.SubVersion != null)
            {
                PrintWarning($"Target map is ENHANCED format (SubVersion={_target.SubVersion})");
                PrintWarning("This tool is designed for OLD format. Proceed with caution.");
            }
            else
            {
                PrintSuccess("Target map is OLD FORMAT (SubVersion=null) - Compatible!");
            }

            Console.WriteLine();
        }

        static void EnsureProperStructure()
        {
            Console.WriteLine("Ensuring proper category structure...");
            FixCategoryStructure(_source!, "source");
            FixCategoryStructure(_target!, "target");
            PrintSuccess("Category structures validated and fixed!");
            Console.WriteLine();
        }

        #endregion

        #region Core Category Management (Position-Based System)

        /// <summary>
        /// CRITICAL FUNCTION: Ensures category IDs match their positions in TriggerItems array.
        /// This is THE MOST IMPORTANT function for WC3 1.27 old format compatibility.
        /// </summary>
        static void FixCategoryStructure(MapTriggers triggers, string mapName)
        {
            if (triggers.SubVersion != null)
            {
                // Enhanced format - skip position fixing
                if (_debugMode) Console.WriteLine($"[DEBUG] {mapName}: Enhanced format, skipping position fix");
                return;
            }

            var categories = GetCategories(triggers);
            if (categories.Count == 0) return;

            if (_debugMode)
            {
                Console.WriteLine($"\n[DEBUG] Fixing category structure for {mapName}");
                Console.WriteLine($"[DEBUG] Found {categories.Count} categories");
            }

            // STEP 1: Build ID mapping (oldId → newId where newId = position)
            var idMapping = new Dictionary<int, int>();
            for (int i = 0; i < categories.Count; i++)
            {
                idMapping[categories[i].Id] = i;

                if (_debugMode)
                {
                    Console.WriteLine($"[DEBUG] Category '{categories[i].Name}': OldID={categories[i].Id} → NewID={i}");
                }
            }

            // STEP 2: Remove all categories from TriggerItems
            foreach (var cat in categories)
            {
                triggers.TriggerItems.Remove(cat);
            }

            // STEP 3: Re-insert categories at positions 0,1,2,3... with ID = position
            for (int i = 0; i < categories.Count; i++)
            {
                categories[i].Id = i;
                categories[i].ParentId = 0; // OLD FORMAT: ParentId MUST be 0
                triggers.TriggerItems.Insert(i, categories[i]);
            }

            // STEP 4: Update trigger ParentIds using mapping
            var allTriggers = GetTriggers(triggers);
            foreach (var trigger in allTriggers)
            {
                if (idMapping.ContainsKey(trigger.ParentId))
                {
                    var oldParentId = trigger.ParentId;
                    trigger.ParentId = idMapping[oldParentId];

                    if (_debugMode && oldParentId != trigger.ParentId)
                    {
                        Console.WriteLine($"[DEBUG] Trigger '{trigger.Name}': ParentId {oldParentId} → {trigger.ParentId}");
                    }
                }
            }

            if (_debugMode)
            {
                Console.WriteLine($"[DEBUG] Structure fix complete for {mapName}\n");
            }
        }

        /// <summary>
        /// Creates a new category with proper old format settings
        /// </summary>
        static TriggerCategoryDefinition CreateCategory(MapTriggers triggers, string name)
        {
            var categories = GetCategories(triggers);
            int newId = categories.Count; // ID = current category count (position)

            var category = new TriggerCategoryDefinition(TriggerItemType.Category)
            {
                Id = newId,
                ParentId = 0, // OLD FORMAT: Always 0
                Name = name,
                IsComment = false,
                IsExpanded = true
            };

            // Insert at position = category count (before any triggers)
            triggers.TriggerItems.Insert(newId, category);

            if (_debugMode)
            {
                Console.WriteLine($"[DEBUG] Created category '{name}' at position {newId}");
            }

            return category;
        }

        /// <summary>
        /// Gets category by name
        /// </summary>
        static TriggerCategoryDefinition? FindCategory(MapTriggers triggers, string name)
        {
            return GetCategories(triggers)
                .FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all triggers in a category (by ParentId = category position)
        /// </summary>
        static List<TriggerDefinition> GetTriggersInCategory(MapTriggers triggers, TriggerCategoryDefinition category)
        {
            return GetTriggers(triggers)
                .Where(t => t.ParentId == category.Id)
                .ToList();
        }

        /// <summary>
        /// Removes a category and all its triggers
        /// </summary>
        static void RemoveCategory(MapTriggers triggers, string categoryName)
        {
            var category = FindCategory(triggers, categoryName);
            if (category == null) return;

            // Remove all triggers in this category
            var triggersToRemove = GetTriggersInCategory(triggers, category);
            foreach (var trigger in triggersToRemove)
            {
                triggers.TriggerItems.Remove(trigger);
            }

            // Remove the category itself
            triggers.TriggerItems.Remove(category);

            if (_debugMode)
            {
                Console.WriteLine($"[DEBUG] Removed category '{categoryName}' and {triggersToRemove.Count} triggers");
            }
        }

        #endregion

        #region Trigger Operations

        /// <summary>
        /// Copies specific triggers from source to target
        /// </summary>
        static void CopyTriggers(string sourceCategoryName, string[] triggerNames, string destCategoryName)
        {
            var sourceCategory = FindCategory(_source!, sourceCategoryName);
            if (sourceCategory == null)
            {
                PrintError($"Category '{sourceCategoryName}' not found in source");
                return;
            }

            var sourceTriggers = GetTriggersInCategory(_source!, sourceCategory);
            var triggersToCopy = new List<TriggerDefinition>();

            foreach (var triggerName in triggerNames)
            {
                var trigger = sourceTriggers.FirstOrDefault(t => t.Name.Equals(triggerName, StringComparison.OrdinalIgnoreCase));
                if (trigger != null)
                {
                    triggersToCopy.Add(trigger);
                }
                else
                {
                    PrintWarning($"Trigger '{triggerName}' not found in '{sourceCategoryName}'");
                }
            }

            if (triggersToCopy.Count == 0)
            {
                PrintWarning("No triggers to copy");
                return;
            }

            // Find or create destination category
            var destCategory = FindCategory(_target!, destCategoryName);
            if (destCategory == null)
            {
                destCategory = CreateCategory(_target!, destCategoryName);
                Console.WriteLine($"+ Created category '{destCategoryName}'");
            }

            // Copy variables used by triggers
            CopyRequiredVariables(triggersToCopy);

            // Copy triggers
            Console.WriteLine($"\n+ Copying {triggersToCopy.Count} trigger(s) to '{destCategoryName}':");
            foreach (var trigger in triggersToCopy)
            {
                var copy = CloneTrigger(trigger, destCategory.Id);
                _target!.TriggerItems.Add(copy);
                Console.WriteLine($"  + {trigger.Name}");
            }

            // Fix structure after changes
            FixCategoryStructure(_target!, "target");
            _hasChanges = true;
            PrintSuccess($"\nCopied {triggersToCopy.Count} trigger(s) successfully!");
        }

        /// <summary>
        /// Merges an entire category from source to target
        /// </summary>
        static void MergeCategory(string categoryName)
        {
            var sourceCategory = FindCategory(_source!, categoryName);
            if (sourceCategory == null)
            {
                PrintError($"Category '{categoryName}' not found in source");
                return;
            }

            var sourceTriggers = GetTriggersInCategory(_source!, sourceCategory);
            Console.WriteLine($"+ Found {sourceTriggers.Count} triggers in source category '{categoryName}'");

            // Remove existing category if present
            if (FindCategory(_target!, categoryName) != null)
            {
                Console.WriteLine($"+ Removing existing category '{categoryName}' from target");
                RemoveCategory(_target!, categoryName);
                FixCategoryStructure(_target!, "target");
            }

            // Create new category
            var newCategory = CreateCategory(_target!, categoryName);
            Console.WriteLine($"+ Created category '{categoryName}' (ID={newCategory.Id}, ParentId={newCategory.ParentId})");

            // Copy variables
            CopyRequiredVariables(sourceTriggers);

            // Copy all triggers
            Console.WriteLine($"\n+ Copying triggers:");
            foreach (var trigger in sourceTriggers)
            {
                var copy = CloneTrigger(trigger, newCategory.Id);
                _target!.TriggerItems.Add(copy);
                Console.WriteLine($"  + {trigger.Name}");
            }

            // Fix structure
            FixCategoryStructure(_target!, "target");
            _hasChanges = true;
            PrintSuccess($"\nMerged category '{categoryName}' successfully!");
        }

        /// <summary>
        /// Creates a deep copy of a trigger
        /// </summary>
        static TriggerDefinition CloneTrigger(TriggerDefinition source, int newParentId)
        {
            var copy = new TriggerDefinition(source.Type)
            {
                Id = 0, // Old format: ID not saved
                ParentId = newParentId,
                Name = source.Name,
                Description = source.Description,
                IsComment = source.IsComment,
                IsEnabled = source.IsEnabled,
                IsCustomTextTrigger = source.IsCustomTextTrigger,
                IsInitiallyOn = source.IsInitiallyOn,
                RunOnMapInit = source.RunOnMapInit
            };

            // Deep copy functions
            foreach (var func in source.Functions)
            {
                copy.Functions.Add(CloneFunction(func));
            }

            return copy;
        }

        static TriggerFunction CloneFunction(TriggerFunction source)
        {
            var copy = new TriggerFunction
            {
                Type = source.Type,
                Branch = source.Branch,
                Name = source.Name,
                IsEnabled = source.IsEnabled
            };

            foreach (var param in source.Parameters)
            {
                copy.Parameters.Add(CloneParameter(param));
            }

            foreach (var child in source.ChildFunctions)
            {
                copy.ChildFunctions.Add(CloneFunction(child));
            }

            return copy;
        }

        static TriggerFunctionParameter CloneParameter(TriggerFunctionParameter source)
        {
            var copy = new TriggerFunctionParameter
            {
                Type = source.Type,
                Value = source.Value
            };

            if (source.Function != null)
            {
                copy.Function = CloneFunction(source.Function);
            }

            if (source.ArrayIndexer != null)
            {
                copy.ArrayIndexer = CloneParameter(source.ArrayIndexer);
            }

            return copy;
        }

        #endregion

        #region Variable Management

        /// <summary>
        /// Copies variables required by triggers
        /// </summary>
        static void CopyRequiredVariables(List<TriggerDefinition> triggers)
        {
            var variablesNeeded = CollectVariableNames(triggers);
            if (variablesNeeded.Count == 0) return;

            Console.WriteLine($"\n+ Analyzing {variablesNeeded.Count} variable(s)...");

            var targetVarNames = new HashSet<string>(_target!.Variables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
            var sourceVarDict = _source!.Variables.ToDictionary(v => v.Name, v => v, StringComparer.OrdinalIgnoreCase);

            int copied = 0;
            foreach (var varName in variablesNeeded)
            {
                if (!sourceVarDict.ContainsKey(varName))
                {
                    PrintWarning($"Variable '{varName}' not found in source");
                    continue;
                }

                if (!targetVarNames.Contains(varName))
                {
                    var sourceVar = sourceVarDict[varName];
                    var newVar = new VariableDefinition
                    {
                        Name = sourceVar.Name,
                        Type = sourceVar.Type,
                        Unk = sourceVar.Unk,
                        IsArray = sourceVar.IsArray,
                        ArraySize = sourceVar.ArraySize,
                        IsInitialized = sourceVar.IsInitialized,
                        InitialValue = sourceVar.InitialValue,
                        Id = 0, // Old format: ID not saved
                        ParentId = sourceVar.ParentId
                    };

                    _target.Variables.Add(newVar);
                    targetVarNames.Add(newVar.Name);
                    copied++;
                    Console.WriteLine($"  + Copied variable '{varName}'");
                }
            }

            if (copied > 0)
            {
                PrintSuccess($"Copied {copied} variable(s)");
            }
        }

        /// <summary>
        /// Collects all variable names used by triggers
        /// </summary>
        static HashSet<string> CollectVariableNames(List<TriggerDefinition> triggers)
        {
            var variables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var trigger in triggers)
            {
                foreach (var func in trigger.Functions)
                {
                    CollectVariablesFromFunction(func, variables);
                }
            }

            return variables;
        }

        static void CollectVariablesFromFunction(TriggerFunction func, HashSet<string> variables)
        {
            foreach (var param in func.Parameters)
            {
                if (param.Type == TriggerFunctionParameterType.Variable && !string.IsNullOrWhiteSpace(param.Value))
                {
                    variables.Add(param.Value);
                }

                if (param.Function != null)
                {
                    CollectVariablesFromFunction(param.Function, variables);
                }

                if (param.ArrayIndexer != null)
                {
                    CollectVariablesFromParameter(param.ArrayIndexer, variables);
                }
            }

            foreach (var child in func.ChildFunctions)
            {
                CollectVariablesFromFunction(child, variables);
            }
        }

        static void CollectVariablesFromParameter(TriggerFunctionParameter param, HashSet<string> variables)
        {
            if (param.Type == TriggerFunctionParameterType.Variable && !string.IsNullOrWhiteSpace(param.Value))
            {
                variables.Add(param.Value);
            }

            if (param.Function != null)
            {
                CollectVariablesFromFunction(param.Function, variables);
            }

            if (param.ArrayIndexer != null)
            {
                CollectVariablesFromParameter(param.ArrayIndexer, variables);
            }
        }

        #endregion

        #region File I/O

        static MapTriggers LoadMapTriggers(string path)
        {
            if (IsMapArchive(path))
            {
                return LoadFromArchive(path);
            }
            else
            {
                return LoadFromWTG(path);
            }
        }

        static MapTriggers LoadFromWTG(string path)
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);

            var constructor = typeof(MapTriggers).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(BinaryReader), typeof(TriggerData) },
                null) ?? throw new Exception("Cannot find MapTriggers constructor");

            return (MapTriggers)constructor.Invoke(new object[] { reader, TriggerData.Default });
        }

        static MapTriggers LoadFromArchive(string path)
        {
            using var archive = MpqArchive.Open(path, true);

            if (!archive.FileExists(MapTriggers.FileName))
            {
                throw new FileNotFoundException($"'{MapTriggers.FileName}' not found in archive");
            }

            using var stream = archive.OpenFile(MapTriggers.FileName);
            using var reader = new BinaryReader(stream);

            var constructor = typeof(MapTriggers).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(BinaryReader), typeof(TriggerData) },
                null) ?? throw new Exception("Cannot find MapTriggers constructor");

            return (MapTriggers)constructor.Invoke(new object[] { reader, TriggerData.Default });
        }

        static void SaveMapTriggers(MapTriggers triggers, string targetPath, string outputPath)
        {
            if (IsMapArchive(outputPath))
            {
                SaveToArchive(triggers, targetPath, outputPath);
            }
            else
            {
                SaveToWTG(triggers, outputPath);
            }
        }

        static void SaveToWTG(MapTriggers triggers, string path)
        {
            using var stream = File.Create(path);
            using var writer = new BinaryWriter(stream);

            var writeMethod = typeof(MapTriggers).GetMethod(
                "WriteTo",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(BinaryWriter) },
                null) ?? throw new Exception("Cannot find WriteTo method");

            writeMethod.Invoke(triggers, new object[] { writer });
        }

        static void SaveToArchive(MapTriggers triggers, string originalPath, string outputPath)
        {
            using var originalArchive = MpqArchive.Open(originalPath, true);

            var builder = new MpqArchiveBuilder(originalArchive);

            // Serialize triggers
            using var triggerStream = new MemoryStream();
            using var writer = new BinaryWriter(triggerStream);

            var writeMethod = typeof(MapTriggers).GetMethod(
                "WriteTo",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(BinaryWriter) },
                null) ?? throw new Exception("Cannot find WriteTo method");

            writeMethod.Invoke(triggers, new object[] { writer });
            writer.Flush();
            triggerStream.Position = 0;

            // Replace trigger file
            builder.RemoveFile(MapTriggers.FileName);
            builder.AddFile(MpqFile.New(triggerStream, MapTriggers.FileName));

            // Ask about JASS file
            Console.WriteLine("\n================================================================");
            Console.WriteLine("                JASS FILE SYNCHRONIZATION");
            Console.WriteLine("================================================================");
            Console.WriteLine("\nThe war3map.j file must be synchronized with war3map.wtg.");
            Console.WriteLine("World Editor will regenerate it when you open the map.");
            Console.WriteLine("\nDelete war3map.j from output? (RECOMMENDED)");
            Console.Write("\n[Y]es / [N]o: ");

            var response = Console.ReadLine()?.Trim().ToLower();
            if (response == "y" || response == "yes" || string.IsNullOrEmpty(response))
            {
                var jassFiles = new[] { "war3map.j", "scripts/war3map.j" };
                foreach (var jassFile in jassFiles)
                {
                    if (originalArchive.FileExists(jassFile))
                    {
                        builder.RemoveFile(jassFile);
                        Console.WriteLine($"+ Removed {jassFile}");
                    }
                }
            }

            // Save archive
            Console.WriteLine("\n+ Saving archive...");
            builder.SaveTo(outputPath);
        }

        #endregion

        #region Validation & Save

        static void SaveWithValidation()
        {
            Console.WriteLine("\n================================================================");
            Console.WriteLine("                   PRE-SAVE VALIDATION");
            Console.WriteLine("================================================================\n");

            // Check SubVersion
            if (_target!.SubVersion != null)
            {
                PrintError("ERROR: SubVersion was changed from null!");
                PrintError("This breaks WC3 1.27 compatibility. Aborting save.");
                return;
            }
            PrintSuccess("SubVersion is null - OK");

            // Validate category structure
            var categories = GetCategories(_target);
            bool structureOk = true;

            for (int i = 0; i < categories.Count; i++)
            {
                if (_target.TriggerItems[i] != categories[i])
                {
                    PrintError($"Category '{categories[i].Name}' is not at expected position {i}");
                    structureOk = false;
                }

                if (categories[i].Id != i)
                {
                    PrintError($"Category '{categories[i].Name}' has ID={categories[i].Id} but position={i}");
                    structureOk = false;
                }

                if (categories[i].ParentId != 0)
                {
                    PrintError($"Category '{categories[i].Name}' has ParentId={categories[i].ParentId} (should be 0)");
                    structureOk = false;
                }
            }

            if (!structureOk)
            {
                Console.WriteLine("\n+ Attempting to fix structure...");
                FixCategoryStructure(_target, "target");
                PrintSuccess("Structure fixed!");
            }
            else
            {
                PrintSuccess("Category structure is valid");
            }

            // Show statistics
            Console.WriteLine($"\nStatistics:");
            Console.WriteLine($"  Categories: {categories.Count}");
            Console.WriteLine($"  Triggers: {CountTriggers(_target)}");
            Console.WriteLine($"  Variables: {_target.Variables.Count}");

            // Save
            Console.WriteLine("\n+ Saving merged map...");
            SaveMapTriggers(_target, _targetPath, _outputPath);

            // Verify
            Console.WriteLine("\n================================================================");
            Console.WriteLine("                 POST-SAVE VERIFICATION");
            Console.WriteLine("================================================================\n");

            try
            {
                var verify = LoadMapTriggers(_outputPath);

                bool allGood = true;

                if (verify.SubVersion != null)
                {
                    PrintError($"SubVersion changed to {verify.SubVersion}!");
                    allGood = false;
                }
                else
                {
                    PrintSuccess("SubVersion is still null");
                }

                if (verify.Variables.Count != _target.Variables.Count)
                {
                    PrintWarning($"Variable count changed: {_target.Variables.Count} → {verify.Variables.Count}");
                    allGood = false;
                }
                else
                {
                    PrintSuccess($"Variables verified ({verify.Variables.Count})");
                }

                if (CountCategories(verify) != categories.Count)
                {
                    PrintWarning($"Category count changed: {categories.Count} → {CountCategories(verify)}");
                    allGood = false;
                }
                else
                {
                    PrintSuccess($"Categories verified ({CountCategories(verify)})");
                }

                if (allGood)
                {
                    Console.WriteLine("\n================================================================");
                    PrintSuccess("MERGE COMPLETE - ALL VERIFICATIONS PASSED!");
                    Console.WriteLine("================================================================");
                    Console.WriteLine($"\nOutput saved to: {Path.GetFileName(_outputPath)}");
                    Console.WriteLine("\nYou can now open this map in World Editor 1.27");
                }
            }
            catch (Exception ex)
            {
                PrintWarning($"Could not verify saved file: {ex.Message}");
            }
        }

        #endregion

        #region User Interface

        static void InteractiveMenu()
        {
            while (true)
            {
                Console.WriteLine("\n================================================================");
                Console.WriteLine("                      MERGE OPTIONS");
                Console.WriteLine("================================================================");
                Console.WriteLine("1. List SOURCE categories");
                Console.WriteLine("2. List TARGET categories");
                Console.WriteLine("3. List triggers in category");
                Console.WriteLine("4. Copy ENTIRE category from source to target");
                Console.WriteLine("5. Copy SPECIFIC triggers");
                Console.WriteLine("6. Show structure debug info");
                Console.WriteLine($"7. Toggle debug mode (currently: {(_debugMode ? "ON" : "OFF")})");
                Console.WriteLine("8. Save and exit");
                Console.WriteLine("0. Exit without saving");
                Console.WriteLine("================================================================");
                Console.Write("\nSelect option: ");

                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        ListCategories(_source!, "SOURCE");
                        break;

                    case "2":
                        ListCategories(_target!, "TARGET");
                        break;

                    case "3":
                        Console.Write("\nEnter category name: ");
                        var catName = Console.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(catName))
                        {
                            ListTriggersInCategory(catName);
                        }
                        break;

                    case "4":
                        Console.Write("\nEnter category name to merge: ");
                        var mergeCat = Console.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(mergeCat))
                        {
                            MergeCategory(mergeCat);
                        }
                        break;

                    case "5":
                        Console.Write("\nSource category name: ");
                        var sourceCat = Console.ReadLine()?.Trim();
                        if (string.IsNullOrEmpty(sourceCat)) break;

                        ListTriggersInCategory(sourceCat);
                        Console.Write("\nTrigger names (comma-separated): ");
                        var triggerNames = Console.ReadLine()?.Trim();
                        if (string.IsNullOrEmpty(triggerNames)) break;

                        Console.Write("Destination category name (empty = same): ");
                        var destCat = Console.ReadLine()?.Trim();
                        if (string.IsNullOrEmpty(destCat)) destCat = sourceCat;

                        var triggers = triggerNames.Split(',').Select(s => s.Trim()).ToArray();
                        CopyTriggers(sourceCat, triggers, destCat);
                        break;

                    case "6":
                        ShowDebugInfo();
                        break;

                    case "7":
                        _debugMode = !_debugMode;
                        Console.WriteLine($"\n+ Debug mode: {(_debugMode ? "ON" : "OFF")}");
                        break;

                    case "8":
                        if (!_hasChanges)
                        {
                            Console.WriteLine("\nNo changes made.");
                            return;
                        }
                        SaveWithValidation();
                        return;

                    case "0":
                        Console.WriteLine("\nExiting without saving.");
                        return;

                    default:
                        PrintWarning("Invalid option");
                        break;
                }
            }
        }

        static void ListCategories(MapTriggers triggers, string mapName)
        {
            Console.WriteLine($"\n=== {mapName} Categories ===\n");

            var categories = GetCategories(triggers);
            if (categories.Count == 0)
            {
                Console.WriteLine("  (No categories)");
                return;
            }

            Console.WriteLine($"Total: {categories.Count}\n");

            for (int i = 0; i < categories.Count; i++)
            {
                var cat = categories[i];
                var position = triggers.TriggerItems.IndexOf(cat);
                var triggerCount = GetTriggersInCategory(triggers, cat).Count;

                Console.WriteLine($"[{i + 1}] {cat.Name}");
                Console.WriteLine($"    ID={cat.Id}, Position={position}, ParentId={cat.ParentId}");
                Console.WriteLine($"    Triggers: {triggerCount}");

                // Highlight issues
                if (cat.Id != position)
                {
                    PrintError($"    ! ID mismatch (ID={cat.Id} but Position={position})");
                }
                if (triggers.SubVersion == null && cat.ParentId != 0)
                {
                    PrintError($"    ! Wrong ParentId for old format (should be 0)");
                }

                Console.WriteLine();
            }
        }

        static void ListTriggersInCategory(string categoryName)
        {
            var category = FindCategory(_source!, categoryName);
            if (category == null)
            {
                PrintError($"Category '{categoryName}' not found in source");
                return;
            }

            var triggers = GetTriggersInCategory(_source!, category);
            Console.WriteLine($"\n=== Triggers in '{categoryName}' ===\n");

            if (triggers.Count == 0)
            {
                Console.WriteLine("  (No triggers)");
                return;
            }

            Console.WriteLine($"Total: {triggers.Count}\n");

            for (int i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                Console.WriteLine($"[{i + 1}] {trigger.Name}");
                Console.WriteLine($"    Enabled: {trigger.IsEnabled}");
                Console.WriteLine($"    ParentId: {trigger.ParentId}");
                Console.WriteLine();
            }
        }

        static void ShowDebugInfo()
        {
            Console.WriteLine("\n================================================================");
            Console.WriteLine("                    DEBUG INFORMATION");
            Console.WriteLine("================================================================\n");

            Console.WriteLine("=== TARGET MAP ===");
            Console.WriteLine($"FormatVersion: {_target!.FormatVersion}");
            Console.WriteLine($"SubVersion: {(_target.SubVersion == null ? "null (OLD FORMAT)" : _target.SubVersion.ToString())}");
            Console.WriteLine($"GameVersion: {_target.GameVersion}");

            var categories = GetCategories(_target);
            Console.WriteLine($"\n=== CATEGORY STRUCTURE ===");
            Console.WriteLine($"Total categories: {categories.Count}");
            Console.WriteLine($"Total triggers: {CountTriggers(_target)}");
            Console.WriteLine($"Total variables: {_target.Variables.Count}");

            Console.WriteLine($"\n=== CATEGORY POSITION vs ID ===");
            for (int i = 0; i < categories.Count && i < 10; i++)
            {
                var cat = categories[i];
                var position = _target.TriggerItems.IndexOf(cat);
                Console.Write($"  '{cat.Name}': Position={position}, ID={cat.Id}, ParentId={cat.ParentId}");

                if (cat.Id != position)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(" [MISMATCH!]");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }

            if (categories.Count > 10)
            {
                Console.WriteLine($"  ... and {categories.Count - 10} more");
            }

            Console.WriteLine("\nPress Enter to continue...");
            Console.ReadLine();
        }

        #endregion

        #region Helpers

        static List<TriggerCategoryDefinition> GetCategories(MapTriggers triggers)
        {
            return triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
        }

        static List<TriggerDefinition> GetTriggers(MapTriggers triggers)
        {
            return triggers.TriggerItems.OfType<TriggerDefinition>().ToList();
        }

        static int CountCategories(MapTriggers triggers) => GetCategories(triggers).Count;
        static int CountTriggers(MapTriggers triggers) => GetTriggers(triggers).Count;

        static bool IsMapArchive(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".w3x" || ext == ".w3m";
        }

        static string DetectMapFile(string folder)
        {
            if (!Directory.Exists(folder))
            {
                throw new DirectoryNotFoundException($"Folder not found: {folder}");
            }

            var w3x = Directory.GetFiles(folder, "*.w3x");
            if (w3x.Length > 0) return w3x[0];

            var w3m = Directory.GetFiles(folder, "*.w3m");
            if (w3m.Length > 0) return w3m[0];

            var wtg = Path.Combine(folder, "war3map.wtg");
            if (File.Exists(wtg)) return wtg;

            throw new FileNotFoundException($"No map files found in {folder}");
        }

        static string GenerateOutputPath(string targetPath)
        {
            if (IsMapArchive(targetPath))
            {
                var dir = Path.GetDirectoryName(targetPath) ?? ".";
                var name = Path.GetFileNameWithoutExtension(targetPath);
                var ext = Path.GetExtension(targetPath);
                return Path.Combine(dir, $"{name}_merged{ext}");
            }
            else
            {
                var dir = Path.GetDirectoryName(targetPath) ?? ".";
                return Path.Combine(dir, "war3map_merged.wtg");
            }
        }

        static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"+ {message}");
            Console.ResetColor();
        }

        static void PrintWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"! {message}");
            Console.ResetColor();
        }

        static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"X {message}");
            Console.ResetColor();
        }

        #endregion
    }
}
