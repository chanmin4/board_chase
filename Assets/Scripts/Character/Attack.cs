using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField] private AttackConfigSO _attackConfigSO;

    public AttackConfigSO AttackConfig => _attackConfigSO;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(gameObject.tag))
            return;

        if (!other.TryGetComponent(out Damageable damageableComp))
            return;

        if (!damageableComp.CanReceiveDamage)
            return;

        damageableComp.ReceiveAnAttack(_attackConfigSO.AttackStrength);
    }
}
