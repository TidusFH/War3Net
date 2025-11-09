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
                // Category name (null-terminated)
                WriteNullTerminatedString(writer, category.Name);

                // Unknown field 1 (int32) - observed as 0
                writer.Write(0);

                // Unknown field 2 (int32) - observed as 0
                writer.Write(0);

                // Category ID (int32)
                writer.Write(category.Id);

                // Parent ID (int32, -1 for root)
                writer.Write(category.ParentId);
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
            // Simplified for now - just write minimal data
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

                // Trigger ID (int32)
                writer.Write(trigger.Id);

                // Category ID/Parent ID (int32)
                writer.Write(trigger.ParentId);

                // Filter functions by type
                var events = trigger.Functions.Where(f => f.Type == TriggerFunctionType.Event).ToList();
                var conditions = trigger.Functions.Where(f => f.Type == TriggerFunctionType.Condition).ToList();
                var actions = trigger.Functions.Where(f => f.Type == TriggerFunctionType.Action).ToList();

                // === EVENTS ===
                writer.Write(events.Count);
                foreach (var evt in events)
                {
                    WriteTriggerFunction(writer, evt);
                }

                // === CONDITIONS ===
                writer.Write(conditions.Count);
                foreach (var condition in conditions)
                {
                    WriteTriggerFunction(writer, condition);
                }

                // === ACTIONS ===
                writer.Write(actions.Count);
                foreach (var action in actions)
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
