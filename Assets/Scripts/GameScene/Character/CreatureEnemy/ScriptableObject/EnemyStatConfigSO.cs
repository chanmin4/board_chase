using UnityEngine;

public abstract class EnemyStatConfigSO : EntityStatConfigSO
{
    [Header("Identity")]
    [SerializeField] private string _id = "enemy_common";
    [SerializeField] private string _displayName = "Enemy";

    [Header("Kill Rewards")]
    [SerializeField, Min(0f)] private float _xpOnDeath = 25f;
    [SerializeField, Min(0)] private int _runCurrencyOnDeath = 0;

    [Header("Enemy Screen Space UI")]
    [SerializeField] private EnemyScreenSpaceWidgetLayout _healthBarUI =
        new EnemyScreenSpaceWidgetLayout(new Vector2(0f, 50f));
    [SerializeField] private EnemyScreenSpaceWidgetLayout _infectionCastBarUI =
        new EnemyScreenSpaceWidgetLayout(new Vector2(0f, 40f));

    public string Id => _id;
    public string DisplayName => _displayName;
    public abstract Enemy EnemyPrefab { get; }
    public bool IsValid => EnemyPrefab != null;

    public float XpOnDeath => Mathf.Max(0f, _xpOnDeath);
    public int RunCurrencyOnDeath => Mathf.Max(0, _runCurrencyOnDeath);

    public EnemyScreenSpaceWidgetLayout HealthBarUI => _healthBarUI;
    public EnemyScreenSpaceWidgetLayout InfectionCastBarUI => _infectionCastBarUI;
}
