using System;
using BehaviourTree.Data.Node;

namespace BehaviourTree.Runtime.TreeNode.Decorator
{
    public class Repeater : DecorateNode
    {
        private const int Infinite = -1;

        private readonly int repeatCount;
        private int remainCount;

        public Repeater(RepeaterNodeData data)
        {
            // 에디터 단계에서 0보다 작은 수를 넣을 수 없게 하겠지만 혹시 모르니..
            if (data.Count <= 0 && data.IsInfinite == false)
            {
                throw new ArgumentException($"{nameof(RepeaterNodeData.Count)} must be greater than zero");
            }

            repeatCount = data.IsInfinite ? Infinite : data.Count;
        }

        protected override void Enter()
        {
            remainCount = repeatCount;
        }

        protected override NodeState OnEvaluate(float dt)
        {
            var childState = child.Evaluate(dt);

            if (repeatCount == Infinite)
            {
                return NodeState.Running;
            }

            if (childState is NodeState.Failure or NodeState.Success)
            {
                remainCount--;
            }

            return remainCount > 0 ? NodeState.Running : NodeState.Success;
        }
    }
}
