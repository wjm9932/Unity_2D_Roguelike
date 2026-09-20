using Foundations;
using EventSystem;
using Pawn.Interface;
using System;

namespace Pawn.Runtime
{
    internal class Pawn : IPawn, IEventListener, IDisposable
    {
        public IPawnController PawnController => pawnController;
        public IPawnStatsBehaviour PawnStatsBehaviour => pawnStatsBehaviour;
        public IPawnBehaviour PawnBehaviour => pawnBehaviour;

        private IPawnController pawnController;
        private IPawnStatsBehaviour pawnStatsBehaviour;
        private IPawnBehaviour pawnBehaviour;
        private IPawnMovement pawnMovement;

        internal Pawn(IPawnController controller, IPawnStatsBehaviour statsBehaviour, IPawnBehaviour behaviour, IPawnMovement movement)
        {
            pawnController = controller;
            pawnStatsBehaviour = statsBehaviour;
            pawnBehaviour = behaviour;
            pawnMovement = movement;
        }

        internal void Update()
        {
            var dt = TimeManager.Instance.InGameDeltaTime;

            this.ThrowUpdate(dt);

            // 이동 요청은 큐에 쌓아놓고 한번에 처리
            pawnMovement.OnMove(dt);
        }

        public bool OnEvent(Event e)
        {
            return this.ThrowEvent(e);
        }

        public void Dispose()
        {
            pawnMovement.Dispose();
        }
    }
}