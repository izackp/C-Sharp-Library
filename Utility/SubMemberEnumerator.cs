using System;
using System.Collections;
using System.Collections.Generic;

namespace CSharp_Library.Utility {

    public class SubMemberEnumerator<O, T> : IEnumerator<T>, IEnumerable<T> {

        int _index = -1;
        Func<O, T> _predicate;
        IList<O> _list;

        public SubMemberEnumerator(IList<O> list, Func<O, T> predicate) {
            _list = list;
            _predicate = predicate;
        }

        public T Current {
            get {
                return _predicate(_list[_index]);
            }
        }

        object IEnumerator.Current {
            get {
                return Current;
            }
        }

        public void Dispose() {
        }

        public bool MoveNext() {
            if (_index + 1 >= _list.Count)
                return false;
            _index += 1;
            return true;
        }

        public void Reset() {
            _index = -1;
        }

        public IEnumerator GetEnumerator() {
            return this;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this;
        }
    }
}