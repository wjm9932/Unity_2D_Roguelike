#nullable enable

namespace Roguelike.StateMachine
{
    public interface IState
    {
        void Enter(IState? previousState);

        void Exit(IState nextState);
    }
}
