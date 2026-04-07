using UnityEngine;

public class VSplatterAttackController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VSplatter_Character _character;
    [SerializeField] private Animator _animator;

    [Header("Fire Settings")]
    [SerializeField] private float shotsPerSecond = 2f;

    private float _nextFireTime;

    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int IsShootingHash = Animator.StringToHash("IsShooting");

    private void Update()
    {
        if (_character == null || _animator == null)
            return;

        // 홀드 입력을 그대로 Animator bool에 전달
        _animator.SetBool(IsShootingHash, _character.attackInput);
              Debug.Log(
            $"attackInput={_character.attackInput}, " +
            $"animatorBool={_animator.GetBool(IsShootingHash)}, " +
            $"animator={_animator.name}, " +
            $"controller={_animator.runtimeAnimatorController?.name}"
        );

        // 누르고 있는 동안 연사
        if (_character.attackInput && Time.time >= _nextFireTime)
        {
            FireOnce();
            _nextFireTime = Time.time + (1f / shotsPerSecond);
        }
    }

    private void FireOnce()
    {
        Debug.Log("Fire!");

        // 실제 발사 처리
        // raycast, projectile spawn, muzzle flash, sfx 등

        _animator.SetTrigger(ShootHash);
    }
}