using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Quests;
using AetherEcho.Rendering;
using AetherEcho.World;

namespace AetherEcho.UI
{
    public class QuestNpcIndicatorUI : MonoBehaviour
    {
        private GUIStyle markerFallbackStyle;

        private void OnGUI()
        {
            Camera camera = CombatPickUtility.ResolveGameplayCamera();
            if (camera == null)
            {
                return;
            }

            ArtCatalog art = ArtAssetResolver.Catalog;
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
                Sprite bubble = marker == QuestNpcMarker.Available
                    ? art?.GetQuestBubble(QuestBubbleKind.Available)
                    : art?.GetQuestBubble(QuestBubbleKind.TurnIn);

                if (bubble != null)
                {
                    float size = 34f;
                    var bubbleRect = new Rect(screenPosition.x - size * 0.5f, screenPosition.y - size * 0.65f, size, size);
                    DrawSprite(bubbleRect, bubble);
                    continue;
                }

                EnsureFallbackStyle();
                string label = marker == QuestNpcMarker.Available ? "!" : "?";
                var labelRect = new Rect(screenPosition.x - 10f, screenPosition.y - 18f, 20f, 24f);
                GUI.Label(labelRect, label, markerFallbackStyle);
            }
        }

        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            Rect texCoords = sprite.textureRect;
            texCoords.x /= sprite.texture.width;
            texCoords.y /= sprite.texture.height;
            texCoords.width /= sprite.texture.width;
            texCoords.height /= sprite.texture.height;
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, texCoords, true);
        }

        private void EnsureFallbackStyle()
        {
            if (markerFallbackStyle != null)
            {
                return;
            }

            markerFallbackStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.9f, 0.15f, 1f) }
            };
        }
    }
}
