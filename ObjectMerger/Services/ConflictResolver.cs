using ObjectMerger.Models;

namespace ObjectMerger.Services
{
    /// <summary>
    /// Detects and resolves ID conflicts between source and target objects
    /// </summary>
    public class ConflictResolver
    {
        private readonly ObjectRegistry sourceRegistry;
        private readonly ObjectRegistry targetRegistry;

        public ConflictResolver(ObjectRegistry sourceRegistry, ObjectRegistry targetRegistry)
        {
            this.sourceRegistry = sourceRegistry;
            this.targetRegistry = targetRegistry;
        }

        /// <summary>
        /// Detect all ID conflicts for a list of objects
        /// </summary>
        public List<ObjectConflict> DetectConflicts(List<ObjectInfo> objectsToCopy)
        {
            var conflicts = new List<ObjectConflict>();

            foreach (var sourceObj in objectsToCopy)
            {
                var targetObj = targetRegistry.GetObject(sourceObj.Code, sourceObj.Type);

                if (targetObj != null)
                {
                    // Conflict detected!
                    conflicts.Add(new ObjectConflict
                    {
                        ObjectCode = sourceObj.Code,
                        Type = sourceObj.Type,
                        SourceObject = sourceObj,
                        TargetObject = targetObj,
                        Resolution = ConflictResolution.Skip // Default
                    });
                }
            }

            return conflicts;
        }

        /// <summary>
        /// Interactively resolve conflicts with user
        /// </summary>
        public void ResolveConflictsInteractive(List<ObjectConflict> conflicts)
        {
            if (!conflicts.Any())
            {
                Console.WriteLine("✓ No ID conflicts detected");
                return;
            }

            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"║  ⚠ WARNING: {conflicts.Count} ID CONFLICT(S) DETECTED");
            Console.ResetColor();
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

            foreach (var conflict in conflicts)
            {
                ResolveConflictInteractive(conflict);
            }
        }

        private void ResolveConflictInteractive(ObjectConflict conflict)
        {
            Console.WriteLine($"\n⚠ Conflict: {conflict.ObjectCode} ({conflict.Type})");
            Console.WriteLine($"  Source: {conflict.SourceObject.Name}");
            Console.WriteLine($"  Target: {conflict.TargetObject.Name}");
            Console.WriteLine();
            Console.WriteLine("How to resolve?");
            Console.WriteLine("  [S] Skip - Don't copy this object");
            Console.WriteLine("  [O] Overwrite - Replace target's version");
            Console.WriteLine("  [R] Rename - Find new ID (Phase 3 feature)");
            Console.WriteLine("  [A] Skip All - Skip all remaining conflicts");

            while (true)
            {
                Console.Write("\nChoice [S/O/R/A]: ");
                string? choice = Console.ReadLine()?.Trim().ToUpper();

                switch (choice)
                {
                    case "S":
                        conflict.Resolution = ConflictResolution.Skip;
                        Console.WriteLine("→ Will skip this object");
                        return;

                    case "O":
                        conflict.Resolution = ConflictResolution.Overwrite;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("→ Will overwrite target's version");
                        Console.ResetColor();
                        return;

                    case "R":
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("⚠ Rename feature is Phase 3 - defaulting to Skip");
                        Console.ResetColor();
                        conflict.Resolution = ConflictResolution.Skip;
                        // TODO Phase 3: Implement ID remapping
                        return;

                    case "A":
                        Console.WriteLine("→ Skipping all remaining conflicts");
                        conflict.Resolution = ConflictResolution.Skip;
                        // Mark all remaining as skip
                        return;

                    default:
                        Console.WriteLine("Invalid choice. Please enter S, O, R, or A");
                        break;
                }
            }
        }

        /// <summary>
        /// Auto-resolve conflicts based on default strategy
        /// </summary>
        public void ResolveConflictsAutomatic(List<ObjectConflict> conflicts, ConflictResolution defaultResolution)
        {
            if (!conflicts.Any())
            {
                Console.WriteLine("✓ No ID conflicts detected");
                return;
            }

            Console.WriteLine($"\n⚠ {conflicts.Count} conflict(s) detected");
            Console.WriteLine($"Auto-resolving with strategy: {defaultResolution}");

            foreach (var conflict in conflicts)
            {
                conflict.Resolution = defaultResolution;
                Console.WriteLine($"  {conflict.ObjectCode}: {defaultResolution}");
            }
        }

        /// <summary>
        /// Find an unused object ID for the given type (Phase 3 feature)
        /// </summary>
        public string FindUnusedId(ObjectType type)
        {
            // TODO Phase 3: Implement smart ID finding
            // For now, just return a placeholder

            string prefix = type switch
            {
                ObjectType.Unit => "h",
                ObjectType.Item => "I",
                ObjectType.Ability => "A",
                ObjectType.Destructable => "B",
                ObjectType.Doodad => "D",
                ObjectType.Buff => "B",
                ObjectType.Upgrade => "R",
                _ => "X"
            };

            // Try to find an unused ID
            for (int i = 0; i < 1000; i++)
            {
                string candidateId = $"{prefix}{i:D3}";

                if (!targetRegistry.HasObject(candidateId, type))
                {
                    return candidateId;
                }
            }

            throw new InvalidOperationException($"No unused IDs available for {type}!");
        }

        /// <summary>
        /// Show conflict resolution summary
        /// </summary>
        public void ShowSummary(List<ObjectConflict> conflicts)
        {
            if (!conflicts.Any())
                return;

            int skipped = conflicts.Count(c => c.Resolution == ConflictResolution.Skip);
            int overwritten = conflicts.Count(c => c.Resolution == ConflictResolution.Overwrite);
            int renamed = conflicts.Count(c => c.Resolution == ConflictResolution.Rename);

            Console.WriteLine("\nConflict Resolution Summary:");
            if (skipped > 0)
                Console.WriteLine($"  Skipped: {skipped}");
            if (overwritten > 0)
                Console.WriteLine($"  Overwritten: {overwritten}");
            if (renamed > 0)
                Console.WriteLine($"  Renamed: {renamed}");
        }
    }
}
