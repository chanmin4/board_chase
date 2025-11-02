using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// 레벨업 시 3개 패시브 제시 → 선택하면 적용.
/// - 시간 정지(Time.timeScale=0) / 선택 후 원복
/// - UI는 인스펙터로 간단 연결 (없으면 자동선택 폴백)
public class LevelUpChoiceUI : MonoBehaviour
{
    [Header("Refs")]
    public DiskInkLeveler leveler;
    public DiskPassiveBank bank;

    [Header("Pool")]
    public PassiveUpgradeDef[] library;     // 전체 후보 풀 (SO 배열)

    [Header("Panel & Slots")]
    public GameObject panel;                // 전체 패널(토글용)
    public Button[] optionButtons = new Button[3];
    public Image[]  optionIcons   = new Image[3];
    public TextMeshProUGUI[] optionTitles = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] optionDescs  = new TextMeshProUGUI[3];

    float savedScale = 1f;
    PassiveUpgradeDef[] current = new PassiveUpgradeDef[3];

    void Awake()
    {
        if (!leveler) leveler = FindAnyObjectByType<DiskInkLeveler>();
        if (!bank)    bank    = FindAnyObjectByType<DiskPassiveBank>();
    }

    void OnEnable()  { if (leveler) leveler.OnLevelUp.AddListener(OnLevelUp); }
    void OnDisable() { if (leveler) leveler.OnLevelUp.RemoveListener(OnLevelUp); }

    void OnLevelUp(int newLv)
    {
        // 1) 후보 3개 픽
        var offers = Pick3();
        for (int i = 0; i < 3; i++) current[i] = (i < offers.Count) ? offers[i] : null;

        // 2) 시간 정지 + 패널 ON
        savedScale = Time.timeScale;
        Time.timeScale = 0f;

        if (panel) panel.SetActive(true);

        // 3) 슬롯 채우기
        for (int i = 0; i < 3; i++)
        {
            var def = current[i];

            if (optionButtons != null && i < optionButtons.Length && optionButtons[i])
            {
                int idx = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => Select(idx));
                optionButtons[i].interactable = def != null;
            }
            if (optionIcons != null && i < optionIcons.Length && optionIcons[i])
                optionIcons[i].sprite = def ? def.icon : null;

            if (optionTitles != null && i < optionTitles.Length && optionTitles[i])
                optionTitles[i].text = def ? def.title : "-";

            if (optionDescs != null && i < optionDescs.Length && optionDescs[i])
                optionDescs[i].text = def ? def.description : "";
        }

        // UI가 하나도 연결 안되어 있으면 폴백: 첫 옵션 자동 선택
        if ((optionButtons == null || optionButtons.Length == 0) && offers.Count > 0)
            Select(0);
    }

    void Select(int idx)
    {
        var def = (idx >= 0 && idx < current.Length) ? current[idx] : null;
        if (def && bank) bank.Apply(def);

        if (panel) panel.SetActive(false);
        Time.timeScale = savedScale;
    }

    List<PassiveUpgradeDef> Pick3()
    {
        var pool = new List<PassiveUpgradeDef>();
        if (library != null)
        {
            foreach (var d in library)
                if (d) pool.Add(d);
        }

        // 간단한 중복 방지 샘플링
        var res = new List<PassiveUpgradeDef>(3);
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int k = Random.Range(0, pool.Count);
            res.Add(pool[k]);
            pool.RemoveAt(k);
        }
        return res;
    }
}
