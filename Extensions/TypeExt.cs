using System;

namespace CSharp_Library.Extensions {
    public static class TypeExt {
        // In 4.5+, TypeInfo has most of the reflection methods previously on type
        // This allows code to be shared between 3.5 && 4.5+ projects
#if NET35 || NET40
        public static Type GetTypeInfo(this Type type) {
            return type;
        }
#endif
    }
}
