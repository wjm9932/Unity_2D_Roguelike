using System;

namespace StateMachine
{
    public sealed class StateMachine : IDisposable
    {
        private IState currentState;

        public IState CurrentState => currentState;

        public void ChangeState(IState nextState)
        {
            if (nextState == null) return;

            var previousState = currentState;

            previousState?.Exit(nextState);
            currentState = nextState;
            nextState.Enter(previousState);

            previousState?.Dispose();
        }

        public void Dispose()
        {
            currentState?.Exit(null);
            currentState?.Dispose();
            currentState = null;
        }
    }
}
