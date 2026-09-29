/*
 * Role hierarchy: Gameplay > Entities > Creature
 * Primary responsibility: Handles EnemyContactAttack combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
//contact damage적용할곳에  달아주면됨 적의contact damage를 주는 여러부위들에 부착
public class EnemyContactAttack : MonoBehaviour
{
    private EnemyContactDamage _owner;

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(EnemyContactDamage owner)
    {
        _owner = owner;
    }

    // Purpose: Responds to the trigger stay callback.
    private void OnTriggerStay(Collider other)
    {
        if (_owner == null)
            return;

        _owner.TryDamage(other);
    }
}
