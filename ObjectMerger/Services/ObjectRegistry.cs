using System;
using System.Collections.Generic;
using System.Linq;
using War3Net.Build;
using War3Net.Build.Object;
using War3Net.Common.Extensions;
using ObjectMerger.Models;

namespace ObjectMerger.Services
{
    /// <summary>
    /// Registry of all custom objects in a map
    /// </summary>
    public class ObjectRegistry
    {
        public Dictionary<string, ObjectInfo> Units { get; } = new();
        public Dictionary<string, ObjectInfo> Items { get; } = new();
        public Dictionary<string, ObjectInfo> Abilities { get; } = new();
        public Dictionary<string, ObjectInfo> Destructables { get; } = new();
        public Dictionary<string, ObjectInfo> Doodads { get; } = new();
        public Dictionary<string, ObjectInfo> Buffs { get; } = new();
        public Dictionary<string, ObjectInfo> Upgrades { get; } = new();

        private Map? sourceMap;
        private StringTableReader? stringTable;

        /// <summary>
        /// Load all custom objects from a map
        /// </summary>
        public static ObjectRegistry LoadFromMap(string mapPath)
        {
            Console.WriteLine($"Loading objects from: {mapPath}");

            var registry = new ObjectRegistry();

            try
            {
                registry.sourceMap = Map.Open(mapPath);

                // Load string table for resolving TRIGSTR references
                registry.stringTable = StringTableReader.LoadFromMap(registry.sourceMap, mapPath);
            }
            catch (Exception ex)
            {
                // Try to open with lenient parsing if strict parsing fails
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: Map contains unknown data types. Attempting lenient load...");
                Console.WriteLine($"  Error: {ex.Message}");
                Console.ResetColor();

                // Re-throw for now - we'll need to implement custom parsing if this persists
                throw new Exception($"Failed to load map '{mapPath}'. The map may contain custom object modifications that are not supported by War3Net. Error: {ex.Message}", ex);
            }

            registry.LoadUnits();
            registry.LoadItems();
            registry.LoadAbilities();
            registry.LoadDestructables();
            registry.LoadDoodads();
            registry.LoadBuffs();
            registry.LoadUpgrades();

            Console.WriteLine($"Loaded {registry.GetTotalObjectCount()} custom objects");

            return registry;
        }

        private void LoadUnits()
        {
            if (sourceMap?.UnitObjectData == null) return;

            foreach (var unit in sourceMap.UnitObjectData.NewUnits)
            {
                string code = unit.NewId.ToRawcode();
                string baseCode = unit.OldId.ToRawcode();
                string rawName = GetModificationValue(unit, "unam") ?? code;
                string displayName = stringTable?.Resolve(rawName) ?? rawName;

                Units[code] = new ObjectInfo
                {
                    Code = code,
                    BaseCode = baseCode,
                    Type = ObjectType.Unit,
                    Name = displayName,
                    IsCustom = true,
                    SourceObject = unit
                };
            }

            Console.WriteLine($"  Units: {Units.Count}");
        }

        private void LoadItems()
        {
            if (sourceMap?.ItemObjectData == null) return;

            foreach (var item in sourceMap.ItemObjectData.NewItems)
            {
                string code = item.NewId.ToRawcode();
                string baseCode = item.OldId.ToRawcode();
                string rawName = GetModificationValue(item, "unam") ?? code;
                string displayName = stringTable?.Resolve(rawName) ?? rawName;

                Items[code] = new ObjectInfo
                {
                    Code = code,
                    BaseCode = baseCode,
                    Type = ObjectType.Item,
                    Name = displayName,
                    IsCustom = true,
                    SourceObject = item
                };
            }

            Console.WriteLine($"  Items: {Items.Count}");
        }

        private void LoadAbilities()
        {
            if (sourceMap?.AbilityObjectData == null) return;

            foreach (var ability in sourceMap.AbilityObjectData.NewAbilities)
            {
                string code = ability.NewId.ToRawcode();
                string baseCode = ability.OldId.ToRawcode();
                string rawName = GetModificationValue(ability, "anam") ?? code;
                string displayName = stringTable?.Resolve(rawName) ?? rawName;

                Abilities[code] = new ObjectInfo
                {
                    Code = code,
                    BaseCode = baseCode,
                    Type = ObjectType.Ability,
                    Name = displayName,
                    IsCustom = true,
                    SourceObject = ability
                };
            }

            Console.WriteLine($"  Abilities: {Abilities.Count}");
        }

        private void LoadDestructables()
        {
            if (sourceMap?.DestructableObjectData == null) return;

            foreach (var dest in sourceMap.DestructableObjectData.NewDestructables)
            {
                string code = dest.NewId.ToRawcode();
                string baseCode = dest.OldId.ToRawcode();
                string rawName = GetModificationValue(dest, "bnam") ?? code;
                string displayName = stringTable?.Resolve(rawName) ?? rawName;

                Destructables[code] = new ObjectInfo
                {
                    Code = code,
                    BaseCode = baseCode,
                    Type = ObjectType.Destructable,
                    Name = displayName,
                    IsCustom = true,
                    SourceObject = dest
                };
            }

            Console.WriteLine($"  Destructables: {Destructables.Count}");
        }

