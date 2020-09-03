using System.Collections.Generic;
using CSharp_Library.Extensions;

namespace CSharp_Library.Utility {
    public interface ICacheableInstance {
        bool IsCached { get; set; } //Sometimes we need to check if the instance is 'alive.' Especially with callbacks.
        int ID { get; set; }

        void Clear();
    }

    //TODO: Maybe I can split off the 'ID' functionality.. 
    //Or just remove it completely and just use a static variable on a per class basis...
    //but then I lose the ability to have multiple pools of instances 
    //Which may not be necessary to have..
    /// <summary>
    /// A simple index cache designed for unique instances. Hence the 'ID' field.
    /// </summary>
    /// <typeparam name="T">Instance Type</typeparam>
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
