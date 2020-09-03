using System;
using System.Collections.Generic;

namespace CSharp_Library.Extensions {
    public static class ArrayExt {

        public static T SafeObjectAt<T>(this IList<T> list, int index) {
            if (index >= list.Count || index < 0)
                return default(T);

            return list[index];
        }
        
        public static T Last<T>(this T[] list) {
            return list[list.Length - 1];
        }

        public static T Last<T>(this IList<T> list) {
            return list[list.Count - 1];
        }

        public static T LastOrNull<T>(this IList<T> list) {
            int index = list.Count;
            if (index == 0)
                return default(T);
            return list[list.Count - 1];
        }

        public static T PopLast<T>(this IList<T> list) {
            int index = list.Count;
            if (index == 0)
                return default(T);
            index -= 1;
            T instance = list[index];
            list.RemoveAt(index);
            return instance;
        }

        public static T[] SubArray<T>(this T[] data, int index, int length) {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static void Concat<T>(this T[] x, T[] y) {
            if (x == null) throw new ArgumentNullException("x");
            if (y == null) throw new ArgumentNullException("y");
            int oldLen = x.Length;
            Array.Resize(ref x, x.Length + y.Length);
            Array.Copy(y, 0, x, oldLen, y.Length);
        }

        public static T[] Combine<T>(this T[] x, T[] y) {
            if (x == null) throw new ArgumentNullException("x");
            if (y == null) throw new ArgumentNullException("y");
            T[] newArray = new T[x.Length + y.Length];

            Array.Copy(x, 0, newArray, 0, x.Length);
            Array.Copy(y, 0, newArray, x.Length, y.Length);
            return newArray;
        }

        public static R[] Map<T, R>(this T[] target, Func<T,R> func) {
            var result = new R[target.Length];
            for(int i = 0; i < target.Length; i+=1) {
                result[i] = func(target[i]);
            }
            return result;
        }
        /*
        public static T First<T>(this IEnumerable<T> list, T altValue) {
            var enumerator = list.GetEnumerator();
            var hasValue = enumerator.MoveNext();
            if (hasValue == false) {
                return altValue;
            }
            return enumerator.Current;
        }*/
        
    }
}