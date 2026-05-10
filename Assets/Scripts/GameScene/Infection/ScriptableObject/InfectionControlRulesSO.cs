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

    [Header("Special Drain From Sector Summary")]
    [Tooltip("Extra drain per named active sector counted by SectorOccupancySummary. Usually keep 0 if Named Phase Drain is used.")]
    [SerializeField] private float _namedDrainBonus = 0f;

    [Tooltip("Extra drain per boss active sector counted by SectorOccupancySummary.")]
    [SerializeField] private float _bossDrainBonus = 0f;

    [Header("Named Phase Drain")]
    [Tooltip("Extra control loss per second while a named sector is present but not entered.")]
    [SerializeField] private float _namedPresentDrainPerSecond = 0.5f;

    [Tooltip("Extra control loss per second while named battle is active.")]
    [SerializeField] private float _namedBattleDrainPerSecond = 0.2f;

    [Header("Recovery")]
    [SerializeField] private float _recoverOnNamedDefeated = 500f;
    [SerializeField] private float _recoverOnSectorExpanded = 1000f;

    public float MaxControl => _maxControl;
    public float StartControl => Mathf.Clamp(_startControl, 0f, _maxControl);
    public float NamedPresentDrainPerSecond => Mathf.Max(0f, _namedPresentDrainPerSecond);
    public float NamedBattleDrainPerSecond => Mathf.Max(0f, _namedBattleDrainPerSecond);
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

    public float GetNamedPhaseDrainPerSecond(NamedSectorPhase phase)
    {
        switch (phase)
        {
            case NamedSectorPhase.Present:
                return NamedPresentDrainPerSecond;

            case NamedSectorPhase.Battle:
                return NamedBattleDrainPerSecond;

            default:
                return 0f;
        }
    }
}
