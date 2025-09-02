using UnityEngine;
using UnityEngine.UI;

public class ToggleAlpha : MonoBehaviour
{
    public Toggle toggle;
    public CanvasGroup canvasGroup;
    [Range(0f,1f)] public float offAlpha = 0.6f;  // 안 눌린 상태(살짝 투명)
    [Range(0f,1f)] public float onAlpha  = 1.0f;  // 눌린(선택) 상태

    void Reset() {
        toggle = GetComponentInChildren<Toggle>(true);
        canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Awake() {
        if (!toggle) toggle = GetComponentInChildren<Toggle>(true);
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        // 리스너 등록
        if (toggle) toggle.onValueChanged.AddListener(Sync);
        // 초기 반영은 외부에서 SetIsOnWithoutNotify 한 뒤 Sync를 한 번 더 호출해줄 것
    }

    public void Sync(bool isOn) {
        if (!canvasGroup) return;
        canvasGroup.alpha = isOn ? onAlpha : offAlpha;
    }
}
