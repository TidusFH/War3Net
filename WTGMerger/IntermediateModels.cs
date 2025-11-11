using System;
using System.Collections.Generic;
using System.Linq;
using War3Net.Build.Script;

namespace WTGMerger
{
    /// <summary>
    /// Intermediate representation for triggers, categories, and variables
    /// Used for disassembling War3Net objects, merging, and rebuilding with predictable IDs
    /// </summary>

    /// <summary>
    /// Base class for all trigger hierarchy nodes
    /// </summary>
    public abstract class HierarchyNode
    {
        public string Name { get; set; }
        public int OriginalId { get; set; }  // For reference/debugging
        public HierarchyNode Parent { get; set; }
        public List<HierarchyNode> Children { get; set; } = new List<HierarchyNode>();

        /// <summary>
        /// Source file this node came from (for conflict resolution)
        /// </summary>
        public string SourceFile { get; set; }

        public abstract NodeType Type { get; }

        /// <summary>
        /// Add a child node
        /// </summary>
        public void AddChild(HierarchyNode child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        /// <summary>
        /// Remove a child node
        /// </summary>
        public void RemoveChild(HierarchyNode child)
        {
            child.Parent = null;
            Children.Remove(child);
        }

        /// <summary>
        /// Get all descendants of a specific type
        /// </summary>
        public IEnumerable<T> GetDescendants<T>() where T : HierarchyNode
        {
            foreach (var child in Children)
            {
                if (child is T typedChild)
                    yield return typedChild;

                foreach (var descendant in child.GetDescendants<T>())
                    yield return descendant;
            }
        }

        /// <summary>
        /// Get direct children of a specific type
        /// </summary>
        public IEnumerable<T> GetChildren<T>() where T : HierarchyNode
        {
            return Children.OfType<T>();
        }
    }

    public enum NodeType
    {
        Root,
        Category,
        Trigger,
        Variable
    }

    /// <summary>
    /// Root node of the trigger hierarchy
    /// </summary>
    public class RootNode : HierarchyNode
    {
        public override NodeType Type => NodeType.Root;

        public RootNode()
        {
            Name = "Root";
            OriginalId = -1;
        }
    }

    /// <summary>
    /// Represents a category/folder in the trigger hierarchy
    /// </summary>
    public class CategoryNode : HierarchyNode
    {
        public override NodeType Type => NodeType.Category;
        public bool IsComment { get; set; }

        /// <summary>
        /// Whether this category was expanded in the editor
        /// </summary>
        public bool IsExpanded { get; set; }

        public CategoryNode(TriggerCategoryDefinition category)
        {
            Name = category.Name;
            OriginalId = category.Id;
            IsComment = category.IsComment;
            IsExpanded = category.IsExpanded;
        }
    }

    /// <summary>
    /// Represents a trigger in the hierarchy
    /// </summary>
    public class TriggerItemNode : HierarchyNode
    {
        public override NodeType Type => NodeType.Trigger;
        public string Description { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsCustomTextTrigger { get; set; }
        public bool RunOnMapInit { get; set; }
        public bool IsComment { get; set; }

        /// <summary>
        /// The actual trigger definition (for rebuilding)
        /// </summary>
        public TriggerDefinition OriginalTrigger { get; set; }

        public TriggerItemNode(TriggerDefinition trigger)
        {
            Name = trigger.Name;
            OriginalId = trigger.Id;
            Description = trigger.Description;
            IsEnabled = trigger.IsEnabled;
            IsCustomTextTrigger = trigger.IsCustomTextTrigger;
            RunOnMapInit = trigger.RunOnMapInit;
            IsComment = trigger.IsComment;
            OriginalTrigger = trigger;
        }
    }

    /// <summary>
    /// Represents a global variable
    /// </summary>
    public class VariableNode
    {
        public string Name { get; set; }
        public int OriginalId { get; set; }
        public VariableDefinition OriginalVariable { get; set; }
        public string SourceFile { get; set; }

        public VariableNode(VariableDefinition variable)
        {
            Name = variable.Name;
            OriginalId = variable.Id;
            OriginalVariable = variable;
        }
    }

    /// <summary>
    /// Complete intermediate representation of a WTG file
    /// </summary>
    public class IntermediateWTG
    {
        public RootNode Root { get; set; } = new RootNode();
        public List<VariableNode> Variables { get; set; } = new List<VariableNode>();
        public string SourceFile { get; set; }
        public MapTriggersFormatVersion FormatVersion { get; set; }
        public MapTriggersSubVersion? SubVersion { get; set; }

        /// <summary>
        /// Get all categories in the tree
        /// </summary>
        public IEnumerable<CategoryNode> GetAllCategories()
        {
            return Root.GetDescendants<CategoryNode>();
        }

        /// <summary>
        /// Get all triggers in the tree
        /// </summary>
        public IEnumerable<TriggerItemNode> GetAllTriggers()
        {
            return Root.GetDescendants<TriggerItemNode>();
        }

        /// <summary>
        /// Find a category by name (case-sensitive)
        /// </summary>
        public CategoryNode FindCategoryByName(string name)
        {
            return GetAllCategories().FirstOrDefault(c => c.Name == name);
        }

        /// <summary>
        /// Find a trigger by name (case-sensitive)
        /// </summary>
        public TriggerItemNode FindTriggerByName(string name)
        {
            return GetAllTriggers().FirstOrDefault(t => t.Name == name);
        }

        /// <summary>
        /// Find a variable by name (case-sensitive)
        /// </summary>
        public VariableNode FindVariableByName(string name)
        {
            return Variables.FirstOrDefault(v => v.Name == name);
        }
    }

    /// <summary>
    /// Represents a conflict detected during merge
    /// </summary>
    public class MergeConflict
    {
        public ConflictType Type { get; set; }
        public string Name { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public string Message { get; set; }

        public enum ConflictType
        {
            DuplicateTriggerName,
            DuplicateCategoryName,
            DuplicateVariableName,
            CircularReference,
            InvalidParent
        }
    }
}
