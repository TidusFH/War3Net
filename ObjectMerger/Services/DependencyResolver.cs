using System;
using System.Collections.Generic;
using System.Linq;
using ObjectMerger.Models;

namespace ObjectMerger.Services
{
    /// <summary>
    /// Resolves dependencies between objects (Phase 2 feature)
    /// </summary>
    public class DependencyResolver
    {
        private readonly ObjectRegistry sourceRegistry;

        public DependencyResolver(ObjectRegistry sourceRegistry)
        {
            this.sourceRegistry = sourceRegistry;
        }

        /// <summary>
        /// Find all dependencies for a list of objects
        /// </summary>
        public List<ObjectInfo> ResolveDependencies(List<string> objectCodes, bool recursive = true)
        {
            var allObjects = new List<ObjectInfo>();
            var visited = new HashSet<string>();

            foreach (var code in objectCodes)
            {
                // Try to find object in any category
                var obj = FindObjectByCode(code);
                if (obj != null)
                {
                    ResolveDependenciesRecursive(obj, allObjects, visited, recursive);
                }
            }

            return allObjects;
        }

        private void ResolveDependenciesRecursive(ObjectInfo obj, List<ObjectInfo> result, HashSet<string> visited, bool recursive)
        {
            if (visited.Contains(obj.Code))
                return;

            visited.Add(obj.Code);
            result.Add(obj);

            if (!recursive)
                return;

            // TODO Phase 2: Implement actual dependency detection
            // For now, just check if base object is custom
            if (!IsBlizzardObject(obj.BaseCode))
            {
                var baseObj = FindObjectByCode(obj.BaseCode);
                if (baseObj != null)
                {
                    ResolveDependenciesRecursive(baseObj, result, visited, recursive);
                }
            }

            // TODO Phase 2: Scan modifications for object references
            // Examples:
            // - Units can reference abilities, items
            // - Abilities can reference buffs
            // - Upgrades can reference units
        }

        private ObjectInfo? FindObjectByCode(string code)
        {
            // Try each type
            foreach (ObjectType type in Enum.GetValues<ObjectType>())
            {
                var obj = sourceRegistry.GetObject(code, type);
                if (obj != null)
                    return obj;
            }
            return null;
        }

        private bool IsBlizzardObject(string code)
        {
            // Blizzard objects typically have 4 lowercase letters
            // Custom objects usually start with uppercase or numbers
            // This is a simple heuristic - Phase 2 can use proper game data

            if (code.Length != 4)
                return false;

            // Blizzard units: hfoo, hpea, etc.
            // Custom units: h001, H001, etc.
            return code.All(c => char.IsLower(c));
        }

        /// <summary>
        /// Analyze dependencies and show them to user
        /// </summary>
        public void ShowDependencies(List<ObjectInfo> objects)
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              DEPENDENCY ANALYSIS                         ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

            foreach (var obj in objects)
            {
                Console.WriteLine($"\n{obj.Code} - {obj.Name}");
                Console.WriteLine($"  Type: {obj.Type}");
                Console.WriteLine($"  Base: {obj.BaseCode}");

                if (obj.Dependencies.Any())
                {
                    Console.WriteLine("  Dependencies:");
                    foreach (var dep in obj.Dependencies)
                    {
                        Console.WriteLine($"    → {dep}");
                    }
                }
                else
                {
                    Console.WriteLine("  Dependencies: None detected");
                }
            }

            Console.WriteLine("\nNote: Dependency detection is basic in Phase 1.");
            Console.WriteLine("Phase 2 will add comprehensive dependency scanning.");
        }
    }
}
