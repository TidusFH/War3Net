using System;
using System.Collections.Generic;
using System.Linq;
using War3Net.Build;
using War3Net.Build.Object;
using ObjectMerger.Models;

namespace ObjectMerger.Services
{
    /// <summary>
    /// Copies objects from source map to target map
    /// </summary>
    public class ObjectCopier
    {
        private readonly ObjectRegistry sourceRegistry;
        private readonly ObjectRegistry targetRegistry;

        public ObjectCopier(ObjectRegistry sourceRegistry, ObjectRegistry targetRegistry)
        {
            this.sourceRegistry = sourceRegistry;
            this.targetRegistry = targetRegistry;
        }

        /// <summary>
        /// Copy objects to target map, respecting conflict resolutions
        /// </summary>
        public MergeResult CopyObjects(List<ObjectInfo> objectsToCopy, List<ObjectConflict> conflicts)
        {
            var result = new MergeResult { Success = true };

            var sourceMap = sourceRegistry.GetMap();
            var targetMap = targetRegistry.GetMap();

            if (sourceMap == null || targetMap == null)
            {
                result.Success = false;
                result.ErrorMessage = "Source or target map not loaded";
                return result;
            }

            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                   COPYING OBJECTS                        ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

            foreach (var obj in objectsToCopy)
            {
                // Check if this object has a conflict
                var conflict = conflicts.FirstOrDefault(c => c.ObjectCode == obj.Code);

                if (conflict != null)
                {
                    if (conflict.Resolution == ConflictResolution.Skip)
                    {
                        Console.WriteLine($"⊘ Skipping {obj.Code} - {obj.Name} (conflict)");
                        result.ObjectsSkipped++;
                        result.SkippedObjects.Add(obj.Code);
                        continue;
                    }
                    else if (conflict.Resolution == ConflictResolution.Overwrite)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"⚠ Overwriting {obj.Code} - {obj.Name}");
                        Console.ResetColor();
                        RemoveObject(targetMap, obj);
                    }
                    else if (conflict.Resolution == ConflictResolution.Rename)
                    {
                        // TODO Phase 3: Implement renaming
                        Console.WriteLine($"⊘ Skipping {obj.Code} - {obj.Name} (rename not implemented)");
                        result.ObjectsSkipped++;
                        result.SkippedObjects.Add(obj.Code);
                        continue;
                    }
                }

                // Copy the object
                bool copied = CopyObject(sourceMap, targetMap, obj);

