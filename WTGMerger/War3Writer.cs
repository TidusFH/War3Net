using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using War3Net.Build.Script;
using War3Net.Common.Extensions;

namespace WTGMerger
{
    /// <summary>
    /// Custom WTG writer with enhanced debugging and explicit format control
    /// Replaces War3Net's WriteTo for better transparency and control
    /// </summary>
    public static class War3Writer
    {
        private static bool DebugMode = false;

        public static void SetDebugMode(bool enabled)
        {
            DebugMode = enabled;
        }

        /// <summary>
        /// Writes a null-terminated string (WTG format)
        /// CRITICAL: War3Net's WriteString adds extra bytes - use our own!
        /// </summary>
        private static void WriteWTGString(BinaryWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                writer.Write((byte)0); // Just null terminator
                return;
            }

            // Write UTF-8 bytes ONE AT A TIME (fixes BinaryWriter length prefix bug)
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            foreach (byte b in bytes)
            {
                writer.Write(b);
            }

            // Write null terminator
            writer.Write((byte)0);
        }


        /// <summary>
        /// Writes MapTriggers to a WTG file with full control over format
        /// </summary>
        public static void WriteMapTriggers(string filePath, MapTriggers triggers)
        {
            if (DebugMode)
            {
                Console.WriteLine($"\n[War3Writer] Writing to: {filePath}");
                Console.WriteLine($"[War3Writer] Format: {triggers.FormatVersion}, SubVersion: {triggers.SubVersion?.ToString() ?? "null"}");
                Console.WriteLine($"[War3Writer] Variables: {triggers.Variables.Count}");
                Console.WriteLine($"[War3Writer] Trigger Items: {triggers.TriggerItems.Count}");
            }

            using var fileStream = File.Create(filePath);
            using var writer = new BinaryWriter(fileStream);

            WriteMapTriggers(writer, triggers);

            if (DebugMode)
            {
                Console.WriteLine($"[War3Writer] File size: {fileStream.Length} bytes");
                Console.WriteLine($"[War3Writer] Write complete");
            }
        }

        /// <summary>
        /// Writes MapTriggers to a BinaryWriter (stream overload)
        /// </summary>
        public static void WriteMapTriggers(BinaryWriter writer, MapTriggers triggers)
        {
            if (DebugMode)
            {
                Console.WriteLine($"[War3Writer] Writing to stream");
                Console.WriteLine($"[War3Writer] Format: {triggers.FormatVersion}, SubVersion: {triggers.SubVersion?.ToString() ?? "null"}");
                Console.WriteLine($"[War3Writer] Variables: {triggers.Variables.Count}");
                Console.WriteLine($"[War3Writer] Trigger Items: {triggers.TriggerItems.Count}");
            }

            // Write file signature
            writer.Write(0x21475457); // 'WTG!'

            if (triggers.SubVersion == null)
            {
                // WC3 1.27 format (SubVersion=null)
                WriteFormat127(writer, triggers);
            }
            else
            {
                // WC3 1.31+ format (SubVersion=v4 or v7)
                WriteFormatNew(writer, triggers);
            }
        }

        /// <summary>
        /// Writes WTG in WC3 1.27 format (SubVersion=null)
        /// </summary>
        private static void WriteFormat127(BinaryWriter writer, MapTriggers triggers)
        {
            if (DebugMode)
            {
                Console.WriteLine("[War3Writer] Using 1.27 format (SubVersion=null)");
            }

            // Format version (7)
            writer.Write((int)triggers.FormatVersion);

            // Write categories
            var categories = triggers.TriggerItems
                .Where(item => item is TriggerCategoryDefinition && item.Type != TriggerItemType.RootCategory)
                .Cast<TriggerCategoryDefinition>()
                .ToList();

            writer.Write(categories.Count);

            if (DebugMode)
            {
                Console.WriteLine($"[War3Writer] Writing {categories.Count} categories");
            }

            foreach (var category in categories)
            {
                WriteCategoryDefinition127(writer, category, triggers.FormatVersion);
            }

            // Game version
            writer.Write(triggers.GameVersion);

            // Write variables
            writer.Write(triggers.Variables.Count);

            if (DebugMode)
            {
                Console.WriteLine($"[War3Writer] Writing {triggers.Variables.Count} variables");
            }

            foreach (var variable in triggers.Variables)
            {
                WriteVariableDefinition127(writer, variable, triggers.FormatVersion);
            }

            // Write triggers
            var triggerList = triggers.TriggerItems
                .Where(item => item is TriggerDefinition)
                .Cast<TriggerDefinition>()
                .ToList();

            writer.Write(triggerList.Count);

            if (DebugMode)
            {
                Console.WriteLine($"[War3Writer] Writing {triggerList.Count} triggers");
            }

            foreach (var trigger in triggerList)
            {
                WriteTriggerDefinition127(writer, trigger, triggers.FormatVersion);
            }
        }

