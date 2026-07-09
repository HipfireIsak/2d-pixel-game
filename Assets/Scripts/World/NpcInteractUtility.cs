using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Player;
using AetherEcho.Rendering;
using AetherEcho.UI;

namespace AetherEcho.World
{
    public static class NpcInteractUtility
    {
        private const float QuestGiverInteractRadiusMeters = 2.8f;
        private const float QuestGiverPickPadding = 10f;

        public static NetworkedCombatant FindLocalPlayer()
        {
            NetworkedCombatant[] players = Object.FindObjectsOfType<NetworkedCombatant>();
            foreach (NetworkedCombatant player in players)
            {
                if (player.isLocalPlayer)
                {
                    return player;
                }
            }

            return null;
        }

        public static bool TryHandleNearestInteractableOnKey(Vector3 playerPosition)
        {
            if (QuestDialogUI.Instance != null && QuestDialogUI.Instance.IsOpen)
            {
                return false;
            }

            if (VendorUI.Instance != null && VendorUI.Instance.IsOpen)
            {
                return false;
            }

            NetworkedCombatant localPlayer = FindLocalPlayer();
            if (localPlayer == null)
            {
                return false;
            }

            float bestDistance = float.MaxValue;
            DungeonExitInteractable bestExit = null;
            DungeonPortalInteractable bestPortal = null;
            QuestNpcInteractable bestQuestNpc = null;

            DungeonExitInteractable[] exits = Object.FindObjectsOfType<DungeonExitInteractable>();
            foreach (DungeonExitInteractable exit in exits)
            {
                if (exit == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(playerPosition, exit.transform.position);
                if (distance <= QuestGiverInteractRadiusMeters && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestExit = exit;
                    bestPortal = null;
                    bestQuestNpc = null;
                }
            }

            DungeonPortalInteractable[] portals = Object.FindObjectsOfType<DungeonPortalInteractable>();
            foreach (DungeonPortalInteractable portal in portals)
            {
                if (portal == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(playerPosition, portal.transform.position);
                if (distance <= QuestGiverInteractRadiusMeters && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestExit = null;
                    bestPortal = portal;
                    bestQuestNpc = null;
                }
            }

            QuestNpcInteractable[] questNpcs = Object.FindObjectsOfType<QuestNpcInteractable>();
            foreach (QuestNpcInteractable questNpc in questNpcs)
            {
                if (questNpc == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(playerPosition, questNpc.transform.position);
                if (distance <= QuestGiverInteractRadiusMeters && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestExit = null;
                    bestPortal = null;
                    bestQuestNpc = questNpc;
                }
            }

            if (bestExit != null)
            {
                localPlayer.CmdExitDungeon();
                return true;
            }

            if (bestPortal != null)
            {
                localPlayer.CmdEnterDungeon();
                return true;
            }

            if (bestQuestNpc != null)
            {
                localPlayer.CmdInteractWithQuestNpc(bestQuestNpc.QuestGiverName);
                return true;
            }

            return false;
        }

        public static bool TryInteractWithQuestNpcAtCursor(Vector3 playerPosition)
        {
            if (QuestDialogUI.Instance != null && QuestDialogUI.Instance.IsOpen)
            {
                return false;
            }

            if (VendorUI.Instance != null && VendorUI.Instance.IsOpen)
            {
                return false;
            }

            NetworkedCombatant localPlayer = FindLocalPlayer();
            if (localPlayer == null)
            {
                return false;
            }

            if (!TryPickQuestNpcAtCursor(playerPosition, out QuestNpcInteractable questNpc))
            {
                return false;
            }

            localPlayer.CmdInteractWithQuestNpc(questNpc.QuestGiverName);
            return true;
        }

        public static bool TryPickQuestNpcAtCursor(Vector3 playerPosition, out QuestNpcInteractable questNpc)
        {
            questNpc = null;
            Camera camera = CombatPickUtility.ResolveGameplayCamera();
            if (camera == null)
            {
                return false;
            }

            QuestNpcInteractable bestNpc = null;
            float bestScore = float.MaxValue;
            QuestNpcInteractable[] questNpcs = Object.FindObjectsOfType<QuestNpcInteractable>();
            foreach (QuestNpcInteractable npc in questNpcs)
            {
                if (npc == null)
                {
                    continue;
                }

                if (Vector3.Distance(playerPosition, npc.transform.position) > QuestGiverInteractRadiusMeters)
                {
                    continue;
                }

                if (!TryGetNpcScreenBounds(camera, npc.transform, out Rect screenBounds))
                {
                    continue;
                }

                screenBounds = ExpandRect(screenBounds, QuestGiverPickPadding);
                if (!screenBounds.Contains(Input.mousePosition))
                {
                    continue;
                }

                Vector2 center = screenBounds.center;
                float score = Vector2.Distance(Input.mousePosition, center);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestNpc = npc;
                }
            }

            questNpc = bestNpc;
            return questNpc != null;
        }

        private static bool TryGetNpcScreenBounds(Camera camera, Transform npcTransform, out Rect screenBounds)
        {
            screenBounds = default;
            if (camera == null || npcTransform == null)
            {
                return false;
            }

            PixelBillboardVisual visual = npcTransform.GetComponent<PixelBillboardVisual>();
            if (visual != null && visual.SpriteRenderer != null && visual.SpriteRenderer.sprite != null)
            {
                Bounds worldBounds = visual.SpriteRenderer.bounds;
                Vector3 center = worldBounds.center;
                Vector3 screenCenter = camera.WorldToScreenPoint(center);
                if (screenCenter.z <= 0f)
                {
                    return false;
                }

                Vector3 extents = worldBounds.extents;
                Vector3[] corners =
                {
                    center + new Vector3(-extents.x, -extents.y, -extents.z),
                    center + new Vector3(-extents.x, -extents.y, extents.z),
                    center + new Vector3(-extents.x, extents.y, -extents.z),
                    center + new Vector3(-extents.x, extents.y, extents.z),
                    center + new Vector3(extents.x, -extents.y, -extents.z),
                    center + new Vector3(extents.x, -extents.y, extents.z),
                    center + new Vector3(extents.x, extents.y, -extents.z),
                    center + new Vector3(extents.x, extents.y, extents.z)
                };

                float minX = float.MaxValue;
                float minY = float.MaxValue;
                float maxX = float.MinValue;
                float maxY = float.MinValue;
                int visibleCorners = 0;
                foreach (Vector3 corner in corners)
                {
                    Vector3 screen = camera.WorldToScreenPoint(corner);
                    if (screen.z <= 0f)
                    {
                        continue;
                    }

                    visibleCorners++;
                    minX = Mathf.Min(minX, screen.x);
                    minY = Mathf.Min(minY, screen.y);
                    maxX = Mathf.Max(maxX, screen.x);
                    maxY = Mathf.Max(maxY, screen.y);
                }

                if (visibleCorners == 0)
                {
                    return false;
                }

                screenBounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
                return true;
            }

            Vector3 fallbackAnchor = npcTransform.position + Vector3.up * 1.2f;
            Vector3 fallbackScreen = camera.WorldToScreenPoint(fallbackAnchor);
            if (fallbackScreen.z <= 0f)
            {
                return false;
            }

            const float fallbackSize = 48f;
            screenBounds = new Rect(
                fallbackScreen.x - fallbackSize * 0.5f,
                fallbackScreen.y - fallbackSize * 0.5f,
                fallbackSize,
                fallbackSize);
            return true;
        }

        private static Rect ExpandRect(Rect rect, float padding)
        {
            return new Rect(
                rect.x - padding,
                rect.y - padding,
                rect.width + padding * 2f,
                rect.height + padding * 2f);
        }
    }
}
