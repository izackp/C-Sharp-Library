//Modified Version of https://github.com/mbdavid/LiteDB/blob/master/LiteDB/Mapper/Reflection/Reflection.cs
//Under MIT License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using CSharp_Library.Extensions;

namespace CSharp_Library.Utility
{
    #region Delegates

    public delegate object CreateObject();

    public delegate void GenericSetter(object target, object value);

    public delegate object GenericGetter(object obj);

    #endregion

    /// <summary>
    /// Helper class to get entity properties and map as BsonValue
    /// </summary>
    public static partial class Reflection
    {
        private static Dictionary<Type, CreateObject> _cacheCtor = new Dictionary<Type, CreateObject>();

        #region CreateInstance

        /// <summary>
        /// Create a new instance from a Type
        /// </summary>
        public static object CreateInstance(Type type)
        {
            try
            {
                if (_cacheCtor.TryGetValue(type, out CreateObject c))
                {
                    return c();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Failed to create instance for type '{type.FullName}' from assembly '{type.AssemblyQualifiedName}'. Checks if the class has a public constructor with no parameters.");
            }

            lock (_cacheCtor)
            {
                try
                {
                    if (_cacheCtor.TryGetValue(type, out CreateObject c))
                    {
                        return c();
                    }

                    if (type.GetTypeInfo().IsClass)
                    {
                        _cacheCtor.Add(type, c = CreateClass(type));
                    }
                    else if (type.GetTypeInfo().IsInterface) // some know interfaces
                    {
                        if (type.GetTypeInfo().IsGenericType)
                        {
                            var typeDef = type.GetGenericTypeDefinition();

                            if (typeDef == typeof(IList<>) || 
                                typeDef == typeof(ICollection<>) ||
                                typeDef == typeof(IEnumerable<>))
                            {
                                return CreateInstance(GetGenericListOfType(UnderlyingTypeOf(type)));
                            }
                            else if (typeDef == typeof(IDictionary<,>)) {
#if NET35 || NET40
                                var k = type.GetGenericArguments()[0];
                                var v = type.GetGenericArguments()[1];
#else
                                var k = type.GetTypeInfo().GenericTypeArguments[0];
                                var v = type.GetTypeInfo().GenericTypeArguments[1];
#endif
                                return CreateInstance(GetGenericDictionaryOfType(k, v));
                            }
                        } else {
                            c = () => { return (object)FormatterServices.GetUninitializedObject(type); };
                            _cacheCtor.Add(type, c);
                            return c();
                        }
                        
                        throw new Exception($"Failed to create instance for type '{type.FullName}' from assembly '{type.AssemblyQualifiedName}'. Checks if the class has a public constructor with no parameters.");
                    }
                    else // structs
                    {
                        _cacheCtor.Add(type, c = CreateStruct(type));
                    }

                    return c();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to create instance for type '{type.FullName}' from assembly '{type.AssemblyQualifiedName}'. Checks if the class has a public constructor with no parameters.");
                }
            }
        }

        #endregion

        #region Utils

        public static bool IsNullable(Type type)
        {
            if (!type.GetTypeInfo().IsGenericType)
                return false;
            Type g = type.GetGenericTypeDefinition();
            return g.Equals(typeof(Nullable<>));
        }

        /// <summary>
        /// Get underlying get - using to get inner Type from Nullable type
        /// </summary>
        public static Type UnderlyingTypeOf(Type type)
        {
            // works only for generics (if type is not generic, returns same type)
#if NET35 || NET40
            if (!type.IsGenericType) return type;

            return type.GetGenericArguments()[0];
#else
            if (!type.GetTypeInfo().IsGenericType) return type;

            return type.GetTypeInfo().GenericTypeArguments[0];
#endif
        }

        public static Type GetGenericListOfType(Type type)
        {
            var listType = typeof(List<>);
            return listType.MakeGenericType(type);
        }

        public static Type GetGenericDictionaryOfType(Type k, Type v)
        {
            var listType = typeof(Dictionary<,>);
            return listType.MakeGenericType(k, v);
        }

