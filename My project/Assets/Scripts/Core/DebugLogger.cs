using UnityEngine;

public static class DebugLogger
{
    public static bool IsEnabled = true;

    public static void Log(string tag, string message, Object context = null)
    {
        if (!IsEnabled) return;
        Debug.Log($"[{tag}] {message}", context);
    }

    public static void LogWarning(string tag, string message, Object context = null)
    {
        if (!IsEnabled) return;
        Debug.LogWarning($"[{tag}] {message}", context);
    }

    public static void LogError(string tag, string message, Object context = null)
    {
        if (!IsEnabled) return;
        Debug.LogError($"[{tag}] {message}", context);
    }
}
