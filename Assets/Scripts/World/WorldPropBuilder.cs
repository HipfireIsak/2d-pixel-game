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
            float tileWidth = tileSize.x * 0.985f;
            float tileDepth = tileSize.y * 0.985f;
            int countX = Mathf.CeilToInt(worldWidth / tileWidth);
            int countZ = Mathf.CeilToInt(worldDepth / tileDepth);
            float startX = (-worldWidth * 0.5f) + (tileWidth * 0.5f);
            float startZ = (-worldDepth * 0.5f) + (tileDepth * 0.5f);
            Vector3 chunkOrigin = parent != null ? parent.position : Vector3.zero;

            for (int x = 0; x < countX; x++)
            {
                for (int z = 0; z < countZ; z++)
                {
                    var tile = new GameObject($"Floor_{x}_{z}");
                    tile.transform.SetParent(groundRoot.transform, false);
                    tile.transform.rotation = Quaternion.identity;
                    float localZ = startZ + (z * tileDepth);
                    tile.transform.localPosition = new Vector3(
                        startX + (x * tileWidth),
                        GameConstants.FloorVisualHeight,
                        localZ);

                    var renderer = tile.AddComponent<SpriteRenderer>();
                    renderer.sprite = ((x + z) & 1) == 0 ? tileA : tileB;
                    ApplyGroundSorting(renderer, chunkOrigin.z + localZ);
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

        public static void ApplyGroundSorting(SpriteRenderer renderer, float worldZ)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.sortingOrder = GameConstants.GroundSortingOrder
                                    + Mathf.RoundToInt(worldZ * GameConstants.GroundSortingOrderPerMeter);
        }

        public static void ApplyDepthSorting(SpriteRenderer renderer, Vector3 worldPosition)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.sortingOrder = GameConstants.EntitySortingBaseOffset
                                    + Mathf.RoundToInt(-worldPosition.z * GameConstants.SortingOrderPerMeter);
        }

        public static void CreateChunkUnderlay(Transform parent, float chunkSize, Color tint, Sprite fillSprite)
        {
            if (fillSprite == null)
            {
                return;
            }

            var underlay = new GameObject("ChunkUnderlay");
            underlay.transform.SetParent(parent, false);
            underlay.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            underlay.transform.rotation = Quaternion.identity;

            var renderer = underlay.AddComponent<SpriteRenderer>();
            renderer.sprite = fillSprite;
            renderer.color = tint;
            renderer.sortingOrder = GameConstants.GroundSortingOrder - 500;

            float native = Mathf.Max(fillSprite.bounds.size.x, fillSprite.bounds.size.y);
            float scale = native > 0.001f ? chunkSize / native : chunkSize;
            underlay.transform.localScale = Vector3.one * scale;
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