        /// <summary>
        /// Get item type from a generic List or Array
        /// </summary>
        public static Type GetListItemType(Type listType)
        {
            if (listType.IsArray)
                return listType.GetElementType();

#if NET35 || NET40
            foreach (var i in listType.GetInterfaces())
#else
            foreach (var i in listType.GetTypeInfo().ImplementedInterfaces)
#endif
            {
                if (i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
#if NET35 || NET40
                    return i.GetGenericArguments()[0];
#else
                    return i.GetTypeInfo().GenericTypeArguments[0];
#endif
                }
                // if interface is IEnumerable (non-generic), let's get from listType and not from interface
                // from #395
                else if(listType.GetTypeInfo().IsGenericType && i == typeof(IEnumerable))
                {
#if NET35 || NET40
                    return listType.GetGenericArguments()[0];
#else
                    return listType.GetTypeInfo().GenericTypeArguments[0];
#endif
                }
            }

            return typeof(object);
        }

        public enum ListType
        {
            None,
            Array,
            GenericEnumerable,
            Enumerable
        }

        /// <summary>
        /// Returns true if Type is any kind of Array/IList/ICollection/....
        /// </summary>
        public static ListType FindListType(Type type)
        {
            if (type.IsArray) return ListType.Array; //True for myType[]

            if (GetMethod(type, "Add", 1) == null)
                return ListType.None;


#if NET35 || NET40
            var list = type.GetInterfaces();
#else
            var list = type.GetTypeInfo().ImplementedInterfaces;

#endif
            foreach (var @interface in list)
            {
                if (@interface.GetTypeInfo().IsGenericType == false)
                    continue;
                if (@interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return ListType.GenericEnumerable;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
                return ListType.Enumerable;

            return ListType.None;
        }

        public static MethodInfo GetMethod(Type type, string name, int numParameters) {
            MethodInfo[] listMethods = type.GetMethods();
            foreach (MethodInfo method in listMethods) {
                if (method.Name != name)
                    continue;
                if (method.GetParameters().Length != numParameters)
                    continue;
                return method;
            }
            return null;
        }

        /// <summary>
        /// Select member from a list of member using predicate order function to select
        /// </summary>
        public static MemberInfo SelectMember(IEnumerable<MemberInfo> members, params Func<MemberInfo, bool>[] predicates)
        {
            foreach (var predicate in predicates)
            {
                var member = members.FirstOrDefault(predicate);

                if (member != null)
                {
                    return member;
                }
            }

            return null;
        }

        public static Type[] GetGenericArgumentsExt(Type type) {
#if NET35 || NET40
            Type[] genericArguments = type.GetGenericArguments();
#else
            Type[] genericArguments = type.GetTypeInfo().GenericTypeArguments;
#endif
            return genericArguments;
        }

        public static MethodInfo MethodWithAttribute<T>(MethodInfo[] listMethods) {
            foreach (MethodInfo method in listMethods) {
                if (method.GetCustomAttributes(typeof(T), true).Length > 0) {
                    return method;
                }
            }
            return null;
        }

        public static IEnumerable<MemberInfo> GetMembers(this Type type, bool includeProperties = true, bool includeFields = false, bool includeNonPublic = false) {

            BindingFlags flags = (BindingFlags.Public | BindingFlags.Instance);
            if (includeNonPublic)
                flags |= BindingFlags.NonPublic;

            if (includeProperties && includeFields) {
                MemberInfo[] properties = type.GetProperties(flags).Where(x => x.CanRead).ToArray<MemberInfo>();
                MemberInfo[] fields = type.GetNonStaticFields(flags);
                MemberInfo[] allValues = ArrayExt.Combine(properties, fields);
                return allValues;
            }

            if (includeProperties) {
                MemberInfo[] properties = type.GetProperties(flags).Where(x => x.CanRead).ToArray<MemberInfo>();
                return properties;
            }

            if (includeFields) {
                MemberInfo[] fields = type.GetNonStaticFields(flags);
                return fields;
            }

            return null;
        }

        public static PropertyInfo[] GetReadableProperties(this Type type, BindingFlags flags) {
            PropertyInfo[] properties = type.GetProperties(flags).Where(x => x.CanRead).ToArray();
            return properties;
        }

        public static FieldInfo[] GetNonStaticFields(this Type type, BindingFlags flags) {
            FieldInfo[] fields = type.GetFields(flags).Where(x => !x.Name.EndsWith("k__BackingField") && x.IsStatic == false).ToArray();
            return fields;
        }
        #endregion
    }
}