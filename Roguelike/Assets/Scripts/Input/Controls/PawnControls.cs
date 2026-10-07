using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using static RogueLikeInput;

namespace Input.Controls
{
    public struct MoveInfo
    {
        public readonly Vector2 RawValue;
        public readonly bool IsMoving;

        public MoveInfo(Vector2 rawValue)
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
        public ReadOnlyReactiveProperty<MoveInfo> Move { get; }

#if UNITY_EDITOR
        public ReadOnlyReactiveProperty<MoveInfo> TestMove { get; }
#endif

        public void Enable();

        public void Disable();
    }

    /// <summary>
    /// 폰 입력 상태를 관리하고 R3 스트림으로 제공한다.
    /// </summary>
    internal partial class PawnControls : IPawnControls
    {
        public ReadOnlyReactiveProperty<MoveInfo> Move => move;
        private readonly ReactiveProperty<MoveInfo> move = new();

#if UNITY_EDITOR
        public ReadOnlyReactiveProperty<MoveInfo> TestMove => testMove;
        private readonly ReactiveProperty<MoveInfo> testMove = new();
#endif

        public void Enable()
        {
            // 혹시 모를 중복 등록을 막기 위해 명시적으로 Disable 한 번 수행
            Disable();

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
        /// IPawnControls로 제공한 객체를 IPawnActions로 형변환하여
        /// 콜백을 직접 호출하는 경로를 차단하기 위해 Adapter로 분리한다.
        /// Adapter는 콜백 등록과 해제를 담당한다.
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

            public void OnMove(InputAction.CallbackContext context) => controls.move.Value = new MoveInfo(context.ReadValue<Vector2>());

            public void OnTestMove(InputAction.CallbackContext context) => controls.testMove.Value = new MoveInfo(context.ReadValue<Vector2>());
        }

        private PawnInputAdapter pawnInputAdapter;

        internal PawnControls(RogueLikeInput inputs) => pawnInputAdapter = new PawnInputAdapter(this, inputs.Pawn);
    }
}