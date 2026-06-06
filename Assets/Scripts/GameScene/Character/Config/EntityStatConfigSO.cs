using UnityEngine;

public abstract class EntityStatConfigSO : ScriptableObject
{
    public abstract float InitialHealth { get; }
    public abstract float ReferenceMoveSpeed { get; }

    public virtual float ResolveInitialHealth()
    {
        return Mathf.Max(1f, InitialHealth);
    }

    public virtual float ResolveReferenceMoveSpeed()
    {
        return Mathf.Max(0.01f, ReferenceMoveSpeed);
    }
}