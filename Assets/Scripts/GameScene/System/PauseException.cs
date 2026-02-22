using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-900)]
public class PauseExceptionRouter : MonoBehaviour
{
    public bool handleOnlyWhenPaused = true;

    [Header("Tab (Status)")]
    public KeyCode tabKey = KeyCode.Tab;
    public UnityEvent onTab;   // 인스펙터에서 DiskPassiveHUD의 Toggle 메서드 연결

    [Header("Esc (Settings)")]
    public KeyCode escKey = KeyCode.Escape;
    public UnityEvent onEsc;   // 인스펙터에서 설정 패널 Open 메서드 연결

    void Update()
    {

        if (handleOnlyWhenPaused && !GamePause.IsPaused) return;
        Debug.Log($"[R] paused={GamePause.IsPaused} ts={Time.timeScale}");
        if (Input.GetKeyDown(tabKey)) onTab?.Invoke();
        if (Input.GetKeyDown(escKey)) onEsc?.Invoke();
    }
}
