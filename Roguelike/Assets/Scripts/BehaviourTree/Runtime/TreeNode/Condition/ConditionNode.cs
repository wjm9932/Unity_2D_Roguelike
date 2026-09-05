namespace BehaviourTree.Runtime.TreeNode.Condition
{
    public abstract class ConditionNode : Node
    {
        protected BehaviourTreeProcessor Owner { get; private set; }
        protected bool isSatisfied;

        protected ConditionNode(BehaviourTreeProcessor owner)
        {
            Owner = owner;
        }

        protected sealed override void OnEnter()
        {
            Enter();
        }

        protected sealed override NodeState OnEvaluate(float dt)
        {
            isSatisfied = EvaluateCondition(dt);

            return isSatisfied ? NodeState.Success : NodeState.Failure;
        }

        protected sealed override void OnExit()
        {
            Exit();

            isSatisfied = false;
        }

        protected sealed override void OnAbort()
        {
            AbortCondition();
        }

        protected sealed override void OnDispose()
        {
            DisposeCondition();
        }

        protected virtual void Enter() { }
        protected virtual void Exit() { }
        protected virtual void AbortCondition() { }
        protected virtual void DisposeCondition() { }
        protected virtual bool EvaluateCondition(float dt) => isSatisfied;

    }
}
