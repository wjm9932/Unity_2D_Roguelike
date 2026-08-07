namespace Assets.Scripts.StateMachine
{
    public sealed class StateMachine
    {
        private IState currentState;

        public IState CurrentState => currentState;

        public void ChangeState(IState nextState)
        {
            if (nextState == null) return;

            IState? previousState = currentState;

            previousState?.Exit(nextState);
            currentState = nextState;
            nextState.Enter(previousState);
        }
    }
}
