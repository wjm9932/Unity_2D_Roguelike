using System;
using Assets.Scripts.Foundations;
using Assets.Scripts.StateMachine.Internal;

namespace Assets.Scripts.StateMachine
{
    public abstract class PooledState : IDisposable, IState
    {
        private bool isDisposed;

        internal PooledState()
        {
        }

        public abstract void Enter(PooledState previousState);

        public abstract void Update(float dt);

        public abstract void Exit(PooledState nextState);

        public void Dispose()
        {
            if (isDisposed) return;

            OnDispose();
            isDisposed = true;
            ReturnToPool();
        }

        protected abstract void OnDispose();

        private protected abstract void ReturnToPool();

        internal void PrepareForUse()
        {
            isDisposed = false;
        }

        void IState.Enter(IState previousState)
        {
            Enter(previousState as PooledState);
        }

        void IState.Exit(IState nextState)
        {
            Exit(nextState as PooledState);
        }
    }

    public abstract class PooledState<TState> : PooledState
        where TState : PooledState<TState>, new()
    {
        private static readonly ObjectPool<TState> pool = new ObjectPool<TState>();

        public static int PoolCount => pool.Count;

        public static TState GetOrCreate()
        {
            TState state = pool.GetOrCreate();
            state.PrepareForUse();
            return state;
        }

        private protected sealed override void ReturnToPool()
        {
            pool.Return((TState)this);
        }
    }
}
