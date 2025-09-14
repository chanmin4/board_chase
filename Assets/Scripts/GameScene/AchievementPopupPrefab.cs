using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AchievementPopupPrefab : MonoBehaviour
{
    [Header("Bind in Prefab")]
    public GameObject root;                 // 팝업 루트(비우면 프리팹 GO 자체를 root로 사용)
    public TMP_Text titleText;              // 제목 TMP
    public TMP_Text descText;               // 설명 TMP
    public Image achieveImage;
    public Button clickAnywhereButton;      // 전체 클릭 영역(없으면 ShowOnce 클릭 기능만 비활성)
}
