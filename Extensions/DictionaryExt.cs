using System.Collections.Generic;
using System.Linq;

public class Dictionary<TValue> : Dictionary<string, TValue> {

}

public static class DictionaryExt {

	public static bool Equals<TKey, TValue>(this IDictionary<TKey, TValue> x, IDictionary<TKey, TValue> y)
	{
		// early-exit checks
		if (null == y)
			return null == x;
		if (null == x)
			return false;
		if (object.ReferenceEquals(x, y))
			return true;
		if (x.Count != y.Count)
			return false;

		foreach(KeyValuePair<TKey, TValue> entry in x)
		{
			TValue matchingValue;
			if (y.TryGetValue(entry.Key, out matchingValue) == false)
				return false;

			if (matchingValue.Equals(y[entry.Key]) == false)
				return false;
		}

		return true;
	}

	public static T MergeLeft<T,K,V>(this T me, params IDictionary<K,V>[] others) where T : IDictionary<K,V>, new()
	{
		T newMap = new T();
		foreach (IDictionary<K,V> src in (new List<IDictionary<K,V>> { me }).Concat(others)) {
			foreach (KeyValuePair<K,V> p in src) {
				newMap[p.Key] = p.Value;
			}
		}
		return newMap;
	}

	public static TValue GetValueSafe<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key) {
		TValue value;
		dic.TryGetValue(key, out value);
		return value;
	}

    public static TValue GetValueOrNew<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key) where TValue : new()
    {
        TValue value;
        dic.TryGetValue(key, out value);
        if (value == null)
        {
            value = new TValue();
            dic[key] = value;
        }
        return value;
    }
}