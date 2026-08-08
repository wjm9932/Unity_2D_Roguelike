using System;

namespace Assets.Scripts.StateMachine.Contracts
{
    public interface IState : IDisposable
    {
        void Enter(IState previousState);

        void Update(float dt);

        void Exit(IState nextState);
    }
}
