using UnityEngine;
using AetherEcho.Core;
using AetherEcho.Rendering;

namespace AetherEcho.World
{
    public static class WorldPropBuilder
    {
        public static GameObject CreateSeamlessFloorGrid(
            Transform parent,
            Sprite tileA,
            Sprite tileB,
            float worldWidth,
            float worldDepth)
        {
            if (tileA == null)
            {
                return null;
            }

            tileB ??= tileA;

            var groundRoot = new GameObject("GroundField");
            groundRoot.transform.SetParent(parent, false);
            groundRoot.transform.position = Vector3.zero;

            Vector2 tileSize = tileA.bounds.size;
            float tileWidth = tileSize.x;
            float tileDepth = tileSize.y;
            int countX = Mathf.CeilToInt(worldWidth / tileWidth);
            int countZ = Mathf.CeilToInt(worldDepth / tileDepth);
            float startX = (-worldWidth * 0.5f) + (tileWidth * 0.5f);
            float startZ = (-worldDepth * 0.5f) + (tileDepth * 0.5f);

            for (int x = 0; x < countX; x++)
            {
                for (int z = 0; z < countZ; z++)
                {
                    var tile = new GameObject($"Floor_{x}_{z}");
                    tile.transform.SetParent(groundRoot.transform, false);
                    tile.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    tile.transform.position = new Vector3(
                        startX + (x * tileWidth),
                        GameConstants.FloorVisualHeight,
                        startZ + (z * tileDepth));

                    var renderer = tile.AddComponent<SpriteRenderer>();
                    renderer.sprite = ((x + z) & 1) == 0 ? tileA : tileB;
                    renderer.sortingOrder = GameConstants.GroundSortingOrder;
                }
            }

            return groundRoot;
        }

        public static float ComputePropScale(Sprite sprite, float targetHeightMeters)
        {
            if (sprite == null)
            {
                return 1f;
            }

            float nativeHeight = sprite.bounds.size.y;
            return nativeHeight > 0.001f ? targetHeightMeters / nativeHeight : 1f;
        }

        public static GameObject CreateBillboardProp(
            Transform parent,
            string name,
            Sprite sprite,
            Vector3 position,
            float scale,
            bool addObstacleCollider,
            bool isTree)
        {
            var prop = new GameObject(name);
            if (parent != null)
            {
                prop.transform.SetParent(parent, false);
            }

            prop.transform.position = FlatMovementUtility.SnapToGround(position);

            var visual = prop.AddComponent<PixelBillboardVisual>();
            float groundOffset = FlatMovementUtility.GetSpriteGroundOffset(sprite, scale);
            visual.Configure(
                prop.transform,
                sprite,
                directionalHero: false,
                offset: new Vector3(0f, groundOffset, 0f),
                scale: scale);

            if (addObstacleCollider && sprite != null)
            {
                AddFlatObstacleCollider(prop, sprite, scale, isTree);
            }

            ApplyDepthSorting(visual.SpriteRenderer, prop.transform.position);

            return prop;
        }

        public static GameObject CreateDecorProp(
            Transform parent,
            string name,
            Sprite sprite,
            Vector3 position,
            float scale)
        {
            var prop = new GameObject(name);
            prop.transform.SetParent(parent, false);
            prop.transform.position = FlatMovementUtility.SnapToGround(position);

            var visual = prop.AddComponent<PixelBillboardVisual>();
            float groundOffset = FlatMovementUtility.GetSpriteGroundOffset(sprite, scale);
            visual.Configure(
                prop.transform,
                sprite,
                directionalHero: false,
                offset: new Vector3(0f, groundOffset, 0f),
                scale: scale);
            ApplyDepthSorting(visual.SpriteRenderer, prop.transform.position);
            return prop;
        }

        public static void ApplyDepthSorting(SpriteRenderer renderer, Vector3 worldPosition)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.sortingOrder = GameConstants.GroundSortingOrder
                                    + GameConstants.EntitySortingBaseOffset
                                    + Mathf.RoundToInt(-worldPosition.z * GameConstants.SortingOrderPerMeter);
        }

        private static void AddFlatObstacleCollider(GameObject prop, Sprite sprite, float scale, bool isTree)
        {
            prop.layer = GameConstants.ObstacleLayerIndex;
            Vector2 spriteSize = sprite.bounds.size;

            if (isTree)
            {
                var capsule = prop.AddComponent<CapsuleCollider>();
                float trunkRadius = Mathf.Max(0.42f, spriteSize.x * scale * 0.13f);
                float trunkHeight = Mathf.Max(0.55f, spriteSize.y * scale * 0.28f);
                capsule.radius = trunkRadius;
                capsule.height = trunkHeight;
                capsule.direction = 1;
                capsule.center = new Vector3(0f, trunkHeight * 0.5f, 0f);
                return;
            }

            var box = prop.AddComponent<BoxCollider>();
            float width = Mathf.Max(0.45f, spriteSize.x * scale * 0.42f);
            float depth = Mathf.Max(0.4f, spriteSize.x * scale * 0.36f);
            box.size = new Vector3(width, GameConstants.FlatColliderHeight, depth);
            box.center = new Vector3(0f, GameConstants.FlatColliderHeight * 0.5f, 0f);
        }

        public static void AddFlatHitCollider(GameObject entity, float radius)
        {
            CapsuleCollider capsule = entity.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = entity.AddComponent<CapsuleCollider>();
            }

            capsule.height = GameConstants.FlatColliderHeight;
            capsule.radius = radius;
            capsule.center = new Vector3(0f, GameConstants.FlatColliderHeight * 0.5f, 0f);
            capsule.direction = 1;
        }
    }
}
