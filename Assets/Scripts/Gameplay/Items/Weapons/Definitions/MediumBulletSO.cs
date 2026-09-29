/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Defines the MediumBulletSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "MediumBullet",
    menuName = "Game/Bullet Item/Basic/Medium Bullet")]
public class MediumBulletSO : BasicBulletSO
{
    public override BulletFamily Family => BulletFamily.Medium;
}
