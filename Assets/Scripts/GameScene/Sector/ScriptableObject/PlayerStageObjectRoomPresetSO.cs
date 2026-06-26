using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerStageObjectRoomPreset",
    menuName = "Game/Battle/Player Stage Object Room Preset")]
public class PlayerStageObjectRoomPresetSO : ScriptableObject
{
    [Serializable]
    public class ObjectSpawn
    {
        [Tooltip("Player-side object config to spawn in this room.")]
        public PlayerStageObjectConfigSO objectConfig;
    }

    [Header("Display")]
    [SerializeField] private string _displayName;

    [Header("Objects")]
    [SerializeField] private List<ObjectSpawn> _objects = new();

    public string DisplayName =>
        string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;

    public IReadOnlyList<ObjectSpawn> Objects => _objects;

    public bool IsValid
    {
        get
        {
            if (_objects == null)
                return false;

            for (int i = 0; i < _objects.Count; i++)
            {
                ObjectSpawn spawn = _objects[i];

                if (spawn != null &&
                    spawn.objectConfig != null &&
                    spawn.objectConfig.IsValid)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
