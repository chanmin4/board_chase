using UnityEngine;

/// <summary>Prototype shop station. Reuses the existing shop UI, stock and physical trade transactions.</summary>
public static class RaidMerchantFactory
{
    public static void Attach(RaidRuntime raid, SectorRuntime sector, StageRoomNode room, RaidRuntimeSettings settings)
    {
        if (!room.hasMerchant && room.raidRole != RaidRoomRole.Merchant) return;
        if (settings.merchantConfig == null)
        { Debug.LogError($"[Raid] 상인 배치 {room.coord}: Merchant Config가 없습니다. Stage Maker의 상인 상점 설정을 등록하세요.", sector); return; }
        var obj = new GameObject("긴급보급 상인 (Prototype)");
        obj.transform.SetParent(sector.transform, false);
        obj.transform.position = sector.PlayerStartPoint.position + Vector3.right * 3f;
        int layer = LayerMask.NameToLayer("ObjectItem"); obj.layer = layer >= 0 ? layer : 0;
        var collider = obj.AddComponent<SphereCollider>(); collider.radius = 1.7f;
        collider.center = Vector3.up * .6f; collider.isTrigger = true;
        var shop = obj.AddComponent<SectorShop>();
        shop.ConfigureForRaid(sector, settings.merchantConfig, raid.State.layout.stageSeed,
            settings.merchantInitialMoney, settings.merchantOpenChannel, settings.merchantOverlayChannel);
        var label = new GameObject("MerchantLabel"); label.transform.SetParent(obj.transform, false);
        label.transform.localPosition = Vector3.up * 1.6f; label.transform.localRotation = Quaternion.Euler(90, 0, 0);
        var text = label.AddComponent<TMPro.TextMeshPro>();
        text.font = settings.prototypeFont != null ? settings.prototypeFont : TMPro.TMP_Settings.defaultFontAsset;
        text.fontSize = 3; text.alignment = TMPro.TextAlignmentOptions.Center;
        text.rectTransform.sizeDelta = new Vector2(5, 2); text.raycastTarget = false;
        text.color = Color.yellow; text.text = "긴급보급 상인";
    }
}
