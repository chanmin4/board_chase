using UnityEngine;

[CreateAssetMenu(fileName = "InfectionControlRules", menuName = "Game/Infection Control Rules")]
public class InfectionControlRulesSO : ScriptableObject
{
    [Header("Control")]
    [SerializeField] private float _maxControl = 10000f;
    [SerializeField] private float _startControl = 10000f;

    [Header("Drain Formula")]
    [SerializeField] private float _baseDrainPerSecond = 1f;
    [SerializeField] private float _drainPerVirusOwnedSector = 2f;
    [SerializeField, Min(1)] private int _extraDrainEveryVirusSectorCount = 2;
    [SerializeField] private float _extraDrainPerGroup = 2f;

    [Header("Special Drain")]
    [SerializeField] private float _namedDrainBonus = 0f;
    [SerializeField] private float _bossDrainBonus = 0f;

    [Header("Recovery")]
    [SerializeField] private float _recoverOnNamedDefeated = 500f;
    [SerializeField] private float _recoverOnSectorExpanded = 1000f;

    public float MaxControl => _maxControl;
    public float StartControl => Mathf.Clamp(_startControl, 0f, _maxControl);
    public float RecoverOnNamedDefeated => _recoverOnNamedDefeated;
    public float RecoverOnSectorExpanded => _recoverOnSectorExpanded;

    public float CalculateDrainPerSecond(SectorOccupancySummary summary)
    {
        int virusOwnedCount = Mathf.Max(0, summary.virusOwnedCount);

        float drain = _baseDrainPerSecond;
        drain += virusOwnedCount * _drainPerVirusOwnedSector;

        int groupSize = Mathf.Max(1, _extraDrainEveryVirusSectorCount);
        int extraGroups = virusOwnedCount / groupSize;
        drain += extraGroups * _extraDrainPerGroup;

        drain += summary.namedActiveCount * _namedDrainBonus;
        drain += summary.bossActiveCount * _bossDrainBonus;

        return Mathf.Max(0f, drain);
    }
}
