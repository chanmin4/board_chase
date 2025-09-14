using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class AchievementPopupManager : MonoBehaviour
{
    [Header("Prefab & Parent")]
    [Tooltip("업적 팝업 프리팹(안쪽 사각형). 이 프리팹엔 AchievementPopupPrefab 컴포넌트를 붙여두세요.")]
    public GameObject popupPrefab;

    [Tooltip("생성될 부모. 보통 SuccessPanel/ScrollView/Viewport/Content 할당")]
    public Transform popupParent;   // 비우면 매니저 오브젝트 하위

    [Header("Timing (Queue mode)")]
    public float showSeconds = 2.0f;
    public float gapSeconds  = 0.3f;

    readonly Queue<(string title, string desc)> q = new();
    bool playing;

    /// <summary>
    /// 큐에 넣고 자동 재생(시간 지나면 닫힘)
    /// </summary>
    public void Enqueue(string title, string desc)
    {
        q.Enqueue((title, desc));
        if (!playing) StartCoroutine(PlayQueue());
    }

    /// <summary>
    /// 한 장만 띄우고, 클릭(또는 최소시간 경과) 시 종료. (TimeScale=0에서도 동작)
    /// </summary>
    public IEnumerator ShowOnce(string title, string desc, bool requireClick = true, float minShowSeconds = 0f)
    {
        var refs = Spawn();
        if (!refs) yield break;

        if (refs.titleText) refs.titleText.text = title;
        if (refs.descText)  refs.descText.text  = desc;

        bool clicked = false;
        if (requireClick && refs.clickAnywhereButton)
        {
            refs.clickAnywhereButton.onClick.RemoveAllListeners();
            refs.clickAnywhereButton.onClick.AddListener(() => clicked = true);
        }

        refs.root.SetActive(true);

        float t = 0f;
        while (true)
        {
            bool timeOk = t >= minShowSeconds;
            if (requireClick)
            {
                if (timeOk && clicked) break;
            }
            else
            {
                if (timeOk) break;
            }
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Destroy(refs.gameObject);
    }

    IEnumerator PlayQueue()
    {
        playing = true;
        while (q.Count > 0)
        {
            var (t, d) = q.Dequeue();
            var refs = Spawn();
            if (!refs) yield break;

            if (refs.titleText) refs.titleText.text = t;
            if (refs.descText)  refs.descText.text  = d;

            refs.root.SetActive(true);
            yield return new WaitForSecondsRealtime(showSeconds);
            Destroy(refs.gameObject);
            yield return new WaitForSecondsRealtime(gapSeconds);
        }
        playing = false;
    }

    AchievementPopupPrefab Spawn()
    {
        if (!popupPrefab)
        {
            Debug.LogWarning("[AchievementPopupManager] popupPrefab not set");
            return null;
        }
        var inst = Instantiate(popupPrefab, popupParent ? popupParent : transform);
        var refs = inst.GetComponent<AchievementPopupPrefab>();
        if (!refs)
        {
            Debug.LogError("[AchievementPopupManager] prefab must have AchievementPopupPrefab component.");
            Destroy(inst);
            return null;
        }
        if (!refs.root) refs.root = inst; // root 미지정 시 인스턴스 자체 사용
        refs.root.SetActive(false);
        return refs;
    }
}
