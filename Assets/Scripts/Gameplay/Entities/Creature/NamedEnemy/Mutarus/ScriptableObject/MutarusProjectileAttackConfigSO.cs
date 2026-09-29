/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Mutarus > ScriptableObject
 * Primary responsibility: Defines the MutarusProjectileAttackConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "Mutarus_ProjectileAttack_Config",
    menuName = "ScriptableObject/GameScene/Enemy/Definition_Config/NamedEnemy/Mutarus/Projectile Attack Config")]
public class MutarusProjectileAttackConfigSO : NamedProjectileBurstConfigSO
{
}
