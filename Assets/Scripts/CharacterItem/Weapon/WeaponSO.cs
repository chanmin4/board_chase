using UnityEngine;

public class WeaponSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string weaponId = "weapon_pistol";
    [SerializeField] private string displayName = "Pistol";

    [Header("Shared")]
    [Min(0.01f)]
    [SerializeField] private float shotsPerSecond = 2f;

    [Min(0.1f)]
    [SerializeField] private float maxRange = 12f;

    [Min(1f)]
    [SerializeField] private int magazineSize = 6;

    [Min(1f)]
    [SerializeField] private float reloadDuration = 3;

    [SerializeField] private LayerMask aimHitMask = ~0;
    [SerializeField] private bool allowFallbackPlane = true;
    [SerializeField] private float fallbackPlaneY = 0f;

    [Header("Attack")]
    [Min(0f)]
    [SerializeField] private float damage = 10f;

    [SerializeField] private LayerMask damageHitMask = ~0;

    [Header("Paint")]
    [Min(0.05f)]
    [SerializeField] private float paintRadiusWorld = 1.25f;

    [SerializeField] private int paintPriority = 0;

    public string WeaponId => weaponId;
    public string DisplayName => displayName;
    public float ShotsPerSecond => shotsPerSecond;
    public int MagazineSize => magazineSize;
    public float ReloadDuration => reloadDuration;
    public float MaxRange => maxRange;
    public LayerMask AimHitMask => aimHitMask;
    public bool AllowFallbackPlane => allowFallbackPlane;
    public float FallbackPlaneY => fallbackPlaneY;
    public float Damage => damage;
    public LayerMask DamageHitMask => damageHitMask;
    public float PaintRadiusWorld => paintRadiusWorld;
    public int PaintPriority => paintPriority;
}