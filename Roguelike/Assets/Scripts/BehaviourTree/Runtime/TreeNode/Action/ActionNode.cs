using System;

namespace BehaviourTree.Runtime.TreeNode.Action
{
    public abstract class ActionNode : Node
    {
        /// <summary>
		/// NodeState로도 표현이 가능하지만 의미가 모호해지고 의도를 파악하기 힘들어, 액션 상태는 ActionState로 분리.
		/// </summary>
		protected enum ActionState
        {
            Idle,
            Running,
            Finished,
            Cancelled,
        }

        protected BehaviourTreeProcessor Owner { get; private set; }
        protected ActionState actionState = ActionState.Idle;

        /// <summary>
        /// 액션이 트리거 되었는지 여부
        /// </summary>
        private bool isTriggered;

        protected ActionNode(BehaviourTreeProcessor owner)
        {
            Owner = owner;
        }

        protected sealed override void OnEnter()
        {
            // 실행 가능한 상태인지 확인
            isTriggered = CanTriggerAction();

            // 실행 불가하면 바로 종료
            if (isTriggered == false) return;

            Enter();

            actionState = ActionState.Running;
        }

        protected sealed override NodeState OnEvaluate(float dt)
        {
            // 트리거되지 않은 액션은 실행 가능해질 때까지 대기 또는 실패 반환
            if (isTriggered == false) return StateIfNotTriggered;

            actionState = EvaluateAction(dt);

            return actionState switch
            {
                ActionState.Running => NodeState.Running,
                ActionState.Finished => NodeState.Success,
                ActionState.Cancelled => NodeState.Failure,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        protected sealed override void OnExit()
        {
            Exit();

            actionState = ActionState.Idle;
            isTriggered = false;
        }

        protected sealed override void OnAbort()
        {
            if (actionState != ActionState.Running) return;

            AbortAction();
        }

        protected sealed override void OnDispose()
        {
            DisposeAction();
        }

        protected virtual void Enter() { }
        protected virtual void Exit() { }
        protected virtual void AbortAction() { }
        protected virtual void DisposeAction() { }
        protected virtual ActionState EvaluateAction(float dt) => actionState;
        /// <summary>
        /// 현재 Owner가 액션을 실행 할 수 있는 상태인지 체크
        /// </summary>
        protected virtual bool CanTriggerAction() => true;
        /// <summary>
        /// 트리거 되지 않았을 경우 반환할 노드 상태
        /// </summary>
        protected virtual NodeState StateIfNotTriggered => NodeState.Failure;
    }
}
