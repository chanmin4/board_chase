using UnityEngine;

public class EnemyContactAttackbox : MonoBehaviour
{
    private EnemyContactDamage _owner;

    public void Initialize(EnemyContactDamage owner)
    {
        _owner = owner;
    }

    private void OnTriggerStay(Collider other)
    {
        if (_owner == null)
            return;

        _owner.TryDamage(other);
    }
}
