using UnityEngine;

namespace AetherEcho.Networking
{
    public static class MovementDebugLogger
    {
        public static bool Enabled = true;

        public static void Log(string phase, string details)
        {
            if (!Enabled)
            {
                return;
            }

            Debug.Log("[MoveDebug] " + phase + " | " + details);
        }

        public static void LogEnemy(string entityLabel, uint netId, string phase, string details)
        {
            if (!Enabled)
            {
                return;
            }

            Debug.Log("[EnemyMoveDebug] " + entityLabel + "#" + netId + " | " + phase + " | " + details);
        }

        public static string FormatVector(Vector3 value)
        {
            return "(" + value.x.ToString("F3") + ", " + value.y.ToString("F3") + ", " + value.z.ToString("F3") + ")";
        }

        public static float HorizontalDistance(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to);
        }
    }
}
