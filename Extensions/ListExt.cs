using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgeOfWrath.Extensions
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
    }
}
