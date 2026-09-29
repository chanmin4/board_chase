/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Defines the LightBulletSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "LightBullet",
    menuName = "Game/Bullet Item/Basic/Light Bullet")]
public class LightBulletSO : BasicBulletSO
{
    public override BulletFamily Family => BulletFamily.Light;
}
