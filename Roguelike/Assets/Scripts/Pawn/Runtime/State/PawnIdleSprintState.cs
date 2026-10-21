using Input;
using Input.Controls;
using Pawn.Data;
using Pawn.Interface;
using R3;
using StateMachine;
using System;
using UnityEngine;

namespace Pawn.Runtime.State
{
    public class PawnIdleSprintState : PooledState<PawnIdleSprintState>
    {
        private float sprintSpeed;
        private IMoveRequestCollector collector;
        private PawnBody pawnBody;
        private IPawnControls controls;

        private IDisposable dashSubscription;

        public override void Enter(IState previousState)
        {
            dashSubscription = InputManager.Instance.PawnControls.Dash
                .Where(value => value)
                .Subscribe(_ => 
                {
                    collector.Enqueue(new MoveRequest(MoveType.Passive, 10f * Vector2.right)); 
                });
        }

        public override void Update(float dt)
        {
            var moveInfo = controls.Move.CurrentValue;

            if (moveInfo.IsMoving == false) return;

            var velocity = sprintSpeed * moveInfo.RawValue;

            collector.Enqueue(new MoveRequest(MoveType.Active, velocity));
        }

        public override void Exit(IState nextState)
        {
            dashSubscription.Dispose();
            dashSubscription = null;
        }

        public override void OnDispose()
        {
            collector = null;
            pawnBody = null;
        }

        public static PawnIdleSprintState Create(float sprintSpeed, IMoveRequestCollector receiver, PawnBody body)
        {
            var state = GetOrCreate();

            state.sprintSpeed = sprintSpeed;
            state.collector = receiver;
            state.pawnBody = body;
            state.controls = InputManager.Instance.PawnControls;

            return state;
        }
    }
}
