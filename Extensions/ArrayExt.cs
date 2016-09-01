using UnityEngine;
using System.Collections.Generic;

public static class ArrayExt {

	public static T SafeObjectAt<T>(this IList<T> list, int index) {
		if (index >= list.Count) 
			return default(T);

		return list[index];
	}
}
