namespace BehaviourTree.Runtime.TreeNode.Decorator
{
    public class Inverter : DecorateNode
    {
        public Inverter()
        {
        }

        public Inverter(Node child)
        {
            this.child = child;
        }

        protected override NodeState OnEvaluate(float dt)
        {
            var state = child.Evaluate(dt);

            return state switch
            {
                NodeState.Success => NodeState.Failure,
                NodeState.Failure => NodeState.Success,
                _ => NodeState.Running
            };
        }
    }
}
