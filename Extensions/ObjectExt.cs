using System;

namespace CSharp_Library.Extensions {
    public static class ObjectExt {

        //Because I hate so many parathesis T final = (T)((WeakReference)context).Target;
        public static T WeakToStrong<T>(this object obj) where T : class {
            WeakReference weak = (WeakReference)obj;
            if (weak.IsAlive == false)
                return null;
            return (T)weak.Target;
        }
    }
}