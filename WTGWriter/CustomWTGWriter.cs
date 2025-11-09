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
            // From diagnostic: TARGET shows 07 00 00 00 (format 7)
            int formatVersion = (int)triggers.FormatVersion;
            writer.Write(formatVersion);

            // Get counts
            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var triggerDefs = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();

            Console.WriteLine($"[CustomWriter] Writing format {formatVersion}");
            Console.WriteLine($"[CustomWriter] Categories: {categories.Count}");
            Console.WriteLine($"[CustomWriter] Triggers: {triggerDefs.Count}");
            Console.WriteLine($"[CustomWriter] Variables: {triggers.Variables.Count}");

            // === CATEGORY COUNT (int32) ===
            // From diagnostic: TARGET shows 20 00 00 00 at offset 0x08 (32 categories)
            writer.Write(categories.Count);

            // === SUBVERSION (int32) ===
            // From diagnostic: TARGET shows 00 00 00 00 at offset 0x0C
            // SubVersion appears to be written as int32, with null = 0
            int subVersion = triggers.SubVersion.HasValue ? (int)triggers.SubVersion.Value : 0;
            writer.Write(subVersion);

            // === CATEGORY DEFINITIONS ===
            foreach (var category in categories)
            {
                // From hex dump: category name is null-terminated string
                WriteNullTerminatedString(writer, category.Name);

                // Appears to be followed by padding/flags (need to analyze more)
                writer.Write((int)0); // Unknown field 1
                writer.Write((int)0); // Unknown field 2

                // Category ID
                writer.Write(category.Id);

                // Parent ID (-1 for root, or ID of parent category)
                writer.Write(category.ParentId);
            }

            // === VARIABLE COUNT (int32) ===
            writer.Write(triggers.Variables.Count);

            // === VARIABLE DEFINITIONS ===
            foreach (var variable in triggers.Variables)
            {
                // Variable name (null-terminated)
                WriteNullTerminatedString(writer, variable.Name);

                // Type (null-terminated)
                WriteNullTerminatedString(writer, variable.Type);

                // Unknown field (appears to be 01 00 00 00 in hex dumps)
                writer.Write((int)1);

                // IsArray (int32)
                writer.Write(variable.IsArray ? 1 : 0);

                // ArraySize (int32)
                writer.Write(variable.ArraySize);

                // IsInitialized (int32)
                writer.Write(variable.IsInitialized ? 1 : 0);

                // InitialValue (null-terminated string)
                WriteNullTerminatedString(writer, variable.InitialValue);

                // ID
                writer.Write(variable.Id);

                // ParentId
                writer.Write(variable.ParentId);
            }

            // === TRIGGER COUNT (int32) ===
            writer.Write(triggerDefs.Count);

            // === TRIGGER DEFINITIONS ===
            foreach (var trigger in triggerDefs)
            {
                // Trigger name (null-terminated)
                WriteNullTerminatedString(writer, trigger.Name);

                // Description (null-terminated)
                WriteNullTerminatedString(writer, trigger.Description ?? string.Empty);

                // IsEnabled (int32)
                writer.Write(trigger.IsEnabled ? 1 : 0);

                // IsCustomScript (int32)
                writer.Write(trigger.IsCustomTextTrigger ? 1 : 0);

                // IsInitiallyOn (int32)
                writer.Write(trigger.IsInitiallyOn ? 1 : 0);

                // RunOnMapInit (int32)
                writer.Write(trigger.RunOnMapInit ? 1 : 0);

                // ID
                writer.Write(trigger.Id);

                // Category ID (ParentId)
                writer.Write(trigger.ParentId);

                // === EVENTS ===
                writer.Write(trigger.Events.Count);
                foreach (var evt in trigger.Events)
                {
                    WriteTriggerFunction(writer, evt);
                }

                // === CONDITIONS ===
                writer.Write(trigger.Conditions.Count);
                foreach (var condition in trigger.Conditions)
                {
                    WriteTriggerFunction(writer, condition);
                }

                // === ACTIONS ===
                writer.Write(trigger.Actions.Count);
                foreach (var action in trigger.Actions)
                {
                    WriteTriggerFunction(writer, action);
                }
            }

            writer.Flush();
            Console.WriteLine($"[CustomWriter] Write complete - {stream.Position} bytes");
        }

        private static void WriteNullTerminatedString(BinaryWriter writer, string value)
        {
            // Write string bytes
            if (!string.IsNullOrEmpty(value))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                writer.Write(bytes);
            }

            // Write null terminator
            writer.Write((byte)0);
        }

        private static void WriteTriggerFunction(BinaryWriter writer, TriggerFunction function)
        {
            // Function type (int32)
            writer.Write((int)function.Type);

            // Function name (null-terminated)
            WriteNullTerminatedString(writer, function.Name ?? string.Empty);

            // IsEnabled (int32)
            writer.Write(function.IsEnabled ? 1 : 0);

            // Parameters count
            writer.Write(function.Parameters.Count);

            // Parameters
            foreach (var param in function.Parameters)
            {
                WriteTriggerFunctionParameter(writer, param);
            }
        }

        private static void WriteTriggerFunctionParameter(BinaryWriter writer, TriggerFunctionParameter param)
        {
            // Parameter type (int32)
            writer.Write((int)param.Type);

            // Value (null-terminated string)
            WriteNullTerminatedString(writer, param.Value ?? string.Empty);

            // Has function (int32 boolean)
            bool hasFunction = param.Function != null;
            writer.Write(hasFunction ? 1 : 0);

            if (hasFunction)
            {
                WriteTriggerFunction(writer, param.Function!);
            }

            // Has array index (int32 boolean)
            bool hasArrayIndex = param.ArrayIndexer != null;
            writer.Write(hasArrayIndex ? 1 : 0);

            if (hasArrayIndex)
            {
                WriteTriggerFunctionParameter(writer, param.ArrayIndexer!);
            }
        }
    }
}
