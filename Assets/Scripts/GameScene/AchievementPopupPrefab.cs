using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AchievementPopupPrefab : MonoBehaviour
{
    [Header("Bind in Prefab")]
    public GameObject root;                 // 비우면 이 GO 자체 사용
    public TMP_Text titleText;
    public TMP_Text descText;
    public Image achieveImage;              // 선택 (없어도 됨)
    public Button clickAnywhereButton;      // 전체 클릭 영역(없으면 패널 버튼으로 대체)

    void Reset()
    {
        if (!root) root = gameObject;
        if (!titleText) titleText = GetComponentInChildren<TMP_Text>(true);
        if (!clickAnywhereButton) clickAnywhereButton = GetComponentInChildren<Button>(true);
    }
}
