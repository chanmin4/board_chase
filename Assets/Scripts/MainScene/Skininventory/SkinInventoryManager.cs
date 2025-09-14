using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkinInventoryManager : MonoBehaviour
{
    [Header("Root")]
    public GameObject panelRoot;
    public Button closeButton;

    [Header("Grid")]
    public Transform content;           // ScrollView/Viewport/Content
    public ToggleGroup toggleGroup;     // 단일 선택
    public SkinGridItem itemPrefab;     // 프리팹
    public Sprite lockSprite;           // 잠금 스프라이트(선택)
    [Header("skin so 수동 등록 (Inspector 등록)")]
    public List<SkinRewardSO> catalog = new List<SkinRewardSO>(); // ← 여기 수동 등록
    readonly List<SkinGridItem> _items = new();
    ProgressManager PM => ProgressManager.Instance;


    void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(() => panelRoot.SetActive(false));
        if (panelRoot) panelRoot.SetActive(false);
    }

    public void Open()
    {
        // ★ 패널 열릴 때 그리드 하위의 모든 아이콘/락을 초기화(혹시 프리팹이 켜져 저장된 경우 대비)
        if (string.IsNullOrEmpty(PM.Data.equippedSkinId))
        {
            PM.Data.equippedSkinId = "skin_default";
            PM.Save();
        }

        RewardDB.SyncFrom(catalog);
        if (panelRoot) panelRoot.SetActive(true);
        Rebuild();
    }


    void Rebuild()
    {
        if (!PM || PM.Data == null || !content || !itemPrefab) return;

        // 클리어
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        // 상태
        var equipped = string.IsNullOrEmpty(PM.Data.equippedSkinId) ? "skin_default" : PM.Data.equippedSkinId;
        var unlocked = new HashSet<string>(PM.Data.unlockedSkins);
        if (!unlocked.Contains("skin_default")) unlocked.Add("skin_default");


        // 생성
        foreach (var so in catalog)
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
            _items.Add(item);
        }
    }

    void OnToggleEquip(string id, bool isOn)
    {
        if (!isOn || PM == null || PM.Data == null) return;
        if (!PM.Data.unlockedSkins.Contains(id) && id != "skin_default") return;

        PM.Data.equippedSkinId = id;
        PM.Save();
        ApplyEquippedState();  // ← 리빌드 없이 상태만 업데이트
    }
    void ApplyEquippedState()
    {
        var eq = string.IsNullOrEmpty(PM.Data.equippedSkinId) ? "skin_default" : PM.Data.equippedSkinId;
        foreach (var it in _items)
            if (!string.IsNullOrEmpty(it._id))
                it.SetEquippedVisual(it._id == eq);
        // name 안쓸 거면  교체
        //  name 비교 대신 다음처럼 ID 비교를 쓰려면 SkinGridItem에 public Id 프로퍼티 하나 노출:
        // it.SetEquippedVisual(it.Id == eq);
    }

}
