/*
 * Role hierarchy: Core > Application > Pause
 * Primary responsibility: Implements the focused GamePause responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// 스케일드 타임 기반 전역 일시정지(초간단)
public static class GamePause
{
    private static bool  _isPaused;
    private static float _prevTimeScale = 1f;

    public static bool IsPaused => _isPaused;

    // Purpose: Responds to the on callback.
    public static void On()
    {
        if (_isPaused) return;
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _isPaused = true;
    }

    // Purpose: Performs the off workflow for this component.
    public static void Off()
    {
        if (!_isPaused) return;
        Time.timeScale = (_prevTimeScale <= 0f) ? 1f : _prevTimeScale;
        _isPaused = false;
    }
}
