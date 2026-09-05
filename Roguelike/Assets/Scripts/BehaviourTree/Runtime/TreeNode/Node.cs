namespace BehaviourTree.Runtime.TreeNode
{
    public enum NodeState
    {
        Idle,
        Pending,
        Running,
        Success,
        Failure,
    }

    public abstract class Node
    {
        private NodeState currentState = NodeState.Idle;

        private void TryEnter()
        {
            if (currentState is NodeState.Idle or NodeState.Pending)
            {
                OnEnter();
            }
        }

        public NodeState Evaluate(float dt)
        {
            TryEnter();

            var result = currentState = OnEvaluate(dt);

            TryExit();

            return result;
        }

        private void TryExit()
        {
            if (currentState is NodeState.Success or NodeState.Failure)
            {
                OnExit();

                currentState = NodeState.Idle;
            }
        }

        public void Abort()
        {
            if (currentState != NodeState.Running) return;

            OnAbort();
            OnExit();

            currentState = NodeState.Idle;
        }

        // 리소스/구독 해제 등 정리 작업
        public void Dispose()
        {
            OnDispose();
        }

        protected abstract void OnEnter();
        protected abstract void OnExit();
        protected abstract void OnAbort();
        protected abstract void OnDispose();
        protected abstract NodeState OnEvaluate(float dt);
    }
}
