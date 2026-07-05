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
            groundRoot.transform.localPosition = Vector3.zero;

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
                    tile.transform.localPosition = new Vector3(
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

        public static GameObject CreateFixedProp(
            Transform parent,
            string name,
            Sprite sprite,
            Vector3 position,
            float scale,
            bool isTree)
        {
            var prop = new GameObject(name);
            if (parent != null)
            {
                prop.transform.SetParent(parent, false);
            }

            prop.transform.position = FlatMovementUtility.SnapToGround(position);
            prop.transform.rotation = Quaternion.identity;

            var visual = prop.AddComponent<PixelBillboardVisual>();
            float groundOffset = FlatMovementUtility.GetSpriteGroundOffset(sprite, scale);
            visual.Configure(
                prop.transform,
                sprite,
                directionalHero: false,
                offset: new Vector3(0f, groundOffset, 0f),
                scale: scale,
                facing: SpriteFacingMode.FixedSouth);

            AddFlatObstacleCollider(prop, sprite, scale, isTree);
            ApplyDepthSorting(visual.SpriteRenderer, prop.transform.position);
            return prop;
        }

        public static GameObject CreateBillboardProp(
            Transform parent,
            string name,
            Sprite sprite,
            Vector3 position,
            float scale,
            bool addObstacleCollider,
            bool isTree,
            SpriteFacingMode facingMode = SpriteFacingMode.BillboardYWhenMoving)
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
                scale: scale,
                facing: facingMode);

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
            prop.transform.rotation = Quaternion.identity;

            var visual = prop.AddComponent<PixelBillboardVisual>();
            float groundOffset = FlatMovementUtility.GetSpriteGroundOffset(sprite, scale);
            visual.Configure(
                prop.transform,
                sprite,
                directionalHero: false,
                offset: new Vector3(0f, groundOffset, 0f),
                scale: scale,
                facing: SpriteFacingMode.FixedSouth);
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
            float width = spriteSize.x * scale;
            float depth = Mathf.Max(spriteSize.x, spriteSize.y) * scale * 0.55f;

            if (isTree)
            {
                var capsule = prop.AddComponent<CapsuleCollider>();
                float trunkRadius = Mathf.Clamp(width * 0.16f, 0.28f, 0.55f);
                float trunkHeight = Mathf.Clamp(spriteSize.y * scale * 0.22f, 0.45f, 1.1f);
                capsule.radius = trunkRadius;
                capsule.height = trunkHeight;
                capsule.direction = 1;
                capsule.center = new Vector3(0f, trunkHeight * 0.5f, 0f);
                return;
            }

            var box = prop.AddComponent<BoxCollider>();
            box.size = new Vector3(
                Mathf.Clamp(width * 0.55f, 0.35f, 1.2f),
                GameConstants.FlatColliderHeight,
                Mathf.Clamp(depth * 0.55f, 0.35f, 1.0f));
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
