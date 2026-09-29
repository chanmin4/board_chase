/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > ScriptableObject
 * Primary responsibility: Defines the NamedProjectileBurstConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "NamedProjectileBurstConfig",
    menuName = "ScriptableObject/GameScene/Enemy/Definition_Config/Named Projectile Burst Config")]
public class NamedProjectileBurstConfigSO : EnemyFireProjectileConfigSO
{
}