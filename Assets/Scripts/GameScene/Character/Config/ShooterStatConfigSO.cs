using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public sealed class ShooterStatSettings
{
    [Header("Weapon Base")]
    [FormerlySerializedAs("attackDamage")]
    [FormerlySerializedAs("_damage")]
    [SerializeField, Min(0f)] private float _attackDamage = 10f;

    [FormerlySerializedAs("maxRange")]
    [FormerlySerializedAs("_maxRange")]
    [SerializeField, Min(0.1f)] private float _maxRange = 12f;

    [FormerlySerializedAs("shotsPerSecond")]
    [FormerlySerializedAs("attackShotsPerSecond")]
    [FormerlySerializedAs("_shotsPerSecond")]
    [SerializeField, Min(0.01f)] private float _shotsPerSecond = 2f;

    [FormerlySerializedAs("reloadDurationSeconds")]
    [FormerlySerializedAs("_reloadDurationSeconds")]
    [SerializeField, Min(0.01f)] private float _reloadDurationSeconds = 1.2f;

    [FormerlySerializedAs("magazineSize")]
    [FormerlySerializedAs("_magazineSize")]
    [SerializeField, Min(1)] private int _magazineSize = 6;

    [Header("Paint Base")]
    [FormerlySerializedAs("paintRadius")]
    [FormerlySerializedAs("_paintRadius")]
    [SerializeField, Min(0f)] private float _paintRadius = 1.25f;

    [FormerlySerializedAs("paintPriority")]
    [FormerlySerializedAs("_paintPriority")]
    [SerializeField] private int _paintPriority = 0;

    [Header("Aim")]
    [SerializeField, Min(0.01f)] private float _aimSpeed = 8f;
    [SerializeField, Min(0.1f)] private float _aimRangeMultiplier = 1.25f;
    [SerializeField, Range(0.05f, 1f)] private float _aimMoveSpeedMultiplier = 0.6f;
    [SerializeField, Min(0f)] private float _hipFireSpreadAngleDeg = 6f;
    [SerializeField, Min(0f)] private float _aimSpreadAngleDeg = 0.5f;
    [SerializeField, Min(0f)] private float _recoilAngleDeg = 1f;
    [SerializeField, Min(0f)] private float _recoilRecoverySpeedDegPerSecond = 18f;

    [Header("Sound")]
    [FormerlySerializedAs("gunshotSoundRadius")]
    [FormerlySerializedAs("_gunshotSoundRadius")]
    [SerializeField, Min(0f)] private float _gunshotSoundRadius = 16f;

    [FormerlySerializedAs("soundInvestigateDelaySeconds")]
    [FormerlySerializedAs("_soundInvestigateDelaySeconds")]
    [SerializeField, Min(0f)] private float _soundInvestigateDelaySeconds = 0.25f;

    [FormerlySerializedAs("footstepSoundRadius")]
    [FormerlySerializedAs("_footstepSoundRadius")]
    [SerializeField, Min(0f)] private float _footstepSoundRadius = 5f;

    [FormerlySerializedAs("footstepSoundInterval")]
    [FormerlySerializedAs("_footstepSoundInterval")]
    [SerializeField, Min(0.05f)] private float _footstepSoundInterval = 0.35f;

    public float AttackDamage => Mathf.Max(0f, _attackDamage);
    public float Damage => AttackDamage;
    public float MaxRange => Mathf.Max(0.1f, _maxRange);
    public float ShotsPerSecond => Mathf.Max(0.01f, _shotsPerSecond);
    public float ReloadDurationSeconds => Mathf.Max(0.01f, _reloadDurationSeconds);
    public int MagazineSize => Mathf.Max(1, _magazineSize);
    public float PaintRadius => Mathf.Max(0f, _paintRadius);
    public int PaintPriority => _paintPriority;

    public float AimSpeed => Mathf.Max(0.01f, _aimSpeed);
    public float AimRangeMultiplier => Mathf.Max(0.1f, _aimRangeMultiplier);
    public float AimMoveSpeedMultiplier => Mathf.Clamp(_aimMoveSpeedMultiplier, 0.05f, 1f);
    public float HipFireSpreadAngleDeg => Mathf.Max(0f, _hipFireSpreadAngleDeg);
    public float AimSpreadAngleDeg => Mathf.Max(0f, _aimSpreadAngleDeg);
    public float RecoilAngleDeg => Mathf.Max(0f, _recoilAngleDeg);
    public float RecoilRecoverySpeedDegPerSecond => Mathf.Max(0f, _recoilRecoverySpeedDegPerSecond);

    public float GunshotSoundRadius => Mathf.Max(0f, _gunshotSoundRadius);
    public float SoundInvestigateDelaySeconds => Mathf.Max(0f, _soundInvestigateDelaySeconds);
    public float FootstepSoundRadius => Mathf.Max(0f, _footstepSoundRadius);
    public float FootstepSoundInterval => Mathf.Max(0.05f, _footstepSoundInterval);
}
