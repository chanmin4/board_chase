/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Action
 * Primary responsibility: Defines NamedEnemyFireProjectileBurstActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "NamedEnemyFireProjectileBurstAction",
    menuName = "State Machines/Named Enemy Actions/Fire Projectile Burst")]
public class NamedEnemyFireProjectileBurstActionSO : EnemyFireProjectileActionSO
{
}