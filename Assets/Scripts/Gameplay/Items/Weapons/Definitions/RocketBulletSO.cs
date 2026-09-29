/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Defines the RocketBulletSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "RocketBullet",
    menuName = "Game/Bullet Item/Basic/Rocket")]
public class RocketBulletSO : BasicBulletSO
{
    public override BulletFamily Family => BulletFamily.Rocket;
}
