using UnityEngine;

public class WeaponSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string weaponId = "weapon_pistol";
    [SerializeField] private string displayName = "Pistol";

    [Header("UI")]
    [SerializeField] private Sprite icon;

    [Header("Aim")]
    [SerializeField] private LayerMask aimHitMask = ~0;
    [SerializeField] private bool allowFallbackPlane = true;
    [SerializeField] private float fallbackPlaneY = 0f;

    [Header("Stat Modifiers")]
    [Tooltip("Weapon bonus only. Default pistol should usually leave this empty.")]
    [SerializeField] private PlayerStatModifier[] statModifiers;

    public string WeaponId => weaponId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;

    public LayerMask AimHitMask => aimHitMask;
    public bool AllowFallbackPlane => allowFallbackPlane;
    public float FallbackPlaneY => fallbackPlaneY;

    public PlayerStatModifier[] StatModifiers => statModifiers;
}