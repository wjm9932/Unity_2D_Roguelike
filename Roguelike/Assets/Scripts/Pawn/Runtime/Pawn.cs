using UnityEngine;
using Foundations;
using Pawn.Interface;
using EventSystem;
using Pawn.Runtime.Controller;
using Pawn.Runtime;
using Pawn.Data;
using System;
using Input;

namespace Pawn
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class Pawn : MonoBehaviour, IEventListener
    {
        public IPawnController PawnController => pawnController;
        public IPawnStatsBehaviour PawnStatsBehaviour => pawnStatsBehaviour;
        public IPawnBehaviour PawnBehaviour => pawnBehaviour;

        private IPawnController pawnController;
        private IPawnStatsBehaviour pawnStatsBehaviour;
        private IPawnBehaviour pawnBehaviour;

        private IPawnMovement pawnMovement;

        public AnimationCurve test;

        private void Awake()
        {
            // FIXME: 일단 메뉴얼만. 이후 분기쳐서 controller 할당
            pawnController = new ManualPawnController();

            pawnBehaviour = new PawnBehaviour();

            //pawnMovement = new PawnMovement(GetComponent<Rigidbody2D>(), pawnStatsBehaviour.StatsDefinition.AccelerationCurve);
            #region Test
            pawnMovement = new PawnMovement(GetComponent<Rigidbody2D>(), test);
            #endregion
        }

        private void Start()
        {
        }

        private void Update()
        {
            var dt = TimeManager.Instance.InGameDeltaTime;

            this.ThrowUpdate(dt);

            #region Test
            var move = InputManager.Instance.PawnControls.Move.CurrentValue;
            if (move.IsMoving)
            {
                pawnMovement.Enqueue(new MoveRequest(MoveType.Active, move.RawValue * 2f));
            }
            #endregion

            // 이동 요청은 큐에 쌓아놓고 한번에 처리
            pawnMovement.OnMove(dt);
        }

        public bool OnEvent(EventSystem.Event e)
        {
            return this.ThrowEvent(e);
        }
    }
}