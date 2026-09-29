/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > Spitter > StateMachine > Action
 * Primary responsibility: Defines SpitterFireProjectileActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "SpitterFireProjectileAction",
    menuName = "State Machines/Enemy Actions/Spitter/Fire Projectile")]
public class SpitterFireProjectileActionSO : EnemyFireProjectileActionSO
{
}