using UnityEngine;

public class VSplatterClickController : MonoBehaviour
{
    public enum FireMode
    {
        None,
        Attack,
        Paint
    }

    [Header("Refs")]
    [SerializeField] private VSplatter_Character _character;
    [SerializeField] private Animator _animator;

    [Header("Fire Settings")]
    [SerializeField] private float shotsPerSecond = 2f;
    [SerializeField] private int upperBodyLayerIndex = 1;

    private float _nextFireTime;
    private bool _wasHoldingLastFrame;

    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int IsShootingHash = Animator.StringToHash("IsShooting");

    private void Update()
    {
        if (_character == null || _animator == null)
            return;

        bool holdAttack = _character.attackInput;
        bool holdPaint = _character.paintInput;

        bool holdAny = holdAttack || holdPaint;
        Debug.Log("holdany "+holdAny);
        _animator.SetBool(IsShootingHash, holdAny);

        if (!holdAny)
        {
            _wasHoldingLastFrame = false;
            return;
        }

        // 처음 누른 프레임엔 Aiming 상태로만 들어가게 하고,
        // Shoot 트리거는 다음 프레임부터 쏜다.
        Debug.Log("1");
        if (!_wasHoldingLastFrame)
        {
            _wasHoldingLastFrame = true;
            return;
        }
Debug.Log("2");
        if (Time.time < _nextFireTime)
            return;

        FireMode mode = holdPaint ? FireMode.Paint : FireMode.Attack;
        Debug.Log("what mode" +mode);
        FireOnce(mode);

        _nextFireTime = Time.time + (1f / shotsPerSecond);
    }

    private void FireOnce(FireMode mode)
    {
        _animator.ResetTrigger(ShootHash);
        _animator.SetTrigger(ShootHash);

        switch (mode)
        {
            case FireMode.Attack:
                Debug.Log("AttackFire!");
                // TODO: 적 공격 처리
                break;

            case FireMode.Paint:
                Debug.Log("PaintFire!");
                // TODO: 페인트 처리
                break;
        }
    }
}