using System;
using System.Reflection;

public class WeakAction {
    WeakReference weakHolder;
    MethodInfo method;

    public WeakAction(Action action) {
        weakHolder = new WeakReference(action.Target);
        method = action.Method;
    }

    public void Execute() {
        object strongTarget = null;

        if (weakHolder.IsAlive)
            strongTarget = weakHolder.Target;
        method.Invoke(strongTarget, new object[] { });
    }

#if UNITY_5_3_OR_NEWER
    public static implicit operator UnityEngine.Events.UnityAction(WeakAction m) {

        return m.Execute;
    }
#endif

    public static implicit operator Action(WeakAction m) {

        return m.Execute;
    }
}