using EventSystem;
using Pawn.Interface;
using Pawn.Data;
using Pawn.Runtime.State;

namespace Pawn.Runtime.Behaviour
{
    public class PawnBehaviour : IPawnBehaviour, IEventListener
    {
        private StateMachine.StateMachine stateMachine;

        private IMoveRequestCollector moveRequestCollector;
        private PawnBody pawnBody;
        private IPawnStatsDefinition pawnStats;

        public PawnBehaviour(IPawnStatsDefinition stats, IMoveRequestCollector collector, PawnBody body)
        {
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

        public bool OnEvent(Event e)
        {
            if ((stateMachine.CurrentState as IEventListener)?.OnEvent(e) == true)
            {
                return true;
            }

            return false;
        }
    }
}
