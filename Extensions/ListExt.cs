using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharp_Library.Extensions
{
    public static class ListExt
    {
        public static void Push<T>(this IList<T> list, T item)
        {
            list.Add(item);
        }

        public static T Pop<T>(this IList<T> list)
        {
            int pos = list.Count() - 1;
            T obj = list[pos];
            list.RemoveAt(pos);
            return obj;
        }

        static Random sRnd = new Random(DateTime.Now.Millisecond);

        public static T PopRandom<T>(this IList<T> list)
        {
            int pos = sRnd.Next(0, list.Count());
            T obj = list[pos];
            list.RemoveAt(pos);
            return obj;
        }

        public static bool RemoveFirst<T>(this List<T> list, Predicate<T> predicate) {
            int index = list.FindIndex(predicate);
            if (index == -1)
                return false;
            list.RemoveAt(index);
            return true;
        }

        public static void AddSorted<T>(this List<T> list, T item) where T : IComparable<T> {
            if (list.Count == 0) {
                list.Add(item);
                return;
            }
            if (list[list.Count - 1].CompareTo(item) <= 0) {
                list.Add(item);
                return;
            }
            if (list[0].CompareTo(item) >= 0) {
                list.Insert(0, item);
                return;
            }
            int index = list.BinarySearch(item);
            if (index < 0)
                index = ~index;
            list.Insert(index, item);
        }

        public static void AddSorted<T>(this List<T> list, T item, Comparison<T> comparer) {
            int index = list.SortedIndex(item, comparer);
            list.Insert(index, item);
        }

        public static int SortedIndex<T>(this List<T> list, T item, Comparison<T> comparer) {
            if (list.Count == 0)
                return 0;

            if (comparer(list[list.Count - 1], item) <= 0)
                return list.Count;

            if (comparer(list[0], item) >= 0)
                return 0;

            int index = list.BinarySearch(item, comparer);
            if (index < 0)
                index = ~index;
            return index;
        }

        //With a linear search we can operate on lists containing different types as well as null values
        public static int LinearSearch<T>(this List<object> list, T other, Comparison<T> comparer) where T : class {
            for (int i = 0; i < list.Count; i += 1) {
                object eachItem = list[i];
                if (eachItem == null)
                    continue;
                T castedItem = eachItem as T;
                if (castedItem == null)
                    continue;
                int comparisonResult = comparer(castedItem, other);
                if (comparisonResult >= 0)
                    return i;
            }
            return list.Count;
        }

        public static int LinearSearch<T>(this List<T> list, T other, Comparison<T> comparer) where T : class {
            for (int i = 0; i < list.Count; i += 1) {
                T eachItem = list[i];
                if (eachItem == null)
                    continue;
                int idk = comparer(eachItem, other);
                if (idk >= 0)
                    return i;
            }
            return list.Count;
        }

        public static int LinearSearch<T>(this List<object> list, Func<T, int> compare) where T : class {
            Comparison<T> newCompare = (a, b) => compare(a);
            return list.LinearSearch(null, newCompare);
        }

        public static int LinearSearch<T>(this List<T> list, Func<T, int> compare) where T : class {
            Comparison<T> newCompare = (a, b) => compare(a);
            return list.LinearSearch(null, newCompare);
        }

        public static int BinarySearch<T>(this List<T> list, T item, Comparison<T> compare) {
            return BinarySearch(list.ToArray(), 0, list.Count, item, compare);
        }

        public static int BinarySearch<T>(this List<T> list, Func<T, int> compare) {
            Comparison<T> newCompare = (a, b) => compare(a);
            return BinarySearch(list.ToArray(), 0, list.Count, default(T), newCompare);
        }
        
        public static T BinarySearchOrDefault<T>(this List<T> list, T item, Comparison<T> compare) {
            int i = list.BinarySearch(item, compare);
            if (i >= 0)
                return list[i];
            return default(T);
        }

        public static List<int> BinarySearchMultiple<T>(this List<T> list, T item, Comparison<T> compare) {
            var results = new List<int>();
            int i = list.BinarySearch(item, compare);
            if (i >= 0) {
                results.Add(i);
                int below = i;
                while (--below >= 0) {
                    int belowIndex = compare(list[below], item);
                    if (belowIndex < 0)
                        break;
                    results.Add(belowIndex);
                }

                int above = i;
                while (++above < list.Count) {
                    int aboveIndex = compare(list[above], item);
                    if (aboveIndex > 0)
                        break;
                    results.Add(aboveIndex);
                }
            }
            return results;
        }

        public static Predicate<object> FilterByType<T>(this Predicate<T> predicate) where T : class {
            return (x) => {
                T value = x as T;
                if (value == null)
                    return false;
                return predicate(value);
            };
        }

        public static T Find<T>(this List<object> list, Predicate<T> predicate) where T : class {
            return (T)list.Find(FilterByType(predicate));
        }

        public static int LinearSearch<T>(this List<object> list, Predicate<T> predicate) where T : class {
            return list.FindIndex(FilterByType(predicate));
        }

        // No idea why the system implementation is passing value to the first parameter instead of the second
        static int BinarySearch<T>(this T[] array, int index, int length, T value, Comparison<T> comparer) {

            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi) {
                int i = lo + ((hi - lo) >> 1);
                int order = comparer(array[i], value); 

                if (order == 0) return i;
                if (order < 0) {
                    lo = i + 1;
                } else {
                    hi = i - 1;
                }
            }

            return ~lo;
        }
    }

    public class ComparisonComparer<T> : IComparer<T> {
        private readonly Comparison<T> comparison;

        public ComparisonComparer(Func<T, T, int> compare) {
            if (compare == null)
                throw new ArgumentNullException("comparison");

            comparison = new Comparison<T>(compare);
        }

        public ComparisonComparer(Comparison<T> compare) {
            if (compare == null)
                throw new ArgumentNullException("comparison");

            comparison = compare;
        }

        public int Compare(T x, T y) {
            return comparison(x, y);
        }
    }
}
