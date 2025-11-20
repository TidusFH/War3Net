using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using War3Net.Build;
using War3Net.Build.Object;
using War3Net.Common.Extensions;

namespace ObjectDataExporter
{
    class Program
    {
        static void Main(string[] args)
        {
            ShowBanner();

            // Command-line mode (if args provided)
            if (args.Length >= 1)
            {
                CommandLineMode(args);
                return;
            }

            // Interactive mode
            InteractiveMode();
        }

        static void ShowBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║        WARCRAFT 3 OBJECT DATA EXPORTER v2.0              ║");
            Console.WriteLine("║    Export all object data to human-readable format       ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        static void CommandLineMode(string[] args)
        {
            string mapPath = args[0];
            string outputPath = args.Length >= 2 ? args[1] : Path.ChangeExtension(mapPath, null) + "_objects";
            string format = args.Length >= 3 ? args[2].ToLower() : "txt";

            if (!File.Exists(mapPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Error: Map file not found: {mapPath}");
                Console.ResetColor();
                return;
            }

            ExportMap(mapPath, outputPath, format);
        }

        static void InteractiveMode()
        {
            while (true)
            {
                // Scan for maps
                var maps = ScanForMaps();

                if (!maps.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠ No .w3x or .w3m files found in current directory");
                    Console.ResetColor();
                    Console.WriteLine("\nOptions:");
                    Console.WriteLine("  1. Enter map path manually");
                    Console.WriteLine("  2. Exit");
                    Console.Write("\nChoice: ");
                    string? choice = Console.ReadLine();

                    if (choice == "1")
                    {
                        Console.Write("\nMap file path: ");
                        string? manualPath = Console.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(manualPath) && File.Exists(manualPath))
                        {
                            ProcessMap(manualPath);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("✗ Invalid file path");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    // Show detected maps
                    Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"║  DETECTED {maps.Count} MAP(S) IN CURRENT DIRECTORY");
                    Console.ResetColor();
                    Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                    Console.WriteLine();

                    // List maps with numbers
                    for (int i = 0; i < maps.Count; i++)
                    {
                        var map = maps[i];
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"  [{i + 1}] ");
                        Console.ResetColor();
                        Console.Write($"{map.Name}");

                        // Show file size
                        var fileInfo = new FileInfo(map.FullPath);
                        double sizeMB = fileInfo.Length / 1024.0 / 1024.0;
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($" ({sizeMB:F2} MB)");
                        Console.ResetColor();
                    }

                    Console.WriteLine();
                    Console.WriteLine("  [A] Export ALL maps");
                    Console.WriteLine("  [M] Enter manual path");
                    Console.WriteLine("  [0] Exit");

                    Console.Write("\nSelect map number (or A/M/0): ");
                    string? input = Console.ReadLine()?.Trim().ToUpper();

                    if (input == "0")
                    {
                        Console.WriteLine("Goodbye!");
                        return;
                    }
                    else if (input == "A")
                    {
                        // Export all maps
                        Console.WriteLine();
                        foreach (var map in maps)
                        {
                            ProcessMap(map.FullPath);
                            Console.WriteLine();
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ Exported all {maps.Count} maps!");
                        Console.ResetColor();
                        Console.WriteLine("\nPress Enter to continue...");
                        Console.ReadLine();
                    }
                    else if (input == "M")
                    {
                        Console.Write("\nMap file path: ");
                        string? manualPath = Console.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(manualPath) && File.Exists(manualPath))
                        {
                            ProcessMap(manualPath);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("✗ Invalid file path");
                            Console.ResetColor();
                        }
                    }
                    else if (int.TryParse(input, out int mapIndex) && mapIndex >= 1 && mapIndex <= maps.Count)
                    {
                        // Export selected map
                        ProcessMap(maps[mapIndex - 1].FullPath);
                        Console.WriteLine("\nPress Enter to continue...");
                        Console.ReadLine();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("✗ Invalid selection");
                        Console.ResetColor();
                    }
                }

                Console.Clear();
                ShowBanner();
            }
        }

        static List<MapInfo> ScanForMaps()
        {
            var maps = new List<MapInfo>();

            try
            {
                var currentDir = Directory.GetCurrentDirectory();

                // Find .w3x files
                foreach (var file in Directory.GetFiles(currentDir, "*.w3x"))
                {
                    maps.Add(new MapInfo
                    {
                        Name = Path.GetFileName(file),
                        FullPath = file,
                        Extension = ".w3x"
                    });
                }

                // Find .w3m files
                foreach (var file in Directory.GetFiles(currentDir, "*.w3m"))
                {
                    maps.Add(new MapInfo
                    {
                        Name = Path.GetFileName(file),
                        FullPath = file,
                        Extension = ".w3m"
                    });
                }

                // Sort by name
                maps = maps.OrderBy(m => m.Name).ToList();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error scanning directory: {ex.Message}");
                Console.ResetColor();
            }

            return maps;
        }

        static void ProcessMap(string mapPath)
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"PROCESSING: {Path.GetFileName(mapPath)}");
            Console.ResetColor();
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine();

