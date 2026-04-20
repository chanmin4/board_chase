using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerInfectionRules",
    menuName = "Game/Player Infection Rules")]
public class PlayerInfectionRulesSO : ScriptableObject
{
    [Header("Zone")]
    [Min(0f)]
    [SerializeField] private float virusZoneGainPerSecond = 8f;

    [Min(0f)]
    [SerializeField] private float vaccineZoneRecoverPerSecond = 10f;

    [Header("Hit")]
    [Min(0f)]
    [SerializeField] private float infectionOnHit = 10f;

    [Header("Rewards")]
    [Min(0f)]
    [SerializeField] private float recoverOnSectorCaptured = 20f;

    [Min(0f)]
    [SerializeField] private float recoverOnNamedKilled = 30f;

    [Min(0f)]
    [SerializeField] private float recoverOnBossKilled = 50f;

    public float VirusZoneGainPerSecond => virusZoneGainPerSecond;
    public float VaccineZoneRecoverPerSecond => vaccineZoneRecoverPerSecond;
    public float InfectionOnHit => infectionOnHit;
    public float RecoverOnSectorCaptured => recoverOnSectorCaptured;
    public float RecoverOnNamedKilled => recoverOnNamedKilled;
    public float RecoverOnBossKilled => recoverOnBossKilled;
}
