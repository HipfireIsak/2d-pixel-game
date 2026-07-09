using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Quests;
using AetherEcho.World;

namespace AetherEcho.UI
{
    public class QuestNpcIndicatorUI : MonoBehaviour
    {
        private GUIStyle markerStyle;

        private void OnGUI()
        {
            EnsureStyles();
            Camera camera = CombatPickUtility.ResolveGameplayCamera();
            if (camera == null)
            {
                return;
            }

            QuestNpcInteractable[] questNpcs = FindObjectsOfType<QuestNpcInteractable>();
            for (int i = 0; i < questNpcs.Length; i++)
            {
                QuestNpcInteractable npc = questNpcs[i];
                if (npc == null)
                {
                    continue;
                }

                QuestNpcMarker marker = QuestClientState.GetNpcMarker(npc.QuestGiverName);
                if (marker == QuestNpcMarker.None)
                {
                    continue;
                }

                Vector3 worldPosition = npc.transform.position + Vector3.up * 1.85f;
                Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);
                if (screenPosition.z < 0f)
                {
                    continue;
                }

                screenPosition.y = Screen.height - screenPosition.y;
                string label = marker == QuestNpcMarker.Available ? "!" : "?";
                var labelRect = new Rect(screenPosition.x - 10f, screenPosition.y - 18f, 20f, 24f);
                GUI.Label(labelRect, label, markerStyle);
            }
        }

        private void EnsureStyles()
        {
            if (markerStyle != null)
            {
                return;
            }

            markerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.9f, 0.15f, 1f) }
            };
        }
    }
}
