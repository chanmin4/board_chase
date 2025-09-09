using System;
using UnityEngine;
using UnityEngine.UI;

public class SkinGridItem : MonoBehaviour
{
    [Header("Refs")]
    public Toggle toggle;       // 루트 Toggle
    public Image icon;          // 아이콘
    public Image lockOverlay;   // 잠금 표시(자물쇠 스프라이트/반투명)

    string _id;
    Action<string, bool> _onToggle; // (id, isOn)

    public void Bind(string id, Sprite sprite, bool unlocked, bool isEquipped,
                     ToggleGroup group, Action<string, bool> onToggle,
                     Sprite lockSprite = null)
    {
        _id = id;
        _onToggle = onToggle;

        if (toggle)
        {
            toggle.group = group;
            // 잠금이면 토글 비활성화(장착 불가), 풀리면 활성
            toggle.interactable = unlocked;
            // 초기 선택 상태
            toggle.isOn = isEquipped;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(on =>
            {
                if (on) _onToggle?.Invoke(_id, on);
            });
        }

        if (icon)
        {
            icon.enabled = sprite != null;
            icon.sprite  = sprite;
        }

        if (lockOverlay)
        {
            lockOverlay.enabled = !unlocked;
            if (!unlocked && lockSprite) lockOverlay.sprite = lockSprite;
        }
    }
}
