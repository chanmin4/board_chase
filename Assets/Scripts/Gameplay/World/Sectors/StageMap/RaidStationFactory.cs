using UnityEngine;

public static class RaidStationFactory
{
    public static void Attach(RaidRuntime raid, SectorRuntime sector, StageRoomNode room, RaidRuntimeSettings settings)
    {
        RaidStationKind kind;
        if (room.roomType == StageRoomType.Start) kind = RaidStationKind.StartReturn;
        else if (room.raidRole == RaidRoomRole.Generator) kind = RaidStationKind.Generator;
        else if (room.raidRole == RaidRoomRole.Hatch) kind = RaidStationKind.Hatch;
        else if (room.raidRole == RaidRoomRole.Extraction) kind = RaidStationKind.NormalExtraction;
        else return;

        RaidStation station = sector.GetComponentInChildren<RaidStation>(true);
        if (station == null && settings.terminalPrefab != null)
            station = Object.Instantiate(settings.terminalPrefab, sector.transform);
        if (station == null)
        {
            // Replace with authored geometry later; no unlit/error-shader primitives are generated.
            var terminal = new GameObject($"Raid_{kind}_Prototype");
            terminal.transform.SetParent(sector.transform, false);
            terminal.transform.position = sector.PlayerStartPoint.position + Vector3.right * 2.5f;
            int interactionLayer = LayerMask.NameToLayer("ObjectItem");
            terminal.layer = interactionLayer >= 0 ? interactionLayer : 0;
            var trigger = terminal.AddComponent<SphereCollider>();
            trigger.radius = 1.7f;
            trigger.center = Vector3.up * .6f;
            trigger.isTrigger = true;
            station = terminal.AddComponent<RaidStation>();
            var label = new GameObject("PrototypeLabel");
            label.transform.SetParent(terminal.transform, false);
            label.transform.localPosition = Vector3.up * 1.6f;
            label.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var text = label.AddComponent<TMPro.TextMeshPro>();
            text.font = settings.prototypeFont != null ? settings.prototypeFont : TMPro.TMP_Settings.defaultFontAsset;
            text.fontSize = 3;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(5, 2);
            text.raycastTarget = false;
            text.color = kind == RaidStationKind.Generator ? Color.yellow : Color.green;
            station.SetPrototypeLabel(text);
        }
        station.Configure(raid, sector, kind);
    }
}
