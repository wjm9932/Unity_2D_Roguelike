namespace Assets.Scripts.StateMachine
{
    public interface IState
    {
        public void Enter(IState previousState);

        public void Update(float dt);

        public void Exit(IState nextState);

        public void OnDispose();
    }
}
