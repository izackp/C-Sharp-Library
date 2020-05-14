using System.Collections.Generic;
using CSharp_Library.Extensions;

namespace CSharp_Library.Utility {
    //Note: data is not contiguous; but likely to be if initial capacity is set correctly
    public class ObjectPool<T> where T : new() {
        List<T> _poolData = new List<T>(); //Only Value Types are contiguous

        public ObjectPool(int initialCapacity) {
            for (int i = 0; i < initialCapacity; i+=1) {
                _poolData.Push(new T());
            }
        }

        public void Return(T obj) {
            _poolData.Push(obj);
        }

        public T Retrieve() {
            if (_poolData.Count > 0) {
                return _poolData.Pop();
            }
            return new T();
        }
    }
}