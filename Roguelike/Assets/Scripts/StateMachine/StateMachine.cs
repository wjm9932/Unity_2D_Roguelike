#nullable enable

using System;

namespace Roguelike.StateMachine
{
    public sealed class StateMachine
    {
        private IState? currentState;

        public IState? CurrentState => currentState;

        public StateMachine()
        {
        }

        public StateMachine(IState initialState)
        {
            ChangeState(initialState);
        }

        public void ChangeState(IState nextState)
        {
            if (nextState == null)
            {
                throw new ArgumentNullException(nameof(nextState));
            }

            if (ReferenceEquals(currentState, nextState))
            {
                return;
            }

            IState? previousState = currentState;

            previousState?.Exit(nextState);
            currentState = nextState;
            nextState.Enter(previousState);
        }
    }
}
