using UnityEngine;
using AetherEcho.Core;
using AetherEcho.Player;
using AetherEcho.Rendering;
using AetherEcho.World;

namespace AetherEcho.UI
{
    public class MinimapUI : MonoBehaviour
    {
        public static MinimapUI Instance { get; private set; }

        private NetworkedCombatant localPlayer;
        private static readonly Color[] ChunkColors =
        {
            new Color(0.45f, 0.35f, 0.22f),
            new Color(0.25f, 0.55f, 0.28f),
            new Color(0.42f, 0.42f, 0.46f),
            new Color(0.2f, 0.48f, 0.24f),
            new Color(0.35f, 0.62f, 0.38f),
            new Color(0.38f, 0.28f, 0.16f),
            new Color(0.55f, 0.22f, 0.2f),
            new Color(0.5f, 0.5f, 0.55f),
            new Color(0.62f, 0.58f, 0.28f)
        };

        private void Awake()
        {
            Instance = this;
        }

        public void BindLocalPlayer(NetworkedCombatant player)
        {
            localPlayer = player;
        }

        private void OnGUI()
        {
            if (localPlayer == null)
            {
                return;
            }

            const float size = 168f;
            const float margin = 18f;
            var mapRect = new Rect(Screen.width - size - margin, margin, size, size);
            GUI.Box(mapRect, string.Empty);

            float chunkSize = GameConstants.ChunkHalfExtentMeters * 2f;
            int grid = GameConstants.BiomeGridSize;
            float origin = -(grid * chunkSize * 0.5f) + (chunkSize * 0.5f);
            float worldSpan = grid * chunkSize;
            float cell = size / grid;

            for (int z = 0; z < grid; z++)
            {
                for (int x = 0; x < grid; x++)
                {
                    int index = (z * grid) + x;
                    Color color = ChunkColors[Mathf.Clamp(index, 0, ChunkColors.Length - 1)];
                    var cellRect = new Rect(
                        mapRect.x + 2f + (x * cell),
                        mapRect.y + 2f + ((grid - 1 - z) * cell),
                        cell - 1f,
                        cell - 1f);
                    Color previous = GUI.color;
                    GUI.color = color;
                    GUI.DrawTexture(cellRect, Texture2D.whiteTexture);
                    GUI.color = previous;
                }
            }

            Vector3 playerPos = localPlayer.transform.position;
            float nx = (playerPos.x - origin + chunkSize * 0.5f) / worldSpan;
            float nz = (playerPos.z - origin + chunkSize * 0.5f) / worldSpan;
            nx = Mathf.Clamp01(nx);
            nz = Mathf.Clamp01(nz);
            float dotX = mapRect.x + 2f + (nx * (size - 4f)) - 3f;
            float dotY = mapRect.y + 2f + ((1f - nz) * (size - 4f)) - 3f;
            Color dotPrevious = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(dotX, dotY, 6f, 6f), Texture2D.whiteTexture);
            GUI.color = dotPrevious;

            Vector3 npcPos = new Vector3(-2f, 0f, -2f);
            float npcNx = (npcPos.x - origin + chunkSize * 0.5f) / worldSpan;
            float npcNz = (npcPos.z - origin + chunkSize * 0.5f) / worldSpan;
            float npcX = mapRect.x + 2f + (Mathf.Clamp01(npcNx) * (size - 4f)) - 2f;
            float npcY = mapRect.y + 2f + ((1f - Mathf.Clamp01(npcNz)) * (size - 4f)) - 2f;
            GUI.color = new Color(1f, 0.85f, 0.2f);
            GUI.DrawTexture(new Rect(npcX, npcY, 4f, 4f), Texture2D.whiteTexture);
            GUI.color = dotPrevious;

            GUI.Label(new Rect(mapRect.x, mapRect.y + size + 2f, size, 18f), "Map (9 biomes)", GUI.skin.label);
        }
    }
}
