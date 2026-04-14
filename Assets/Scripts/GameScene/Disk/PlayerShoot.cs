using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerShoot : MonoBehaviour
{
    [Header("Refs")]
    public PlayerDisk disk;
    public Camera aimCamera;

    [Header("Weapon")]
    public MonoBehaviour weaponBehaviour; // 인스펙터에 Weapon_Pistol 넣어도 되고, 비워도 자동탐색
    IPlayerWeapon _weapon;

    [Header("Input")]
    public int fireMouseButton = 0;     // 좌클릭
    public bool holdToFire = false;

    float _cooldownAdd = 0f;
    UnityAction<float> _onCooldownAdd;

    public float CooldownAddSeconds => _cooldownAdd;

    void Awake()
    {
        if (!disk) disk = GetComponent<PlayerDisk>();
        if (!aimCamera) aimCamera = (disk && disk.aimCamera) ? disk.aimCamera : Camera.main;

        ResolveWeapon();

        // 스턴/디버프(쿨타임 가산) 연결(있으면)
        if (disk  != null)
        {
            _onCooldownAdd = (sec) => _cooldownAdd = Mathf.Max(0f, sec);
        }
    }

    void OnEnable() => ResolveWeapon();

    void ResolveWeapon()
    {
        // 1) 인스펙터에 지정되어 있으면 그걸 우선
        if (weaponBehaviour != null && weaponBehaviour is IPlayerWeapon iw)
        {
            _weapon = iw;
            return;
        }

        // 2) 자기 자신 + 자식들에서 IPlayerWeapon 구현체를 모두 찾고 첫 번째 사용
        var all = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in all)
        {
            if (mb is IPlayerWeapon w)
            {
                weaponBehaviour = mb;
                _weapon = w;
                return;
            }
        }

        _weapon = null;
        weaponBehaviour = null;
        Debug.LogWarning("[PlayerShoot] IPlayerWeapon을 찾지 못함. Weapon_Pistol이 붙어있는지 확인!");
    }

    void Update()
    {
        if (_weapon == null)
        {
            // 런타임 중에 무기 붙였으면 자동으로 다시 잡음
            if (weaponBehaviour != null) ResolveWeapon();
            return;
        }

        float dt = Time.deltaTime;
        _weapon.Tick(dt);

        bool pressed = holdToFire ? Input.GetMouseButton(fireMouseButton)
                                  : Input.GetMouseButtonDown(fireMouseButton);

        if (!pressed) return;
        if (!_weapon.CanFire) return;
        if (!disk) return;

        if (!disk.TryGetAimPoint(out Vector3 aimPoint))
            return;

        bool ok = _weapon.TryFire(this, aimPoint);
        if (!ok)
        {
            // 칠하기 실패 원인 디버그(게이지/참조 누락 등)
            // Debug.Log("[PlayerShoot] TryFire failed.");
        }
    }
}