        /// <summary>
        /// Writes category in 1.27 format (no ParentId)
        /// </summary>
        private static void WriteCategoryDefinition127(BinaryWriter writer, TriggerCategoryDefinition category, MapTriggersFormatVersion formatVersion)
        {
            writer.Write(category.Id);
            WriteWTGString(writer, category.Name);

            if (formatVersion >= MapTriggersFormatVersion.v7)
            {
                writer.WriteBool(category.IsComment);
            }

            // NOTE: ParentId is NOT written in 1.27 format
            if (DebugMode)
            {
                Console.WriteLine($"[War3Writer]   Category: '{category.Name}' (ID={category.Id}, ParentId={category.ParentId} - not written)");
            }
        }

        /// <summary>
        /// Writes variable in 1.27 format (no Id)
        /// </summary>
        private static void WriteVariableDefinition127(BinaryWriter writer, VariableDefinition variable, MapTriggersFormatVersion formatVersion)
        {
            WriteWTGString(writer, variable.Name);
            WriteWTGString(writer, variable.Type);
            writer.Write(variable.Unk);
            writer.WriteBool(variable.IsArray);

            if (formatVersion >= MapTriggersFormatVersion.v7)
            {
                writer.Write(variable.ArraySize);
            }

            writer.WriteBool(variable.IsInitialized);
            WriteWTGString(writer, variable.InitialValue);

            // NOTE: Id and ParentId are NOT written in 1.27 format
            if (DebugMode)
            {
                Console.WriteLine($"[War3Writer]   Variable: '{variable.Name}' ({variable.Type}, ID={variable.Id} - not written)");
            }
        }

        /// <summary>
        /// Writes trigger in 1.27 format (ParentId is written!)
        /// </summary>
        private static void WriteTriggerDefinition127(BinaryWriter writer, TriggerDefinition trigger, MapTriggersFormatVersion formatVersion)
        {
            WriteWTGString(writer, trigger.Name);
            WriteWTGString(writer, trigger.Description);

            if (formatVersion >= MapTriggersFormatVersion.v7)
            {
                writer.WriteBool(trigger.IsComment);
            }

            // NOTE: Trigger Id is NOT written in 1.27 format

            writer.WriteBool(trigger.IsEnabled);
            writer.WriteBool(trigger.IsCustomTextTrigger);
            writer.WriteBool(!trigger.IsInitiallyOn);
            writer.WriteBool(trigger.RunOnMapInit);

            // CRITICAL: ParentId IS written in 1.27 format for triggers!
            writer.Write(trigger.ParentId);

            if (DebugMode)
            {
                Console.WriteLine($"[War3Writer]   Trigger: '{trigger.Name}' (ID={trigger.Id} - not written, ParentId={trigger.ParentId} - written)");
            }

            // Write functions
            writer.Write(trigger.Functions.Count);
            foreach (var function in trigger.Functions)
            {
                WriteTriggerFunction(writer, function, formatVersion, null, isChildFunction: false);
            }
        }

        /// <summary>
        /// Writes WTG in WC3 1.31+ format (SubVersion=v4 or v7)
        /// </summary>
        private static void WriteFormatNew(BinaryWriter writer, MapTriggers triggers)
        {
            if (DebugMode)
            {
                Console.WriteLine($"[War3Writer] Using 1.31+ format (SubVersion={triggers.SubVersion})");
            }

            // SubVersion first
            writer.Write((int)triggers.SubVersion!);
            writer.Write((int)triggers.FormatVersion);

            // Write trigger item counts
            foreach (TriggerItemType triggerItemType in Enum.GetValues(typeof(TriggerItemType)))
            {
                int count = triggers.TriggerItemCounts.TryGetValue(triggerItemType, out var c)
                    ? c
                    : triggers.TriggerItems.Count(item => item.Type == triggerItemType);

                writer.Write(count);

                // Write deleted items (not implemented in this version)
                writer.Write(0); // deletedCount
            }

            // Game version
            writer.Write(triggers.GameVersion);

            // Write variables
            writer.Write(triggers.Variables.Count);
            foreach (var variable in triggers.Variables)
            {
                WriteVariableDefinitionNew(writer, variable, triggers.FormatVersion);
            }

            // Write trigger items
            var items = triggers.TriggerItems.Where(item => item is not DeletedTriggerItem).ToList();
            writer.Write(items.Count);

            foreach (var item in items)
            {
                writer.Write((int)item.Type);

                if (item is TriggerCategoryDefinition category)
                {
                    WriteCategoryDefinitionNew(writer, category, triggers.FormatVersion);
                }
                else if (item is TriggerDefinition trigger)
                {
                    WriteTriggerDefinitionNew(writer, trigger, triggers.FormatVersion);
                }
            }
        }

