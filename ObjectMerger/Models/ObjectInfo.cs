using War3Net.Build.Object;

namespace ObjectMerger.Models
{
    /// <summary>
    /// Type of WC3 object
    /// </summary>
    public enum ObjectType
    {
        Unit,
        Item,
        Ability,
        Destructable,
        Doodad,
        Buff,
        Upgrade
    }

    /// <summary>
    /// Information about a custom object
    /// </summary>
    public class ObjectInfo
    {
        public required string Code { get; set; }           // e.g., "h001"
        public required string BaseCode { get; set; }       // e.g., "hfoo"
        public required ObjectType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsCustom { get; set; }                  // vs Blizzard standard
        public List<string> Dependencies { get; set; } = new();

        // The actual War3Net object (for copying)
        public object? SourceObject { get; set; }

        public override string ToString() => $"{Code} - {Name} (base: {BaseCode})";
    }

    /// <summary>
    /// ID conflict information
    /// </summary>
    public class ObjectConflict
    {
        public required string ObjectCode { get; set; }
        public required ObjectType Type { get; set; }
        public required ObjectInfo SourceObject { get; set; }
        public required ObjectInfo TargetObject { get; set; }
        public ConflictResolution Resolution { get; set; } = ConflictResolution.Skip;
        public string? NewCode { get; set; }  // If renamed
    }

    /// <summary>
    /// How to handle ID conflicts
    /// </summary>
    public enum ConflictResolution
    {
        Skip,       // Don't copy this object
        Overwrite,  // Replace target's version
        Rename      // Find new ID and remap references
    }

    /// <summary>
    /// Options for merge operation
    /// </summary>
    public class MergeOptions
    {
        public List<string> SelectedObjectCodes { get; set; } = new();
        public bool AutoResolveDependencies { get; set; } = true;
        public ConflictResolution DefaultConflictResolution { get; set; } = ConflictResolution.Skip;
        public bool InteractiveMode { get; set; } = true;
        public bool VerboseLogging { get; set; } = true;
    }

    /// <summary>
    /// Result of merge operation
    /// </summary>
    public class MergeResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int ObjectsCopied { get; set; }
        public int ObjectsSkipped { get; set; }
        public int ConflictsResolved { get; set; }
        public List<string> CopiedObjects { get; set; } = new();
        public List<string> SkippedObjects { get; set; } = new();
        public List<ObjectConflict> ResolvedConflicts { get; set; } = new();
    }
}
