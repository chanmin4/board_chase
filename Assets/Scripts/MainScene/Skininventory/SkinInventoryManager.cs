using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkinInventoryManager : MonoBehaviour
{
    [Header("Root")]
    public GameObject panelRoot;
    public Button closeButton;

    [Header("Grid")]
    public Transform content;          // ScrollView/Viewport/Content
    public ToggleGroup toggleGroup;
    public SkinGridItem itemPrefab;    // 프리팹에 SkinGridItem 붙어있어야 함
    public Sprite lockSprite;          // 잠금 오버레이 스프라이트(선택)

    ProgressManager PM => ProgressManager.Instance;

    void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(() => panelRoot.SetActive(false));
        if (panelRoot) panelRoot.SetActive(false);
    }

    public void Open()
    {
        RewardDB.EnsureLoaded();
        if (panelRoot) panelRoot.SetActive(true);
        Rebuild();
    }

    void Rebuild()
    {
        if (!PM || PM.Data == null || !content || !itemPrefab) return;

        // 비우기
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        // 보유/장착 상태
        var equipped = string.IsNullOrEmpty(PM.Data.equippedSkinId) ? "skin_default" : PM.Data.equippedSkinId;
        var unlocked = new HashSet<string>(PM.Data.unlockedSkins);
        if (!unlocked.Contains("skin_default")) unlocked.Add("skin_default");

        // 카탈로그: Resources/Rewards 아래의 SkinRewardSO 전체
        var allSkins = Resources.LoadAll<SkinRewardSO>("Rewards");

        // 아이템 생성
        foreach (var so in allSkins)
        {
            if (!so) continue;
            bool has = unlocked.Contains(so.id);
            bool isEquipped = (so.id == equipped);

            var item = Instantiate(itemPrefab, content);
            item.Bind(
                id: so.id,
                sprite: so.icon,
                unlocked: has,
                isEquipped: isEquipped,
                group: toggleGroup,
                onToggle: OnToggleEquip,
                lockSprite: lockSprite
            );
        }
    }

    void OnToggleEquip(string id, bool isOn)
    {
        if (!isOn || PM == null || PM.Data == null) return;

        // 잠금 안전 가드
        if (!PM.Data.unlockedSkins.Contains(id) && id != "skin_default") return;

        PM.Data.equippedSkinId = id;
        PM.Save();
        PM.OnUnlocksChanged?.Invoke();
        Debug.Log($"[SkinGrid] Equip = {id}");
    }
}