        /// <summary>
        /// Writes category in 1.31+ format (with ParentId)
        /// </summary>
        private static void WriteCategoryDefinitionNew(BinaryWriter writer, TriggerCategoryDefinition category, MapTriggersFormatVersion formatVersion)
        {
            writer.Write(category.Id);
            WriteWTGString(writer, category.Name);

            if (formatVersion >= MapTriggersFormatVersion.v7)
            {
                writer.WriteBool(category.IsComment);
            }

            writer.WriteBool(category.IsExpanded);
            writer.Write(category.ParentId); // Written in new format

            if (DebugMode)
            {
                Console.WriteLine($"[War3Writer]   Category: '{category.Name}' (ID={category.Id}, ParentId={category.ParentId})");
            }
        }

        /// <summary>
        /// Writes variable in 1.31+ format (with Id)
        /// </summary>
        private static void WriteVariableDefinitionNew(BinaryWriter writer, VariableDefinition variable, MapTriggersFormatVersion formatVersion)
        {
            WriteWTGString(writer, variable.Name);
            WriteWTGString(writer, variable.Type);
            writer.Write(variable.Unk);
            writer.WriteBool(variable.IsArray);

            if (formatVersion >= MapTriggersFormatVersion.v7)
            {
                writer.Write(variable.ArraySize);
            }

            writer.WriteBool(variable.IsInitialized);
            WriteWTGString(writer, variable.InitialValue);

            writer.Write(variable.Id);
            writer.Write(variable.ParentId);

            if (DebugMode)
            {
                Console.WriteLine($"[War3Writer]   Variable: '{variable.Name}' ({variable.Type}, ID={variable.Id})");
            }
        }

        /// <summary>
        /// Writes trigger in 1.31+ format (with Id)
        /// </summary>
        private static void WriteTriggerDefinitionNew(BinaryWriter writer, TriggerDefinition trigger, MapTriggersFormatVersion formatVersion)
        {
            WriteWTGString(writer, trigger.Name);
            WriteWTGString(writer, trigger.Description);

            if (formatVersion >= MapTriggersFormatVersion.v7)
            {
                writer.WriteBool(trigger.IsComment);
            }

            writer.Write(trigger.Id); // Written in new format

            writer.WriteBool(trigger.IsEnabled);
            writer.WriteBool(trigger.IsCustomTextTrigger);
            writer.WriteBool(!trigger.IsInitiallyOn);
            writer.WriteBool(trigger.RunOnMapInit);
            writer.Write(trigger.ParentId);

            if (DebugMode)
            {
                Console.WriteLine($"[War3Writer]   Trigger: '{trigger.Name}' (ID={trigger.Id}, ParentId={trigger.ParentId})");
            }

            // Write functions
            writer.Write(trigger.Functions.Count);
            foreach (var function in trigger.Functions)
            {
                WriteTriggerFunction(writer, function, formatVersion, null, isChildFunction: false);
            }
        }

        /// <summary>
        /// Writes trigger function (events, conditions, actions)
        /// </summary>
        private static void WriteTriggerFunction(BinaryWriter writer, TriggerFunction function, MapTriggersFormatVersion formatVersion, MapTriggersSubVersion? subVersion, bool isChildFunction = false)
        {
            writer.Write((int)function.Type);

            // BUGFIX #2: Write Branch field for child functions
            if (isChildFunction && function.Branch.HasValue)
            {
                writer.Write(function.Branch.Value);
            }

            // BUGFIX #3: Name should ALWAYS be written (not conditional on format version)
            WriteWTGString(writer, function.Name ?? string.Empty);

            writer.WriteBool(function.IsEnabled);

            // BUGFIX #4: Do NOT write parameter count - War3Net uses TriggerData to determine count
            foreach (var param in function.Parameters)
            {
                WriteTriggerFunctionParameter(writer, param, formatVersion, subVersion);
            }

            // BUGFIX #5: Check FORMAT VERSION, not function type
            if (formatVersion >= MapTriggersFormatVersion.v7)
            {
                writer.Write(function.ChildFunctions.Count);
                foreach (var child in function.ChildFunctions)
                {
                    // BUGFIX #6: Pass isChildFunction=true for recursive calls
                    WriteTriggerFunction(writer, child, formatVersion, subVersion, isChildFunction: true);
                }
            }
        }

        /// <summary>
        /// Writes trigger function parameter
        /// </summary>
        private static void WriteTriggerFunctionParameter(BinaryWriter writer, TriggerFunctionParameter param, MapTriggersFormatVersion formatVersion, MapTriggersSubVersion? subVersion)
        {
            writer.Write((int)param.Type);
            WriteWTGString(writer, param.Value ?? string.Empty);

            // BUGFIX #1: Always write bool flags before Function and ArrayIndexer
            // These are NOT mutually exclusive - both flags must be written separately

            // Write Function flag and data
            writer.WriteBool(param.Function is not null);
            if (param.Function is not null)
            {
                WriteTriggerFunction(writer, param.Function, formatVersion, subVersion, isChildFunction: false);
            }

            // Write ArrayIndexer flag and data
            writer.WriteBool(param.ArrayIndexer is not null);
            if (param.ArrayIndexer is not null)
            {
                WriteTriggerFunctionParameter(writer, param.ArrayIndexer, formatVersion, subVersion);
            }
        }
    }
}
