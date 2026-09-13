using Input;
using Input.Controls;
using Pawn.Interface;
using UnityEngine;

namespace Pawn.Runtime.Controller
{
    public class ManualPawnController : IPawnController
    {
        public bool IsStickNeutral { get; private set; }
        public Vector2 LastStickDirection { get; private set; }

        private IPawnControls pawnControls;

        public ManualPawnController()
        {
            pawnControls = InputManager.Instance.PawnControls;
        }

        public void OnUpdate(float dt)
        {
            IsStickNeutral = !pawnControls.Move.CurrentValue.IsMoving;
            LastStickDirection = pawnControls.Move.CurrentValue.RawValue;
        }
    }
}
