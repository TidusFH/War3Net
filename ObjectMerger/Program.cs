using ObjectMerger.Models;
using ObjectMerger.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ObjectMerger
{
    class Program
    {
        private static ObjectRegistry? sourceRegistry;
        private static ObjectRegistry? targetRegistry;
        private static string sourcePath = string.Empty;
        private static string targetPath = string.Empty;
        private static string outputPath = string.Empty;

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║          WARCRAFT 3 OBJECT MERGER v1.0                   ║");
            Console.WriteLine("║     Copy custom units, items, abilities between maps     ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            // Get input paths
            if (args.Length >= 3)
            {
                sourcePath = args[0];
                targetPath = args[1];
                outputPath = args[2];
            }
            else
            {
                // Interactive mode - scan for maps
                if (!SelectMapsInteractive())
                {
                    return;
                }
            }

            // Validate paths
            if (!File.Exists(sourcePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✗ Source file not found: {sourcePath}");
                Console.ResetColor();
                return;
            }

            if (!File.Exists(targetPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✗ Target file not found: {targetPath}");
                Console.ResetColor();
                return;
            }

            // Load maps
            try
            {
                Console.WriteLine("\n═══════════════════════════════════════════════════════════");
                Console.WriteLine("LOADING MAPS");
                Console.WriteLine("═══════════════════════════════════════════════════════════\n");

                Console.WriteLine("Loading Source Map...");
                sourceRegistry = ObjectRegistry.LoadFromMap(sourcePath);

                Console.WriteLine("\nLoading Target Map...");
                targetRegistry = ObjectRegistry.LoadFromMap(targetPath);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ Maps loaded successfully!");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✗ Error loading maps: {ex.Message}");
                Console.ResetColor();
                return;
            }

            // Interactive menu
            InteractiveMode();
        }

        static bool SelectMapsInteractive()
        {
            var maps = ScanForMaps();

            if (!maps.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ No .w3x or .w3m files found in current directory");
                Console.ResetColor();
                Console.WriteLine("\nPlease enter paths manually:");
                Console.Write("\nSource map path: ");
                sourcePath = Console.ReadLine()?.Trim() ?? string.Empty;
                Console.Write("Target map path: ");
                targetPath = Console.ReadLine()?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(targetPath))
                {
                    Console.WriteLine("Invalid paths");
                    return false;
                }
            }
            else
            {
                // Show detected maps
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("DETECTED MAPS IN CURRENT FOLDER");
                Console.WriteLine("═══════════════════════════════════════════════════════════\n");

                for (int i = 0; i < maps.Count; i++)
                {
                    var map = maps[i];
                    double sizeMB = new FileInfo(map.FullPath).Length / (1024.0 * 1024.0);
                    Console.WriteLine($"  [{i + 1}] {map.Name} ({sizeMB:F2} MB)");
                }

                Console.WriteLine("\n  [M] Enter manual path");
                Console.WriteLine("  [0] Exit");

                // Select SOURCE map
                Console.WriteLine("\n───────────────────────────────────────────────────────────");
                Console.Write("Select SOURCE map (copy FROM): ");
                string? sourceChoice = Console.ReadLine()?.Trim();

                if (sourceChoice == "0")
                {
                    return false;
                }
                else if (sourceChoice?.ToUpper() == "M")
                {
                    Console.Write("Enter source map path: ");
                    sourcePath = Console.ReadLine()?.Trim() ?? string.Empty;
                }
                else if (int.TryParse(sourceChoice, out int sourceIndex) && sourceIndex > 0 && sourceIndex <= maps.Count)
                {
                    sourcePath = maps[sourceIndex - 1].FullPath;
                }
                else
                {
                    Console.WriteLine("Invalid selection");
                    return false;
                }

                // Select TARGET map
                Console.Write("Select TARGET map (copy TO): ");
                string? targetChoice = Console.ReadLine()?.Trim();

                if (targetChoice == "0")
                {
                    return false;
                }
                else if (targetChoice?.ToUpper() == "M")
                {
                    Console.Write("Enter target map path: ");
                    targetPath = Console.ReadLine()?.Trim() ?? string.Empty;
                }
                else if (int.TryParse(targetChoice, out int targetIndex) && targetIndex > 0 && targetIndex <= maps.Count)
                {
                    targetPath = maps[targetIndex - 1].FullPath;
                }
                else
                {
                    Console.WriteLine("Invalid selection");
                    return false;
                }
            }

            // Set output path
            Console.Write("\nOutput map path (or press Enter for auto): ");
            outputPath = Console.ReadLine()?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.Combine(
                    Path.GetDirectoryName(targetPath) ?? ".",
                    Path.GetFileNameWithoutExtension(targetPath) + "_merged" + Path.GetExtension(targetPath)
                );
            }

            // Show summary
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("CONFIGURATION:");
            Console.WriteLine($"  Source: {Path.GetFileName(sourcePath)}");
            Console.WriteLine($"  Target: {Path.GetFileName(targetPath)}");
            Console.WriteLine($"  Output: {Path.GetFileName(outputPath)}");
            Console.WriteLine("═══════════════════════════════════════════════════════════");

            return true;
        }

        static List<MapInfo> ScanForMaps()
        {
            var maps = new List<MapInfo>();
            var currentDir = Directory.GetCurrentDirectory();

            try
            {
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

        static void InteractiveMode()
        {
            while (true)
            {
                Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                    MAIN MENU                             ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("COPY OPTIONS:");
                Console.ResetColor();
                Console.WriteLine("  1. Copy SPECIFIC objects (by code - e.g., h001, A001)");
                Console.WriteLine("  2. Copy ALL objects of a type (all units, all items, etc.)");
                Console.WriteLine();
                Console.WriteLine("VIEW OPTIONS:");
                Console.WriteLine("  3. List objects from SOURCE");
                Console.WriteLine("  4. List objects from TARGET");
                Console.WriteLine("  5. Show statistics");
                Console.WriteLine();
                Console.WriteLine("  6. Save and exit");
                Console.WriteLine("  0. Exit without saving");

                Console.Write("\nChoice: ");
                string? choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            CopySpecificObjects();
                            break;
                        case "2":
                            CopyObjectsByType();
                            break;
                        case "3":
                            ListObjects(sourceRegistry!, "SOURCE");
                            break;
                        case "4":
                            ListObjects(targetRegistry!, "TARGET");
                            break;
                        case "5":
                            ShowStatistics();
                            break;
                        case "6":
                            SaveAndExit();
                            return;
                        case "0":
                            Console.WriteLine("Exiting without saving...");
                            return;
                        default:
                            Console.WriteLine("Invalid choice");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        static void ListObjects(ObjectRegistry registry, string mapName)
        {
            Console.WriteLine($"\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  OBJECTS IN {mapName,-45}║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

            foreach (ObjectType type in Enum.GetValues<ObjectType>())
            {
                var objects = registry.GetObjectsByType(type).ToList();
                if (objects.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n{type}s ({objects.Count}):");
                    Console.ResetColor();

                    foreach (var obj in objects.OrderBy(o => o.Code))
                    {
                        Console.WriteLine($"  [{obj.Code}] {obj.Name} (base: {obj.BaseCode})");
                    }
                }
            }
        }

        static void CopySpecificObjects()
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("COPY SPECIFIC OBJECTS");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            Console.WriteLine("Enter object codes to copy (comma-separated):");
            Console.WriteLine("Example: h001,h002,I001,A001");
            Console.Write("\nObject codes: ");

            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("No objects selected");
                return;
            }

            var objectCodes = input.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            // Resolve dependencies
            var dependencyResolver = new DependencyResolver(sourceRegistry!);
            var objectsToCopy = dependencyResolver.ResolveDependencies(objectCodes);

            if (!objectsToCopy.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ No objects found with those codes");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"\nFound {objectsToCopy.Count} object(s) to copy:");
            foreach (var obj in objectsToCopy)
            {
                Console.WriteLine($"  {obj.Code} - {obj.Name} ({obj.Type})");
            }

            // Detect conflicts
            var conflictResolver = new ConflictResolver(sourceRegistry!, targetRegistry!);
            var conflicts = conflictResolver.DetectConflicts(objectsToCopy);

            // Resolve conflicts
            Console.WriteLine("\nResolve conflicts? [Y/N] (or press Enter to auto-skip): ");
            string? resolveChoice = Console.ReadLine()?.Trim().ToUpper();

            if (resolveChoice == "Y")
            {
                conflictResolver.ResolveConflictsInteractive(conflicts);
            }
            else
            {
                conflictResolver.ResolveConflictsAutomatic(conflicts, ConflictResolution.Skip);
            }

            // Copy objects
            var copier = new ObjectCopier(sourceRegistry!, targetRegistry!);
            var result = copier.CopyObjects(objectsToCopy, conflicts);

            // Show results
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Copied: {result.ObjectsCopied} object(s)");
            Console.ResetColor();
            if (result.ObjectsSkipped > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⊘ Skipped: {result.ObjectsSkipped} object(s)");
                Console.ResetColor();
            }

            Console.WriteLine("\nNote: Changes are in memory. Use option 6 to save.");
        }

        static void CopyObjectsByType()
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("COPY ALL OBJECTS OF A TYPE");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            Console.WriteLine("Select object type:");
            int i = 1;
            foreach (ObjectType type in Enum.GetValues<ObjectType>())
            {
                Console.WriteLine($"{i}. {type}");
                i++;
            }

            Console.Write("\nChoice: ");
            string? choice = Console.ReadLine();

            if (!int.TryParse(choice, out int typeIndex) || typeIndex < 1 || typeIndex > 7)
            {
                Console.WriteLine("Invalid choice");
                return;
            }

            ObjectType selectedType = (ObjectType)(typeIndex - 1);
            var objectsToCopy = sourceRegistry!.GetObjectsByType(selectedType).ToList();

            if (!objectsToCopy.Any())
            {
                Console.WriteLine($"No {selectedType}s found in source map");
                return;
            }

            Console.WriteLine($"\nFound {objectsToCopy.Count} {selectedType}(s) in source map:");
            foreach (var obj in objectsToCopy.Take(10))
            {
                Console.WriteLine($"  {obj.Code} - {obj.Name}");
            }
            if (objectsToCopy.Count > 10)
            {
                Console.WriteLine($"  ... and {objectsToCopy.Count - 10} more");
            }

            Console.Write($"\nCopy all {objectsToCopy.Count} {selectedType}(s)? [Y/N]: ");
            string? confirm = Console.ReadLine()?.Trim().ToUpper();

            if (confirm != "Y")
            {
                Console.WriteLine("Cancelled");
                return;
            }

            // Detect conflicts
            var conflictResolver = new ConflictResolver(sourceRegistry!, targetRegistry!);
            var conflicts = conflictResolver.DetectConflicts(objectsToCopy);

            // Auto-skip conflicts
            conflictResolver.ResolveConflictsAutomatic(conflicts, ConflictResolution.Skip);

            // Copy objects
            var copier = new ObjectCopier(sourceRegistry!, targetRegistry!);
            var result = copier.CopyObjects(objectsToCopy, conflicts);

            // Show results
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Copied: {result.ObjectsCopied} object(s)");
            Console.ResetColor();
            if (result.ObjectsSkipped > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⊘ Skipped: {result.ObjectsSkipped} object(s) (conflicts)");
                Console.ResetColor();
            }

            Console.WriteLine("\nNote: Changes are in memory. Use option 6 to save.");
        }

        static void ShowStatistics()
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    STATISTICS                            ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("SOURCE MAP:");
            ShowRegistryStats(sourceRegistry!);

            Console.WriteLine("\nTARGET MAP:");
            ShowRegistryStats(targetRegistry!);
        }

        static void ShowRegistryStats(ObjectRegistry registry)
        {
            Console.WriteLine($"  Total objects: {registry.GetTotalObjectCount()}");
            Console.WriteLine($"    Units: {registry.Units.Count}");
            Console.WriteLine($"    Items: {registry.Items.Count}");
            Console.WriteLine($"    Abilities: {registry.Abilities.Count}");
            Console.WriteLine($"    Destructables: {registry.Destructables.Count}");
            Console.WriteLine($"    Doodads: {registry.Doodads.Count}");
            Console.WriteLine($"    Buffs: {registry.Buffs.Count}");
            Console.WriteLine($"    Upgrades: {registry.Upgrades.Count}");
        }

        static void SaveAndExit()
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("SAVING MAP");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            Console.WriteLine($"Output path: {outputPath}");

            try
            {
                var targetMap = targetRegistry!.GetMap();
                if (targetMap == null)
                {
                    throw new Exception("Target map not loaded");
                }

                Console.WriteLine("Saving map...");
                targetMap.Save(outputPath);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n✓ Map saved successfully to: {outputPath}");
                Console.ResetColor();

                Console.WriteLine("\nYou can now open the merged map in World Editor!");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✗ Error saving map: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
}
