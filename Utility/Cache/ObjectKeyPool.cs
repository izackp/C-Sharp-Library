using System.Collections.Generic;
using CSharp_Library.Extensions;

namespace CSharp_Library.Utility {

    public class ObjectKeyPool {

        Dictionary<string, List<object>> _poolData = new Dictionary<string, List<object>>();

        public void Add(string key, object obj) {
            if (key == null)
                return;

            List<object> listForKey = _poolData.GetValueSafe(key);
            if (listForKey == null) {
                listForKey = new List<object>();
                _poolData[key] = listForKey;
            }
            listForKey.Add(obj);
        }

        public object Retrieve(string key) {
            if (key == null)
                return null;

            List<object> listForKey = _poolData.GetValueSafe(key);
            if (listForKey == null)
                return null;

            if (listForKey.Count == 0)
                return null;

            object obj = listForKey[listForKey.Count - 1];
            listForKey.RemoveAt(listForKey.Count - 1);
            return obj;
        }
    }
}