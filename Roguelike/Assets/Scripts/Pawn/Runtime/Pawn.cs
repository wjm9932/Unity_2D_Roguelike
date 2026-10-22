using Foundations;
using EventSystem;
using Pawn.Interface;
using System;
using Kinematic.Runtime;

namespace Pawn.Runtime
{
    internal class Pawn : IPawn, IEventListener, IDisposable
    {
        public IPawnController PawnController => pawnController;
        public IPawnStatsBehaviour PawnStatsBehaviour => pawnStatsBehaviour;
        public IPawnBehaviour PawnBehaviour => pawnBehaviour;

        private readonly IPawnController pawnController;
        private readonly IPawnStatsBehaviour pawnStatsBehaviour;
        private readonly IPawnBehaviour pawnBehaviour;
        private readonly IPawnMoveProcessor pawnMoveProcessor;
        private readonly KinematicBody kinematicBody;

        internal Pawn(IPawnController controller, IPawnStatsBehaviour statsBehaviour, IPawnBehaviour behaviour, IPawnMoveProcessor moveProcessor, KinematicBody body)
        {
            pawnController = controller;
            pawnStatsBehaviour = statsBehaviour;
            pawnBehaviour = behaviour;
            pawnMoveProcessor = moveProcessor;
            kinematicBody = body;

            KinematicWorld.Register(kinematicBody);
            this.RegisterConsumeMoveRequest();
        }

        internal void Update()
        {
            var dt = TimeManager.Instance.InGameDeltaTime;

            this.ThrowUpdate(dt);
        }

        internal void ConsumeMoveRequest()
        {
            var dt = TimeManager.Instance.InGameDeltaTime;

            // 이동 요청은 큐에 쌓아놓고 한번에 처리
            pawnMoveProcessor.OnMove(dt);
        }

        public bool OnEvent(Event e)
        {
            return this.ThrowEvent(e);
        }

        public void Dispose()
        {
            this.UnregisterConsumeMoveRequest();
            KinematicWorld.Unregister(kinematicBody);
            pawnMoveProcessor.Dispose();
            // Controller, Stats, Behaviour Dispose 호출
        }
    }
}
