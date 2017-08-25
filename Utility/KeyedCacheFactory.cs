using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharp_Library.Utility {
    /// <summary>
    /// Pairs a builder callback with a Dictionary, so if an instance doesn't exist for a key then the class can 'build it' then cache it.
    /// </summary>
    /// <typeparam name="K">Key</typeparam>
    /// <typeparam name="T">Value</typeparam>
    public class KeyedCacheFactory<K, T> {

        public Func<K, T> Builder = (K key) => default(T);
        public Dictionary<K, T> PoolData = new Dictionary<K, T>();

        public void Add(K key, T obj) {
            if (key == null)
                return;

            PoolData.Add(key, obj);
        }

        public T GetInstance(K key) {
            if (key == null)
                return default(T);

            T value;
            if (PoolData.TryGetValue(key, out value)) {
                return value;
            }
            return default(T);
        }

        public bool GetInstance(K key, out T ret) {
            if (key == null) {
                ret = default(T);
                return false;
            }

            return PoolData.TryGetValue(key, out ret);
        }

        public void Remove(K key) {
            PoolData.Remove(key);
        }

        public T GetOrBuildInstance(K key) {
            return GetOrBuildInstance(key, Builder);
        }

        public T GetOrBuildInstance(K key, Func<K, T> CustomBuilder) {
            T value;
            if (PoolData.TryGetValue(key, out value)) {
                return value;
            }
            value = CustomBuilder(key);
            PoolData[key] = value;
            return value;
        }
    }
}