        private void LoadDoodads()
        {
            if (sourceMap?.DoodadObjectData == null) return;

            foreach (var doodad in sourceMap.DoodadObjectData.NewDoodads)
            {
                string code = doodad.NewId.ToRawcode();
                string baseCode = doodad.OldId.ToRawcode();
                string rawName = GetModificationValue(doodad, "dnam") ?? code;
                string displayName = stringTable?.Resolve(rawName) ?? rawName;

                Doodads[code] = new ObjectInfo
                {
                    Code = code,
                    BaseCode = baseCode,
                    Type = ObjectType.Doodad,
                    Name = displayName,
                    IsCustom = true,
                    SourceObject = doodad
                };
            }

            Console.WriteLine($"  Doodads: {Doodads.Count}");
        }

        private void LoadBuffs()
        {
            if (sourceMap?.BuffObjectData == null) return;

            foreach (var buff in sourceMap.BuffObjectData.NewBuffs)
            {
                string code = buff.NewId.ToRawcode();
                string baseCode = buff.OldId.ToRawcode();
                string rawName = GetModificationValue(buff, "fnam") ?? code;
                string displayName = stringTable?.Resolve(rawName) ?? rawName;

                Buffs[code] = new ObjectInfo
                {
                    Code = code,
                    BaseCode = baseCode,
                    Type = ObjectType.Buff,
                    Name = displayName,
                    IsCustom = true,
                    SourceObject = buff
                };
            }

            Console.WriteLine($"  Buffs: {Buffs.Count}");
        }

        private void LoadUpgrades()
        {
            if (sourceMap?.UpgradeObjectData == null) return;

            foreach (var upgrade in sourceMap.UpgradeObjectData.NewUpgrades)
            {
                string code = upgrade.NewId.ToRawcode();
                string baseCode = upgrade.OldId.ToRawcode();
                string rawName = GetModificationValue(upgrade, "gnam") ?? code;
                string displayName = stringTable?.Resolve(rawName) ?? rawName;

                Upgrades[code] = new ObjectInfo
                {
                    Code = code,
                    BaseCode = baseCode,
                    Type = ObjectType.Upgrade,
                    Name = displayName,
                    IsCustom = true,
                    SourceObject = upgrade
                };
            }

            Console.WriteLine($"  Upgrades: {Upgrades.Count}");
        }

        /// <summary>
        /// Get a modification value from an object (e.g., name)
        /// </summary>
        private string? GetModificationValue(SimpleObjectModification obj, string modCode)
        {
            try
            {
                int modId = modCode.FromRawcode();
                var mod = obj.Modifications.FirstOrDefault(m => m.Id == modId);
                return mod?.Value?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get a modification value from a level object (abilities, upgrades)
        /// </summary>
        private string? GetModificationValue(War3Net.Build.Object.LevelObjectModification obj, string modCode)
        {
            try
            {
                int modId = modCode.FromRawcode();
                var mod = obj.Modifications.FirstOrDefault(m => m.Id == modId && m.Level == 0);
                return mod?.Value?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get a modification value from a variation object (doodads)
        /// </summary>
        private string? GetModificationValue(War3Net.Build.Object.VariationObjectModification obj, string modCode)
        {
            try
            {
                int modId = modCode.FromRawcode();
                var mod = obj.Modifications.FirstOrDefault(m => m.Id == modId);
                return mod?.Value?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get object by code and type
        /// </summary>
        public ObjectInfo? GetObject(string code, ObjectType type)
        {
            return type switch
            {
                ObjectType.Unit => Units.GetValueOrDefault(code),
                ObjectType.Item => Items.GetValueOrDefault(code),
                ObjectType.Ability => Abilities.GetValueOrDefault(code),
                ObjectType.Destructable => Destructables.GetValueOrDefault(code),
                ObjectType.Doodad => Doodads.GetValueOrDefault(code),
                ObjectType.Buff => Buffs.GetValueOrDefault(code),
                ObjectType.Upgrade => Upgrades.GetValueOrDefault(code),
                _ => null
            };
        }

        /// <summary>
        /// Check if object exists
        /// </summary>
        public bool HasObject(string code, ObjectType type)
        {
            return GetObject(code, type) != null;
        }

        /// <summary>
        /// Get all objects of a specific type
        /// </summary>
        public IEnumerable<ObjectInfo> GetObjectsByType(ObjectType type)
        {
            return type switch
            {
                ObjectType.Unit => Units.Values,
                ObjectType.Item => Items.Values,
                ObjectType.Ability => Abilities.Values,
                ObjectType.Destructable => Destructables.Values,
                ObjectType.Doodad => Doodads.Values,
                ObjectType.Buff => Buffs.Values,
                ObjectType.Upgrade => Upgrades.Values,
                _ => Enumerable.Empty<ObjectInfo>()
            };
        }

        /// <summary>
        /// Get total count of all objects
        /// </summary>
        public int GetTotalObjectCount()
        {
            return Units.Count + Items.Count + Abilities.Count +
                   Destructables.Count + Doodads.Count + Buffs.Count + Upgrades.Count;
        }

        /// <summary>
        /// Get the underlying War3Net Map object (for saving)
        /// </summary>
        public Map? GetMap() => sourceMap;
    }
}
