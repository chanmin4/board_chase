using UnityEngine;

[CreateAssetMenu(
    fileName = "AttackBullet",
    menuName = "CharacterItem/Weapon/Bullets/Attack Bullet")]
public class AttackBulletSO : BulletSO
{
    [Header("Effect")]
    [Tooltip("damage to target layer mask")]
    [SerializeField] private LayerMask damageTargetMask = 0;

    [Header("2.5D Damage Hit")]
    [SerializeField] private bool useFlatDamageHit = true;
    [SerializeField] private float flatHitHalfHeight = 1.4f;

    public LayerMask DamageTargetMask => damageTargetMask;
    public bool UseFlatDamageHit => useFlatDamageHit;
    public float FlatHitHalfHeight => flatHitHalfHeight;
}
