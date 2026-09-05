using BehaviourTree.Runtime.TreeNode.Interface;

namespace BehaviourTree.Runtime.TreeNode.Decorator
{
    public abstract class DecorateNode : Node, IParentNode
    {
        protected Node child;

        public void AddChild(Node child)
        {
            this.child = child;
        }

        protected sealed override void OnEnter()
        {
            Enter();
        }

        protected sealed override void OnExit()
        {
            Exit();
        }

        protected sealed override void OnAbort()
        {
            child.Abort();
        }

        protected sealed override void OnDispose()
        {
            child.Dispose();
        }

        protected virtual void Enter() { }

        protected virtual void Exit() { }
    }
}
