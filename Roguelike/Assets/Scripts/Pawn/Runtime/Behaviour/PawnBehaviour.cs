using EventSystem;

namespace Pawn.Runtime.Behaviour
{
    public class PawnBehaviour : IPawnBehaviour, IEventListener
    {
        private StateMachine.StateMachine stateMachine;

        public PawnBehaviour()
        {
            stateMachine = new StateMachine.StateMachine();
        }

        public void OnUpdate(float dt)
        {
            //stateMachine.CurrentState.Update(dt);
        }

        public bool OnEvent(Event e)
        {
            return false;
        }
    }
}
