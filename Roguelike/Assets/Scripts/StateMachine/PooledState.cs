using System;
using Assets.Scripts.Foundations;

namespace Assets.Scripts.StateMachine
{
    public abstract class PooledState<TState> : IState
        where TState : PooledState<TState>, new()
    {
        private static readonly ObjectPool<TState> pool = new ObjectPool<TState>();

        public static TState GetOrCreate()
        {
            TState state = pool.GetOrCreate();
            return state;
        }

        public abstract void Enter(IState previousState);

        public abstract void Update(float dt);

        public abstract void Exit(IState nextState);

        void IState.Dispose()
        {
            (this as IDisposable)?.Dispose();

            pool.Return((TState)this);
        }
    }
}
