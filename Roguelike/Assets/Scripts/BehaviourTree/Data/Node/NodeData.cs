using System;

namespace BehaviourTree.Data.Node
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NodeCategoryAttribute : Attribute
    {
        public NodeCategory Category { get; }
        public NodeCategoryAttribute(NodeCategory category) => Category = category;
    }

    public enum NodeCategory
    {
        Root,
        Flow,
        Decorator,
        Condition,
        Action,
        Composite,
        Misc,
    }

    public enum NodeType
    {
        [NodeCategory(NodeCategory.Root)] Root,

        [NodeCategory(NodeCategory.Flow)] Selector,
        [NodeCategory(NodeCategory.Flow)] Sequence,

        [NodeCategory(NodeCategory.Decorator)] Inverter,
        [NodeCategory(NodeCategory.Decorator)] Repeater,
        [NodeCategory(NodeCategory.Decorator)] Interrupter,
        [NodeCategory(NodeCategory.Decorator)] Succeeder,

        [NodeCategory(NodeCategory.Condition)] TargetInRange,
        [NodeCategory(NodeCategory.Condition)] TargetInAngle,
        [NodeCategory(NodeCategory.Condition)] HpRatioUnder,
        [NodeCategory(NodeCategory.Condition)] CoolDown,

        [NodeCategory(NodeCategory.Action)] Wait,
        [NodeCategory(NodeCategory.Action)] SubTree,

        [NodeCategory(NodeCategory.Composite)] Composite,
    }


    public abstract class NodeData
    {
        public abstract NodeType Type { get; }
        public virtual string Tooltip => string.Empty;
    }
}
