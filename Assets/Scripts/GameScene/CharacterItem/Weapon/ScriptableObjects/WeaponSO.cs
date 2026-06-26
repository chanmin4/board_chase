using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "Weapon",
    menuName = "Game/Character Item/Weapon")]
public class WeaponSO : ItemSO
{
    [Header("Identity")]
    [SerializeField] private string _weaponId = "weapon";
    [SerializeField] private string _displayName = "Weapon";


    [Header("View")]
    [FormerlySerializedAs("weaponPrefab")]
    [Tooltip("Prefab used when this weapon is equipped in hand. Keep this separate from the world pickup prefab.")]
    [SerializeField] private WeaponView _weaponViewPrefab;
    [SerializeField] private Vector3 _weaponViewLocalPosition;
    [SerializeField] private Vector3 _weaponViewLocalEulerAngles;
    [SerializeField] private Vector3 _weaponViewLocalScale = Vector3.one;

    [Header("Audio")]
    [SerializeField] private AudioCueSO _fireAudioCue;
    [Tooltip("players sound of hit bullet to enemy ")]
    [SerializeField] private AudioCueSO _hitConfirmAudioCue;

    [FormerlySerializedAs("reloadAudioCue")]
    [SerializeField] private AudioCueSO _playerReloadAudioCue;

    [SerializeField] private AudioCueSO _enemyReloadAudioCue;

    [Header("Stat Modifiers")]
    [SerializeField] private PlayerStatModifier[] _statModifiers;

    [Header("Sound")]
    [SerializeField, Min(0f)] private float _gunshotSoundRadiusMultiplier = 1f;
    [SerializeField] private float _gunshotSoundRadiusAdd = 0f;

    public string WeaponId => _weaponId;
    public string DisplayName =>
        Name != null && !Name.IsEmpty
            ? Name.GetLocalizedString()
            : _weaponId;

    public WeaponView WeaponViewPrefab => _weaponViewPrefab;

    public AudioCueSO FireAudioCue => _fireAudioCue;
    public AudioCueSO HitConfirmAudioCue => _hitConfirmAudioCue;
    public AudioCueSO PlayerReloadAudioCue => _playerReloadAudioCue;
    public AudioCueSO EnemyReloadAudioCue => _enemyReloadAudioCue;

    public PlayerStatModifier[] StatModifiers => _statModifiers;
    public float GunshotSoundRadiusMultiplier => Mathf.Max(0f, _gunshotSoundRadiusMultiplier);
    public float GunshotSoundRadiusAdd => _gunshotSoundRadiusAdd;

    public float ResolveGunshotSoundRadius(float baseRadius)
    {
        return Mathf.Max(
            0f,
            Mathf.Max(0f, baseRadius) * GunshotSoundRadiusMultiplier + GunshotSoundRadiusAdd);
    }

    public void ApplyWeaponViewTransform(Transform viewTransform)
    {
        if (viewTransform == null)
            return;

        viewTransform.localPosition = _weaponViewLocalPosition;
        viewTransform.localRotation = Quaternion.Euler(_weaponViewLocalEulerAngles);
        viewTransform.localScale = SanitizeScale(_weaponViewLocalScale);
    }

    private static Vector3 SanitizeScale(Vector3 scale)
    {
        const float min = 0.0001f;

        return new Vector3(
            Mathf.Abs(scale.x) < min ? 1f : scale.x,
            Mathf.Abs(scale.y) < min ? 1f : scale.y,
            Mathf.Abs(scale.z) < min ? 1f : scale.z);
    }
}