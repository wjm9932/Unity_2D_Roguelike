using System.Collections.Generic;
using BehaviourTree.Runtime.TreeNode.Interface;

namespace BehaviourTree.Runtime.TreeNode.CompositeNode
{
    public abstract class CompositeNode : Node, IParentNode
    {
        protected readonly List<Node> children = new();
        protected int currentChildIndex;

        public void AddChild(Node child)
        {
            children.Add(child);
        }

        /// <summary>
        /// OnExit과 일관성을 위해 구조 통일
        /// </summary>
        protected sealed override void OnEnter()
        {
            Enter();
        }

        /// <summary>
        /// 자식에서 OnExit 재정의 후 base.Exit() 누락 방지를 위해 OnExit을 sealed로 고정하고, 확장 메서드를 Exit()로 분리한다.
        /// </summary>
        protected sealed override void OnExit()
        {
            Exit();

            currentChildIndex = 0;
        }

        protected sealed override void OnAbort()
        {
            foreach (var child in children)
            {
                child.Abort();
            }
        }

        protected sealed override void OnDispose()
        {
            foreach (var child in children)
            {
                child.Dispose();
            }
        }

        protected virtual void Enter() { }
        protected virtual void Exit() { }
    }

}