namespace Assets.Scripts.StateMachine.Contracts
{
    public interface IState
    {
        void Enter(IState previousState);

        void Update(float dt);

        void Exit(IState nextState);

        void Dispose();
    }
}
