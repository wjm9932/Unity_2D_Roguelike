using System;
using Assets.Scripts.Foundations;
using Assets.Scripts.StateMachine.Contracts;

namespace Assets.Scripts.StateMachine
{
    public abstract class PooledState<TState> : IDisposable, IState
        where TState : PooledState<TState>, new()
    {
        private static readonly ObjectPool<TState> pool = new ObjectPool<TState>();
        private bool isDisposed;

        public static int PoolCount => pool.Count;

        public static TState GetOrCreate()
        {
            TState state = pool.GetOrCreate();
            state.isDisposed = false;
            return state;
        }

        public abstract void Enter(IState previousState);

        public abstract void Update(float dt);

        public abstract void Exit(IState nextState);

        public void Dispose()
        {
            if (isDisposed) return;

            OnDispose();
            isDisposed = true;
            pool.Return((TState)this);
        }

        protected abstract void OnDispose();
    }
}
