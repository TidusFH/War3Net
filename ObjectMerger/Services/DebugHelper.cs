using System;
using System.Linq;
using System.Text;
using War3Net.Build.Object;
using War3Net.Common.Extensions;

namespace ObjectMerger.Services
{
    /// <summary>
    /// Helper class for debug output with hex dumps
    /// </summary>
    public static class DebugHelper
    {
        /// <summary>
        /// Show detailed information about a SimpleObjectModification
        /// </summary>
        public static void ShowObjectDetails(SimpleObjectModification obj, string objectName)
        {
            if (!Program.DebugMode) return;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n╔═══ DEBUG: {objectName} ═══");
            Console.ResetColor();

            Console.WriteLine($"  Object ID: {obj.NewId.ToRawcode()} (0x{obj.NewId:X8})");
            Console.WriteLine($"  Base ID:   {obj.OldId.ToRawcode()} (0x{obj.OldId:X8})");
            Console.WriteLine($"  Modifications: {obj.Modifications.Count}");

            if (obj.Modifications.Any())
            {
                Console.WriteLine("\n  Field Modifications:");
                foreach (var mod in obj.Modifications.Take(10)) // Limit to first 10 for brevity
                {
                    string fieldId = mod.Id.ToRawcode();
                    string typeStr = GetDataTypeName(mod.Type);
                    string valueStr = FormatValue(mod.Value, mod.Type);

                    Console.WriteLine($"    [{fieldId}] Type:{typeStr,-10} Value: {valueStr}");

                    if (Program.DebugMode)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"      Hex: ID=0x{mod.Id:X8}, Type=0x{(int)mod.Type:X8}");
                        Console.ResetColor();
                    }
                }

                if (obj.Modifications.Count > 10)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"    ... and {obj.Modifications.Count - 10} more modifications");
                    Console.ResetColor();
                }
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╚═══════════════════════════════════════════\n");
            Console.ResetColor();
        }

        /// <summary>
        /// Show detailed information about a LevelObjectModification (abilities, upgrades)
        /// </summary>
        public static void ShowLevelObjectDetails(LevelObjectModification obj, string objectName)
        {
            if (!Program.DebugMode) return;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n╔═══ DEBUG: {objectName} ═══");
            Console.ResetColor();

            Console.WriteLine($"  Object ID: {obj.NewId.ToRawcode()} (0x{obj.NewId:X8})");
            Console.WriteLine($"  Base ID:   {obj.OldId.ToRawcode()} (0x{obj.OldId:X8})");
            Console.WriteLine($"  Modifications: {obj.Modifications.Count}");

            if (obj.Modifications.Any())
            {
                Console.WriteLine("\n  Field Modifications:");
                foreach (var mod in obj.Modifications.Take(10))
                {
                    string fieldId = mod.Id.ToRawcode();
                    string typeStr = GetDataTypeName(mod.Type);
                    string valueStr = FormatValue(mod.Value, mod.Type);

                    Console.WriteLine($"    [{fieldId}] Lvl:{mod.Level} Type:{typeStr,-10} Value: {valueStr}");

                    if (Program.DebugMode)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"      Hex: ID=0x{mod.Id:X8}, Type=0x{(int)mod.Type:X8}, Ptr=0x{mod.Pointer:X8}");
                        Console.ResetColor();
                    }
                }

                if (obj.Modifications.Count > 10)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"    ... and {obj.Modifications.Count - 10} more modifications");
                    Console.ResetColor();
                }
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╚═══════════════════════════════════════════\n");
            Console.ResetColor();
        }

        /// <summary>
        /// Show hex dump of data
        /// </summary>
        public static void ShowHexDump(byte[] data, string label, int maxBytes = 64)
        {
            if (!Program.DebugMode) return;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  [HEX {label}]:");

            int bytesToShow = Math.Min(data.Length, maxBytes);
            for (int i = 0; i < bytesToShow; i += 16)
            {
                var hex = new StringBuilder();
                var ascii = new StringBuilder();

                for (int j = 0; j < 16 && (i + j) < bytesToShow; j++)
                {
                    byte b = data[i + j];
                    hex.Append($"{b:X2} ");
                    ascii.Append(b >= 32 && b < 127 ? (char)b : '.');
                }

                Console.WriteLine($"    {i:X4}: {hex,-48} | {ascii}");
            }

            if (data.Length > maxBytes)
            {
                Console.WriteLine($"    ... ({data.Length - maxBytes} more bytes)");
            }

            Console.ResetColor();
        }

        private static string GetDataTypeName(ObjectDataType type)
        {
            return type switch
            {
                ObjectDataType.Int => "Int",
                ObjectDataType.Real => "Real",
                ObjectDataType.Unreal => "Unreal",
                ObjectDataType.String => "String",
                _ => $"Unknown({(int)type})"
            };
        }

        private static string FormatValue(object? value, ObjectDataType type)
        {
            if (value == null) return "(null)";

            return type switch
            {
                ObjectDataType.Int => $"{value} (0x{value:X8})",
                ObjectDataType.Real => $"{value:F4}",
                ObjectDataType.Unreal => $"{value:F4}",
                ObjectDataType.String => $"\"{value}\"",
                _ => value.ToString() ?? "(null)"
            };
        }
    }
}
