using BehaviourTree.Runtime.TreeNode;

namespace BehaviourTree.Runtime
{
    public class BehaviourTreeProcessor
    {
        private Root root;

        public void Evaluate(float dt)
        {
            root.Evaluate(dt);
        }
    }
}
