using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyContactDamageSettings",
    menuName = "Game/Enemy/Enemy Contact Damage Settings")]
public class EnemyContactDamageSettingsSO : ScriptableObject
{
    [Header("Contact Attack")]
    [SerializeField] private float _healthDamage = 5f;
    [SerializeField] private float _infectionDamage = 10f;
    [SerializeField] private float _hitCooldown = 1f;

    public float HealthDamage => Mathf.Max(0f, _healthDamage);
    public float InfectionDamage => Mathf.Max(0f, _infectionDamage);
    public float HitCooldown => Mathf.Max(0f, _hitCooldown);
}
