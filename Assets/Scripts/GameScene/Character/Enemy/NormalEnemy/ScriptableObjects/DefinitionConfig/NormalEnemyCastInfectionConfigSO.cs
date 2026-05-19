using UnityEngine;

[CreateAssetMenu(
    fileName = "NormalEnemyCastInfectionConfig",
    menuName = "ScriptableObject/GameScene/Enemy/Definition_Config/Normal Enemy Cast Infection Config")]
public class NormalEnemyCastInfectionConfigSO : ScriptableObject
{
    [SerializeField] private float _infectionRadius = 1.5f;
    [SerializeField] private bool _applyOnStateExit = true;
    [SerializeField] private int _paintPriority = 0;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = true;
    [SerializeField] private bool _debugDraw = true;
    [SerializeField] private float _debugDrawDuration = 2f;

    public float InfectionRadius => DifficultyRuntime.ApplyEnemyVirusPaintRadius(_infectionRadius);
    public bool ApplyOnStateExit => _applyOnStateExit;
    public int PaintPriority => _paintPriority;
    public bool DebugLogs => _debugLogs;
    public bool DebugDraw => _debugDraw;
    public float DebugDrawDuration => Mathf.Max(0f, _debugDrawDuration);
}
