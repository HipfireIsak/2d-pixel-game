using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.World;

namespace AetherEcho.UI
{
    public class WorldInteractableHintUI : MonoBehaviour
    {
        private GUIStyle hintStyle;

        private void OnGUI()
        {
            EnsureStyles();
            Camera camera = CombatPickUtility.ResolveGameplayCamera();
            if (camera == null)
            {
                return;
            }

            InteractableWorldHint[] hints = FindObjectsOfType<InteractableWorldHint>();
            for (int i = 0; i < hints.Length; i++)
            {
                InteractableWorldHint hint = hints[i];
                if (hint == null || string.IsNullOrWhiteSpace(hint.HintText))
                {
                    continue;
                }

                Vector3 worldPosition = hint.transform.position + Vector3.up * 2.1f;
                Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);
                if (screenPosition.z < 0f)
                {
                    continue;
                }

                screenPosition.y = Screen.height - screenPosition.y;
                var labelRect = new Rect(screenPosition.x - 80f, screenPosition.y - 12f, 160f, 22f);
                GUI.Label(labelRect, hint.HintText, hintStyle);
            }
        }

        private void EnsureStyles()
        {
            if (hintStyle != null)
            {
                return;
            }

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.95f, 1f, 1f) }
            };
        }
    }
}
