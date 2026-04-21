using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WeaponAmmoHUD : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private WeaponAmmoEventChannelSO _weaponAmmoEventChannel;
    [SerializeField] private VoidEventChannelSO _requestWeaponAmmoSnapshotChannel;

    [Header("UI")]
    [SerializeField] private Image _weaponIcon;
    [SerializeField] private TextMeshProUGUI _ammoText;

    private void OnEnable()
    {
        if (_weaponAmmoEventChannel != null)
            _weaponAmmoEventChannel.OnEventRaised += OnWeaponAmmoChanged;

        if (_requestWeaponAmmoSnapshotChannel != null)
            _requestWeaponAmmoSnapshotChannel.RaiseEvent();
    }

    private void OnDisable()
    {
        if (_weaponAmmoEventChannel != null)
            _weaponAmmoEventChannel.OnEventRaised -= OnWeaponAmmoChanged;
    }

    private void OnWeaponAmmoChanged(WeaponAmmoSnapshot snapshot)
    {
        if (_weaponIcon != null)
        {
            _weaponIcon.sprite = snapshot.weaponIcon;
            _weaponIcon.enabled = snapshot.weaponIcon != null;
        }

        if (_ammoText != null)
            _ammoText.text = $"{snapshot.currentAmmo}/{snapshot.maxAmmo}";

    }
}
