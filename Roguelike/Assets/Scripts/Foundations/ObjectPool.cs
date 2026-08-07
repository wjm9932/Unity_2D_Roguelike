using System;
using System.Collections.Generic;

namespace Assets.Scripts.Foundations
{
    public sealed class ObjectPool<T> where T : class, new()
    {
        private readonly Stack<T> items;

        public int Count => items.Count;

        public ObjectPool(int initialCapacity = 0)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            items = new Stack<T>(initialCapacity);
        }

        public T GetOrCreate()
        {
            if (items.Count > 0)
            {
                return items.Pop();
            }

            return new T();
        }

        public void Return(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            items.Push(item);
        }

        public void Clear()
        {
            items.Clear();
        }
    }
}
