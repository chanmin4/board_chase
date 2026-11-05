/*
 * Role hierarchy: Gameplay > World > Sectors > Traits
 * Primary responsibility: Owns the deterministic trait selected for one
 * generated sector and exposes only the currently occupied player's effects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class SectorRoomTraitRuntime : MonoBehaviour
{
    private const int RoomTraitPickSalt = 619;

    [Header("Resolved Runtime State")]
    [ReadOnly] [SerializeField] private StageRoomType _roomType;
    [ReadOnly] [SerializeField] private SectorRoomTraitType _rolledRoomTrait = SectorRoomTraitType.Normal;
    [ReadOnly] [SerializeField] private SectorRoomTraitType _selectedRoomTrait = SectorRoomTraitType.Normal;
    [ReadOnly] [SerializeField] private bool _hasDebugRoomTraitOverride;
    [ReadOnly] [SerializeField] private bool _biochemicalGasEnabled;
    [ReadOnly] [SerializeField] private float _baselineGasInfectionPerSecond;
    [ReadOnly] [SerializeField] private float _timerFailureLeakInfectionPerSecond;
    [ReadOnly] [SerializeField] private int _maxTimerFailureLeakStacks;
    [ReadOnly] [SerializeField] private int _timerFailureLeakStacks;
    [ReadOnly] [SerializeField] private float _biochemicalGasTickInterval = 0.5f;
    [ReadOnly] [SerializeField] private SectorRoomTraitTableSO _table;

    private static SectorRoomTraitRuntime _currentPlayerSectorTrait;
    private float _anomalyGasBaseline;

    public StageRoomType RoomType => _roomType;
    public SectorRoomTraitType RolledRoomTrait => _rolledRoomTrait;
    public SectorRoomTraitType SelectedRoomTrait => _selectedRoomTrait;
    public bool HasDebugRoomTraitOverride => _hasDebugRoomTraitOverride;
    public bool HasBiochemicalGas => _biochemicalGasEnabled && CurrentBiochemicalGasInfectionPerSecond > 0f;
    public int TimerFailureLeakStacks => Mathf.Max(0, _timerFailureLeakStacks);
    public float CurrentBiochemicalGasInfectionPerSecond =>
        _biochemicalGasEnabled
            ? Mathf.Max(0f, (_selectedRoomTrait == SectorRoomTraitType.GasBoost ?
                Mathf.Max(_baselineGasInfectionPerSecond, _anomalyGasBaseline) : _baselineGasInfectionPerSecond) +
                _timerFailureLeakInfectionPerSecond * TimerFailureLeakStacks)
            : 0f;
    public bool IsDarknessRoom => _selectedRoomTrait == SectorRoomTraitType.Darkness;
    public void ConfigureVirusRoom(bool enabled, float pressure = 1f)
    {
        _biochemicalGasEnabled=enabled;
        _baselineGasInfectionPerSecond=enabled?Mathf.Max(0,pressure):0;
    }
    public bool IsArmedRoom => _selectedRoomTrait == SectorRoomTraitType.Armed;
    public bool IsAmmoScavengerRoom => _selectedRoomTrait == SectorRoomTraitType.AmmoScavenger;
    public bool IsFearRoom => _selectedRoomTrait == SectorRoomTraitType.Fear;
    public string DisplayName => _selectedRoomTrait.ToString();

    public static SectorRoomTraitRuntime CurrentPlayerSectorTrait => _currentPlayerSectorTrait;
    public static float CurrentPlayerVisionRangeMultiplier =>
        _currentPlayerSectorTrait != null && _currentPlayerSectorTrait.IsDarknessRoom
            ? _currentPlayerSectorTrait._table.DarknessVisionRangeMultiplier
            : 1f;

    public static float CurrentPlayerFogAlphaMultiplier =>
        _currentPlayerSectorTrait != null && _currentPlayerSectorTrait.IsDarknessRoom
            ? _currentPlayerSectorTrait._table.DarknessFogAlphaMultiplier
            : 1f;

    public static int CurrentPlayerAmmoConsumptionMultiplier =>
        _currentPlayerSectorTrait != null && _currentPlayerSectorTrait.IsAmmoScavengerRoom
            ? 2
            : 1;

    public static float CurrentPlayerInjuryChanceMultiplier =>
        _currentPlayerSectorTrait != null && _currentPlayerSectorTrait.IsFearRoom
            ? _currentPlayerSectorTrait._table.FearInjuryChanceMultiplier
            : 1f;

    public static float CurrentPlayerBiochemicalGasInfectionPerSecond =>
        RaidRuntime.CurrentOvertimeInfection + (
        _currentPlayerSectorTrait != null && _currentPlayerSectorTrait.HasBiochemicalGas
            ? _currentPlayerSectorTrait.CurrentBiochemicalGasInfectionPerSecond
            : 0f);

    public static float CurrentPlayerBiochemicalGasTickInterval =>
        _currentPlayerSectorTrait != null && _currentPlayerSectorTrait.HasBiochemicalGas
            ? _currentPlayerSectorTrait._biochemicalGasTickInterval
            : 0.5f;

    // Purpose: Updates the current player sector state.
    public static void SetCurrentPlayerSector(SectorRuntime sector)
    {
        _currentPlayerSectorTrait = sector != null
            ? sector.GetComponent<SectorRoomTraitRuntime>()
            : null;
    }

    // Purpose: Configures this component''s configure.
    public void Configure(
        SectorRoomTraitTableSO table,
        StageRoomType roomType,
        int stageSeed,
        Vector2Int sectorCoord,
        bool biochemicalGasEnabled,
        StageDefinitionSO.BiochemicalGasSettings biochemicalGas, int stageIndex = 0,
        bool combatRoom = true, float anomalyGasBaseline = 0f, bool useRoomTimerLeaks = true)
    {
        _table = table;
        _roomType = roomType;
        _anomalyGasBaseline = Mathf.Max(0f, anomalyGasBaseline);
        useRoomTimerLeaks = false; // Deadlines are replaced by personal suppression management.
        bool appliesConfiguredGas = biochemicalGas != null &&
                                  biochemicalGas.AppliesTo(roomType);
        _biochemicalGasEnabled = appliesConfiguredGas ||
                                 (biochemicalGas == null && biochemicalGasEnabled);
        _baselineGasInfectionPerSecond = appliesConfiguredGas
            ? Mathf.Max(0f, biochemicalGas.baselineInfectionPerSecond)
            : biochemicalGasEnabled && table != null
                ? table.BiochemicalGasInfectionPerSecond
                : 0f;
        _timerFailureLeakInfectionPerSecond = appliesConfiguredGas && useRoomTimerLeaks
            ? Mathf.Max(0f, biochemicalGas.timerFailureLeakInfectionPerSecond)
            : 0f;
        _maxTimerFailureLeakStacks = appliesConfiguredGas && useRoomTimerLeaks
            ? Mathf.Max(0, biochemicalGas.maxTimerFailureLeakStacks)
            : 0;
        _biochemicalGasTickInterval = appliesConfiguredGas
            ? Mathf.Max(0.1f, biochemicalGas.tickInterval)
            : table != null ? table.BiochemicalGasTickInterval : 0.5f;
        _timerFailureLeakStacks = 0;
        _rolledRoomTrait = table != null
            ? table.PickTrait(
                StageDefinitionSO.BuildSectorSeed(
                    stageSeed,
                    sectorCoord,
                    RoomTraitPickSalt + (int)roomType),
                roomType, stageIndex, combatRoom)
            : SectorRoomTraitType.Normal;
        _selectedRoomTrait = _rolledRoomTrait;
        _hasDebugRoomTraitOverride = false;
    }

    /// <summary>
    /// Starts or intensifies this room's gas leak after the Normal Sector
    /// clear timer expires. Only the current room retains the stack, so
    /// leaving or clearing it removes the pressure naturally with the sector.
    /// </summary>
    public bool TryAddTimerFailureGasLeak(out int resultingStacks)
    {
        resultingStacks = TimerFailureLeakStacks;

        if (!_biochemicalGasEnabled ||
            _roomType != StageRoomType.Normal ||
            _timerFailureLeakInfectionPerSecond <= 0f ||
            _maxTimerFailureLeakStacks <= 0 ||
            _timerFailureLeakStacks >= _maxTimerFailureLeakStacks)
        {
            return false;
        }

        _timerFailureLeakStacks++;
        resultingStacks = _timerFailureLeakStacks;
        return true;
    }

    // Purpose: Applies the requested to spawned enemy change.
    public void ApplyToSpawnedEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        SectorEnemyTraitModifier modifier =
            enemy.GetComponent<SectorEnemyTraitModifier>();

        if (modifier == null && (IsArmedRoom || _selectedRoomTrait == SectorRoomTraitType.EnemyDamage) && _table != null)
            modifier = enemy.gameObject.AddComponent<SectorEnemyTraitModifier>();

        if (modifier != null)
            modifier.Configure(_table, IsArmedRoom, _selectedRoomTrait == SectorRoomTraitType.EnemyDamage);
    }

    public void RestoreGasLeakStacks(int stacks) => _timerFailureLeakStacks = Mathf.Clamp(stacks, 0, _maxTimerFailureLeakStacks);

    /// <summary>
    /// Debug-only runtime override. This never changes the shared trait table,
    /// stage seed, or generated map data and is cleared with the sector.
    /// </summary>
    public void SetDebugRoomTraitOverride(SectorRoomTraitType trait)
    {
        _selectedRoomTrait = trait;
        _hasDebugRoomTraitOverride = true;
        RefreshSpawnedEnemyTraitModifiers();
    }

    public void ConfigureAuthoredRoomTrait(SectorRoomTraitType trait)
    {
        _rolledRoomTrait = trait;
        _selectedRoomTrait = trait;
        _hasDebugRoomTraitOverride = false;
        RefreshSpawnedEnemyTraitModifiers();
    }

    // Purpose: Clears the current debug room trait override state.
    public void ClearDebugRoomTraitOverride()
    {
        _selectedRoomTrait = _rolledRoomTrait;
        _hasDebugRoomTraitOverride = false;
        RefreshSpawnedEnemyTraitModifiers();
    }

    // Purpose: Refreshes the displayed or cached spawned enemy trait modifiers.
    private void RefreshSpawnedEnemyTraitModifiers()
    {
        Enemy[] enemies = GetComponentsInChildren<Enemy>(true);
        for (int i = 0; i < enemies.Length; i++)
            ApplyToSpawnedEnemy(enemies[i]);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_currentPlayerSectorTrait == this)
            _currentPlayerSectorTrait = null;
    }
}
