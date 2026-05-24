using UnityEngine;

[CreateAssetMenu(
    fileName = "PaintBullet",
    menuName = "CharacterItem/Weapon/Bullets/Paint Bullet")]
public class PaintBulletSO : BulletSO
{
    public override BulletAmmoType AmmoType => BulletAmmoType.Paint;
}