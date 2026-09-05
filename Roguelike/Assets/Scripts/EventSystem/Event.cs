using Foundations;

namespace EventSystem
{
    public abstract class Event
    {
        public abstract void Dispose();
    }

    public abstract class PooledEvent<TEvent> : Event
        where TEvent : PooledEvent<TEvent>, new()
    {
        private static readonly ObjectPool<TEvent> pool = new();

        public static TEvent GetOrCreate() => pool.GetOrCreate();

        public virtual void OnDispose() { }

        public sealed override void Dispose()
        {
            OnDispose();

            pool.Return((TEvent)this);
        }
    }
}
