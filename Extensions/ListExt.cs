﻿using System;
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
    }
}
