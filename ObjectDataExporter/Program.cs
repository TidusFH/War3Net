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
            if (map.UnitObjectData == null || !map.UnitObjectData.NewUnits.Any())
            {
                Console.WriteLine("  Units: None");
                return;
            }

            var sb = new StringBuilder();

            if (format == FormatType.Csv)
            {
                sb.AppendLine("ObjectCode,BaseCode,ModificationId,ModificationName,Value,Type");
            }
            else if (format == FormatType.Txt)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine("                      CUSTOM UNITS");
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine();
            }

            foreach (var unit in map.UnitObjectData.NewUnits)
            {
                ExportObject(sb, unit, "Unit", format);
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"  ✓ Units: {map.UnitObjectData.NewUnits.Count} → {Path.GetFileName(filePath)}");
        }

        private void ExportItems(string filePath, FormatType format)
        {
            if (map.ItemObjectData == null || !map.ItemObjectData.NewItems.Any())
            {
                Console.WriteLine("  Items: None");
                return;
            }

            var sb = new StringBuilder();

            if (format == FormatType.Csv)
            {
                sb.AppendLine("ObjectCode,BaseCode,ModificationId,ModificationName,Value,Type");
            }
            else if (format == FormatType.Txt)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine("                      CUSTOM ITEMS");
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine();
            }

            foreach (var item in map.ItemObjectData.NewItems)
            {
                ExportObject(sb, item, "Item", format);
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"  ✓ Items: {map.ItemObjectData.NewItems.Count} → {Path.GetFileName(filePath)}");
        }

        private void ExportAbilities(string filePath, FormatType format)
        {
            if (map.AbilityObjectData == null || !map.AbilityObjectData.NewAbilities.Any())
            {
                Console.WriteLine("  Abilities: None");
                return;
            }

            var sb = new StringBuilder();

            if (format == FormatType.Csv)
            {
                sb.AppendLine("ObjectCode,BaseCode,ModificationId,ModificationName,Value,Type");
            }
            else if (format == FormatType.Txt)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine("                    CUSTOM ABILITIES");
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine();
            }

            foreach (var ability in map.AbilityObjectData.NewAbilities)
            {
                ExportObject(sb, ability, "Ability", format);
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"  ✓ Abilities: {map.AbilityObjectData.NewAbilities.Count} → {Path.GetFileName(filePath)}");
        }

        private void ExportDestructables(string filePath, FormatType format)
        {
            if (map.DestructableObjectData == null || !map.DestructableObjectData.NewDestructables.Any())
            {
                Console.WriteLine("  Destructables: None");
                return;
            }

            var sb = new StringBuilder();

            if (format == FormatType.Csv)
            {
                sb.AppendLine("ObjectCode,BaseCode,ModificationId,ModificationName,Value,Type");
            }
            else if (format == FormatType.Txt)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine("                  CUSTOM DESTRUCTABLES");
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine();
            }

            foreach (var dest in map.DestructableObjectData.NewDestructables)
            {
                ExportObject(sb, dest, "Destructable", format);
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"  ✓ Destructables: {map.DestructableObjectData.NewDestructables.Count} → {Path.GetFileName(filePath)}");
        }

        private void ExportDoodads(string filePath, FormatType format)
        {
            if (map.DoodadObjectData == null || !map.DoodadObjectData.NewDoodads.Any())
            {
                Console.WriteLine("  Doodads: None");
                return;
            }

            var sb = new StringBuilder();

            if (format == FormatType.Csv)
            {
                sb.AppendLine("ObjectCode,BaseCode,ModificationId,ModificationName,Value,Type");
            }
            else if (format == FormatType.Txt)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine("                     CUSTOM DOODADS");
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine();
            }

            foreach (var doodad in map.DoodadObjectData.NewDoodads)
            {
                ExportObject(sb, doodad, "Doodad", format);
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"  ✓ Doodads: {map.DoodadObjectData.NewDoodads.Count} → {Path.GetFileName(filePath)}");
        }

        private void ExportBuffs(string filePath, FormatType format)
        {
            if (map.BuffObjectData == null || !map.BuffObjectData.NewBuffs.Any())
            {
                Console.WriteLine("  Buffs: None");
                return;
            }

            var sb = new StringBuilder();

            if (format == FormatType.Csv)
            {
                sb.AppendLine("ObjectCode,BaseCode,ModificationId,ModificationName,Value,Type");
            }
            else if (format == FormatType.Txt)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine("                      CUSTOM BUFFS");
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine();
            }

            foreach (var buff in map.BuffObjectData.NewBuffs)
            {
                ExportObject(sb, buff, "Buff", format);
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"  ✓ Buffs: {map.BuffObjectData.NewBuffs.Count} → {Path.GetFileName(filePath)}");
        }

        private void ExportUpgrades(string filePath, FormatType format)
        {
            if (map.UpgradeObjectData == null || !map.UpgradeObjectData.NewUpgrades.Any())
            {
                Console.WriteLine("  Upgrades: None");
                return;
            }

            var sb = new StringBuilder();

            if (format == FormatType.Csv)
            {
                sb.AppendLine("ObjectCode,BaseCode,ModificationId,ModificationName,Value,Type");
            }
            else if (format == FormatType.Txt)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine("                    CUSTOM UPGRADES");
                sb.AppendLine("═══════════════════════════════════════════════════════════");
                sb.AppendLine();
            }

            foreach (var upgrade in map.UpgradeObjectData.NewUpgrades)
            {
                ExportObject(sb, upgrade, "Upgrade", format);
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"  ✓ Upgrades: {map.UpgradeObjectData.NewUpgrades.Count} → {Path.GetFileName(filePath)}");
        }

        private void ExportObject(StringBuilder sb, SimpleObjectModification obj, string objectType, FormatType format)
        {
            string code = obj.NewId.ToRawcode();
            string baseCode = obj.OldId.ToRawcode();

            if (format == FormatType.Txt)
            {
                sb.AppendLine($"[{code}] - Based on [{baseCode}]");
                sb.AppendLine($"Type: {objectType}");
                sb.AppendLine($"Modifications: {obj.Modifications.Count}");

                if (obj.Modifications.Any())
                {
                    sb.AppendLine();
                    foreach (var mod in obj.Modifications)
                    {
                        string modId = mod.Id.ToRawcode();
                        sb.AppendLine($"  {modId} = {FormatValue(mod.Value)} ({mod.Type})");
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

                foreach (var mod in obj.Modifications)
                {
                    string modId = mod.Id.ToRawcode();
                    sb.AppendLine($"{modId} = {FormatValue(mod.Value)}");
                }

                sb.AppendLine();
            }
            else if (format == FormatType.Csv)
            {
                foreach (var mod in obj.Modifications)
                {
                    string modId = mod.Id.ToRawcode();
                    sb.AppendLine($"{code},{baseCode},{modId},,{EscapeCsv(FormatValue(mod.Value))},{mod.Type}");
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
