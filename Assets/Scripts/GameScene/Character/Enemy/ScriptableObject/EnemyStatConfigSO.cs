using UnityEngine;

public abstract class EnemyStatConfigSO : EntityStatConfigSO
{
    [Header("Identity")]
    [SerializeField] private string _id = "enemy_common";
    [SerializeField] private string _displayName = "Enemy";

    [Header("Prefab")]
    [SerializeField] private Enemy _enemyPrefab;

    [Header("Health")]
    [SerializeField, Min(1f)] private float _initialHealth = 20f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float _normalMovementSpeed = 2.2f;
    [SerializeField, Min(0f)] private float _playerChaseMovementSpeed = 3.5f;

    [Header("Contact Damage")]
    [SerializeField, Min(0f)] private float _contactHealthDamage = 5f;
    [SerializeField, Min(0f)] private float _contactInfectionDamage = 10f;
    [SerializeField, Min(0f)] private float _contactHitCooldown = 1f;

    [Header("Kill Rewards")]
    [SerializeField, Min(0f)] private float _xpOnDeath = 25f;
    [SerializeField, Min(0)] private int _runCurrencyOnDeath = 0;

    [Header("Virus Trail")]
    [SerializeField] private bool _virusTrailEnabled = true;
    [SerializeField, Min(0f)] private float _virusTrailRadius = 0.8f;
    [SerializeField, Min(0f)] private float _virusTrailMinSegmentDistance = 0.25f;
    [SerializeField] private int _virusTrailPaintPriority = 10;
    [SerializeField, Min(0f)] private float _virusTrailMinMoveSpeed = 0.05f;
    [SerializeField, Min(0f)] private float _virusTrailTeleportResetDistance = 10f;
    [SerializeField, Min(0f)] private float _virusTrailStampInterval = 0.08f;
    [SerializeField, Min(0f)] private float _virusTrailMaxPaintSegmentDistance = 1.5f;
    [SerializeField, Min(0f)] private float _virusTrailSpacing = 0.75f;
    [SerializeField, Min(1)] private int _virusTrailMaxSteps = 3;

    public override float InitialHealth => Mathf.Max(1f, _initialHealth);

    public override float ReferenceMoveSpeed =>
        Mathf.Max(NormalMovementSpeed, PlayerChaseMovementSpeed);

    public override float ResolveInitialHealth()
    {
        return Mathf.Max(1f, DifficultyRuntime.ApplyEnemyHealth(InitialHealth));
    }

    public string Id => _id;
    public string DisplayName => _displayName;
    public Enemy EnemyPrefab => _enemyPrefab;
    public bool IsValid => _enemyPrefab != null;

    public float NormalMovementSpeed =>
        DifficultyRuntime.ApplyEnemyNormalMoveSpeed(_normalMovementSpeed);

    public float PlayerChaseMovementSpeed =>
        DifficultyRuntime.ApplyEnemyChaseMoveSpeed(_playerChaseMovementSpeed);

    public float ContactHealthDamage =>
        DifficultyRuntime.ApplyEnemyDamage(_contactHealthDamage);

    public float ContactInfectionDamage => Mathf.Max(0f, _contactInfectionDamage);
    public float ContactHitCooldown => Mathf.Max(0f, _contactHitCooldown);

    public float XpOnDeath => Mathf.Max(0f, _xpOnDeath);
    public int RunCurrencyOnDeath => Mathf.Max(0, _runCurrencyOnDeath);

    public bool VirusTrailEnabled => _virusTrailEnabled;

    public float VirusTrailRadius =>
        DifficultyRuntime.ApplyEnemyVirusPaintRadius(_virusTrailRadius);

    public float VirusTrailMinSegmentDistance => Mathf.Max(0f, _virusTrailMinSegmentDistance);
    public int VirusTrailPaintPriority => _virusTrailPaintPriority;
    public float VirusTrailMinMoveSpeed => Mathf.Max(0f, _virusTrailMinMoveSpeed);
    public float VirusTrailTeleportResetDistance => Mathf.Max(0f, _virusTrailTeleportResetDistance);
    public float VirusTrailStampInterval => Mathf.Max(0f, _virusTrailStampInterval);
    public float VirusTrailMaxPaintSegmentDistance => Mathf.Max(0f, _virusTrailMaxPaintSegmentDistance);
    public float VirusTrailSpacing => Mathf.Max(0f, _virusTrailSpacing);
    public int VirusTrailMaxSteps => Mathf.Max(1, _virusTrailMaxSteps);
}
