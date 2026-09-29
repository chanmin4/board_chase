/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Defines the HeavyBulletSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "HeavyBullet",
    menuName = "Game/Bullet Item/Basic/Heavy Bullet")]
public class HeavyBulletSO : BasicBulletSO
{
    public override BulletFamily Family => BulletFamily.Heavy;
}
