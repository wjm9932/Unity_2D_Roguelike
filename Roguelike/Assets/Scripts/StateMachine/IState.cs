namespace Assets.Scripts.StateMachine
{
    public interface IState
    {
        void Enter(IState? previousState);

        void Exit(IState nextState);
    }
}
