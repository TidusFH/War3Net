using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using War3Net.Build.Script;

namespace WTGMerger
{
    /// <summary>
    /// Deep binary analysis tool for WTG files - hex dumps, comparisons, structure validation
    /// </summary>
    public static class WTGBinaryAnalyzer
    {
        /// <summary>
        /// Performs comprehensive binary analysis of a WTG file
        /// </summary>
        public static void AnalyzeWTGFile(string filePath, string? comparisonFilePath = null)
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         COMPREHENSIVE BINARY ANALYSIS                    ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine($"\nAnalyzing: {filePath}");
            if (comparisonFilePath != null)
            {
                Console.WriteLine($"Comparing with: {comparisonFilePath}");
            }

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"❌ File not found: {filePath}");
                return;
            }

            var fileBytes = File.ReadAllBytes(filePath);
            Console.WriteLine($"\nFile size: {fileBytes.Length} bytes ({fileBytes.Length:N0})");

            // Parse the structure
            ParseWTGStructure(fileBytes);

            // Hex dump of first 512 bytes
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         HEX DUMP (First 512 bytes)                       ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            HexDump(fileBytes, 0, Math.Min(512, fileBytes.Length));

            // If comparison file provided, do byte-by-byte comparison
            if (comparisonFilePath != null && File.Exists(comparisonFilePath))
            {
                var comparisonBytes = File.ReadAllBytes(comparisonFilePath);
                CompareFiles(fileBytes, comparisonBytes, Path.GetFileName(filePath), Path.GetFileName(comparisonFilePath));
            }
        }

        /// <summary>
        /// Parses and displays WTG binary structure
        /// </summary>
        private static void ParseWTGStructure(byte[] data)
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         BINARY STRUCTURE ANALYSIS                        ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            try
            {
                // File signature
                int signature = reader.ReadInt32();
                Console.WriteLine($"\n[Offset 0x{0:X8}] Signature: 0x{signature:X8} ({ToAscii(signature)})");
                if (signature != 0x21475457) // 'WTG!'
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ❌ INVALID SIGNATURE! Expected 0x21475457 ('WTG!')");
                    Console.ResetColor();
                    return;
                }

                // Check if this is 1.27 or 1.31+ format
                int firstInt = reader.ReadInt32();
                bool is127Format = firstInt == 7; // FormatVersion=7 comes first in 1.27

                if (is127Format)
                {
                    Console.WriteLine($"\n[Offset 0x{4:X8}] Format: 1.27 (FormatVersion={firstInt}, no SubVersion)");
                    Parse127Format(reader, 4);
                }
                else
                {
                    Console.WriteLine($"\n[Offset 0x{4:X8}] Format: 1.31+ (SubVersion={firstInt})");
                    Parse131Format(reader, firstInt);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Error parsing structure: {ex.Message}");
                Console.WriteLine($"   At offset: 0x{ms.Position:X8}");
                Console.ResetColor();
            }
        }

        private static void Parse127Format(BinaryReader reader, long offset)
        {
            long startPos = offset + 4; // Already read FormatVersion

            // Categories
            int categoryCount = reader.ReadInt32();
            Console.WriteLine($"\n[Offset 0x{startPos:X8}] Category count: {categoryCount}");

            for (int i = 0; i < categoryCount; i++)
            {
                long catOffset = reader.BaseStream.Position;
                int catId = reader.ReadInt32();
                string catName = ReadString(reader);
                bool isComment = reader.ReadBoolean();

                Console.WriteLine($"  [{i}] Offset 0x{catOffset:X8}: ID={catId}, Name='{catName}', IsComment={isComment}");
            }

            // Game version
            long gameVerOffset = reader.BaseStream.Position;
            int gameVersion = reader.ReadInt32();
            Console.WriteLine($"\n[Offset 0x{gameVerOffset:X8}] Game version: {gameVersion}");

            // Variables
            long varCountOffset = reader.BaseStream.Position;
            int varCount = reader.ReadInt32();
            Console.WriteLine($"\n[Offset 0x{varCountOffset:X8}] Variable count: {varCount}");

            for (int i = 0; i < varCount && i < 10; i++) // Show first 10
            {
                long varOffset = reader.BaseStream.Position;
                string name = ReadString(reader);
                string type = ReadString(reader);
                int unk = reader.ReadInt32();
                bool isArray = reader.ReadBoolean();
                int arraySize = reader.ReadInt32();
                bool isInit = reader.ReadBoolean();
                string initValue = ReadString(reader);

                Console.WriteLine($"  [{i}] Offset 0x{varOffset:X8}: '{name}' ({type}), Array={isArray}[{arraySize}]");
            }
            if (varCount > 10)
            {
                Console.WriteLine($"  ... and {varCount - 10} more variables");
            }

            // Triggers
            long trigCountOffset = reader.BaseStream.Position;
            int triggerCount = reader.ReadInt32();
            Console.WriteLine($"\n[Offset 0x{trigCountOffset:X8}] Trigger count: {triggerCount}");

            for (int i = 0; i < triggerCount && i < 5; i++) // Show first 5
            {
                long trigOffset = reader.BaseStream.Position;
                string name = ReadString(reader);
                string desc = ReadString(reader);
                bool isComment = reader.ReadBoolean();
                bool isEnabled = reader.ReadBoolean();
                bool isCustom = reader.ReadBoolean();
                bool initiallyOff = reader.ReadBoolean();
                bool runOnInit = reader.ReadBoolean();
                int parentId = reader.ReadInt32();
                int funcCount = reader.ReadInt32();

                Console.WriteLine($"  [{i}] Offset 0x{trigOffset:X8}: '{name}'");
                Console.WriteLine($"      ParentId={parentId}, Functions={funcCount}, Enabled={isEnabled}");

                // Show first function for first trigger
                if (i == 0 && funcCount > 0)
                {
                    ParseTriggerFunction(reader, "      ", 0);
                }
                else
                {
                    // Skip functions for other triggers
                    for (int j = 0; j < funcCount; j++)
                    {
                        SkipTriggerFunction(reader);
                    }
                }
            }

            Console.WriteLine($"\n[Offset 0x{reader.BaseStream.Position:X8}] End of parse");
        }

        private static void Parse131Format(BinaryReader reader, int subVersion)
        {
            // Not implemented - focus on 1.27 for now
            Console.WriteLine("  (1.31+ format parsing not fully implemented)");
        }

        private static void ParseTriggerFunction(BinaryReader reader, string indent, int depth)
        {
            if (depth > 3) return; // Prevent infinite recursion

            long funcOffset = reader.BaseStream.Position;
            int funcType = reader.ReadInt32();
            string funcName = ReadString(reader);
            bool isEnabled = reader.ReadBoolean();

            Console.WriteLine($"{indent}[Func @ 0x{funcOffset:X8}] Type={funcType}, Name='{funcName}', Enabled={isEnabled}");

            // Parameters - need to read them but War3Net doesn't write count
            // We'd need TriggerData to know how many params to read
            // For now, just note the position
            Console.WriteLine($"{indent}  (Parameter parsing requires TriggerData - skipped)");
        }

        private static void SkipTriggerFunction(BinaryReader reader)
        {
            // This is complex without TriggerData - just note that we're skipping
            // In reality we'd need to parse the entire function structure
        }

        private static string ReadString(BinaryReader reader)
        {
            // WTG strings are null-terminated
            var bytes = new List<byte>();
            byte b;
            while ((b = reader.ReadByte()) != 0)
            {
                bytes.Add(b);
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        /// <summary>
        /// Hex dump of binary data
        /// </summary>
        private static void HexDump(byte[] data, int offset, int length)
        {
            const int bytesPerLine = 16;

            for (int i = 0; i < length; i += bytesPerLine)
            {
                // Offset
                Console.Write($"{offset + i:X8}  ");

                // Hex bytes
                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (i + j < length)
                    {
                        Console.Write($"{data[offset + i + j]:X2} ");
                    }
                    else
                    {
                        Console.Write("   ");
                    }

                    if (j == 7) Console.Write(" "); // Extra space in middle
                }

                Console.Write(" ");

                // ASCII representation
                for (int j = 0; j < bytesPerLine && i + j < length; j++)
                {
                    byte b = data[offset + i + j];
                    char c = (b >= 32 && b < 127) ? (char)b : '.';
                    Console.Write(c);
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Byte-by-byte comparison of two files
        /// </summary>
        private static void CompareFiles(byte[] file1, byte[] file2, string name1, string name2)
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         BYTE-BY-BYTE COMPARISON                          ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

            Console.WriteLine($"\n{name1}: {file1.Length} bytes");
            Console.WriteLine($"{name2}: {file2.Length} bytes");

            if (file1.Length != file2.Length)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n⚠ SIZE DIFFERENCE: {Math.Abs(file1.Length - file2.Length)} bytes");
                Console.ResetColor();
            }

            int minLength = Math.Min(file1.Length, file2.Length);
            var differences = new List<int>();

            for (int i = 0; i < minLength; i++)
            {
                if (file1[i] != file2[i])
                {
                    differences.Add(i);
                }
            }

            if (differences.Count == 0 && file1.Length == file2.Length)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ FILES ARE IDENTICAL!");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"\nFound {differences.Count} byte differences");

            // Show first 20 differences in detail
            int showCount = Math.Min(20, differences.Count);
            Console.WriteLine($"\nShowing first {showCount} differences:");

            for (int i = 0; i < showCount; i++)
            {
                int offset = differences[i];
                Console.WriteLine($"\n  Offset 0x{offset:X8} (byte {offset}):");
                Console.WriteLine($"    {name1}: 0x{file1[offset]:X2} ({file1[offset]})");
                Console.WriteLine($"    {name2}: 0x{file2[offset]:X2} ({file2[offset]})");

                // Show context (8 bytes before and after)
                Console.Write($"    Context: ");
                int contextStart = Math.Max(0, offset - 8);
                int contextEnd = Math.Min(minLength, offset + 9);
                for (int j = contextStart; j < contextEnd; j++)
                {
                    if (j == offset)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($"[{file1[j]:X2}]");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write($"{file1[j]:X2} ");
                    }
                }
                Console.WriteLine();
            }

            if (differences.Count > 20)
            {
                Console.WriteLine($"\n  ... and {differences.Count - 20} more differences");
            }

            // Analyze difference patterns
            AnalyzeDifferencePatterns(differences);
        }

        private static void AnalyzeDifferencePatterns(List<int> differences)
        {
            if (differences.Count == 0) return;

            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         DIFFERENCE PATTERN ANALYSIS                      ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

            // Check if differences are clustered
            var gaps = new List<int>();
            for (int i = 1; i < differences.Count; i++)
            {
                gaps.Add(differences[i] - differences[i - 1]);
            }

            if (gaps.Count > 0)
            {
                int avgGap = (int)gaps.Average();
                int maxGap = gaps.Max();
                int minGap = gaps.Min();

                Console.WriteLine($"\nGap statistics:");
                Console.WriteLine($"  Average gap: {avgGap} bytes");
                Console.WriteLine($"  Min gap: {minGap} bytes");
                Console.WriteLine($"  Max gap: {maxGap} bytes");

                // Check for patterns
                if (minGap == maxGap && minGap == 4)
                {
                    Console.WriteLine("\n  Pattern: Differences every 4 bytes → Likely int32 field differences");
                }
                else if (gaps.All(g => g < 10))
                {
                    Console.WriteLine("\n  Pattern: Clustered differences → Likely string or structure differences");
                }
                else if (maxGap > 1000)
                {
                    Console.WriteLine("\n  Pattern: Large gaps → Differences in separate sections");
                }
            }
        }

        private static string ToAscii(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            var chars = bytes.Select(b => b >= 32 && b < 127 ? (char)b : '.').ToArray();
            return new string(chars);
        }
    }
}
