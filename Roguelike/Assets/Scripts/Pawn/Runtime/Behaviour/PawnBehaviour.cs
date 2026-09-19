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
        private IPawnStatsDefinition pawnStats;
        private IMoveRequestReceiver moveRequestReceiver; 

        public PawnBehaviour(Transform transform, IMoveRequestReceiver receiver, IPawnStatsDefinition stats)
        {
            pawnTransform = transform;
            pawnStats = stats;
            moveRequestReceiver = receiver;

            stateMachine = new StateMachine.StateMachine();
            stateMachine.ChangeState(PawnIdleSprintState.Create(stats.Speed, moveRequestReceiver));
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
