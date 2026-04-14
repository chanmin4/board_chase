using UnityEngine;

public interface IInkDamageable
{
    void ApplyInkDamage(float damage, Vector3 hitPoint, GameObject source);
}
