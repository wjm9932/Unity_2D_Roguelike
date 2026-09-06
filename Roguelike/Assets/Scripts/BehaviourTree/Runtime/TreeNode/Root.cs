using BehaviourTree.Runtime.TreeNode.Interface;
using UnityEngine;

namespace BehaviourTree.Runtime.TreeNode
{
    public class Root : Node, IParentNode
    {
        public Node Child { get; private set; }

        public void Initialize(Node child)
        {
            if (Child != null)
            {
                Debug.LogError("Root is already initialized");
                return;
            }

            Child = child;
        }

        protected override void OnEnter()
        {
        }

        protected override NodeState OnEvaluate(float dt)
        {
            return Child?.Evaluate(dt) ?? NodeState.Failure;
        }

        protected override void OnExit()
        {
        }

        protected override void OnAbort()
        {
            OnExit();

            Child?.Abort();
        }

        public void AddChild(Node child)
        {
            Child = child;
        }

        protected override void OnDispose()
        {
            Child?.Dispose();
        }
    }
}
