using UnityEngine;

[CreateAssetMenu(
    fileName = "AttackBullet",
    menuName = "CharacterItem/Weapon/Bullets/Attack Bullet")]
public class AttackBulletSO : BulletSO
{
    [Header("Effect")]
    [SerializeField] private LayerMask damageTargetMask = 0;

    public override BulletAmmoType AmmoType => BulletAmmoType.Attack;
    public LayerMask DamageTargetMask => damageTargetMask;
}