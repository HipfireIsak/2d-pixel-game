using System.Collections.Generic;
using UnityEngine;
using AetherEcho.Core;
using AetherEcho.Rendering;

namespace AetherEcho.World
{
    /// <summary>
    /// Places Dungeon Tale props with spacing — trees on borders, sparse interior clutter.
    /// </summary>
    public static class WorldChunkDresser
    {
        private const float MinPropSpacing = 5f;
        private const float TreeHeightMeters = 2.1f;
        private const float RockHeightMeters = 0.9f;
        private const float DecorHeightMeters = 0.45f;

        public static void DressChunk(
            Transform chunkRoot,
            Vector3 center,
            float halfExtent,
            DungeonTaleWorldBuilder.ChunkLayout layout,
            int seed,
            bool isHub)
        {
            Random.InitState(seed);
            var occupied = new List<Vector2>();

            PlaceBorderTrees(chunkRoot, center, halfExtent, layout.borderTrees, occupied, isHub);
            PlaceInteriorProps(chunkRoot, center, halfExtent, layout.collisionProps, occupied, isHub, RockHeightMeters, TreeHeightMeters, colliders: true);
            PlaceInteriorProps(chunkRoot, center, halfExtent, layout.decorProps, occupied, isHub, DecorHeightMeters, DecorHeightMeters, colliders: false);
        }

        private static void PlaceBorderTrees(
            Transform parent,
            Vector3 center,
            float half,
            Sprite[] trees,
            List<Vector2> occupied,
            bool avoidCenter)
        {
            if (trees == null || trees.Length == 0)
            {
                return;
            }

            int treesPerEdge = 6;
            float inset = half - 5f;
            for (int edge = 0; edge < 4; edge++)
            {
                for (int i = 0; i < treesPerEdge; i++)
                {
                    float t = Mathf.Lerp(-inset, inset, (i + 0.5f) / treesPerEdge);
                    Vector3 pos = edge switch
                    {
                        0 => center + new Vector3(t, 0f, -inset),
                        1 => center + new Vector3(t, 0f, inset),
                        2 => center + new Vector3(-inset, 0f, t),
                        _ => center + new Vector3(inset, 0f, t)
                    };

                    if (avoidCenter && Vector2.Distance(new Vector2(pos.x, pos.z), Vector2.zero) < GameConstants.SpawnSafeRadiusMeters + 6f)
                    {
                        continue;
                    }

                    if (!TryReserveSpot(pos, occupied, MinPropSpacing))
                    {
                        continue;
                    }

                    Sprite tree = trees[Random.Range(0, trees.Length)];
                    float scale = WorldPropBuilder.ComputePropScale(tree, TreeHeightMeters * Random.Range(0.92f, 1.08f));
                    WorldPropBuilder.CreateFixedProp(parent, "BorderTree", tree, pos, scale, isTree: true);
                }
            }
        }

        private static void PlaceInteriorProps(
            Transform parent,
            Vector3 center,
            float half,
            Sprite[] sprites,
            List<Vector2> occupied,
            bool avoidCenter,
            float maxHeight,
            float treeHeight,
            bool colliders)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return;
            }

            int target = colliders ? 10 : 16;
            int attempts = target * 12;
            int placed = 0;

            for (int attempt = 0; attempt < attempts && placed < target; attempt++)
            {
                Vector3 pos = DungeonTaleWorldBuilder.RandomPointInChunk(center, half - 10f, avoidCenter);
                if (!TryReserveSpot(pos, occupied, MinPropSpacing))
                {
                    continue;
                }

                Sprite sprite = sprites[Random.Range(0, sprites.Length)];
                if (sprite == null)
                {
                    continue;
                }

                string lower = sprite.name.ToLowerInvariant();
                bool isTree = lower.Contains("tree") || lower.Contains("root");
                float height = isTree ? treeHeight : Random.Range(maxHeight * 0.75f, maxHeight);
                float scale = WorldPropBuilder.ComputePropScale(sprite, height);

                if (colliders)
                {
                    WorldPropBuilder.CreateFixedProp(parent, "Prop", sprite, pos, scale, isTree);
                }
                else
                {
                    WorldPropBuilder.CreateDecorProp(parent, "Decor", sprite, pos, scale);
                }

                placed++;
            }
        }

        private static bool TryReserveSpot(Vector3 worldPos, List<Vector2> occupied, float minDistance)
        {
            var point = new Vector2(worldPos.x, worldPos.z);
            foreach (Vector2 existing in occupied)
            {
                if (Vector2.Distance(existing, point) < minDistance)
                {
                    return false;
                }
            }

            occupied.Add(point);
            return true;
        }
    }
}
