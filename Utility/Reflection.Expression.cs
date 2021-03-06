﻿//Modified Version of https://github.com/mbdavid/LiteDB/blob/master/LiteDB/Mapper/Reflection/Reflection.Expression.cs
//Under MIT License

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace CSharp_Library.Utility {
    /// <summary>
    /// Using Expressions is the easy and fast way to create classes, structs, get/set fields/properties. But it not works in NET35
    /// </summary>
    public static partial class Reflection
    {
        public static CreateObject CreateClass(Type type)
        {
            var newType = Expression.New(type);
            var lambda = Expression.Lambda<CreateObject>(newType);
            CreateObject c = lambda.Compile();
            return c;
        }

        public static CreateObject CreateStruct(Type type)
        {
            var newType = Expression.New(type);
            var convert = Expression.Convert(newType, typeof(object));

            return Expression.Lambda<CreateObject>(convert).Compile();
        }

        public static GenericGetter CreateGenericGetter(Type type, MemberInfo memberInfo)
        {
            if (memberInfo == null) throw new ArgumentNullException("memberInfo");

            // if has no read
            if (memberInfo is PropertyInfo && (memberInfo as PropertyInfo).CanRead == false) return null;

            var obj = Expression.Parameter(typeof(object), "o");
            var accessor = Expression.MakeMemberAccess(Expression.Convert(obj, memberInfo.DeclaringType), memberInfo);

            return Expression.Lambda<GenericGetter>(Expression.Convert(accessor, typeof(object)), obj).Compile();
        }

        public static GenericSetter CreateGenericSetter(Type type, MemberInfo memberInfo)
        {
            if (memberInfo == null) throw new ArgumentNullException("propertyInfo");
            
            var fieldInfo = memberInfo as FieldInfo;
            var propertyInfo = memberInfo as PropertyInfo;

            // if is property and has no write
            if (memberInfo is PropertyInfo && propertyInfo.CanWrite == false) return null;

            // if *Structs*, use direct reflection - net35 has no Expression.Unbox to cast target
            if (type.GetTypeInfo().IsValueType)
            {
                return memberInfo is FieldInfo ?
                    (GenericSetter)fieldInfo.SetValue :
                    ((t, v) => propertyInfo.SetValue(t, v, null));
            }

            var dataType = memberInfo is PropertyInfo ?
                propertyInfo.PropertyType :
                fieldInfo.FieldType;

            return ((t, v) => {
                object convertedTarget = Convert.ChangeType(t, type);
                object convertedValue = (v == null || dataType.IsAssignableFrom(v.GetType())) ? v : Convert.ChangeType(v, dataType);
                if (propertyInfo != null) {
                    propertyInfo.SetValue(convertedTarget, convertedValue, null);
                } else {
                    fieldInfo.SetValue(convertedTarget, convertedValue);
                }
            });
        }
    }
}