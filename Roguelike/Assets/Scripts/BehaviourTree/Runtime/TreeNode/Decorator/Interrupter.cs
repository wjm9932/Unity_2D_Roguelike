using BehaviourTree.Data.Node;

namespace BehaviourTree.Runtime.TreeNode.Decorator
{
    public class Interrupter : DecorateNode
    {
        private readonly Node interruptCondition;

        public Interrupter(InterrupterNodeData data, BehaviourTreeProcessor owner)
        {
            var node = BehaviourTreeNodeFactory.CreateNode(data.ConditionNodeData, owner);
            interruptCondition = data.IsInverted ? new Inverter(node) : node;
        }

        protected override NodeState OnEvaluate(float dt)
        {
            var conditionState = interruptCondition.Evaluate(dt);

            if (conditionState == NodeState.Success)
            {
                child.Abort();
                return NodeState.Failure;
            }

            return child.Evaluate(dt);
        }
    }
}
