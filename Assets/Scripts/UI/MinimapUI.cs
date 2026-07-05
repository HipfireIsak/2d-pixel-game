using UnityEngine;
using AetherEcho.Core;
using AetherEcho.Player;
using AetherEcho.World;

namespace AetherEcho.UI
{
    public class MinimapUI : MonoBehaviour
    {
        public static MinimapUI Instance { get; private set; }

        private NetworkedCombatant localPlayer;
        private bool questTurnInReady;

        private GUIStyle labelStyle;

        private void Awake()
        {
            Instance = this;
        }

        public void BindLocalPlayer(NetworkedCombatant player)
        {
            localPlayer = player;
        }

        public void SetQuestTurnInReady(bool ready)
        {
            questTurnInReady = ready;
        }

        private void OnGUI()
        {
            if (localPlayer == null)
            {
                return;
            }

            EnsureStyles();

            const float size = 168f;
            const float margin = 16f;
            var mapRect = new Rect(Screen.width - size - margin, margin, size, size);
            GUI.Box(mapRect, string.Empty);

            DrawBiomeChunks(mapRect);
            DrawNpcMarker(mapRect);
            DrawPlayerMarker(mapRect);

            GUI.Label(new Rect(mapRect.x, mapRect.yMax + 4f, mapRect.width, 18f), "World Map", labelStyle);
            if (questTurnInReady)
            {
                GUI.Label(
                    new Rect(mapRect.x - 120f, mapRect.y + 4f, 110f, 36f),
                    "! Turn in\nat Sage",
                    labelStyle);
            }
        }

        private static void DrawBiomeChunks(Rect mapRect)
        {
            float chunkWorldSize = GameConstants.ChunkHalfExtentMeters * 2f;
            float chunkMapSize = mapRect.width / GameConstants.BiomeGridSize;
            Color[] colors = WorldBiomeBuilder.MinimapBiomeColors;

            for (int z = 0; z < GameConstants.BiomeGridSize; z++)
            {
                for (int x = 0; x < GameConstants.BiomeGridSize; x++)
                {
                    int index = (z * GameConstants.BiomeGridSize) + x;
                    Color color = index < colors.Length ? colors[index] : Color.gray;
                    var chunkRect = new Rect(
                        mapRect.x + (x * chunkMapSize) + 1f,
                        mapRect.y + (z * chunkMapSize) + 1f,
                        chunkMapSize - 2f,
                        chunkMapSize - 2f);

                    Color previous = GUI.color;
                    GUI.color = color;
                    GUI.DrawTexture(chunkRect, Texture2D.whiteTexture);
                    GUI.color = previous;
                }
            }
        }

        private void DrawPlayerMarker(Rect mapRect)
        {
            Vector3 pos = localPlayer.transform.position;
            Vector2 mapPoint = WorldToMapPoint(pos, mapRect);
            DrawDot(mapPoint, 5f, Color.white);
        }

        private static void DrawNpcMarker(Rect mapRect)
        {
            Vector2 mapPoint = WorldToMapPoint(new Vector3(-2f, 0f, -2f), mapRect);
            DrawDot(mapPoint, 4f, new Color(1f, 0.85f, 0.2f));
        }

        private static Vector2 WorldToMapPoint(Vector3 worldPosition, Rect mapRect)
        {
            float worldSpan = GameConstants.WorldHalfExtentMeters * 2f;
            float normalizedX = (worldPosition.x + GameConstants.WorldHalfExtentMeters) / worldSpan;
            float normalizedZ = (worldPosition.z + GameConstants.WorldHalfExtentMeters) / worldSpan;
            return new Vector2(
                mapRect.x + (normalizedX * mapRect.width),
                mapRect.y + mapRect.height - (normalizedZ * mapRect.height));
        }

        private static void DrawDot(Vector2 center, float radius, Color color)
        {
            var rect = new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f);
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.92f, 0.92f, 0.92f) }
            };
        }
    }
}
