using System;
using System.IO;
using System.Linq;
using System.Text;
using War3Net.Build.Script;

namespace WTGWriter
{
    /// <summary>
    /// Custom WTG binary writer that outputs correct format (War3Net's writer is broken).
    /// Based on analysis of working WTG files from the diagnostic tool.
    /// </summary>
    public static class CustomWTGWriter
    {
        /// <summary>
        /// Writes MapTriggers to a binary stream in correct WTG format.
        /// </summary>
        public static void Write(Stream stream, MapTriggers triggers)
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            // === HEADER ===
            // File ID: "WTG!" (4 bytes)
            writer.Write(Encoding.ASCII.GetBytes("WTG!"));

            // Format Version (int32)
            int formatVersion = (int)triggers.FormatVersion;
            writer.Write(formatVersion);

            Console.WriteLine($"[CustomWriter] Header written:");
            Console.WriteLine($"  Format Version: {formatVersion}");

            // Get counts
            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var triggerDefs = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();

            Console.WriteLine($"[CustomWriter] Counts:");
            Console.WriteLine($"  Categories: {categories.Count}");
            Console.WriteLine($"  Triggers: {triggerDefs.Count}");
            Console.WriteLine($"  Variables: {triggers.Variables.Count}");

            // === CATEGORY COUNT (int32) ===
            // At offset 0x08 according to format spec
            writer.Write(categories.Count);

            // === SUBVERSION (int32) ===
            // At offset 0x0C according to format spec
            int subVersion = triggers.SubVersion.HasValue ? (int)triggers.SubVersion.Value : 0;
            writer.Write(subVersion);
            Console.WriteLine($"  SubVersion: {subVersion}");

            // === CATEGORY DEFINITIONS ===
            foreach (var category in categories)
            {
                // Category ID (int32) - FIRST!
                writer.Write(category.Id);

                // Category name (null-terminated string)
                WriteNullTerminatedString(writer, category.Name);

                // IsComment (bool) - only if formatVersion >= v7
                if (formatVersion >= 7)
                {
                    writer.Write(category.IsComment ? 1 : 0);
                }

                // IsExpanded (bool) - only if subVersion is not null
                if (subVersion != 0)
                {
                    writer.Write(category.IsExpanded ? 1 : 0);
                    writer.Write(category.ParentId);
                }
            }

            // === VARIABLE COUNT (int32) ===
            writer.Write(triggers.Variables.Count);
            Console.WriteLine($"[CustomWriter] Writing {triggers.Variables.Count} variables...");

            // === VARIABLE DEFINITIONS ===
            int varIndex = 0;
            foreach (var variable in triggers.Variables)
            {
                if (varIndex < 3) // Log first few for debugging
                {
                    Console.WriteLine($"[CustomWriter]   Var {varIndex}: {variable.Name} ({variable.Type})");
                }

                // Variable name (null-terminated)
                WriteNullTerminatedString(writer, variable.Name);

                // Type (null-terminated)
                WriteNullTerminatedString(writer, variable.Type);

                // Unk field (int32, usually 1)
                writer.Write(variable.Unk);

                // IsArray (int32)
                writer.Write(variable.IsArray ? 1 : 0);

                // ArraySize (int32)
                writer.Write(variable.ArraySize);

                // IsInitialized (int32)
                writer.Write(variable.IsInitialized ? 1 : 0);

                // InitialValue (null-terminated string)
                WriteNullTerminatedString(writer, variable.InitialValue);

                // Variable ID (int32)
                writer.Write(variable.Id);

                // Parent ID (int32)
                writer.Write(variable.ParentId);

                varIndex++;
            }

            // === TRIGGER COUNT (int32) ===
            writer.Write(triggerDefs.Count);
            Console.WriteLine($"[CustomWriter] Writing {triggerDefs.Count} triggers...");

            // === TRIGGER DEFINITIONS ===
            foreach (var trigger in triggerDefs)
            {
                // Name (null-terminated)
                WriteNullTerminatedString(writer, trigger.Name);

                // Description (null-terminated)
                WriteNullTerminatedString(writer, trigger.Description ?? string.Empty);

                // IsComment (bool) - only if formatVersion >= v7
                if (formatVersion >= 7)
                {
                    writer.Write(trigger.IsComment ? 1 : 0);
                }

                // ID (int32) - only if subVersion is not null
                if (subVersion != 0)
                {
                    writer.Write(trigger.Id);
                }

                // IsEnabled (bool)
                writer.Write(trigger.IsEnabled ? 1 : 0);

                // IsCustomTextTrigger (bool)
                writer.Write(trigger.IsCustomTextTrigger ? 1 : 0);

                // IsInitiallyOn (bool) - NOTE: NEGATED!
                writer.Write(trigger.IsInitiallyOn ? 0 : 1);

                // RunOnMapInit (bool)
                writer.Write(trigger.RunOnMapInit ? 1 : 0);

                // ParentId (int32)
                writer.Write(trigger.ParentId);

                // Functions count
                writer.Write(trigger.Functions.Count);

                // Write all functions (not separated by type)
                foreach (var function in trigger.Functions)
                {
                    WriteTriggerFunction(writer, function, formatVersion, subVersion);
                }
            }

            writer.Flush();
            Console.WriteLine($"[CustomWriter] Write complete - {stream.Position} bytes");
        }

        private static void WriteNullTerminatedString(BinaryWriter writer, string value)
        {
            // Write each character as 2 bytes (char in C# is UTF-16)
            // This matches War3Net's WriteString behavior
            if (!string.IsNullOrEmpty(value))
            {
                foreach (var c in value)
                {
                    writer.Write(c);  // Writes 2 bytes per character
                }
            }

            // Write null terminator (2 bytes for char.MinValue)
            writer.Write(char.MinValue);
        }

        private static void WriteTriggerFunction(BinaryWriter writer, TriggerFunction function, int formatVersion, int subVersion)
        {
            // Type (int32)
            writer.Write((int)function.Type);

            // Branch (int32) - only for child functions
            if (function.Branch.HasValue)
            {
                writer.Write(function.Branch.Value);
            }

            // Name (null-terminated string)
            WriteNullTerminatedString(writer, function.Name ?? string.Empty);

            // IsEnabled (bool as int32)
            writer.Write(function.IsEnabled ? 1 : 0);

            // Parameters (NO count written - determined from TriggerData)
            foreach (var param in function.Parameters)
            {
                WriteTriggerFunctionParameter(writer, param, formatVersion, subVersion);
            }

            // ChildFunctions (only if formatVersion >= v7)
            if (formatVersion >= 7)
            {
                writer.Write(function.ChildFunctions.Count);
                foreach (var childFunction in function.ChildFunctions)
                {
                    WriteTriggerFunction(writer, childFunction, formatVersion, subVersion);
                }
            }
        }

        private static void WriteTriggerFunctionParameter(BinaryWriter writer, TriggerFunctionParameter param, int formatVersion, int subVersion)
        {
            // Type (int32)
            writer.Write((int)param.Type);

            // Value (null-terminated string)
            WriteNullTerminatedString(writer, param.Value ?? string.Empty);

            // Has function (bool as int32)
            writer.Write(param.Function != null ? 1 : 0);
            if (param.Function != null)
            {
                WriteTriggerFunction(writer, param.Function, formatVersion, subVersion);
            }

            // Has array indexer (bool as int32)
            writer.Write(param.ArrayIndexer != null ? 1 : 0);
            if (param.ArrayIndexer != null)
            {
                WriteTriggerFunctionParameter(writer, param.ArrayIndexer, formatVersion, subVersion);
            }
        }
    }
}
