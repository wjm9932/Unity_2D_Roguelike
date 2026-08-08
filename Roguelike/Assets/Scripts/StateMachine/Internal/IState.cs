using System;

namespace Assets.Scripts.StateMachine.Internal
{
    internal interface IState : IDisposable
    {
        void Enter(IState previousState);

        void Update(float dt);

        void Exit(IState nextState);
    }
}
