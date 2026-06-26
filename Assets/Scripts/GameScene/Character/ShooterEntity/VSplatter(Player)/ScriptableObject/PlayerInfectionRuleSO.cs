using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "PlayerInfectionRules",
    menuName = "Game/Player Infection Rules")]
public class PlayerInfectionRulesSO : ScriptableObject
{
    [Header("Zone Tick")]
    [SerializeField, Min(0.01f)] private float _zoneTickInterval = 0.2f;

    [Header("Virus Zone")]
    [SerializeField, Min(0f)] private float _virusZoneInfectionGainPerTick = 1f;

    [Header("Vaccine Zone")]
    [SerializeField, Min(0f)] private float _vaccineZoneRecoverPerTick = 1f;

    [Header("Rewards")]
    [FormerlySerializedAs("recoverOnSectorCaptured")]
    [SerializeField, Min(0f)] private float _recoverOnSectorCaptured = 20f;

    [FormerlySerializedAs("recoverOnNamedKilled")]
    [SerializeField, Min(0f)] private float _recoverOnNamedKilled = 30f;

    [FormerlySerializedAs("recoverOnBossKilled")]
    [SerializeField, Min(0f)] private float _recoverOnBossKilled = 50f;

    public float ZoneTickInterval => Mathf.Max(0.01f, _zoneTickInterval);
    public float VirusZoneInfectionGainPerTick => _virusZoneInfectionGainPerTick;
    public float VaccineZoneRecoverPerTick => _vaccineZoneRecoverPerTick;
    public float RecoverOnSectorCaptured => _recoverOnSectorCaptured;
    public float RecoverOnNamedKilled => _recoverOnNamedKilled;
    public float RecoverOnBossKilled => _recoverOnBossKilled;
}