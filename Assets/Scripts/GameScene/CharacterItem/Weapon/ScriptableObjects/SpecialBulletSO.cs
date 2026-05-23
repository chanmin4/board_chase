using UnityEngine;

[CreateAssetMenu(
    fileName = "SpecialBullet",
    menuName = "Game/Weapon/Bullet/Special Bullet")]
public class SpecialBulletSO : BulletSO
{
    public override BulletAmmoType AmmoType => BulletAmmoType.Special;
}