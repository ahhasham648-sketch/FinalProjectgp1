using UnityEngine;

public static class AnimatorUtil
{
    public static bool HasParameter(Animator anim, string name, AnimatorControllerParameterType type)
    {
        if (anim == null || string.IsNullOrEmpty(name) || anim.runtimeAnimatorController == null) return false;
        foreach (var p in anim.parameters)
        {
            if (p.type == type && p.name == name) return true;
        }
        return false;
    }

    public static void SetTrigger(Animator anim, string name)
    {
        if (HasParameter(anim, name, AnimatorControllerParameterType.Trigger)) anim.SetTrigger(name);
    }

    public static void ResetTrigger(Animator anim, string name)
    {
        if (HasParameter(anim, name, AnimatorControllerParameterType.Trigger)) anim.ResetTrigger(name);
    }

    public static void SetBool(Animator anim, string name, bool value)
    {
        if (HasParameter(anim, name, AnimatorControllerParameterType.Bool)) anim.SetBool(name, value);
    }

    public static void SetFloat(Animator anim, string name, float value)
    {
        if (HasParameter(anim, name, AnimatorControllerParameterType.Float)) anim.SetFloat(name, value);
    }
}
