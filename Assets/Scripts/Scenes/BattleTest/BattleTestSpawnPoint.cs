/*
 * Role hierarchy: Scenes > BattleTest
 * Primary responsibility: Manages BattleTestSpawnPointKind stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public enum BattleTestSpawnPointKind
{
    Player = 0,
    Enemy = 1
}

[DisallowMultipleComponent]
public sealed class BattleTestSpawnPoint : MonoBehaviour
{
    [SerializeField] private BattleTestSpawnPointKind _kind;

    public BattleTestSpawnPointKind Kind => _kind;
}
