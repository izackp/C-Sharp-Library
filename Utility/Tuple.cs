﻿
//TODO: Tuple was introduce in C#7 so we need check better than NET35
#if NET35
namespace CSharp_Library.Utility {

    public class Tuple<T1, T2> {
        public T1 First { get; private set; }
        public T2 Second { get; private set; }
        internal Tuple(T1 first, T2 second) {
            First = first;
            Second = second;
        }
    }

    public static class Tuple {
        public static Tuple<T1, T2> Create<T1, T2>(T1 first, T2 second) {
            var tuple = new Tuple<T1, T2>(first, second);
            return tuple;
        }
    }
}
#endif