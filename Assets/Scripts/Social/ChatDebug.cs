using UnityEngine;

namespace AetherEcho.Social
{
    public static class ChatDebug
    {
        public const bool Enabled = true;
        private const string Prefix = "[ChatDebug]";

        public static void Log(string message)
        {
            if (Enabled)
            {
                Debug.Log(Prefix + " " + message);
            }
        }

        public static void LogWarning(string message)
        {
            if (Enabled)
            {
                Debug.LogWarning(Prefix + " " + message);
            }
        }
    }
}
