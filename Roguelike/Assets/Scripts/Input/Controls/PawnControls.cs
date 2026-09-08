using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using static RogueLikeInput;

namespace Input.Controls
{
    public struct Move
    {
        public readonly Vector2 RawValue;
        public readonly bool IsMoving;

        public Move(Vector2 rawValue)
        {
            RawValue = rawValue;
            IsMoving = !(Mathf.Abs(rawValue.x) < Mathf.Epsilon && Mathf.Abs(rawValue.y) < Mathf.Epsilon);
        }
    }

    /// <summary>
    /// 외부에는 폰 입력 상태와 활성화 제어만 노출한다.
    /// 리소스의 해제는 외부로 노출하지 않고 InputManager가 담당한다.
    /// </summary>
    public interface IPawnControls
    {
        public ReadOnlyReactiveProperty<Move> Move { get; }
        
        public void Enable();

        public void Disable();
    }

    /// <summary>
    /// 폰 입력 상태를 관리하고 R3 스트림으로 제공한다.
    /// </summary>
    internal partial class PawnControls : IPawnControls
    {
        public ReadOnlyReactiveProperty<Move> Move => move;
        private readonly ReactiveProperty<Move> move = new();

        public void Enable()
        {
            pawnInputAdapter.Register();
        }

        public void Disable()
        {
            pawnInputAdapter.UnRegister();
            move.Value = default;
        }

        public void Dispose()
        {
            Disable();

            move.Dispose();
        }
    }

    internal partial class PawnControls
    {
        /// <summary>
        /// PawnControls 객체로 접근해서 OnMove 호출 막기 위해 Adapter로 분리
        /// Adapter는 콜백 등록/해제의 책임을 가진다.
        /// </summary>
        private class PawnInputAdapter : IPawnActions
        {
            private PawnControls controls;
            private PawnActions actions;

            public PawnInputAdapter(PawnControls pawnControls, PawnActions pawnActions)
            {
                controls = pawnControls;
                actions = pawnActions;

                Register();
            }

            public void Register() => actions.AddCallbacks(this);

            public void UnRegister() => actions.RemoveCallbacks(this);

            public void OnMove(InputAction.CallbackContext context) => controls.move.Value = new Move(context.ReadValue<Vector2>());
        }

        private PawnInputAdapter pawnInputAdapter;

        internal PawnControls(RogueLikeInput inputs) => pawnInputAdapter = new PawnInputAdapter(this, inputs.Pawn);
    }
}