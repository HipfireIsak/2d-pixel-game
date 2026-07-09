using UnityEngine;
using AetherEcho.Core;
using AetherEcho.Rendering;

namespace AetherEcho.World
{
    public static class WaterAreaBuilder
    {
        public static GameObject Create(
            Transform parent,
            Vector3 worldCenter,
            float widthMeters,
            float depthMeters,
            ArtCatalog art)
        {
            if (art == null)
            {
                return null;
            }

            var waterRoot = new GameObject("WaterArea");
            waterRoot.transform.SetParent(parent, false);
            waterRoot.transform.position = FlatMovementUtility.SnapToGround(worldCenter);

            Sprite floorSprite = art.waterFloorTile != null ? art.waterFloorTile : art.floorDirt;
            if (floorSprite != null)
            {
                CreateFloorLayer(waterRoot.transform, floorSprite, widthMeters, depthMeters, new Color(0.35f, 0.72f, 0.95f, 0.92f));
            }

            if (art.waterCausticFrames != null && art.waterCausticFrames.Length > 0)
            {
                CreateCausticOverlay(waterRoot.transform, art.waterCausticFrames, widthMeters, depthMeters);
            }

            if (art.vfxWaterRipple != null)
            {
                SpawnRippleDecals(waterRoot.transform, art.vfxWaterRipple, widthMeters, depthMeters);
            }

            return waterRoot;
        }

        private static void CreateFloorLayer(
            Transform parent,
            Sprite sprite,
            float widthMeters,
            float depthMeters,
            Color tint)
        {
            Vector2 tileSize = sprite.bounds.size;
            float tileWidth = tileSize.x * 0.985f;
            float tileDepth = tileSize.y * 0.985f;
            int countX = Mathf.Max(1, Mathf.CeilToInt(widthMeters / tileWidth));
            int countZ = Mathf.Max(1, Mathf.CeilToInt(depthMeters / tileDepth));
            float startX = (-widthMeters * 0.5f) + (tileWidth * 0.5f);
            float startZ = (-depthMeters * 0.5f) + (tileDepth * 0.5f);

            for (int x = 0; x < countX; x++)
            {
                for (int z = 0; z < countZ; z++)
                {
                    var tile = new GameObject($"WaterTile_{x}_{z}");
                    tile.transform.SetParent(parent, false);
                    tile.transform.localPosition = new Vector3(
                        startX + (x * tileWidth),
                        GameConstants.FloorVisualHeight + 0.01f,
                        startZ + (z * tileDepth));

                    var renderer = tile.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.color = tint;
                    WorldPropBuilder.ApplyGroundSorting(renderer, parent.position.z + tile.transform.localPosition.z);
                }
            }
        }

        private static void CreateCausticOverlay(
            Transform parent,
            Sprite[] frames,
            float widthMeters,
            float depthMeters)
        {
            var overlay = new GameObject("WaterCaustics");
            overlay.transform.SetParent(parent, false);
            overlay.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            var renderer = overlay.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.color = new Color(0.75f, 0.95f, 1f, 0.45f);
            renderer.sortingOrder = GameConstants.GroundSortingOrder + 2;

            float native = Mathf.Max(frames[0].bounds.size.x, frames[0].bounds.size.y);
            float scale = native > 0.001f ? Mathf.Max(widthMeters, depthMeters) / native : 1f;
            overlay.transform.localScale = Vector3.one * scale;

            var animator = overlay.AddComponent<CatalogSpriteAnimator>();
            animator.Configure(renderer, frames, 10f, true);
        }

        private static void SpawnRippleDecals(Transform parent, Sprite rippleSprite, float widthMeters, float depthMeters)
        {
            int rippleCount = 3;
            for (int i = 0; i < rippleCount; i++)
            {
                float x = Random.Range(-widthMeters * 0.35f, widthMeters * 0.35f);
                float z = Random.Range(-depthMeters * 0.35f, depthMeters * 0.35f);
                WorldPropBuilder.CreateDecorProp(parent, "WaterRipple", rippleSprite, new Vector3(x, 0f, z), 0.8f + i * 0.1f);
            }
        }
    }
}
