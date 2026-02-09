using UnityEngine;

namespace HitWaves.Core
{
    public static class DebugLogger
    {
        private static bool _isEnabled = true;

        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public static void Log(string message, Object context = null)
        {
            if (!_isEnabled) return;

            if (context != null)
                Debug.Log(message, context);
            else
                Debug.Log(message);
        }

        public static void Log(string tag, string message, Object context = null)
        {
            if (!_isEnabled) return;

            string formatted = $"[{tag}] {message}";
            if (context != null)
                Debug.Log(formatted, context);
            else
                Debug.Log(formatted);
        }

        public static void LogWarning(string message, Object context = null)
        {
            if (!_isEnabled) return;

            if (context != null)
                Debug.LogWarning(message, context);
            else
                Debug.LogWarning(message);
        }

        public static void LogWarning(string tag, string message, Object context = null)
        {
            if (!_isEnabled) return;

            string formatted = $"[{tag}] {message}";
            if (context != null)
                Debug.LogWarning(formatted, context);
            else
                Debug.LogWarning(formatted);
        }

        public static void LogError(string message, Object context = null)
        {
            if (!_isEnabled) return;

            if (context != null)
                Debug.LogError(message, context);
            else
                Debug.LogError(message);
        }

        public static void LogError(string tag, string message, Object context = null)
        {
            if (!_isEnabled) return;

            string formatted = $"[{tag}] {message}";
            if (context != null)
                Debug.LogError(formatted, context);
            else
                Debug.LogError(formatted);
        }
    }
}