            // Select format
            Console.WriteLine("Select export format:");
            Console.WriteLine("  [1] TXT - Human-readable text (recommended)");
            Console.WriteLine("  [2] INI - Configuration style (version control friendly)");
            Console.WriteLine("  [3] CSV - Spreadsheet format (Excel/LibreOffice)");
            Console.Write("\nChoice (1-3) or press Enter for TXT: ");

            string? formatChoice = Console.ReadLine()?.Trim();
            string format = formatChoice switch
            {
                "2" => "ini",
                "3" => "csv",
                _ => "txt"
            };

            // Auto-generate output path
            string outputPath = Path.ChangeExtension(mapPath, null) + "_objects";

            Console.Write($"\nOutput folder (or press Enter for '{Path.GetFileName(outputPath)}'): ");
            string? customOutput = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(customOutput))
            {
                outputPath = customOutput;
            }

            // Export
            ExportMap(mapPath, outputPath, format);
        }

        static void ExportMap(string mapPath, string outputPath, string format)
        {
            // Validate format
            if (format != "txt" && format != "ini" && format != "csv")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Warning: Unknown format '{format}', using 'txt'");
                Console.ResetColor();
                format = "txt";
            }

            // Load map
            Map map;
            try
            {
                Console.WriteLine($"\nLoading map...");
                map = Map.Open(mapPath);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Map loaded successfully");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Error loading map: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("═══ DETAILED ERROR INFORMATION ═══");
                Console.WriteLine($"Exception Type: {ex.GetType().FullName}");
                Console.WriteLine($"Message: {ex.Message}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("Inner Exception:");
                    Console.WriteLine($"  Type: {ex.InnerException.GetType().FullName}");
                    Console.WriteLine($"  Message: {ex.InnerException.Message}");
                }

                Console.WriteLine();
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(ex.StackTrace);

                if (ex is System.IO.FileLoadException || ex is System.IO.FileNotFoundException)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠ This looks like a missing dependency issue.");
                    Console.WriteLine("⚠ Make sure all required DLL files are in the same folder as the executable.");
                    Console.ResetColor();
                }

                Console.ResetColor();
                return;
            }

            // Export object data
            try
            {
                Console.WriteLine($"\nExporting to {format.ToUpper()} format...\n");

                var exporter = new ObjectDataExporter(map);

                switch (format)
                {
                    case "txt":
                        exporter.ExportToTxt(outputPath);
                        break;
                    case "ini":
                        exporter.ExportToIni(outputPath);
                        break;
                    case "csv":
                        exporter.ExportToCsv(outputPath);
                        break;
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Export completed successfully!");
                Console.ResetColor();
                Console.WriteLine($"\nOutput location: {Path.GetFullPath(outputPath)}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✗ Error during export: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("═══ DETAILED ERROR INFORMATION ═══");
                Console.WriteLine($"Exception Type: {ex.GetType().FullName}");
                Console.WriteLine($"Message: {ex.Message}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("Inner Exception:");
                    Console.WriteLine($"  Type: {ex.InnerException.GetType().FullName}");
                    Console.WriteLine($"  Message: {ex.InnerException.Message}");
                }

                Console.WriteLine();
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
        }
    }

    class MapInfo
    {
        public required string Name { get; set; }
        public required string FullPath { get; set; }
        public required string Extension { get; set; }
    }

    static class FieldNameDatabase
    {
        // Maps field codes to human-readable names for AI-friendly output
        private static readonly Dictionary<string, string> FieldNames = new()
        {
            // Unit fields
            ["unam"] = "Name",
            ["upro"] = "Proper Names",
            ["umas"] = "Model File",
            ["umdl"] = "Model File (AI)",
            ["uico"] = "Icon - Game Interface",
            ["umvs"] = "Movement Speed (base)",
            ["uhpm"] = "Hit Points (maximum)",
            ["uhp0"] = "Hit Points (start)",
            ["umpm"] = "Mana (maximum)",
            ["ump0"] = "Mana (start)",
            ["uarm"] = "Defense Type",
            ["udty"] = "Defense Value (base)",
            ["uabi"] = "Abilities - Normal",
            ["uabt"] = "Abilities - Tech Tree Dependence",
            ["ua1g"] = "Attack 1 - Damage Dice (sides)",
            ["ua1b"] = "Attack 1 - Damage Base",
            ["ua1d"] = "Attack 1 - Damage Dice (count)",
            ["ua1t"] = "Attack 1 - Attack Type",
            ["ua1w"] = "Attack 1 - Weapon Type",
            ["ua1r"] = "Attack 1 - Range",
            ["ua1p"] = "Attack 1 - Projectile Speed",
            ["ua2g"] = "Attack 2 - Damage Dice (sides)",
            ["ua2b"] = "Attack 2 - Damage Base",
            ["ua2d"] = "Attack 2 - Damage Dice (count)",
            ["ua2t"] = "Attack 2 - Attack Type",
            ["ua2w"] = "Attack 2 - Weapon Type",
            ["ua2r"] = "Attack 2 - Range",
            ["ugol"] = "Gold Cost",
            ["ulum"] = "Lumber Cost",
            ["ubld"] = "Build Time",
            ["urac"] = "Race",
            ["usca"] = "Scale",
            ["ucol"] = "Collision Size",
            ["ufoo"] = "Food Cost",
            ["ufma"] = "Food Produced",
            ["upri"] = "Priority (selection)",
            ["usid"] = "Sight Radius (day)",
            ["usin"] = "Sight Radius (night)",
            ["uhor"] = "Has Water Shadow",
            ["ushb"] = "Shadow Image (unit)",
            ["umvt"] = "Movement Type",
            ["umvh"] = "Movement Height",
            ["umvf"] = "Movement - Height (minimum)",
            ["uclr"] = "Classification - Campaign",
            ["utyp"] = "Unit Classification",
            ["uspe"] = "Special",
            ["utar"] = "Targets Allowed",
            ["utco"] = "Tinting Color 1 (red)",
            ["utc2"] = "Tinting Color 2 (green)",
            ["utc3"] = "Tinting Color 3 (blue)",
            ["ubui"] = "Builds",
            ["utra"] = "Trains",
            ["ures"] = "Researches",
            ["useu"] = "Sells Units",
            ["usei"] = "Sells Items",
            ["umki"] = "Makes Items",
            ["urev"] = "Revives Dead Heroes",
            ["ucam"] = "Can Flee",
            ["urun"] = "Run Speed",
            ["uwlk"] = "Walk Speed",
            ["uflh"] = "Fly Height",
            ["uani"] = "Animation - Run Speed",
            ["uwal"] = "Animation - Walk Speed",
            ["urpo"] = "Repair Gold Cost Ratio",
            ["urlm"] = "Repair Lumber Cost Ratio",
            ["urep"] = "Repair Time Ratio",

            // Item fields
            ["inam"] = "Name",
            ["iico"] = "Icon",
            ["igol"] = "Gold Cost",
            ["ilum"] = "Lumber Cost",
            ["iabi"] = "Abilities",
            ["iuse"] = "Usable",
            ["ipow"] = "Powerup",
            ["ipaw"] = "Pawnable",
            ["isel"] = "Sellable",
            ["idro"] = "Drop On Death",
            ["idrp"] = "Can Be Dropped",
            ["istr"] = "Stock Replenish Interval",
            ["isto"] = "Stock Maximum",
            ["istk"] = "Stock Start Delay",
            ["icla"] = "Class",
            ["ilev"] = "Level",
            ["ilvo"] = "Level (unclassified)",
            ["ipri"] = "Priority",
            ["imod"] = "Model Used",
            ["isca"] = "Scaling Value",
            ["ides"] = "Description - Tooltip (extended)",
            ["itp1"] = "Description - Tooltip (basic)",
            ["ihtp"] = "Hit Points",

            // Ability fields
            ["anam"] = "Name",
            ["aart"] = "Art - Icon",
            ["aeat"] = "Art - Effect (target)",
            ["aaea"] = "Art - Effect (area)",
            ["atp1"] = "Tooltip - Learn (basic)",
            ["ades"] = "Description - Tooltip (extended)",
            ["aher"] = "Hero Ability",
            ["alev"] = "Levels",
            ["areq"] = "Requirements",
            ["arqa"] = "Requirements Levels",
            ["aman"] = "Mana Cost",
            ["acol"] = "Cooldown",
            ["acdn"] = "Casting Time",
            ["adur"] = "Duration (hero)",
            ["ahdu"] = "Duration (normal)",
            ["atar"] = "Targets Allowed",
            ["aran"] = "Cast Range",
            ["aare"] = "Area of Effect",
            ["aeff"] = "Data - Effects",
            ["asta"] = "Stats - Bonus per Level",

            // Upgrade fields
            ["gnam"] = "Name",
            ["gico"] = "Icon",
            ["gef1"] = "Effect - Ability",
            ["gtp1"] = "Tooltip - Learn (basic)",
            ["gub1"] = "Tooltip - Upgrade (basic)",
            ["gdes"] = "Description - Tooltip (extended)",
            ["ggol"] = "Gold Cost (base)",
            ["glmb"] = "Lumber Cost (base)",
            ["gti1"] = "Time (base)",
            ["glev"] = "Levels",
            ["gtyp"] = "Class",
            ["greq"] = "Requirements",
            ["grqa"] = "Requirements Levels",
        };

        public static string GetFieldName(string rawCode)
        {
            if (FieldNames.TryGetValue(rawCode, out string? name))
            {
                return name;
            }
            return rawCode; // Return raw code if not found
        }
    }

    class ObjectDataExporter
    {
        private readonly Map map;

        public ObjectDataExporter(Map map)
        {
            this.map = map;
        }

        public void ExportToTxt(string basePath)
        {
            Directory.CreateDirectory(basePath);

            ExportUnits(Path.Combine(basePath, "units.txt"), FormatType.Txt);
            ExportItems(Path.Combine(basePath, "items.txt"), FormatType.Txt);
            ExportAbilities(Path.Combine(basePath, "abilities.txt"), FormatType.Txt);
            ExportDestructables(Path.Combine(basePath, "destructables.txt"), FormatType.Txt);
            ExportDoodads(Path.Combine(basePath, "doodads.txt"), FormatType.Txt);
            ExportBuffs(Path.Combine(basePath, "buffs.txt"), FormatType.Txt);
            ExportUpgrades(Path.Combine(basePath, "upgrades.txt"), FormatType.Txt);

            CreateSummary(Path.Combine(basePath, "summary.txt"));
        }

        public void ExportToIni(string basePath)
        {
            Directory.CreateDirectory(basePath);

            ExportUnits(Path.Combine(basePath, "units.ini"), FormatType.Ini);
            ExportItems(Path.Combine(basePath, "items.ini"), FormatType.Ini);
            ExportAbilities(Path.Combine(basePath, "abilities.ini"), FormatType.Ini);
            ExportDestructables(Path.Combine(basePath, "destructables.ini"), FormatType.Ini);
            ExportDoodads(Path.Combine(basePath, "doodads.ini"), FormatType.Ini);
            ExportBuffs(Path.Combine(basePath, "buffs.ini"), FormatType.Ini);
            ExportUpgrades(Path.Combine(basePath, "upgrades.ini"), FormatType.Ini);

            CreateSummary(Path.Combine(basePath, "summary.txt"));
        }

        public void ExportToCsv(string basePath)
        {
            Directory.CreateDirectory(basePath);

            ExportUnits(Path.Combine(basePath, "units.csv"), FormatType.Csv);
            ExportItems(Path.Combine(basePath, "items.csv"), FormatType.Csv);
            ExportAbilities(Path.Combine(basePath, "abilities.csv"), FormatType.Csv);
            ExportDestructables(Path.Combine(basePath, "destructables.csv"), FormatType.Csv);
            ExportDoodads(Path.Combine(basePath, "doodads.csv"), FormatType.Csv);
            ExportBuffs(Path.Combine(basePath, "buffs.csv"), FormatType.Csv);
            ExportUpgrades(Path.Combine(basePath, "upgrades.csv"), FormatType.Csv);

            CreateSummary(Path.Combine(basePath, "summary.txt"));
        }

        private void ExportUnits(string filePath, FormatType format)
        {
            if (map.UnitObjectData == null)
            {
                Console.WriteLine("  Units: None");
                return;
            }

            int newCount = map.UnitObjectData.NewUnits.Count;
            int modifiedCount = map.UnitObjectData.BaseUnits.Count;

            if (newCount == 0 && modifiedCount == 0)
            {
                Console.WriteLine("  Units: None");
                return;
            }

            var sb = new StringBuilder();

            if (format == FormatType.Csv)
            {
                sb.AppendLine("ObjectCode,BaseCode,ModificationType,ModificationId,ModificationName,Level,Value,Type");
            }
            else if (format == FormatType.Txt)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine("                      UNITS");
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine();
            }

            // Export MODIFIED objects first (edited existing units like Peasant)
            if (modifiedCount > 0 && format == FormatType.Txt)
            {
                sb.AppendLine("─── MODIFIED EXISTING UNITS ───");
                sb.AppendLine();
            }

            foreach (var unit in map.UnitObjectData.BaseUnits)
            {
                ExportObject(sb, unit, "Unit", format, isModified: true);
            }

            // Export NEW custom objects
            if (newCount > 0 && format == FormatType.Txt)
            {
                if (modifiedCount > 0)
                {
                    sb.AppendLine();
                }
                sb.AppendLine("─── NEW CUSTOM UNITS ───");
                sb.AppendLine();
            }

            foreach (var unit in map.UnitObjectData.NewUnits)
            {
                ExportObject(sb, unit, "Unit", format, isModified: false);
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"  ✓ Units: {newCount} new, {modifiedCount} modified → {Path.GetFileName(filePath)}");
        }

        private void ExportItems(string filePath, FormatType format)
        {
            if (map.ItemObjectData == null)
            {
                Console.WriteLine("  Items: None");
                return;
            }

            int newCount = map.ItemObjectData.NewItems.Count;
            int modifiedCount = map.ItemObjectData.BaseItems.Count;

            if (newCount == 0 && modifiedCount == 0)
            {
                Console.WriteLine("  Items: None");
                return;
            }

            var sb = new StringBuilder();

            if (format == FormatType.Csv)
            {
                sb.AppendLine("ObjectCode,BaseCode,ModificationType,ModificationId,ModificationName,Level,Value,Type");
            }
            else if (format == FormatType.Txt)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine("                      ITEMS");
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine();
            }

            if (modifiedCount > 0 && format == FormatType.Txt)
            {
                sb.AppendLine("─── MODIFIED EXISTING ITEMS ───");
                sb.AppendLine();
            }

            foreach (var item in map.ItemObjectData.BaseItems)
            {
                ExportObject(sb, item, "Item", format, isModified: true);
            }

            if (newCount > 0 && format == FormatType.Txt)
            {
                if (modifiedCount > 0) sb.AppendLine();
                sb.AppendLine("─── NEW CUSTOM ITEMS ───");
                sb.AppendLine();
            }

            foreach (var item in map.ItemObjectData.NewItems)
            {
                ExportObject(sb, item, "Item", format, isModified: false);
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"  ✓ Items: {newCount} new, {modifiedCount} modified → {Path.GetFileName(filePath)}");
        }

        private void ExportAbilities(string filePath, FormatType format)
        {
            if (map.AbilityObjectData == null)
            {
                Console.WriteLine("  Abilities: None");
                return;
            }

            int newCount = map.AbilityObjectData.NewAbilities.Count;
            int modifiedCount = map.AbilityObjectData.BaseAbilities.Count;

            if (newCount == 0 && modifiedCount == 0)
            {
                Console.WriteLine("  Abilities: None");
                return;
            }

            var sb = new StringBuilder();

            if (format == FormatType.Csv)
            {
                sb.AppendLine("ObjectCode,BaseCode,ModificationType,ModificationId,ModificationName,Level,Value,Type");
            }
            else if (format == FormatType.Txt)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine("                    ABILITIES");
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine();
            }

            if (modifiedCount > 0 && format == FormatType.Txt)
            {
                sb.AppendLine("─── MODIFIED EXISTING ABILITIES ───");
                sb.AppendLine();
            }

            foreach (var ability in map.AbilityObjectData.BaseAbilities)
            {
                ExportObject(sb, ability, "Ability", format, isModified: true);
            }

            if (newCount > 0 && format == FormatType.Txt)
            {
                if (modifiedCount > 0) sb.AppendLine();
                sb.AppendLine("─── NEW CUSTOM ABILITIES ───");
                sb.AppendLine();
            }

            foreach (var ability in map.AbilityObjectData.NewAbilities)
            {
                ExportObject(sb, ability, "Ability", format, isModified: false);
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"  ✓ Abilities: {newCount} new, {modifiedCount} modified → {Path.GetFileName(filePath)}");
        }

        private void ExportDestructables(string filePath, FormatType format)
        {
            if (map.DestructableObjectData == null)
            {
                Console.WriteLine("  Destructables: None");
                return;
            }

            int newCount = map.DestructableObjectData.NewDestructables.Count;
            int modifiedCount = map.DestructableObjectData.BaseDestructables.Count;

            if (newCount == 0 && modifiedCount == 0)
            {
                Console.WriteLine("  Destructables: None");
                return;
            }

            var sb = new StringBuilder();

            if (format == FormatType.Csv)
            {
                sb.AppendLine("ObjectCode,BaseCode,ModificationType,ModificationId,ModificationName,Level,Value,Type");
            }
            else if (format == FormatType.Txt)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine("                  DESTRUCTABLES");
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine();
            }

            if (modifiedCount > 0 && format == FormatType.Txt)
            {
                sb.AppendLine("─── MODIFIED EXISTING DESTRUCTABLES ───");
                sb.AppendLine();
            }

            foreach (var dest in map.DestructableObjectData.BaseDestructables)
            {
                ExportObject(sb, dest, "Destructable", format, isModified: true);
            }

            if (newCount > 0 && format == FormatType.Txt)
            {
                if (modifiedCount > 0) sb.AppendLine();
                sb.AppendLine("─── NEW CUSTOM DESTRUCTABLES ───");
                sb.AppendLine();
            }

            foreach (var dest in map.DestructableObjectData.NewDestructables)
            {
                ExportObject(sb, dest, "Destructable", format, isModified: false);
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"  ✓ Destructables: {newCount} new, {modifiedCount} modified → {Path.GetFileName(filePath)}");
        }

        private void ExportDoodads(string filePath, FormatType format)
        {
            if (map.DoodadObjectData == null)
            {
                Console.WriteLine("  Doodads: None");
                return;
            }

            int newCount = map.DoodadObjectData.NewDoodads.Count;
            int modifiedCount = map.DoodadObjectData.BaseDoodads.Count;

            if (newCount == 0 && modifiedCount == 0)
            {
                Console.WriteLine("  Doodads: None");
                return;
            }

            var sb = new StringBuilder();

            if (format == FormatType.Csv)
            {
                sb.AppendLine("ObjectCode,BaseCode,ModificationType,ModificationId,ModificationName,Level,Value,Type");
            }
            else if (format == FormatType.Txt)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine("                     DOODADS");
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine();
            }

            if (modifiedCount > 0 && format == FormatType.Txt)
            {
                sb.AppendLine("─── MODIFIED EXISTING DOODADS ───");
                sb.AppendLine();
            }

            foreach (var doodad in map.DoodadObjectData.BaseDoodads)
            {
                ExportObject(sb, doodad, "Doodad", format, isModified: true);
            }

            if (newCount > 0 && format == FormatType.Txt)
            {
                if (modifiedCount > 0) sb.AppendLine();
                sb.AppendLine("─── NEW CUSTOM DOODADS ───");
                sb.AppendLine();
            }

            foreach (var doodad in map.DoodadObjectData.NewDoodads)
            {
                ExportObject(sb, doodad, "Doodad", format, isModified: false);
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"  ✓ Doodads: {newCount} new, {modifiedCount} modified → {Path.GetFileName(filePath)}");
        }

        private void ExportBuffs(string filePath, FormatType format)
        {
            if (map.BuffObjectData == null)
            {
                Console.WriteLine("  Buffs: None");
                return;
            }

            int newCount = map.BuffObjectData.NewBuffs.Count;
            int modifiedCount = map.BuffObjectData.BaseBuffs.Count;

            if (newCount == 0 && modifiedCount == 0)
            {
                Console.WriteLine("  Buffs: None");
                return;
            }

            var sb = new StringBuilder();

            if (format == FormatType.Csv)
            {
                sb.AppendLine("ObjectCode,BaseCode,ModificationType,ModificationId,ModificationName,Level,Value,Type");
            }
            else if (format == FormatType.Txt)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine("                      BUFFS");
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine();
            }

            if (modifiedCount > 0 && format == FormatType.Txt)
            {
                sb.AppendLine("─── MODIFIED EXISTING BUFFS ───");
                sb.AppendLine();
            }

            foreach (var buff in map.BuffObjectData.BaseBuffs)
            {
                ExportObject(sb, buff, "Buff", format, isModified: true);
            }

            if (newCount > 0 && format == FormatType.Txt)
            {
                if (modifiedCount > 0) sb.AppendLine();
                sb.AppendLine("─── NEW CUSTOM BUFFS ───");
                sb.AppendLine();
            }

            foreach (var buff in map.BuffObjectData.NewBuffs)
            {
                ExportObject(sb, buff, "Buff", format, isModified: false);
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"  ✓ Buffs: {newCount} new, {modifiedCount} modified → {Path.GetFileName(filePath)}");
        }

        private void ExportUpgrades(string filePath, FormatType format)
        {
            if (map.UpgradeObjectData == null)
            {
                Console.WriteLine("  Upgrades: None");
                return;
            }

            int newCount = map.UpgradeObjectData.NewUpgrades.Count;
            int modifiedCount = map.UpgradeObjectData.BaseUpgrades.Count;

            if (newCount == 0 && modifiedCount == 0)
            {
                Console.WriteLine("  Upgrades: None");
                return;
            }

            var sb = new StringBuilder();

            if (format == FormatType.Csv)
            {
                sb.AppendLine("ObjectCode,BaseCode,ModificationType,ModificationId,ModificationName,Level,Value,Type");
            }
            else if (format == FormatType.Txt)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine("                    UPGRADES");
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine();
            }

            if (modifiedCount > 0 && format == FormatType.Txt)
            {
                sb.AppendLine("─── MODIFIED EXISTING UPGRADES ───");
                sb.AppendLine();
            }

            foreach (var upgrade in map.UpgradeObjectData.BaseUpgrades)
            {
                ExportObject(sb, upgrade, "Upgrade", format, isModified: true);
            }

            if (newCount > 0 && format == FormatType.Txt)
            {
                if (modifiedCount > 0) sb.AppendLine();
                sb.AppendLine("─── NEW CUSTOM UPGRADES ───");
                sb.AppendLine();
            }

            foreach (var upgrade in map.UpgradeObjectData.NewUpgrades)
            {
                ExportObject(sb, upgrade, "Upgrade", format, isModified: false);
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"  ✓ Upgrades: {newCount} new, {modifiedCount} modified → {Path.GetFileName(filePath)}");
        }

        private void ExportObject(StringBuilder sb, SimpleObjectModification obj, string objectType, FormatType format, bool isModified = false)
        {
            string code = obj.NewId.ToRawcode();
            string baseCode = obj.OldId.ToRawcode();
            string modificationType = isModified ? "Modified" : "New";

            if (format == FormatType.Txt)
            {
                sb.AppendLine($"[{code}] {(isModified ? $"(Modified {baseCode})" : $"- Based on [{baseCode}]")}");
                sb.AppendLine($"Type: {objectType} ({modificationType})");
                sb.AppendLine($"Modifications: {obj.Modifications.Count}");

                if (obj.Modifications.Any())
                {
                    sb.AppendLine();
                    foreach (var mod in obj.Modifications)
                    {
                        string modId = mod.Id.ToRawcode();
                        string fieldName = FieldNameDatabase.GetFieldName(modId);
                        string value = FormatValue(mod.Value);

                        if (fieldName != modId)
                        {
                            sb.AppendLine($"  {fieldName} ({modId})");
                            sb.AppendLine($"    = {value}");
                            sb.AppendLine($"    Type: {mod.Type}");
                        }
                        else
                        {
                            sb.AppendLine($"  {modId} = {value} ({mod.Type})");
                        }
                    }
                }

                sb.AppendLine();
                sb.AppendLine("───────────────────────────────────────────────────────────");
                sb.AppendLine();
            }
            else if (format == FormatType.Ini)
            {
                sb.AppendLine($"[{code}]");
                sb.AppendLine($"base = {baseCode}");
                sb.AppendLine($"type = {objectType}");
                sb.AppendLine($"modification_type = {modificationType}");

                foreach (var mod in obj.Modifications)
                {
                    string modId = mod.Id.ToRawcode();
                    string fieldName = FieldNameDatabase.GetFieldName(modId);
                    sb.AppendLine($"; {fieldName}");
                    sb.AppendLine($"{modId} = {FormatValue(mod.Value)}");
                }

                sb.AppendLine();
            }
            else if (format == FormatType.Csv)
            {
                foreach (var mod in obj.Modifications)
                {
                    string modId = mod.Id.ToRawcode();
                    string fieldName = FieldNameDatabase.GetFieldName(modId);
                    sb.AppendLine($"{code},{baseCode},{modificationType},{modId},{EscapeCsv(fieldName)},,{EscapeCsv(FormatValue(mod.Value))},{mod.Type}");
                }
            }
        }

        // Overload for LevelObjectModification (Abilities, Upgrades)
        private void ExportObject(StringBuilder sb, War3Net.Build.Object.LevelObjectModification obj, string objectType, FormatType format, bool isModified = false)
        {
            string code = obj.NewId.ToRawcode();
            string baseCode = obj.OldId.ToRawcode();
            string modificationType = isModified ? "Modified" : "New";

            if (format == FormatType.Txt)
            {
                sb.AppendLine($"[{code}] {(isModified ? $"(Modified {baseCode})" : $"- Based on [{baseCode}]")}");
                sb.AppendLine($"Type: {objectType} ({modificationType})");
                sb.AppendLine($"Modifications: {obj.Modifications.Count}");

                if (obj.Modifications.Any())
                {
                    sb.AppendLine();
                    foreach (var mod in obj.Modifications)
                    {
                        string modId = mod.Id.ToRawcode();
                        string fieldName = FieldNameDatabase.GetFieldName(modId);
                        string value = FormatValue(mod.Value);

                        if (fieldName != modId)
                        {
                            sb.AppendLine($"  {fieldName} ({modId}) - Level {mod.Level}");
                            sb.AppendLine($"    = {value}");
                            sb.AppendLine($"    Type: {mod.Type}");
                        }
                        else
                        {
                            sb.AppendLine($"  {modId} (Level {mod.Level}) = {value} ({mod.Type})");
                        }
                    }
                }

                sb.AppendLine();
                sb.AppendLine("───────────────────────────────────────────────────────────");
                sb.AppendLine();
            }
            else if (format == FormatType.Ini)
            {
                sb.AppendLine($"[{code}]");
                sb.AppendLine($"base = {baseCode}");
                sb.AppendLine($"type = {objectType}");
                sb.AppendLine($"modification_type = {modificationType}");

                foreach (var mod in obj.Modifications)
                {
                    string modId = mod.Id.ToRawcode();
                    string fieldName = FieldNameDatabase.GetFieldName(modId);
                    sb.AppendLine($"; {fieldName}");
                    sb.AppendLine($"{modId}_level{mod.Level} = {FormatValue(mod.Value)}");
                }

                sb.AppendLine();
            }
            else if (format == FormatType.Csv)
            {
                foreach (var mod in obj.Modifications)
                {
                    string modId = mod.Id.ToRawcode();
                    string fieldName = FieldNameDatabase.GetFieldName(modId);
                    sb.AppendLine($"{code},{baseCode},{modificationType},{modId},{EscapeCsv(fieldName)},{mod.Level},{EscapeCsv(FormatValue(mod.Value))},{mod.Type}");
                }
            }
        }

        // Overload for VariationObjectModification (Doodads)
        private void ExportObject(StringBuilder sb, War3Net.Build.Object.VariationObjectModification obj, string objectType, FormatType format, bool isModified = false)
        {
            string code = obj.NewId.ToRawcode();
            string baseCode = obj.OldId.ToRawcode();
            string modificationType = isModified ? "Modified" : "New";

            if (format == FormatType.Txt)
            {
                sb.AppendLine($"[{code}] {(isModified ? $"(Modified {baseCode})" : $"- Based on [{baseCode}]")}");
                sb.AppendLine($"Type: {objectType} ({modificationType})");
                sb.AppendLine($"Modifications: {obj.Modifications.Count}");

                if (obj.Modifications.Any())
                {
                    sb.AppendLine();
                    foreach (var mod in obj.Modifications)
                    {
                        string modId = mod.Id.ToRawcode();
                        string fieldName = FieldNameDatabase.GetFieldName(modId);
                        string value = FormatValue(mod.Value);

                        if (fieldName != modId)
                        {
                            sb.AppendLine($"  {fieldName} ({modId})");
                            sb.AppendLine($"    = {value}");
                            sb.AppendLine($"    Type: {mod.Type}");
                        }
                        else
                        {
                            sb.AppendLine($"  {modId} = {value} ({mod.Type})");
                        }
                    }
                }

                sb.AppendLine();
                sb.AppendLine("───────────────────────────────────────────────────────────");
                sb.AppendLine();
            }
            else if (format == FormatType.Ini)
            {
                sb.AppendLine($"[{code}]");
                sb.AppendLine($"base = {baseCode}");
                sb.AppendLine($"type = {objectType}");
                sb.AppendLine($"modification_type = {modificationType}");

                foreach (var mod in obj.Modifications)
                {
                    string modId = mod.Id.ToRawcode();
                    string fieldName = FieldNameDatabase.GetFieldName(modId);
                    sb.AppendLine($"; {fieldName}");
                    sb.AppendLine($"{modId} = {FormatValue(mod.Value)}");
                }

                sb.AppendLine();
            }
            else if (format == FormatType.Csv)
            {
                foreach (var mod in obj.Modifications)
                {
                    string modId = mod.Id.ToRawcode();
                    string fieldName = FieldNameDatabase.GetFieldName(modId);
                    sb.AppendLine($"{code},{baseCode},{modificationType},{modId},{EscapeCsv(fieldName)},,{EscapeCsv(FormatValue(mod.Value))},{mod.Type}");
                }
            }
        }

        private string FormatValue(object? value)
        {
            if (value == null)
                return "null";

            if (value is int intValue && intValue.ToString().Length == 4)
            {
                // Might be a rawcode, try to convert
                try
                {
                    string rawcode = intValue.ToRawcode();
                    return $"{value} ('{rawcode}')";
                }
                catch
                {
                    return value.ToString() ?? "null";
                }
            }

            return value.ToString() ?? "null";
        }

        private string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        private void CreateSummary(string filePath)
        {
            var sb = new StringBuilder();

            sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
            sb.AppendLine("║              OBJECT DATA EXPORT SUMMARY                  ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            int totalObjects = 0;

            if (map.UnitObjectData != null)
            {
                int count = map.UnitObjectData.NewUnits.Count;
                sb.AppendLine($"Units:         {count,5} custom objects");
                totalObjects += count;
            }

            if (map.ItemObjectData != null)
            {
                int count = map.ItemObjectData.NewItems.Count;
                sb.AppendLine($"Items:         {count,5} custom objects");
                totalObjects += count;
            }

            if (map.AbilityObjectData != null)
            {
                int count = map.AbilityObjectData.NewAbilities.Count;
                sb.AppendLine($"Abilities:     {count,5} custom objects");
                totalObjects += count;
            }

            if (map.DestructableObjectData != null)
            {
                int count = map.DestructableObjectData.NewDestructables.Count;
                sb.AppendLine($"Destructables: {count,5} custom objects");
                totalObjects += count;
            }

            if (map.DoodadObjectData != null)
            {
                int count = map.DoodadObjectData.NewDoodads.Count;
                sb.AppendLine($"Doodads:       {count,5} custom objects");
                totalObjects += count;
            }

            if (map.BuffObjectData != null)
            {
                int count = map.BuffObjectData.NewBuffs.Count;
                sb.AppendLine($"Buffs:         {count,5} custom objects");
                totalObjects += count;
            }

            if (map.UpgradeObjectData != null)
            {
                int count = map.UpgradeObjectData.NewUpgrades.Count;
                sb.AppendLine($"Upgrades:      {count,5} custom objects");
                totalObjects += count;
            }

            sb.AppendLine();
            sb.AppendLine("─────────────────────────────────────────────────────────");
            sb.AppendLine($"TOTAL:         {totalObjects,5} custom objects");
            sb.AppendLine();

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"\n  ✓ Summary: {Path.GetFileName(filePath)}");
        }
    }

    enum FormatType
    {
        Txt,
        Ini,
        Csv
    }
}
