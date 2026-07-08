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
        private bool minimapUnlocked;
        private Rect minimapRect;
        private bool minimapRectInitialized;

        private const float MapSize = 168f;
        private const float MapMargin = 18f;
        private const int MinimapDragControlId = 81002;

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

            UpdateMinimapRect();
            Rect mapRect = minimapRect;
            GUI.Box(mapRect, string.Empty);
            if (minimapUnlocked)
            {
                GUI.Label(new Rect(mapRect.x, mapRect.y - 16f, mapRect.width, 14f), "Mini map (drag to move)", GUI.skin.label);
            }

            float chunkSize = GameConstants.ChunkHalfExtentMeters * 2f;
            int grid = GameConstants.BiomeGridSize;
            float origin = -(grid * chunkSize * 0.5f) + (chunkSize * 0.5f);
            float worldSpan = grid * chunkSize;
            float cell = MapSize / grid;

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
            float dotX = mapRect.x + 2f + (nx * (MapSize - 4f)) - 3f;
            float dotY = mapRect.y + 2f + ((1f - nz) * (MapSize - 4f)) - 3f;
            Color dotPrevious = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(dotX, dotY, 6f, 6f), Texture2D.whiteTexture);
            GUI.color = dotPrevious;

            Vector3 npcPos = new Vector3(-2f, 0f, -2f);
            float npcNx = (npcPos.x - origin + chunkSize * 0.5f) / worldSpan;
            float npcNz = (npcPos.z - origin + chunkSize * 0.5f) / worldSpan;
            float npcX = mapRect.x + 2f + (Mathf.Clamp01(npcNx) * (MapSize - 4f)) - 2f;
            float npcY = mapRect.y + 2f + ((1f - Mathf.Clamp01(npcNz)) * (MapSize - 4f)) - 2f;
            GUI.color = new Color(1f, 0.85f, 0.2f);
            GUI.DrawTexture(new Rect(npcX, npcY, 4f, 4f), Texture2D.whiteTexture);
            GUI.color = dotPrevious;

            GUI.Label(new Rect(mapRect.x, mapRect.y + MapSize + 2f, MapSize, 18f), "Map (9 biomes)", GUI.skin.label);

            var interactionRect = new Rect(mapRect.x, mapRect.y - (minimapUnlocked ? 16f : 0f), mapRect.width, mapRect.height + (minimapUnlocked ? 16f : 0f) + 20f);
            HudPanelCustomization.TryOpenContextMenu(interactionRect, GetMinimapMenuItems());
            HudPanelCustomization.HandleDrag(ref minimapRect, minimapUnlocked, MinimapDragControlId);
            HudPanelCustomization.DrawContextMenu();
        }

        private void UpdateMinimapRect()
        {
            if (!minimapRectInitialized)
            {
                minimapRect = new Rect(Screen.width - MapSize - MapMargin, MapMargin, MapSize, MapSize);
                minimapRectInitialized = true;
            }

            if (!minimapUnlocked)
            {
                minimapRect.x = Screen.width - MapSize - MapMargin;
                minimapRect.y = MapMargin;
            }
        }

        private HudPanelCustomization.MenuItem[] GetMinimapMenuItems()
        {
            return new[]
            {
                new HudPanelCustomization.MenuItem(
                    minimapUnlocked ? "Lock Mini map" : "Unlock Mini map",
                    () => minimapUnlocked = !minimapUnlocked)
            };
        }
    }
}
