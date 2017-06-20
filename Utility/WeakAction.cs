using System;
using System.Reflection;

//If there are any problems refer to an alternative implementation here:
//https://codereview.stackexchange.com/questions/8807/weakaction-implementation
public class WeakAction {
    WeakReference weakHolder;
    MethodInfo method;

    public WeakAction(Action action) {
        weakHolder = new WeakReference(action.Target);
        method = action.Method;
    }

    static readonly object[] sNoParams = new object[] { };

    public void Execute() {
        object strongTarget = null;

        if (weakHolder.IsAlive)
            strongTarget = weakHolder.Target;
        method.Invoke(strongTarget, sNoParams);
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