                if (copied)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Copied {obj.Code} - {obj.Name}");
                    Console.ResetColor();
                    result.ObjectsCopied++;
                    result.CopiedObjects.Add(obj.Code);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ Failed to copy {obj.Code} - {obj.Name}");
                    Console.ResetColor();
                    result.ObjectsSkipped++;
                    result.SkippedObjects.Add(obj.Code);
                }
            }

            return result;
        }

        /// <summary>
        /// Copy a single object from source to target
        /// </summary>
        private bool CopyObject(Map sourceMap, Map targetMap, ObjectInfo obj)
        {
            try
            {
                switch (obj.Type)
                {
                    case ObjectType.Unit:
                        return CopyUnit(sourceMap, targetMap, obj);
                    case ObjectType.Item:
                        return CopyItem(sourceMap, targetMap, obj);
                    case ObjectType.Ability:
                        return CopyAbility(sourceMap, targetMap, obj);
                    case ObjectType.Destructable:
                        return CopyDestructable(sourceMap, targetMap, obj);
                    case ObjectType.Doodad:
                        return CopyDoodad(sourceMap, targetMap, obj);
                    case ObjectType.Buff:
                        return CopyBuff(sourceMap, targetMap, obj);
                    case ObjectType.Upgrade:
                        return CopyUpgrade(sourceMap, targetMap, obj);
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
                return false;
            }
        }

        private bool CopyUnit(Map sourceMap, Map targetMap, ObjectInfo obj)
        {
            var sourceUnit = obj.SourceObject as SimpleObjectModification;
            if (sourceUnit == null) return false;

            // Ensure target has UnitObjectData
            if (targetMap.UnitObjectData == null)
            {
                targetMap.UnitObjectData = new UnitObjectData(sourceMap.UnitObjectData!.FormatVersion);
            }

            // Clone the unit
            var clone = CloneSimpleObject(sourceUnit);
            targetMap.UnitObjectData.NewUnits.Add(clone);

            return true;
        }

        private bool CopyItem(Map sourceMap, Map targetMap, ObjectInfo obj)
        {
            var sourceItem = obj.SourceObject as SimpleObjectModification;
            if (sourceItem == null) return false;

            if (targetMap.ItemObjectData == null)
            {
                targetMap.ItemObjectData = new ItemObjectData(sourceMap.ItemObjectData!.FormatVersion);
            }

            var clone = CloneSimpleObject(sourceItem);
            targetMap.ItemObjectData.NewItems.Add(clone);

            return true;
        }

        private bool CopyAbility(Map sourceMap, Map targetMap, ObjectInfo obj)
        {
            var sourceAbility = obj.SourceObject as SimpleObjectModification;
            if (sourceAbility == null) return false;

            if (targetMap.AbilityObjectData == null)
            {
                targetMap.AbilityObjectData = new AbilityObjectData(sourceMap.AbilityObjectData!.FormatVersion);
            }

            var clone = CloneSimpleObject(sourceAbility);
            targetMap.AbilityObjectData.NewAbilities.Add(clone);

            return true;
        }

        private bool CopyDestructable(Map sourceMap, Map targetMap, ObjectInfo obj)
        {
            var sourceDest = obj.SourceObject as SimpleObjectModification;
            if (sourceDest == null) return false;

            if (targetMap.DestructableObjectData == null)
            {
                targetMap.DestructableObjectData = new DestructableObjectData(sourceMap.DestructableObjectData!.FormatVersion);
            }

            var clone = CloneSimpleObject(sourceDest);
            targetMap.DestructableObjectData.NewDestructables.Add(clone);

            return true;
        }

        private bool CopyDoodad(Map sourceMap, Map targetMap, ObjectInfo obj)
        {
            var sourceDoodad = obj.SourceObject as SimpleObjectModification;
            if (sourceDoodad == null) return false;

            if (targetMap.DoodadObjectData == null)
            {
                targetMap.DoodadObjectData = new DoodadObjectData(sourceMap.DoodadObjectData!.FormatVersion);
            }

            var clone = CloneSimpleObject(sourceDoodad);
            targetMap.DoodadObjectData.NewDoodads.Add(clone);

            return true;
        }

        private bool CopyBuff(Map sourceMap, Map targetMap, ObjectInfo obj)
        {
            var sourceBuff = obj.SourceObject as SimpleObjectModification;
            if (sourceBuff == null) return false;

            if (targetMap.BuffObjectData == null)
            {
                targetMap.BuffObjectData = new BuffObjectData(sourceMap.BuffObjectData!.FormatVersion);
            }

            var clone = CloneSimpleObject(sourceBuff);
            targetMap.BuffObjectData.NewBuffs.Add(clone);

            return true;
        }

        private bool CopyUpgrade(Map sourceMap, Map targetMap, ObjectInfo obj)
        {
            var sourceUpgrade = obj.SourceObject as SimpleObjectModification;
            if (sourceUpgrade == null) return false;

            if (targetMap.UpgradeObjectData == null)
            {
                targetMap.UpgradeObjectData = new UpgradeObjectData(sourceMap.UpgradeObjectData!.FormatVersion);
            }

            var clone = CloneSimpleObject(sourceUpgrade);
            targetMap.UpgradeObjectData.NewUpgrades.Add(clone);

            return true;
        }

        /// <summary>
        /// Clone a SimpleObjectModification (deep copy)
        /// </summary>
        private SimpleObjectModification CloneSimpleObject(SimpleObjectModification source)
        {
            var clone = new SimpleObjectModification
            {
                OldId = source.OldId,
                NewId = source.NewId
            };

            // Clone modifications
            foreach (var mod in source.Modifications)
            {
                clone.Modifications.Add(new SimpleObjectDataModification
                {
                    Id = mod.Id,
                    Type = mod.Type,
                    Value = mod.Value,
                    End = mod.End
                });
            }

            return clone;
        }

        /// <summary>
        /// Remove an object from target map (for overwrite)
        /// </summary>
        private void RemoveObject(Map targetMap, ObjectInfo obj)
        {
            switch (obj.Type)
            {
                case ObjectType.Unit:
                    targetMap.UnitObjectData?.NewUnits.RemoveAll(u => u.NewId.ToRawcode() == obj.Code);
                    break;
                case ObjectType.Item:
                    targetMap.ItemObjectData?.NewItems.RemoveAll(i => i.NewId.ToRawcode() == obj.Code);
                    break;
                case ObjectType.Ability:
                    targetMap.AbilityObjectData?.NewAbilities.RemoveAll(a => a.NewId.ToRawcode() == obj.Code);
                    break;
                case ObjectType.Destructable:
                    targetMap.DestructableObjectData?.NewDestructables.RemoveAll(d => d.NewId.ToRawcode() == obj.Code);
                    break;
                case ObjectType.Doodad:
                    targetMap.DoodadObjectData?.NewDoodads.RemoveAll(d => d.NewId.ToRawcode() == obj.Code);
                    break;
                case ObjectType.Buff:
                    targetMap.BuffObjectData?.NewBuffs.RemoveAll(b => b.NewId.ToRawcode() == obj.Code);
                    break;
                case ObjectType.Upgrade:
                    targetMap.UpgradeObjectData?.NewUpgrades.RemoveAll(u => u.NewId.ToRawcode() == obj.Code);
                    break;
            }
        }
    }
}
