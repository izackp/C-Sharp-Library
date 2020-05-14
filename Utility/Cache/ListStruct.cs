using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CSharp_Library.Utility {

    //Not allowed in dotnetcore # -> struct MyStruct { ContiguousHandle<MyStruct> handle; }
    //Creates type safety and pool safety
    public struct ContiguousHandle {
        public int Index;
        public static implicit operator int(ContiguousHandle handle) => handle.Index;
        public static explicit operator ContiguousHandle(int i) => new ContiguousHandle() { Index = i };
    }

    public struct PoolRef<T> where T : struct, IReusable {
        public ContiguousHandle Handle;
        private ListStruct<T> _pool;

        public void Init(ContiguousHandle handle, ListStruct<T> pool) {
            Handle = handle;
            _pool = pool;
        }

        public ref T FetchRef() {
            return ref _pool[Handle];
        }

        public void ReturnToPool() {
            _pool.Return(Handle);
        }
    }

    public interface IReusable {
        void Init();
        void Clean();
        ContiguousHandle ID { get; set; }

        //bool GetIsAlive();
        //void SetIsAlive(bool isAlive);
    }

    //Experimental Class to Achieve Contiguous Memory
    public class ListStruct<T> where T : struct, IReusable {
        const int MinSize = 8;
        public int UnusedIndex = 0;
        public T[] Data;
        public ContiguousHandle[] Pointers;
        public static readonly ListStruct<T> Instance = new ListStruct<T>();

        #if DEBUG
            List<System.Reflection.FieldInfo> _nullableFields = new List<System.Reflection.FieldInfo>(8);
        #endif

        public ListStruct() {
            Data = new T[MinSize];
            Pointers = new ContiguousHandle[MinSize];
            InitData(0, MinSize);

            #if DEBUG
                // collect all marshal-by-reference fields.
                var fields = typeof(T).GetFields();
                for (var i = 0; i < fields.Length; i++) {
                var field = fields[i];
                var type = field.FieldType;
                if (!type.IsValueType || (Nullable.GetUnderlyingType(type) != null) && !Nullable.GetUnderlyingType(type).IsValueType) {
                    if (type != typeof(string)) {// && !Attribute.IsDefined(field, typeof(ListStructDebugAttribute))) {
                        _nullableFields.Add(fields[i]);
                    }
                }
            }
            #endif
        }

        public ListStruct(int capacity) {
            if (capacity < MinSize) {
                capacity = MinSize;
            }
            Data = new T[capacity];
            Pointers = new ContiguousHandle[capacity];
            InitData(0, capacity);
        }

        public void SetCapacity(int capacity) {
            if (capacity < MinSize) {
                capacity = MinSize;
            }
            if (capacity < UnusedIndex) {
                return;
            }
            var lastPos = UnusedIndex;
            Array.Resize(ref Data, capacity);
            Array.Resize(ref Pointers, capacity);
            InitData(lastPos, capacity);
        }

        void InitData(int start, int max) {
            for (int i = start; i < max; i += 1) {
                var handle = new ContiguousHandle() { Index = i };
                Data[i].Init();
                Data[i].ID = handle;
                Pointers[i] = handle;
            }
        }

        // abc represents content; L == Ceil for inuse data
        // > pop out
        // < pop in
        // [123] = pointerArr [1] = availableIndexes

        // a b c d L
        // > a
        // d b c L(a)
        //[4 2 3 1] [1]

        // a b c d L
        // > a
        // d b c L(a)
        //[4 2 3 1] [1]
        // < a
        // d b c a L
        //[4 2 3 1] 
        // > b
        // d a c Lb
        //[2 4 3 1] [2]
        //
        // a b c d L
        // > d
        // a b c L
        //[1 2 3 4] [4]

        public PoolRef<T> Rent() {
            if (UnusedIndex == Data.Length) {
                ExpandArrays();
            }
            var handle = Data[UnusedIndex].ID; //TODO: check if copy happens
            var poolRef = new PoolRef<T>();
            poolRef.Init(handle, this);
            UnusedIndex += 1;
            return poolRef;
            /*
            if (_availableIndexes.Count > 0) {
                //var unusedItem = Data[UnusedIndex];
                UnusedIndex += 1;
                var val = _availableIndexes.Pop();
                //Debug.Assert(val == test.GetID());
                //Debug.Assert(Data[Pointers[val]].GetIsAlive() == false);
                //Data[Pointers[val]].SetIsAlive(true);
                return val;
            }
            if (UnusedIndex == Data.Length) {
                ExpandArrays();
            }
            int id = UnusedIndex;
            UnusedIndex += 1;
            //Debug.Assert(Data[Pointers[id]].GetIsAlive() == false);
            //Data[Pointers[id]].SetIsAlive(true);
            return id;
            */
        }

        void ExpandArrays() {
            var newSize = (UnusedIndex >> 1) + UnusedIndex; //x1.5 //avoiding floats for determinism
            Array.Resize(ref Data, newSize);
            Array.Resize(ref Pointers, newSize);
            InitData(UnusedIndex, Data.Length);
        }

        public void Return(ContiguousHandle index) {//5
            #if DEBUG
                ref var obj = ref Data[Pointers[index].Index];
                obj.Clean();
                // check all marshal-by-reference typed fields for nulls.
                for (var i = 0; i < _nullableFields.Count; i++) {
                    if (_nullableFields[i].GetValue(obj) != null) {
                        var msg = string.Format("Memory leak for \"{0}\" component: \"{1}\" field not nulled.",
                        typeof(T).Name, _nullableFields[i].Name);
                        throw new Exception(msg);
                    }
                }
            #endif
            UnusedIndex -= 1; //4
            var ptr = Data[UnusedIndex].ID; //4
            SwapToAvail(ptr, index); //4, 1
        }

        public int PointerForPos(int index) {
            if (index >= UnusedIndex) {
                return -1;
            }
            var length = Pointers.Length;
            for (int i = 0; i < length; i+= 1) {
                if (Pointers[i] == index) {
                    return i;
                }
            }
            return -1;
        }

        //4, 1
        public void SwapToAvail(ContiguousHandle source, ContiguousHandle dest) {
            if (source == dest) {
                ref var data = ref Data[Pointers[source]];
                //data.SetIsAlive(false);
                data.Clean();
                return;
            }
            ref var ptrSrc = ref Pointers[source];
            ref var ptrDest = ref Pointers[dest];
            int resSource = ptrSrc; // 4->(4)d
            int resDest = ptrDest; // 1->(1)a
            // a b c Ld
            //[1 2 3 4]
            
            if (resSource > UnusedIndex) {
                Debug.Assert(true);
            }
            ref var dataSrc = ref Data[resSource];
            //Debug.Assert(dataSrc.GetIsAlive() == true);
            Data[resDest] = dataSrc;
            // d b c Ld
            //[1 2 3 4]
            ref var dataDest = ref Data[resDest];
            //dataSrc.SetIsAlive(false);
            dataSrc.Clean();
            dataSrc.ID = dest;
            // d b c La
            //[1 2 3 4]

            var swapIndex = ptrSrc;
            ptrSrc = ptrDest; //4 = 1(d)
            // d b c La
            //[1 2 3 1]
            ptrDest = swapIndex; //1 = 4(a)
            // d b c La
            //[4 2 3 1]
            // d b c La
            //[4 2 3 1] [1]
            /*
            Debug.Assert(dataDest.GetID() == source); //d == 4
            Debug.Assert(dataDest.GetIsAlive() == true);
            Debug.Assert(Pointers[source] == resDest);
            Debug.Assert(dataSrc.GetID() == dest);
            Debug.Assert(dataSrc.GetIsAlive() == false);
            Debug.Assert(Pointers[dest] == resSource);
            Debug.Assert(Data[Pointers[dest]].GetIsAlive() == false);
            */
        }

        public ref T this[ContiguousHandle index] {
            get => ref Data[Pointers[index]];
        }

        public void Shrink() { //broke; erases pointers
            int capacity = Data.Length / 3 * 2;
            SetCapacity(capacity);
        }

        public void Squeeze() {
            SetCapacity(UnusedIndex);
        }

        public int Count {
            get { return UnusedIndex; }
        }
    }
}
