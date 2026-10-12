using EventSystem;
using Pawn.Interface;
using Pawn.Runtime.State;
using UnityEngine;

namespace Pawn.Runtime.Behaviour
{
    public class PawnBehaviour : IPawnBehaviour, IEventListener
    {
        private StateMachine.StateMachine stateMachine;

        private Transform pawnTransform;
        private IMoveRequestCollector moveRequestCollector;
        private PawnBody pawnBody;
        private IPawnStatsDefinition pawnStats;

        public PawnBehaviour(IPawnStatsDefinition stats, Transform transform, IMoveRequestCollector collector, PawnBody body)
        {
            pawnTransform = transform;
            moveRequestCollector = collector;
            pawnBody = body;
            pawnStats = stats;

            stateMachine = new StateMachine.StateMachine();
            stateMachine.ChangeState(PawnIdleSprintState.Create(stats.Speed, moveRequestCollector, pawnBody));
        }

        public void OnUpdate(float dt)
        {
            stateMachine.CurrentState.Update(dt);
        }

        public bool OnEvent(EventSystem.Event e)
        {
            if ((stateMachine.CurrentState as IEventListener)?.OnEvent(e) == true)
            {
                return true;
            }

            return false;
        }
    }
}
