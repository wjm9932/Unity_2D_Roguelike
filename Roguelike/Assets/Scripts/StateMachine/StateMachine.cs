using System;
using Assets.Scripts.StateMachine.Internal;

namespace Assets.Scripts.StateMachine
{
    public sealed class StateMachine : IDisposable
    {
        private IState currentState;

        public PooledState CurrentState => currentState as PooledState;

        public void ChangeState(PooledState nextState)
        {
            if (nextState == null) return;

            IState previousState = currentState;
            IState nextInternalState = nextState;

            previousState?.Exit(nextInternalState);
            currentState = nextInternalState;
            nextInternalState.Enter(previousState);
        }

        public void Dispose()
        {
            currentState?.Dispose();
            currentState = null;
        }
    }
}
