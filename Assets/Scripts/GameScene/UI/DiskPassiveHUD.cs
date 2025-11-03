using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[DisallowMultipleComponent]
public class DiskPassiveHUD : MonoBehaviour
{
    [Header("Refs")]
    public DiskPassiveBank bank;
    public TextMeshProUGUI summaryText;

    [Header("Slide Panel")]
    [Tooltip("슬라이드할 패널(보통 이 스크립트가 붙은 오브젝트의 RectTransform)")]
    public RectTransform panel;
    [Tooltip("열렸을 때 패널의 anchoredPosition")]
    public Vector2 openAnchoredPos;
    [Tooltip("닫혔을 때 패널의 anchoredPosition")]
    public Vector2 closedAnchoredPos;
    [Tooltip("슬라이드 시간(초). 언스케일드 시간 기준")]
    public float slideDuration = 0.25f;
    [Tooltip("게임 시작 시 열린 상태로 시작할지")]
    public bool startOpen = false;
    [Tooltip("언스케일드 시간 사용(일시정지/슬로우 중에도 동작)")]
    public bool useUnscaledTime = true;

    [Header("Toggle Input")]
    public Button toggleButton;
    [Tooltip("단축키로 열고닫기 활성화")]
    public bool enableHotkey = false;
    public KeyCode hotkey = KeyCode.Tab;

    bool isOpen;
    Coroutine slideCo;

    void Awake()
    {
        if (!bank) bank = FindAnyObjectByType<DiskPassiveBank>();
        if (!panel) panel = GetComponent<RectTransform>();
    }

    void Start()
    {
        // 시작 상태 적용
        isOpen = startOpen;
        if (panel) panel.anchoredPosition = isOpen ? openAnchoredPos : closedAnchoredPos;

        if (toggleButton)
            toggleButton.onClick.AddListener(Toggle);

        Refresh(); // 최초 갱신
    }

    void OnEnable()
    {
        if (bank) bank.OnChanged.AddListener(Refresh);
        Refresh();
    }

    void OnDisable()
    {
        if (bank) bank.OnChanged.RemoveListener(Refresh);
        if (toggleButton)
            toggleButton.onClick.RemoveListener(Toggle);
    }

    void Update()
    {
        if (enableHotkey && Input.GetKeyDown(hotkey))
            Toggle();
    }

    public void Toggle()
    {
        SetOpen(!isOpen);
    }

    public void Open()  => SetOpen(true);
    public void Close() => SetOpen(false);

    public void SetOpen(bool open)
    {
        if (!panel) return;
        if (isOpen == open) return;

        isOpen = open;
        if (slideCo != null) StopCoroutine(slideCo);
        slideCo = StartCoroutine(SlideCo(isOpen ? openAnchoredPos : closedAnchoredPos));
    }

    IEnumerator SlideCo(Vector2 target)
    {
        Vector2 start = panel.anchoredPosition;
        float t = 0f;
        float dur = Mathf.Max(0.01f, slideDuration);

        while (t < dur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / dur); // 부드러운 가감속
            panel.anchoredPosition = Vector2.LerpUnclamped(start, target, k);
            yield return null;
        }
        panel.anchoredPosition = target;
        slideCo = null;
    }

    void Refresh()
    {
        if (!summaryText || bank == null) return;

        var groups = bank.GetAcquired().GroupBy(d => d.id);
        var sb = new StringBuilder();

        foreach (var g in groups)
        {
            var any = g.First();
            int stacks = bank.GetStacks(any.id);
            sb.AppendLine($"{any.title} x{stacks}");
        }
        summaryText.text = sb.ToString();
    }
}
