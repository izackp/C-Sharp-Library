using System;
using System.Collections.Generic;

public static class ArrayExt {

	public static T SafeObjectAt<T>(this IList<T> list, int index) {
		if (index >= list.Count || index < 0) 
			return default(T);

		return list[index];
	}

    public static T[] SubArray<T>(this T[] data, int index, int length) {
        T[] result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }

    public static T[] Concat<T>(this T[] x, T[] y) {
        if (x == null) throw new ArgumentNullException("x");
        if (y == null) throw new ArgumentNullException("y");
        int oldLen = x.Length;
        Array.Resize(ref x, x.Length + y.Length);
        Array.Copy(y, 0, x, oldLen, y.Length);
        return x;
    }
}
