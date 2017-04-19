using System.Collections.Generic;
using CSharp_Library.Extensions;

namespace CSharp_Library.Utility {
    public interface ICacheableInstance {
        bool IsCached { get; set; }
        int ID { get; set; }

        void Clear();
    }

    public class InstanceCache<T> where T: ICacheableInstance, new() {
        int lastId = 0;
        List<T> cache = new List<T>();

        public T CachedInstance() {
            T instance = default(T);
            lock (cache) {
                instance = cache.PopLast();
                if (instance != null) {
                    instance.IsCached = false;
                    return instance;
                }

                lastId += 1;
                instance = new T();
                instance.ID = lastId;
                cache.Add(instance);
            }
            return instance;
        }

        public void ReturnInstance(T instance) {
            instance.Clear();
            instance.IsCached = true;
            lock (cache) {
                cache.Add(instance);
            }
        }
    }
}
