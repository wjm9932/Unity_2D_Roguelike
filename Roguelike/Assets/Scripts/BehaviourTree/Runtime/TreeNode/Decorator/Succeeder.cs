namespace BehaviourTree.Runtime.TreeNode.Decorator
{
    public class Succeeder : DecorateNode
    {
        protected override NodeState OnEvaluate(float dt)
        {
            var state = child.Evaluate(dt);

            return state switch
            {
                NodeState.Running => NodeState.Running,
                _ => NodeState.Success
            };
        }
    }
}
