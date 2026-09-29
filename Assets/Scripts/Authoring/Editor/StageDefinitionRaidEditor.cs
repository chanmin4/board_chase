using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StageDefinitionSO))]
public sealed class StageDefinitionRaidEditor : Editor
{
    private int _previewSeed = 1;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var stage = (StageDefinitionSO)target;
        if (stage.MapPolicy != StageMapPolicy.OddSquareRaid) return;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Raid layout preview (zero-based coordinates)", EditorStyles.boldLabel);
        _previewSeed = EditorGUILayout.IntField("Preview seed", _previewSeed);
        if (!RaidMapGenerator.TryGenerate(_previewSeed, stage.StageIndex, stage.RoomGridSize, Vector2Int.left,
                stage.RaidMap, out StageMapLayout map, out string error))
        { EditorGUILayout.HelpBox(error, MessageType.Error); return; }
        EditorGUILayout.HelpBox($"S = Start return {map.startSectorCoord}, P = First room {map.firstRoomCoord}.\n" +
            "G = Generator, H = Hatch, E = Extraction, B = Boss (b = absent).\n" +
            "New raids choose one of four corners. Boss/exit: opposite quadrant; generator: either side quadrant. " +
            "Branch topologies are preserved but ignored by this policy.", MessageType.Info);
        int minX = Mathf.Min(0, map.startSectorCoord.x);
        int maxX = Mathf.Max(map.gridSize - 1, map.startSectorCoord.x);
        int minY = Mathf.Min(0, map.startSectorCoord.y);
        int maxY = Mathf.Max(map.gridSize - 1, map.startSectorCoord.y);
        var cellWidth = GUILayout.Width(Mathf.Max(15f,
            (EditorGUIUtility.currentViewWidth - 48f) / (maxX - minX + 1) - 2f));
        for (int y = maxY; y >= minY; y--)
        {
            using (new EditorGUILayout.HorizontalScope())
            for (int x = minX; x <= maxX; x++)
            {
                var coord = new Vector2Int(x, y);
                if (!map.TryGetRoom(coord, out StageRoomNode room))
                {
                    GUILayout.Label(" ", EditorStyles.miniLabel, cellWidth);
                    continue;
                }
                string label = room.raidRole switch
                {
                    RaidRoomRole.Generator => "G", RaidRoomRole.Hatch => "H", RaidRoomRole.Extraction => "E",
                    RaidRoomRole.Boss => map.bossSpawned ? "B" : "b", RaidRoomRole.Armory => "A",
                    RaidRoomRole.Medical => "M", RaidRoomRole.Vault => "V", _ => "."
                };
                if (coord == map.startSectorCoord) label = "S";
                else if (coord == map.firstRoomCoord) label = "P";
                GUILayout.Label(new GUIContent(label, $"({x},{y}) {room.raidRole}"), EditorStyles.miniButton, cellWidth);
            }
        }
        if (stage.RaidRuntime.generatorBattery == null)
            EditorGUILayout.HelpBox("Assign a physical generator Battery ItemSO before testing hatch extraction.", MessageType.Error);
        EditorGUILayout.HelpBox("Raid Runtime > Rooms > Sector Variants: assign room presets and relative weights per role. Empty/invalid pools use the single fallback preset, then the stage Normal pool. Boss layouts use Boss Battles. Blank terminal prefab uses a prototype marker; room templates still need final art.", MessageType.Info);
    }
}
