using UnityEngine;

public class WeaponSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string weaponId = "weapon_pistol";
    [SerializeField] private string displayName = "Pistol";

    [Header("UI")]
    [SerializeField] private Sprite icon;

    [Header("Shared")]
    [Tooltip("Left click attack shots per second.")]
    [Min(0.01f)]
    [SerializeField] private float shotsPerSecond = 2f;

    [Tooltip("Right click paint shots per second. 0이면 좌클릭 공속의 1/2을 사용합니다.")]
    [Min(0f)]
    [SerializeField] private float paintShotsPerSecondOverride = 0f;

    [Min(0.1f)]
    [SerializeField] private float maxRange = 12f;

    [Min(1)]
    [SerializeField] private int magazineSize = 6;

    [Min(0.01f)]
    [SerializeField] private float reloadDuration = 3f;
    [Tooltip("player mouse direction calculation layer mask")]
    [SerializeField] private LayerMask aimHitMask = ~0;
    [SerializeField] private bool allowFallbackPlane = true;
    [SerializeField] private float fallbackPlaneY = 0f;

    [Header("Attack")]
    [Min(0f)]
    [SerializeField] private float damage = 10f;

    [Header("Paint")]
    [Min(0.05f)]
    [SerializeField] private float paintRadiusWorld = 1.25f;

    [SerializeField] private int paintPriority = 0;

    [Header("Bullets")]
    [SerializeField] private AttackBulletSO attackBullet;
    [SerializeField] private PaintBulletSO paintBullet;

    public string WeaponId => weaponId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;

    public float ShotsPerSecond => shotsPerSecond;
    public float AttackShotsPerSecond => shotsPerSecond;
    public float PaintShotsPerSecond =>
    paintShotsPerSecondOverride > 0f
        ? paintShotsPerSecondOverride
        : shotsPerSecond * 0.5f;
    public float MaxRange => maxRange;
    public int MagazineSize => magazineSize;
    public float ReloadDuration => reloadDuration;

    public LayerMask AimHitMask => aimHitMask;
    public bool AllowFallbackPlane => allowFallbackPlane;
    public float FallbackPlaneY => fallbackPlaneY;

    public float Damage => damage;

    public float PaintRadiusWorld => paintRadiusWorld;
    public int PaintPriority => paintPriority;

    public AttackBulletSO AttackBullet => attackBullet;
    public PaintBulletSO PaintBullet => paintBullet;
}
