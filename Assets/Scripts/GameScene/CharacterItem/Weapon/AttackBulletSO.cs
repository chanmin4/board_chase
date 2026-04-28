using UnityEngine;

[CreateAssetMenu(
    fileName = "AttackBullet",
    menuName = "CharacterItem/Weapon/Bullets/Attack Bullet")]
public class AttackBulletSO : BulletSO
{
    [Header("Effect")]
    [Tooltip("damage to target layer mask")]
    [SerializeField] private LayerMask damageTargetMask = 0;

    public LayerMask DamageTargetMask => damageTargetMask;
}
