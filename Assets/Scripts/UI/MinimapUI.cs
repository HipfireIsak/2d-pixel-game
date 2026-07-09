using UnityEngine;
using AetherEcho.Core;
using AetherEcho.Player;
using AetherEcho.Quests;
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
        private Texture2D questMarkerRingTexture;

        private const float MapSize = 168f;
        private const float MapMargin = 18f;
        private const int MinimapDragControlId = 81002;
        private static readonly Color QuestMarkerColor = new Color(1f, 0.85f, 0.2f, 0.95f);

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
            int questCellX = -1;
            int questCellZ = -1;
            if (QuestClientState.HasActiveQuest
                && QuestClientState.TryGetActiveQuestObjectivePosition(out Vector3 questObjectivePos))
            {
                QuestLocationResolver.TryGetBiomeGridCell(questObjectivePos, out questCellX, out questCellZ);
            }

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

                    if (x == questCellX && z == questCellZ)
                    {
                        DrawQuestBiomeHighlight(cellRect);
                    }
                }
            }

            Vector3 playerPos = localPlayer.transform.position;
            if (TryWorldToMinimapPoint(playerPos, mapRect, origin, worldSpan, chunkSize, out Vector2 playerPoint))
            {
                Color dotPrevious = GUI.color;
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(playerPoint.x - 3f, playerPoint.y - 3f, 6f, 6f), Texture2D.whiteTexture);
                GUI.color = dotPrevious;
            }

            if (QuestClientState.TryGetActiveQuestObjectivePosition(out Vector3 objectivePos)
                && TryWorldToMinimapPoint(objectivePos, mapRect, origin, worldSpan, chunkSize, out Vector2 questPoint))
            {
                DrawQuestObjectiveMarker(questPoint);
            }

            string questMapLabel = QuestClientState.HasActiveQuest
                ? QuestLocationResolver.ResolveLocationLabel(
                    QuestClientState.ActiveQuestId,
                    QuestClientState.ObjectivesComplete)
                : string.Empty;
            GUI.Label(new Rect(mapRect.x, mapRect.y + MapSize + 2f, MapSize, 18f),
                string.IsNullOrEmpty(questMapLabel) ? "Map (9 biomes)" : "Quest: " + questMapLabel,
                GUI.skin.label);

            var interactionRect = new Rect(mapRect.x, mapRect.y - (minimapUnlocked ? 16f : 0f), mapRect.width, mapRect.height + (minimapUnlocked ? 16f : 0f) + 20f);
            HudPanelCustomization.TryOpenContextMenu(interactionRect, GetMinimapMenuItems());
            HudPanelCustomization.HandleDrag(ref minimapRect, minimapUnlocked, MinimapDragControlId);
            HudPanelCustomization.DrawContextMenu();
        }

        private static bool TryWorldToMinimapPoint(
            Vector3 worldPosition,
            Rect mapRect,
            float origin,
            float worldSpan,
            float chunkSize,
            out Vector2 minimapPoint)
        {
            float nx = (worldPosition.x - origin + chunkSize * 0.5f) / worldSpan;
            float nz = (worldPosition.z - origin + chunkSize * 0.5f) / worldSpan;
            nx = Mathf.Clamp01(nx);
            nz = Mathf.Clamp01(nz);
            minimapPoint = new Vector2(
                mapRect.x + 2f + (nx * (MapSize - 4f)),
                mapRect.y + 2f + ((1f - nz) * (MapSize - 4f)));
            return true;
        }

        private void DrawQuestObjectiveMarker(Vector2 center)
        {
            EnsureQuestMarkerRingTexture();
            const float radius = 11f;
            Color previous = GUI.color;
            GUI.color = QuestMarkerColor;
            GUI.DrawTexture(new Rect(center.x - 3f, center.y - 3f, 6f, 6f), Texture2D.whiteTexture);
            GUI.DrawTexture(
                new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f),
                questMarkerRingTexture,
                ScaleMode.StretchToFill,
                true);
            GUI.color = previous;
        }

        private static void DrawQuestBiomeHighlight(Rect cellRect)
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            const float border = 2f;
            GUI.DrawTexture(new Rect(cellRect.x, cellRect.y, cellRect.width, border), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cellRect.x, cellRect.yMax - border, cellRect.width, border), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cellRect.x, cellRect.y, border, cellRect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cellRect.xMax - border, cellRect.y, border, cellRect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void EnsureQuestMarkerRingTexture()
        {
            if (questMarkerRingTexture != null)
            {
                return;
            }

            questMarkerRingTexture = new Texture2D(48, 48, TextureFormat.RGBA32, false);
            questMarkerRingTexture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[48 * 48];
            Vector2 ringCenter = new Vector2(23.5f, 23.5f);
            for (int y = 0; y < 48; y++)
            {
                for (int x = 0; x < 48; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), ringCenter);
                    float alpha = distance > 18f && distance < 22f ? 1f : 0f;
                    pixels[(y * 48) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            questMarkerRingTexture.SetPixels(pixels);
            questMarkerRingTexture.Apply();
        }

        private void UpdateMinimapRect()
        {
            if (!minimapRectInitialized)
            {
                minimapRect = new Rect(Screen.width - MapSize - MapMargin, MapMargin, MapSize, MapSize);
                minimapRectInitialized = true;
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
