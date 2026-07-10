using UnityEngine;
using AetherEcho.Core;
using AetherEcho.Items;
using AetherEcho.Player;
using AetherEcho.Rendering;

namespace AetherEcho.Combat
{
    public static class CombatPickUtility
    {
        public static Camera ResolveGameplayCamera()
        {
            if (LostArkCameraRig.Instance != null && LostArkCameraRig.Instance.TargetCamera != null)
            {
                return LostArkCameraRig.Instance.TargetCamera;
            }

            if (Camera.main != null)
            {
                return Camera.main;
            }

            return Object.FindObjectOfType<Camera>();
        }

        public static bool TryPickEnemyAtScreen(
            Camera camera,
            Vector2 screenPosition,
            CombatantState localPlayer,
            out CombatantState picked)
        {
            picked = null;
            if (camera == null || localPlayer == null)
            {
                return false;
            }

            CombatantState best = null;
            float bestScore = float.MaxValue;
            CombatantState[] combatants = Object.FindObjectsOfType<CombatantState>();
            foreach (CombatantState combatant in combatants)
            {
                if (combatant == null
                    || combatant == localPlayer
                    || combatant.CurrentHealth <= 0
                    || !combatant.IsEnemyWith(localPlayer))
                {
                    continue;
                }

                if (!TryGetScreenBounds(camera, combatant, out Rect screenBounds, out float depth))
                {
                    continue;
                }

                screenBounds = ExpandRect(screenBounds, 10f);
                Vector2 center = screenBounds.center;
                float pickRadius = Mathf.Max(screenBounds.width, screenBounds.height) * 0.6f;
                float distance = Vector2.Distance(screenPosition, center);
                if (!screenBounds.Contains(screenPosition) && distance > pickRadius)
                {
                    continue;
                }

                float score = distance + depth * 0.01f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = combatant;
                }
            }

            picked = best;
            return picked != null;
        }

        public static bool TryGetScreenBounds(
            Camera camera,
            CombatantState combatant,
            out Rect screenBounds,
            out float depth)
        {
            screenBounds = default;
            depth = 0f;
            if (camera == null || combatant == null)
            {
                return false;
            }

            PixelBillboardVisual visual = combatant.GetComponent<PixelBillboardVisual>();
            if (visual != null && visual.SpriteRenderer != null && visual.SpriteRenderer.sprite != null)
            {
                return TryProjectBoundsToScreen(camera, visual.SpriteRenderer.bounds, out screenBounds, out depth);
            }

            Vector3 fallbackAnchor = combatant.transform.position + Vector3.up * 1.2f;
            Vector3 screen = camera.WorldToScreenPoint(fallbackAnchor);
            if (screen.z <= 0f)
            {
                return false;
            }

            depth = screen.z;
            const float fallbackSize = 44f;
            screenBounds = new Rect(
                screen.x - fallbackSize * 0.5f,
                screen.y - fallbackSize * 0.5f,
                fallbackSize,
                fallbackSize);
            return true;
        }

        public static bool TryPickGroundLootAtScreen(
            Camera camera,
            Vector2 screenPosition,
            Vector3 playerPosition,
            float pickupRange,
            out GroundLootDrop picked)
        {
            picked = null;
            if (camera == null)
            {
                return false;
            }

            GroundLootDrop best = null;
            float bestScore = float.MaxValue;
            GroundLootDrop[] drops = Object.FindObjectsOfType<GroundLootDrop>();
            foreach (GroundLootDrop drop in drops)
            {
                if (drop == null || !drop.IsAvailableForPickup)
                {
                    continue;
                }

                if (Vector3.Distance(playerPosition, drop.transform.position) > pickupRange)
                {
                    continue;
                }

                Vector3 worldAnchor = drop.transform.position + Vector3.up * 0.8f;
                Vector3 screen = camera.WorldToScreenPoint(worldAnchor);
                if (screen.z <= 0f)
                {
                    continue;
                }

                const float pickRadius = 52f;
                float distance = Vector2.Distance(screenPosition, new Vector2(screen.x, screen.y));
                if (distance > pickRadius)
                {
                    continue;
                }

                if (distance < bestScore)
                {
                    bestScore = distance;
                    best = drop;
                }
            }

            picked = best;
            return picked != null;
        }

        public static bool TryGetGroundPointUnderCursor(Camera camera, Vector2 screenPosition, out Vector3 groundPoint)
        {
            groundPoint = Vector3.zero;
            camera = camera != null ? camera : ResolveGameplayCamera();
            if (camera == null)
            {
                return false;
            }

            float groundY = GameConstants.GroundHeight;
            Ray ray = camera.ScreenPointToRay(screenPosition);
            if (Mathf.Abs(ray.direction.y) < 0.0001f)
            {
                return false;
            }

            float distanceAlongRay = (groundY - ray.origin.y) / ray.direction.y;
            if (distanceAlongRay < 0f)
            {
                return false;
            }

            groundPoint = ray.GetPoint(distanceAlongRay);
            groundPoint.y = groundY;
            return true;
        }

        public static bool TryGetAimPointUnderCursor(
            Camera camera,
            Vector2 screenPosition,
            CombatantState localPlayer,
            float maxRange,
            out Vector3 aimPoint)
        {
            aimPoint = Vector3.zero;
            camera = camera != null ? camera : ResolveGameplayCamera();
            if (camera == null || localPlayer == null)
            {
                return false;
            }

            Vector3 casterPos = localPlayer.transform.position;
            if (TryPickEnemyAtScreen(camera, screenPosition, localPlayer, out CombatantState enemy))
            {
                aimPoint = ClampGroundPointToRange(casterPos, enemy.transform.position, maxRange);
                return true;
            }

            if (!TryGetGroundPointUnderCursor(camera, screenPosition, out Vector3 groundPoint))
            {
                return false;
            }

            aimPoint = ClampGroundPointToRange(casterPos, groundPoint, maxRange);
            return true;
        }

        public static Vector3 ClampGroundPointToRange(Vector3 casterPos, Vector3 desiredPoint, float maxRange)
        {
            Vector3 offset = desiredPoint - casterPos;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.001f)
            {
                return casterPos;
            }

            if (offset.magnitude > maxRange)
            {
                offset = offset.normalized * maxRange;
            }

            Vector3 clamped = casterPos + offset;
            clamped.y = GameConstants.GroundHeight;
            return clamped;
        }

        public static Vector3 ResolveCasterAimOrigin(CombatantState caster)
        {
            if (caster == null)
            {
                return Vector3.zero;
            }

            PixelBillboardVisual visual = caster.GetComponent<PixelBillboardVisual>();
            if (visual != null && visual.SpriteRenderer != null)
            {
                return visual.SpriteRenderer.bounds.center;
            }

            return caster.transform.position + Vector3.up * 0.9f;
        }

        public static Rect ToGuiRect(Rect screenSpaceRect)
        {
            return new Rect(
                screenSpaceRect.x,
                Screen.height - screenSpaceRect.y - screenSpaceRect.height,
                screenSpaceRect.width,
                screenSpaceRect.height);
        }

        private static bool TryProjectBoundsToScreen(
            Camera camera,
            Bounds worldBounds,
            out Rect screenBounds,
            out float depth)
        {
            screenBounds = default;
            depth = 0f;

            Vector3 center = worldBounds.center;
            Vector3 screenCenter = camera.WorldToScreenPoint(center);
            depth = screenCenter.z;
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
