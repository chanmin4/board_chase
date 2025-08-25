using System;
using System.Globalization;
using UnityEngine;

public static class TimeUtils
{
    // 문자열 "mm:ss", "mm:ss.f", "hh:mm:ss" 등을 초(float)로
    public static float ParseClockToSeconds(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0f;
        if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts))
            return (float)ts.TotalSeconds;

        // 실패하면 수동 파싱(소수점 문화권 이슈 방지)
        s = s.Replace(',', '.');
        var parts = s.Split(':');
        if (parts.Length == 2)
        {
            int m = int.TryParse(parts[0], out var mi) ? mi : 0;
            float sec = float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var sf) ? sf : 0f;
            return m * 60f + sec;
        }
        return 0f;
    }

    public static int SecondsToMs(float seconds) => Mathf.RoundToInt(seconds * 1000f);
    public static float MsToSeconds(int ms) => ms * 0.001f;

    // 저장값(밀리초)을 보기 좋은 "mm:ss.t" 텍스트로
    public static string FormatMsClock(int ms, int decimals = 1)
    {
        if (ms < 0) ms = 0;
        var ts = TimeSpan.FromMilliseconds(ms);
        int minutes = (int)ts.TotalMinutes;
        float secFrac = ts.Seconds + ts.Milliseconds / 1000f;
        string fmt = decimals <= 0 ? "00" : $"00.{new string('0', decimals)}";
        return $"{minutes:00}:{secFrac.ToString(fmt, CultureInfo.InvariantCulture)}";
    }
}
