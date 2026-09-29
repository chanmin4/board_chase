/*
 * Role hierarchy: Gameplay > World > Sectors > StageMap
 * Primary responsibility: Defines an optional authored branch-and-merge room topology for a generated stage.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StageTopologyRoom
{
    [Tooltip("Logical grid coordinate. Orthogonally adjacent rooms automatically connect, creating branches and merges from the drawn shape.")]
    public Vector2Int coord;

    [Tooltip("Only Normal and Boss are supported here. Exactly one Boss room is required and becomes the generated stage goal.")]
    public StageRoomType roomType = StageRoomType.Normal;
}

/// <summary>
/// Optional authored graph for one generated stage. A shape made from
/// orthogonally adjacent cells becomes the exact portal topology at runtime:
/// one cell can split into two routes and adjacent routes may merge again.
/// The generated Start Sector remains separate and connects to Entry Room.
/// </summary>
[CreateAssetMenu(
    fileName = "StageBranchTopology",
    menuName = "Game/Stage/Branch Topology")]
public sealed class StageTopologySO : ScriptableObject
{
    [Tooltip("The Normal room that must be directly adjacent to the generated Start Sector. It is Vector2Int.zero by default.")]
    [SerializeField] private Vector2Int _entryRoomCoord = Vector2Int.zero;

    [Tooltip("Every playable generated room. Add orthogonally adjacent cells to form branches and merges. Exactly one Boss cell is required.")]
    [SerializeField] private List<StageTopologyRoom> _rooms = new();

    public Vector2Int EntryRoomCoord => _entryRoomCoord;
    public IReadOnlyList<StageTopologyRoom> Rooms => _rooms;
    public int RoomCount => _rooms != null ? _rooms.Count : 0;

    // Purpose: Attempts to get room and reports whether the request succeeded.
    public bool TryGetRoom(Vector2Int coord, out StageTopologyRoom room)
    {
        if (_rooms != null)
        {
            for (int i = 0; i < _rooms.Count; i++)
            {
                StageTopologyRoom candidate = _rooms[i];
                if (candidate != null && candidate.coord == coord)
                {
                    room = candidate;
                    return true;
                }
            }
        }

        room = null;
        return false;
    }

    // Purpose: Attempts to get goal room coord and reports whether the request succeeded.
    public bool TryGetGoalRoomCoord(out Vector2Int coord)
    {
        if (_rooms != null)
        {
            for (int i = 0; i < _rooms.Count; i++)
            {
                StageTopologyRoom room = _rooms[i];
                if (room != null && room.roomType == StageRoomType.Boss)
                {
                    coord = room.coord;
                    return true;
                }
            }
        }

        coord = default;
        return false;
    }

    // Purpose: Attempts to get bounds and reports whether the request succeeded.
    public bool TryGetBounds(out Vector2Int min, out Vector2Int max)
    {
        min = default;
        max = default;

        if (_rooms == null || _rooms.Count == 0)
            return false;

        bool found = false;
        for (int i = 0; i < _rooms.Count; i++)
        {
            StageTopologyRoom room = _rooms[i];
            if (room == null)
                continue;

            if (!found)
            {
                min = room.coord;
                max = room.coord;
                found = true;
                continue;
            }

            min = Vector2Int.Min(min, room.coord);
            max = Vector2Int.Max(max, room.coord);
        }

        return found;
    }

    /// <summary>
    /// Validates the data-only graph. The StageSectorInstantiator additionally
    /// verifies that Entry Room is adjacent to its authored Start Sector.
    /// </summary>
    public bool TryValidate(out string error)
    {
        if (_rooms == null || _rooms.Count == 0)
        {
            error = "Topology has no rooms.";
            return false;
        }

        Dictionary<Vector2Int, StageTopologyRoom> roomsByCoord = new();
        int bossCount = 0;

        for (int i = 0; i < _rooms.Count; i++)
        {
            StageTopologyRoom room = _rooms[i];
            if (room == null)
            {
                error = $"Topology has an empty room entry at index {i}.";
                return false;
            }

            if (!roomsByCoord.TryAdd(room.coord, room))
            {
                error = $"Topology has duplicate room coordinate {room.coord}.";
                return false;
            }

            if (room.roomType != StageRoomType.Normal && room.roomType != StageRoomType.Boss)
            {
                error = $"Room {room.coord} uses {room.roomType}. Branch Topology only supports Normal and Boss rooms.";
                return false;
            }

            if (room.roomType == StageRoomType.Boss)
                bossCount++;
        }

        if (!roomsByCoord.TryGetValue(_entryRoomCoord, out StageTopologyRoom entryRoom))
        {
            error = $"Entry Room {_entryRoomCoord} is missing from the topology.";
            return false;
        }

        if (entryRoom.roomType != StageRoomType.Normal)
        {
            error = $"Entry Room {_entryRoomCoord} must be a Normal room.";
            return false;
        }

        if (bossCount != 1)
        {
            error = $"Topology requires exactly one Boss room, but found {bossCount}.";
            return false;
        }

        HashSet<Vector2Int> visited = new();
        Queue<Vector2Int> pending = new();
        visited.Add(_entryRoomCoord);
        pending.Enqueue(_entryRoomCoord);

        while (pending.Count > 0)
        {
            Vector2Int current = pending.Dequeue();
            VisitAdjacent(current, Vector2Int.left, roomsByCoord, visited, pending);
            VisitAdjacent(current, Vector2Int.right, roomsByCoord, visited, pending);
            VisitAdjacent(current, Vector2Int.down, roomsByCoord, visited, pending);
            VisitAdjacent(current, Vector2Int.up, roomsByCoord, visited, pending);
        }

        if (visited.Count != roomsByCoord.Count)
        {
            error = $"Topology contains {roomsByCoord.Count - visited.Count} disconnected room(s). Every room must have an orthogonal route from Entry Room {_entryRoomCoord}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    // Purpose: Updates the entry room coord state.
    public void SetEntryRoomCoord(Vector2Int coord)
    {
        _entryRoomCoord = coord;
    }

    // Purpose: Performs the upsert room workflow for this component.
    public void UpsertRoom(Vector2Int coord, StageRoomType roomType)
    {
        _rooms ??= new List<StageTopologyRoom>();

        if (TryGetRoom(coord, out StageTopologyRoom existing))
        {
            existing.roomType = roomType;
            return;
        }

        _rooms.Add(new StageTopologyRoom { coord = coord, roomType = roomType });
    }

    // Purpose: Removes the requested room from this component.
    public bool RemoveRoom(Vector2Int coord)
    {
        if (_rooms == null)
            return false;

        for (int i = _rooms.Count - 1; i >= 0; i--)
        {
            StageTopologyRoom room = _rooms[i];
            if (room != null && room.coord == coord)
            {
                _rooms.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Small, playable branch-and-merge sample used by Sector Maker. It
    /// intentionally does not touch any StageDefinitionSO assignment.
    /// </summary>
    public void ReplaceWithBranchingExample()
    {
        _entryRoomCoord = Vector2Int.zero;
        _rooms = new List<StageTopologyRoom>
        {
            new() { coord = new Vector2Int(0, 0), roomType = StageRoomType.Normal },
            new() { coord = new Vector2Int(1, 0), roomType = StageRoomType.Normal },
            new() { coord = new Vector2Int(1, 1), roomType = StageRoomType.Normal },
            new() { coord = new Vector2Int(2, 1), roomType = StageRoomType.Normal },
            new() { coord = new Vector2Int(2, 0), roomType = StageRoomType.Normal },
            new() { coord = new Vector2Int(3, 0), roomType = StageRoomType.Boss }
        };
    }

    // Purpose: Performs the visit adjacent workflow for this component.
    private static void VisitAdjacent(
        Vector2Int origin,
        Vector2Int offset,
        IReadOnlyDictionary<Vector2Int, StageTopologyRoom> roomsByCoord,
        ISet<Vector2Int> visited,
        Queue<Vector2Int> pending)
    {
        Vector2Int adjacent = origin + offset;
        if (roomsByCoord.ContainsKey(adjacent) && visited.Add(adjacent))
            pending.Enqueue(adjacent);
    }
}
