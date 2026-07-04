using UnityEngine;
using AetherEcho.Core;

namespace AetherEcho.World
{
    /// <summary>
    /// 2D-style movement on the XZ ground plane with flat obstacle checks only.
    /// </summary>
    public static class FlatMovementUtility
    {
        private static int ObstacleMask => 1 << GameConstants.ObstacleLayerIndex;

        public static Vector3 SnapToGround(Vector3 position)
        {
            return new Vector3(position.x, GameConstants.GroundHeight, position.z);
        }

        public static Vector3 MoveWithFlatCollision(Vector3 from, Vector3 delta, float radius)
        {
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.0001f)
            {
                return SnapToGround(from);
            }

            Vector3 target = SnapToGround(from + delta);
            if (!IsBlocked(target, radius))
            {
                return target;
            }

            Vector3 slideX = SnapToGround(new Vector3(target.x, 0f, from.z));
            if (!IsBlocked(slideX, radius))
            {
                return slideX;
            }

            Vector3 slideZ = SnapToGround(new Vector3(from.x, 0f, target.z));
            if (!IsBlocked(slideZ, radius))
            {
                return slideZ;
            }

            return SnapToGround(from);
        }

        public static bool IsBlocked(Vector3 feetPosition, float radius)
        {
            Vector3 checkCenter = feetPosition + Vector3.up * (GameConstants.FlatColliderHeight * 0.5f);
            return Physics.CheckSphere(checkCenter, radius, ObstacleMask, QueryTriggerInteraction.Ignore);
        }

        public static float GetSpriteGroundOffset(Sprite sprite, float scale)
        {
            if (sprite == null)
            {
                return 0.5f * scale;
            }

            return sprite.bounds.extents.y * scale;
        }
    }
}
