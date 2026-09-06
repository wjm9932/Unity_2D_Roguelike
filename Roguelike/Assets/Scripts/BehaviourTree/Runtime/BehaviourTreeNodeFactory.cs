using BehaviourTree.Data.Node;
using BehaviourTree.Runtime.TreeNode;
using BehaviourTree.Runtime.TreeNode.Decorator;
using Maze.BattleSystem.BehaviourTree.Runtime.TreeNode.Composite;
using System;

namespace BehaviourTree.Runtime
{
    public static class BehaviourTreeNodeFactory
    {
        public static Node CreateNode(NodeData data, BehaviourTreeProcessor owner)
        {
            // 형변환을 통한 타입 체크 대신 enum 타입 체크로
            return data.Type switch
            {
                // Composite
                NodeType.Sequence => new Sequence(),
                NodeType.Selector => new Selector(),

                //Decorator
                NodeType.Inverter => new Inverter(),
                NodeType.Repeater => new Repeater(data as RepeaterNodeData),
                NodeType.Interrupter => new Interrupter(data as InterrupterNodeData, owner),
                NodeType.Succeeder => new Succeeder(),

                _ => throw new ArgumentOutOfRangeException(nameof(data.Type), data.Type, $"NodeType not registered in {nameof(BehaviourTreeNodeFactory)}: {data.Type}")

            };
        }
    }
}
