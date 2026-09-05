using BehaviourTree.Runtime.TreeNode;
using BehaviourTree.Runtime.TreeNode.CompositeNode;
using System;
using UnityEngine;

namespace Maze.BattleSystem.BehaviourTree.Runtime.TreeNode.Composite
{
    public class Selector : CompositeNode
    {
        protected override NodeState OnEvaluate(float dt)
        {
            if (children.Count == 0)
            {
                return NodeState.Success;
            }

            for (; currentChildIndex < children.Count; currentChildIndex++)
            {
                var state = children[currentChildIndex].Evaluate(dt);

                switch (state)
                {
                    case NodeState.Success:
                        return NodeState.Success;
                    case NodeState.Running:
                        return NodeState.Running;
                    case NodeState.Pending:
                        return NodeState.Pending;
                    case NodeState.Failure:
                        continue;
                    default:
                        {
                            Debug.LogError($"Invalid State returned in Selector: {state}");
                            throw new ArgumentOutOfRangeException();
                        }
                }
            }

            return NodeState.Failure;
        }
    }
}
