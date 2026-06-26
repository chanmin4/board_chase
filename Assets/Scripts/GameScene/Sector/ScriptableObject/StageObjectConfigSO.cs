using UnityEngine;

public abstract class StageObjectConfigSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _displayName;

    [Header("Prefab")]
    [SerializeField] private GameObject _prefab;

    [Header("Spawn")]
    [Tooltip("If true, this object uses a random Y rotation when spawned. If false, it uses the spawn point rotation.")]
    [SerializeField] private bool _randomizeYRotation = true;

    [Header("Cleanup")]
    [Tooltip("If true, this object is destroyed when the room encounter is cleared. Default false means it stays as part of the room.")]
    [SerializeField] private bool _clearWithRoom = false;

    public string DisplayName => _displayName;
    public GameObject Prefab => _prefab;
    public bool RandomizeYRotation => _randomizeYRotation;
    public bool ClearWithRoom => _clearWithRoom;
    public bool IsValid => _prefab != null;

    public Quaternion ResolveSpawnRotation(Transform spawnPoint, System.Random rng)
    {
        if (!_randomizeYRotation)
            return spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        float y = rng != null
            ? (float)(rng.NextDouble() * 360f)
            : Random.Range(0f, 360f);

        return Quaternion.Euler(0f, y, 0f);
    }
}
