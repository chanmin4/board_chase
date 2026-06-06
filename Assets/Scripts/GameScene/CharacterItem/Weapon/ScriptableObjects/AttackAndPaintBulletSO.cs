using UnityEngine;

[CreateAssetMenu(
    fileName = "AttackAndPaintBullet",
    menuName = "CharacterItem/Weapon/Bullets/Attack And Paint Bullet")]
public class AttackAndPaintBulletSO : AttackBulletSO
{
    public override BulletAmmoType AmmoType => BulletAmmoType.AttackAndPaint;
}
