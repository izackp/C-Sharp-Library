using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CSharp_Library.Extensions;

namespace CSharp_Library.Utility {

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ListStructDebugAttribute : Attribute { }

    //Experimental Class to Achieve Contiguous Memory
    //Array resizing is very rudimentary 
    public class ListStruct<T> where T : struct {
        const int MinSize = 8;
        public int Count = 0;
        public T[] Data;

        public ListStruct() {
            Data = new T[MinSize];
        }

        public ListStruct(int capacity) {
            if (capacity < MinSize) {
                capacity = MinSize;
            }
            Data = new T[capacity];
        }

        public void SetCapacity(int capacity) {
            if (capacity <= Count) {
                return;
            }
            Array.Resize(ref Data, capacity);
        }

        public int NextAvailableIndex() {
            int id = Count;
            if (Count == Data.Length) {
                Array.Resize(ref Data, Count << 1);
            }
            //Data[Count] = (T)Activator.CreateInstance(typeof(T));
            Count += 1;
            return id;
        }

        public void PopLast() {
            Count -= 1;
        }

        public ref T this[int index] {
            get => ref Data[index];
            //set => SetValue(key, value);
        }

        public void Shrink() {
            int capacity = Count < MinSize ? MinSize : Count;
            SetCapacity(capacity);
        }
    }

    //Keeps track of structs in use. //TODO: Data only continues to grow. No cleaning method available.
    //For short lived classes (Particles, animations) simply check if they're in use. Perhaps sort the available ids.
    public sealed class StructPool<T> where T : struct {

        public static readonly StructPool<T> Instance = new StructPool<T>();
        public ListStruct<T> Items = new ListStruct<T>();
        public List<int> ReservedItems = new List<int>(8);
        #if DEBUG
            List<System.Reflection.FieldInfo> _nullableFields = new List<System.Reflection.FieldInfo>(8);
        #endif

        StructPool() {
            #if DEBUG
                // collect all marshal-by-reference fields.
                var fields = typeof(T).GetFields();
                for (var i = 0; i < fields.Length; i++) {
                var field = fields[i];
                var type = field.FieldType;
                if (!type.IsValueType || (Nullable.GetUnderlyingType(type) != null) && !Nullable.GetUnderlyingType(type).IsValueType) {
                    if (type != typeof(string) && !Attribute.IsDefined(field, typeof(ListStructDebugAttribute))) {
                        _nullableFields.Add(fields[i]);
                    }
                }
            }
            #endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int RequestNewId() {
            int id;
            if (ReservedItems.Count > 0) {
                id = ReservedItems.Pop();
                return id;
            }
            id = Items.NextAvailableIndex();
            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecycleById(int id) {
            #if DEBUG
                // check all marshal-by-reference typed fields for nulls.
                var obj = Items[id];
                for (var i = 0; i < _nullableFields.Count; i++) {
                if (_nullableFields[i].GetValue(obj) != null) {
                    throw new Exception(string.Format(
                        "Memory leak for \"{0}\" component: \"{1}\" field not nulled. If you are sure that it's not - mark field with [EcsIgnoreNullCheck] attribute",
                        typeof(T).Name, _nullableFields[i].Name));
                }
            }
            #endif
            ReservedItems.Push(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetExistItemById(int idx) {
            return ref Items.Data[idx];
        }

        public void SetCapacity(int capacity) {
            Items.SetCapacity(capacity);
        }

        public void Shrink() {
            Items.Shrink();
        }
    }
}
