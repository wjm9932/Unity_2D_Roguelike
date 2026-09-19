using System;
using Foundations;

namespace StateMachine
{
    public abstract class PooledState<TState> : IState
        where TState : PooledState<TState>, new()
    {
        private static readonly ObjectPool<TState> pool = new();

        protected static TState GetOrCreate()
        {
            TState state = pool.GetOrCreate();
            return state;
        }

        public abstract void Enter(IState previousState);

        public abstract void Update(float dt);

        public abstract void Exit(IState nextState);

        public abstract void OnDispose();

        void IState.Dispose()
        {
            OnDispose();

            pool.Return((TState)this);
        }
    }
}
