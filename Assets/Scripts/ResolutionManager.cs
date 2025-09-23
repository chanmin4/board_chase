// ResolutionManager.cs  (단일 파일 버전)

// --- usings은 파일 맨 위에 ---
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
#endif

public class ResolutionManager : MonoBehaviour
{
    public static ResolutionManager Instance { get; private set; }

    [Header("General")]
    public bool autoManage = true;
    public bool manageCanvases = true;
    public bool manageMainCamera = false;
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    public Vector2 targetAspect = new Vector2(16, 9);

    [Header("Windowed Defaults")]
    public int windowedDefaultW = 1600;
    public int windowedDefaultH = 900;
    public bool preferBorderlessOnFullscreen = true;

    int lastW, lastH;
    FullScreenMode lastMode;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (preferBorderlessOnFullscreen && IsFullscreen())
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;

        ApplyAll(force:true);
    }

    void OnEnable()  => SceneManager.activeSceneChanged += OnSceneChanged;
    void OnDisable() => SceneManager.activeSceneChanged -= OnSceneChanged;
    void OnSceneChanged(Scene _, Scene __) => ApplyAll(force:true);

    void Update()
    {
        if (!autoManage) return;
        if (Screen.width != lastW || Screen.height != lastH || Screen.fullScreenMode != lastMode)
            ApplyAll();
    }

    public void ApplyAll(bool force=false)
    {
        lastW = Screen.width;
        lastH = Screen.height;
        lastMode = Screen.fullScreenMode;

        if (IsWindowed())
        {
            ApplySafeWindowed(Screen.width, Screen.height);
        }
        else if (preferBorderlessOnFullscreen
              && Screen.fullScreenMode != FullScreenMode.FullScreenWindow)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }

        if (manageCanvases)  FitAllCanvasScalers();
        if (manageMainCamera) FitMainCameraViewport();
    }

    bool IsFullscreen()
    {
        var m = Screen.fullScreenMode;
        return m == FullScreenMode.FullScreenWindow
            || m == FullScreenMode.ExclusiveFullScreen
            || m == FullScreenMode.MaximizedWindow;
    }
    bool IsWindowed() => Screen.fullScreenMode == FullScreenMode.Windowed;

    // ---- UI 안전 스케일 ----
    void FitAllCanvasScalers()
    {
        float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float refAspect = referenceResolution.x / referenceResolution.y;
        float match = (screenAspect > refAspect) ? 1f : 0f;

        var scalers = Resources.FindObjectsOfTypeAll<CanvasScaler>();
        foreach (var sc in scalers)
        {
            if (!sc) continue;
            var canvas = sc.GetComponent<Canvas>();
            if (canvas && canvas.renderMode == RenderMode.WorldSpace) continue;

            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = referenceResolution;
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            sc.matchWidthOrHeight = match;
        }
    }

    // ---- 카메라 16:9 안전 뷰포트(선택) ----
    void FitMainCameraViewport()
    {
        var cam = Camera.main;
        if (!cam) return;

        float windowAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float target = targetAspect.x / targetAspect.y;

        if (windowAspect > target)
        {
            float inset = 1f - target / windowAspect; // 좌우 필러박스
            cam.rect = new Rect(inset * 0.5f, 0f, 1f - inset, 1f);
        }
        else
        {
            float inset = 1f - windowAspect / target; // 상하 레터박스
            cam.rect = new Rect(0f, inset * 0.5f, 1f, 1f - inset);
        }
    }

    // ---- 창모드: WorkArea 내 안전 적용 ----
    public void ApplySafeWindowed(int reqW, int reqH)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        ResolutionSafeWin.ApplyWindowedSafe(reqW, reqH, 16f/9f, center:true);
#else
        int sw = Display.main.systemWidth;
        int sh = Display.main.systemHeight;
        const int marginW = 80, marginH = 120;
        int maxW = Mathf.Max(320, sw - marginW);
        int maxH = Mathf.Max(240, sh - marginH);

        int w = Mathf.Min(reqW, maxW);
        int h = Mathf.Min(reqH, maxH);

        float target = 16f / 9f;
        if (w / (float)h > target) w = Mathf.RoundToInt(h * target);
        else                       h = Mathf.RoundToInt(w / target);

        if (Screen.fullScreenMode != FullScreenMode.Windowed)
            Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.SetResolution(w, h, FullScreenMode.Windowed);
#endif
    }

    // 편의 API
    public void RequestFullscreen(bool on)
    {
        if (on)
        {
            Screen.fullScreenMode = preferBorderlessOnFullscreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.ExclusiveFullScreen;
        }
        else
        {
            ApplySafeWindowed(windowedDefaultW, windowedDefaultH);
        }
        ApplyAll(force:true);
    }
}

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
// ----- Windows 전용 WorkArea 계산 (별도 using은 위로 올렸음) -----
static class ResolutionSafeWin
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct RECT { public int left, top, right, bottom; }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }

    const int MONITOR_DEFAULTTONEAREST = 2;
    const int GWL_STYLE = -16, GWL_EXSTYLE = -20;
    const uint SWP_NOSIZE = 0x0001, SWP_NOZORDER = 0x0004;

    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern System.IntPtr GetActiveWindow();
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern System.IntPtr MonitorFromWindow(System.IntPtr hwnd, uint dwFlags);
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)] static extern bool GetMonitorInfo(System.IntPtr hMonitor, ref MONITORINFO mi);
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern System.IntPtr GetWindowLongPtr(System.IntPtr hWnd, int nIndex);
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool AdjustWindowRectEx(ref RECT lpRect, System.IntPtr dwStyle, bool bMenu, System.IntPtr dwExStyle);
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    static void GetWorkArea(out int wx, out int wy, out int ww, out int wh)
    {
        var hwnd = GetActiveWindow();
        var mi = new MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST), ref mi);
        wx = mi.rcWork.left; wy = mi.rcWork.top;
        ww = mi.rcWork.right - mi.rcWork.left;
        wh = mi.rcWork.bottom - mi.rcWork.top;
    }

    static void GetNonClientAdd(out int addW, out int addH)
    {
        var hwnd   = GetActiveWindow();
        var style  = GetWindowLongPtr(hwnd, GWL_STYLE);
        var ex     = GetWindowLongPtr(hwnd, GWL_EXSTYLE);

        var r = new RECT { left = 0, top = 0, right = 1920, bottom = 1080 };
        AdjustWindowRectEx(ref r, style, false, ex);
        int outerW = r.right - r.left;
        int outerH = r.bottom - r.top;
        addW = outerW - 1920;
        addH = outerH - 1080;
    }

    public static void ApplyWindowedSafe(int reqW, int reqH, float targetAspect, bool center)
    {
        GetWorkArea(out int wx, out int wy, out int ww, out int wh);
        GetNonClientAdd(out int addW, out int addH);

        int maxClientW = Mathf.Max(320, ww - addW);
        int maxClientH = Mathf.Max(240, wh - addH);

        int w = Mathf.Min(reqW, maxClientW);
        int h = Mathf.Min(reqH, maxClientH);

        float cur = w / (float)h;
        if (cur > targetAspect) w = Mathf.RoundToInt(h * targetAspect);
        else                    h = Mathf.RoundToInt(w / targetAspect);

        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.SetResolution(w, h, FullScreenMode.Windowed);

        if (center)
        {
            int x = wx + (ww - (w + addW)) / 2;
            int y = wy + (wh - (h + addH)) / 2;
            SetWindowPos(GetActiveWindow(), System.IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
        }
    }
}
#endif
