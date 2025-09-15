using System;
using UnityEngine;
using UnityEngine.UI;

public class SkinGridItem : MonoBehaviour
{
    [Header("Refs")]
    public Toggle toggle;       // 루트에 Toggle
    public Image icon;          // 스킨 아이콘(클릭 영역)
    public Image lockOverlay;   // 잠금 오버레이(위에 표시)
    public string _id;
    Action<string, bool> _onToggle;
    bool _unlocked;
    void Reset()
    {
        if (toggle && icon) toggle.targetGraphic = icon;
        if (lockOverlay) lockOverlay.raycastTarget = false; // 잠금은 표시만
    }
    public void Bind(string id, Sprite sprite, bool unlocked, bool isEquipped,
                     ToggleGroup group, Action<string, bool> onToggle,
                     Sprite lockSprite = null)
    {
        _id = id;
        _onToggle = onToggle;
        _unlocked = unlocked;
        // 아이콘 셋업
        if (icon)
        {
            icon.sprite = sprite;
            icon.enabled = unlocked && sprite != null; // 해금 전엔 아이콘 비활성
        }

        // 잠금 오버레이
        if (lockOverlay)
        {
            lockOverlay.enabled = !unlocked;
            if (!unlocked && lockSprite) lockOverlay.sprite = lockSprite;
            lockOverlay.raycastTarget = false;
        }

        if (toggle)
        {
            toggle.group = group;
            // 선택된(장착) 항목은 클릭 불가, 항상 On
            toggle.interactable = unlocked && !isEquipped;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.SetIsOnWithoutNotify(isEquipped);

            // 포커스 이동/키보드 네비게이션 방지
            var nav = new Navigation { mode = Navigation.Mode.None };
            toggle.navigation = nav;
            toggle.onValueChanged.AddListener(on =>
            {
                if (on) _onToggle?.Invoke(_id, true);
            });
        }
    }
    public void SetEquippedVisual(bool isEquipped)
    {
        if (!toggle) return;
        toggle.SetIsOnWithoutNotify(isEquipped);
        toggle.interactable = _unlocked && !isEquipped;
    }
}
