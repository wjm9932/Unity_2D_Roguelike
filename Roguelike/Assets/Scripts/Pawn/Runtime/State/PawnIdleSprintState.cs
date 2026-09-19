using Input;
using Input.Controls;
using Pawn.Interface;
using StateMachine;

namespace Pawn.Runtime.State
{
    public class PawnIdleSprintState : PooledState<PawnIdleSprintState>
    {
        private float sprintSpeed;
        private IMoveRequestReceiver receiver;
        private IPawnControls controls;

        public override void Enter(IState previousState)
        {
        }

        public override void Update(float dt)
        {
            var moveInfo = controls.Move.CurrentValue;

            if (moveInfo.IsMoving == false) return;

            receiver.Enqueue(new MoveRequest(MoveType.Active, sprintSpeed * moveInfo.RawValue));
        }

        public override void Exit(IState nextState)
        {
        }

        public override void OnDispose()
        {
            receiver = null;
        }

        public static PawnIdleSprintState Create(float sprintSpeed, IMoveRequestReceiver receiver)
        {
            var state = GetOrCreate();

            state.sprintSpeed = sprintSpeed;
            state.receiver = receiver;
            state.controls = InputManager.Instance.PawnControls;

            return state;
        }
    }
}
