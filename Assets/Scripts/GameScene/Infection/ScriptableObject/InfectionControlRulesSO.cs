using UnityEngine;

[CreateAssetMenu(fileName = "InfectionControlRules", menuName = "Game/Infection Control Rules")]
public class InfectionControlRulesSO : ScriptableObject
{
    [SerializeField] private float _maxControl = 10000f;
    [SerializeField] private float _startControl = 10000f;
    [SerializeField] private float _drainPerVirusSector = 1f;
    [SerializeField] private float _namedDrainBonus = 2f;
    [SerializeField] private float _bossDrainBonus = 5f; 

    public float MaxControl => _maxControl;
    public float StartControl => Mathf.Clamp(_startControl, 0f, _maxControl);
    public float DrainPerVirusSector => _drainPerVirusSector;
    public float NamedDrainBonus => _namedDrainBonus;
    public float BossDrainBonus => _bossDrainBonus;
}
