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

        /// <summary>
        /// Load all custom objects from a map
        /// </summary>
        public static ObjectRegistry LoadFromMap(string mapPath)
        {
            Console.WriteLine($"Loading objects from: {mapPath}");

            var registry = new ObjectRegistry();
            registry.sourceMap = Map.Open(mapPath);

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

                Units[code] = new ObjectInfo
                {
                    Code = code,
                    BaseCode = baseCode,
                    Type = ObjectType.Unit,
                    Name = GetModificationValue(unit, "unam") ?? code,
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

                Items[code] = new ObjectInfo
                {
                    Code = code,
                    BaseCode = baseCode,
                    Type = ObjectType.Item,
                    Name = GetModificationValue(item, "unam") ?? code,
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

                Abilities[code] = new ObjectInfo
                {
                    Code = code,
                    BaseCode = baseCode,
                    Type = ObjectType.Ability,
                    Name = GetModificationValue(ability, "anam") ?? code,
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

                Destructables[code] = new ObjectInfo
                {
                    Code = code,
                    BaseCode = baseCode,
                    Type = ObjectType.Destructable,
                    Name = GetModificationValue(dest, "bnam") ?? code,
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

                Doodads[code] = new ObjectInfo
                {
                    Code = code,
                    BaseCode = baseCode,
                    Type = ObjectType.Doodad,
                    Name = GetModificationValue(doodad, "dnam") ?? code,
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

                Buffs[code] = new ObjectInfo
                {
                    Code = code,
                    BaseCode = baseCode,
                    Type = ObjectType.Buff,
                    Name = GetModificationValue(buff, "fnam") ?? code,
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

                Upgrades[code] = new ObjectInfo
                {
                    Code = code,
                    BaseCode = baseCode,
                    Type = ObjectType.Upgrade,
                    Name = GetModificationValue(upgrade, "gnam") ?? code,
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
