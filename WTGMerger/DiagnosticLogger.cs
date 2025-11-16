using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using War3Net.Build.Script;

namespace WTGMerger
{
    /// <summary>
    /// Comprehensive diagnostic logger that writes everything to a file
    /// </summary>
    public static class DiagnosticLogger
    {
        private static StreamWriter logWriter;
        private static bool isEnabled = false;
        private static string currentLogFile;
        private static int indentLevel = 0;
        private static readonly object lockObject = new object();

        public static bool IsEnabled => isEnabled;

        /// <summary>
        /// Start logging to a new diagnostic file
        /// </summary>
        public static void StartLogging()
        {
            lock (lockObject)
            {
                if (isEnabled)
                {
                    StopLogging();
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                currentLogFile = $"WTGMerger_Diagnostic_{timestamp}.txt";

                logWriter = new StreamWriter(currentLogFile, false);
                logWriter.AutoFlush = true;
                isEnabled = true;

                Log("═══════════════════════════════════════════════════════════════");
                Log($"WTG MERGER COMPREHENSIVE DIAGNOSTIC LOG");
                Log($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Log("═══════════════════════════════════════════════════════════════");
                Log("");

                Console.WriteLine($"✓ Diagnostic logging started: {currentLogFile}");
            }
        }

        /// <summary>
        /// Stop logging and close the file
        /// </summary>
        public static void StopLogging()
        {
            lock (lockObject)
            {
                if (isEnabled && logWriter != null)
                {
                    Log("");
                    Log("═══════════════════════════════════════════════════════════════");
                    Log($"Diagnostic log completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    Log("═══════════════════════════════════════════════════════════════");

                    logWriter.Close();
                    logWriter.Dispose();
                    logWriter = null;
                    isEnabled = false;

                    Console.WriteLine($"✓ Diagnostic log saved: {currentLogFile}");
                }
            }
        }

        /// <summary>
        /// Write a line to the log
        /// </summary>
        public static void Log(string message)
        {
            lock (lockObject)
            {
                if (isEnabled && logWriter != null)
                {
                    string indent = new string(' ', indentLevel * 2);
                    string timestamped = $"[{DateTime.Now:HH:mm:ss.fff}] {indent}{message}";
                    logWriter.WriteLine(timestamped);

                    // Also write to console if in debug mode
                    Console.WriteLine($"[LOG] {message}");
                }
            }
        }

        /// <summary>
        /// Log a section header
        /// </summary>
        public static void LogSection(string title)
        {
            Log("");
            Log($"╔══════════════════════════════════════════════════════════════");
            Log($"║ {title}");
            Log($"╚══════════════════════════════════════════════════════════════");
        }

        /// <summary>
        /// Increase indent level
        /// </summary>
        public static void Indent()
        {
            indentLevel++;
        }

        /// <summary>
        /// Decrease indent level
        /// </summary>
        public static void Unindent()
        {
            if (indentLevel > 0)
                indentLevel--;
        }

        /// <summary>
        /// Log MapTriggers state
        /// </summary>
        public static void LogMapTriggersState(MapTriggers triggers, string label)
        {
            if (!isEnabled) return;

            LogSection($"MapTriggers State: {label}");
            Log($"Format Version: {triggers.FormatVersion}");
            Log($"Sub Version: {triggers.SubVersion?.ToString() ?? "null (1.27 format)"}");
            Log($"Total Trigger Items: {triggers.TriggerItems.Count}");
            Log($"Total Variables: {triggers.Variables.Count}");
            Log("");

            // Count by type
            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var triggersOnly = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();

            Log($"Categories: {categories.Count}");
            Log($"Triggers: {triggersOnly.Count}");
            Log("");

            // List all categories with details
            if (categories.Count > 0)
            {
                Log("Categories Details:");
                Indent();
                foreach (var cat in categories)
                {
                    Log($"- '{cat.Name}' (ID={cat.Id}, ParentId={cat.ParentId}, Type={cat.Type}, IsComment={cat.IsComment})");
                }
                Unindent();
                Log("");
            }

            // List all triggers with details
            if (triggersOnly.Count > 0)
            {
                Log($"Triggers Details: (showing first 50 of {triggersOnly.Count})");
                Indent();
                foreach (var trigger in triggersOnly.Take(50))
                {
                    Log($"- '{trigger.Name}' (ID={trigger.Id}, ParentId={trigger.ParentId}, Enabled={trigger.IsEnabled})");
                }
                if (triggersOnly.Count > 50)
                {
                    Log($"... and {triggersOnly.Count - 50} more triggers");
                }
                Unindent();
                Log("");
            }

            // List all variables
            if (triggers.Variables.Count > 0)
            {
                Log($"Variables Details: (showing first 50 of {triggers.Variables.Count})");
                Indent();
                foreach (var variable in triggers.Variables.Take(50))
                {
                    Log($"- '{variable.Name}' (ID={variable.Id}, Type={variable.Type}, IsArray={variable.IsArray})");
                }
                if (triggers.Variables.Count > 50)
                {
                    Log($"... and {triggers.Variables.Count - 50} more variables");
                }
                Unindent();
                Log("");
            }

            // Analyze ParentId distribution
            Log("ParentId Distribution Analysis:");
            Indent();
            var parentIdGroups = triggers.TriggerItems
                .GroupBy(t => t.ParentId)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in parentIdGroups.Take(20))
            {
                var items = group.ToList();
                var catCount = items.OfType<TriggerCategoryDefinition>().Count();
                var trigCount = items.OfType<TriggerDefinition>().Count();
                Log($"ParentId={group.Key}: {items.Count} items ({catCount} categories, {trigCount} triggers)");
            }
            if (parentIdGroups.Count > 20)
            {
                Log($"... and {parentIdGroups.Count - 20} more ParentId values");
            }
            Unindent();
            Log("");
        }

        /// <summary>
        /// Log IntermediateWTG state
        /// </summary>
        public static void LogIntermediateState(IntermediateWTG intermediate, string label)
        {
            if (!isEnabled) return;

            LogSection($"Intermediate WTG State: {label}");
            Log($"Source File: {intermediate.SourceFile}");
            Log($"Format Version: {intermediate.FormatVersion}");
            Log($"Sub Version: {intermediate.SubVersion?.ToString() ?? "null (1.27 format)"}");
            Log("");

            var allCategories = intermediate.GetAllCategories().ToList();
            var allTriggers = intermediate.GetAllTriggers().ToList();

            Log($"Total Categories: {allCategories.Count}");
            Log($"Total Triggers: {allTriggers.Count}");
            Log($"Total Variables: {intermediate.Variables.Count}");
            Log("");

            // Log hierarchy structure
            Log("Hierarchy Structure:");
            Indent();
            LogHierarchyNode(intermediate.Root, 0);
            Unindent();
            Log("");

            // Log variables
            if (intermediate.Variables.Count > 0)
            {
                Log($"Variables: (showing first 30 of {intermediate.Variables.Count})");
                Indent();
                foreach (var variable in intermediate.Variables.Take(30))
                {
                    Log($"- '{variable.Name}' (OriginalId={variable.OriginalId}, Type={variable.OriginalVariable.Type})");
                }
                if (intermediate.Variables.Count > 30)
                {
                    Log($"... and {intermediate.Variables.Count - 30} more variables");
                }
                Unindent();
                Log("");
            }
        }

        /// <summary>
        /// Recursively log hierarchy node
        /// </summary>
        private static void LogHierarchyNode(HierarchyNode node, int depth)
        {
            if (depth > 10) // Prevent infinite recursion
            {
                Log("... (max depth reached)");
                return;
            }

            string prefix = new string(' ', depth * 2);

            if (node is RootNode)
            {
                Log($"{prefix}[ROOT] {node.Children.Count} children");
            }
            else if (node is CategoryNode cat)
            {
                Log($"{prefix}[CAT] '{cat.Name}' (OriginalId={cat.OriginalId}, ParentId={cat.OriginalParentId}) - {cat.Children.Count} children");
            }
            else if (node is TriggerItemNode trigger)
            {
                Log($"{prefix}[TRG] '{trigger.Name}' (OriginalId={trigger.OriginalId}, ParentId={trigger.OriginalParentId})");
            }

            // Recursively log children
            foreach (var child in node.Children.Take(100)) // Limit to first 100 to avoid huge logs
            {
                LogHierarchyNode(child, depth + 1);
            }

            if (node.Children.Count > 100)
            {
                Log($"{prefix}... and {node.Children.Count - 100} more children");
            }
        }

        /// <summary>
        /// Log operation start
        /// </summary>
        public static void LogOperationStart(string operation)
        {
            if (!isEnabled) return;

            Log("");
            Log($">>> OPERATION START: {operation}");
            Indent();
        }

        /// <summary>
        /// Log operation end
        /// </summary>
        public static void LogOperationEnd(string operation, bool success = true)
        {
            if (!isEnabled) return;

            Unindent();
            Log($"<<< OPERATION END: {operation} - {(success ? "SUCCESS" : "FAILED")}");
            Log("");
        }

        /// <summary>
        /// Log file operation
        /// </summary>
        public static void LogFileOperation(string operation, string filePath)
        {
            if (!isEnabled) return;

            Log($"FILE: {operation} - {filePath}");
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                Log($"  Size: {fileInfo.Length} bytes");
                Log($"  Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            }
        }

        /// <summary>
        /// Log merge conflict
        /// </summary>
        public static void LogConflict(MergeConflict conflict)
        {
            if (!isEnabled) return;

            Log($"CONFLICT: {conflict.Type}");
            Indent();
            Log($"Name: {conflict.Name}");
            Log($"Source: {conflict.SourcePath}");
            Log($"Target: {conflict.TargetPath}");
            Log($"Message: {conflict.Message}");
            Unindent();
        }
    }
}
