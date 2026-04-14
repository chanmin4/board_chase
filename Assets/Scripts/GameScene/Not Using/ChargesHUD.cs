/*
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ChargesHUD : MonoBehaviour
{
    [Header("Refs")]
    public PlayerDisk playerdisk;     // PlayerDisk에 붙어있는 DiskLauncher
    public TMP_Text label;            // "CHARGES 2/∞" 같은 텍스트
    [Tooltip("아이콘(피프)들을 담을 부모 Transform (Horizontal/Vertical Layout Group 권장)")]
    public Transform ChargeRoot;
    [Tooltip("UI Image 프리팹(비활성 템플릿). Source Image=UI/Sprite 추천")]
    public GameObject ChargeImage;

    [Header("Style")]
    public string prefix = "CHARGES ";
    public bool showMaxInLabel = true;      // 2/5 or 2/∞
    public Color activeColor   = new Color(1f, 1f, 1f, 1f);
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.25f);
    public Vector2 pipSize = new Vector2(50, 50);
    [Tooltip("maxCharges<=0(무제한)일 때도 피프를 보여줄지")]
    public bool showPipsForUnlimited = false;
    [Tooltip("무제한인 경우 표시할 피프 개수(현재 보유량까지만 채움)")]
    public int unlimitedPipCount = 5;

    // 내부
    readonly List<Image> pips = new List<Image>();

    void Awake()
    {
        if (!launcher) launcher = FindAnyObjectByType<DiskLauncher>();
        if (launcher != null)
        {
            launcher.OnChargesChanged += OnChargesChanged;
        }
    }
    void Start()
    {
        // 씬 로드 순서에 따라 초기 이벤트를 못 받았을 수도 있으니 강제 갱신
        if (launcher) ForceRefreshFromLauncher();
    }
    void OnDestroy()
    {
        if (launcher != null)
            launcher.OnChargesChanged -= OnChargesChanged;
    }

    void OnChargesChanged(int current, int maxFromEvent)
    {
        Refresh(current, maxFromEvent);
    }

    void ForceRefreshFromLauncher()
    {
        if (!launcher) return;
        int current = launcher.Charges;
        int max = launcher.maxCharges > 0 ? launcher.maxCharges : Mathf.Max(launcher.baseCharges, current);
        Refresh(current, max);
    }

    void Refresh(int current, int max)
    {
        // 1) 라벨
        if (label)
        {
            label.text = $"{prefix}{current}";
        }

        // 2) 피프(아이콘)
        if (!ChargeRoot || (!ChargeImage && pips.Count==0)) return;

        bool unlimited = launcher && launcher.maxCharges <= 0;
        int targetMaxIcons = unlimited ? unlimitedPipCount : max;

        BuildPips(targetMaxIcons);

        // 활성/비활성 색 적용
        for (int i = 0; i < pips.Count; i++)
        {
            bool on = i < current;
            pips[i].color = on ? activeColor : inactiveColor;
        }

        // 무제한인데 피프를 숨기고 싶은 경우
        if (unlimited && !showPipsForUnlimited)
        {
            if (ChargeRoot.gameObject.activeSelf) ChargeRoot.gameObject.SetActive(false);
        }
        else
        {
            if (!ChargeRoot.gameObject.activeSelf) ChargeRoot.gameObject.SetActive(true);
        }
    }

    void BuildPips(int count)
    {
        // 이미 있으면 재사용/감소
        for (int i = pips.Count - 1; i >= count; i--)
        {
            if (pips[i]) Destroy(pips[i].gameObject);
            pips.RemoveAt(i);
        }

        // 부족분 생성
        for (int i = pips.Count; i < count; i++)
        {
            GameObject src = ChargeImage;
            if (!src)
            {
                // 안전장치: 템플릿이 없으면 기본 Image 하나 생성
                src = new GameObject("Pip", typeof(RectTransform), typeof(Image));
                var img = src.GetComponent<Image>();
                img.sprite = UnityEngine.Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
                img.type = Image.Type.Sliced;
            }

            var go = Instantiate(src, ChargeRoot);
            go.name = $"Pip_{i}";
            go.SetActive(true);

            var rt = go.GetComponent<RectTransform>();
            if (rt) { rt.sizeDelta = pipSize; rt.localScale = Vector3.one; }

            var im = go.GetComponent<Image>();
            if (!im) im = go.AddComponent<Image>();
            im.color = inactiveColor;

            pips.Add(im);
        }
    }
}
*/