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
        private IPawnStatsDefinition pawnStats;

        public PawnBehaviour(Transform transform, IMoveRequestCollector receiver, IPawnStatsDefinition stats)
        {
            pawnTransform = transform;
            moveRequestCollector = receiver;
            pawnStats = stats;

            stateMachine = new StateMachine.StateMachine();
            stateMachine.ChangeState(PawnIdleSprintState.Create(stats.Speed, moveRequestCollector));
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
