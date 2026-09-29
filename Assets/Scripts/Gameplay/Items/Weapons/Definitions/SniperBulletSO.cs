/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Defines the SniperBulletSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "SniperBullet",
    menuName = "Game/Bullet Item/Basic/Sniper Bullet")]
public class SniperBulletSO : BasicBulletSO
{
    public override BulletFamily Family => BulletFamily.Sniper;
}
