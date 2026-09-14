using UnityEngine;
using Foundations;
using Pawn.Interface;
using EventSystem;
using Pawn.Runtime.Controller;
using Pawn.Runtime;
using Pawn.Data;
using System;

namespace Pawn
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class Pawn : MonoBehaviour, IEventListener
    {
        public IPawnController PawnController => pawnController;
        public IPawnBehaviour PawnBehaviour => pawnBehaviour;

        private IPawnController pawnController;
        private IPawnBehaviour pawnBehaviour;

        private IPawnMovement pawnMovement;

        [Obsolete("지금은 Pawn 생성 로직이 없어 SerializeField로 선언하지만 추후 주입하는 방식으로 수정 예정")]
        [SerializeField] private PawnDefinition pawnDefinition;

        private void Awake()
        {
            // FIXME: 일단 메뉴얼만. 이후 분기쳐서 controller 할당
            pawnController = new ManualPawnController();

            pawnBehaviour = new PawnBehaviour();

            pawnMovement = new PawnMovement(GetComponent<Rigidbody2D>(), pawnDefinition.PawnStatsDefinition.AccelerationCurve);
        }

        private void Start()
        {

        }

        private void Update()
        {
            var dt = TimeManager.Instance.InGameDeltaTime;

            this.ThrowUpdate(dt);

            // 이동 요청은 큐에 쌓아놓고 한번에 처리
            pawnMovement.OnMove(dt);
        }

        public bool OnEvent(EventSystem.Event e) => this.ThrowEvent(e);
    }
}