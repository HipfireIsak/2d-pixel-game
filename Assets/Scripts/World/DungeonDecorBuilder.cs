using UnityEngine;
using AetherEcho.Core;
using AetherEcho.Rendering;

namespace AetherEcho.World
{
    public static class DungeonDecorBuilder
    {
        public static void BuildEchoVaultDecor(Transform root, ArtCatalog art)
        {
            if (root == null || art == null)
            {
                return;
            }

            PlaceWallRing(root, art, 7.5f);
            PlaceTorches(root, art);
            PlaceProps(root, art);
            PlaceSpikeTraps(root, art);
            WaterAreaBuilder.Create(root, root.position + new Vector3(-5f, 0f, 2f), 4f, 3f, art);
        }

        private static void PlaceWallRing(Transform root, ArtCatalog art, float radius)
        {
            Sprite[] walls = art.wallSegments;
            if (walls == null || walls.Length == 0)
            {
                return;
            }

            int segments = 12;
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Sprite wall = walls[i % walls.Length];
                float scale = WorldPropBuilder.ComputePropScale(wall, 2.2f);
                WorldPropBuilder.CreateFixedProp(root, "VaultWall", wall, root.position + pos, scale, isTree: false);
            }
        }

        private static void PlaceTorches(Transform root, ArtCatalog art)
        {
            Sprite[] torchFrames = art.torchFrames;
            if (torchFrames == null || torchFrames.Length == 0)
            {
                Sprite fallback = art.dungeonTorchStatic;
                if (fallback == null)
                {
                    return;
                }

                SpawnStaticTorch(root, root.position + new Vector3(-6f, 0f, -2f), fallback);
                SpawnStaticTorch(root, root.position + new Vector3(6f, 0f, -2f), fallback);
                return;
            }

            SpawnAnimatedTorch(root, root.position + new Vector3(-6f, 0f, -2f), torchFrames);
            SpawnAnimatedTorch(root, root.position + new Vector3(6f, 0f, -2f), torchFrames);
            SpawnAnimatedTorch(root, root.position + new Vector3(-6f, 0f, 6f), torchFrames);
            SpawnAnimatedTorch(root, root.position + new Vector3(6f, 0f, 6f), torchFrames);
        }

        private static void SpawnStaticTorch(Transform root, Vector3 worldPosition, Sprite sprite)
        {
            float scale = WorldPropBuilder.ComputePropScale(sprite, 1.4f);
            WorldPropBuilder.CreateDecorProp(root, "VaultTorch", sprite, worldPosition, scale);
        }

        private static void SpawnAnimatedTorch(Transform root, Vector3 worldPosition, Sprite[] frames)
        {
            Sprite frame = frames[0];
            float scale = WorldPropBuilder.ComputePropScale(frame, 1.4f);
            GameObject torch = WorldPropBuilder.CreateDecorProp(root, "VaultTorch", frame, worldPosition, scale);
            PixelBillboardVisual visual = torch.GetComponent<PixelBillboardVisual>();
            if (visual != null && visual.SpriteRenderer != null)
            {
                var animator = torch.AddComponent<CatalogSpriteAnimator>();
                animator.Configure(visual.SpriteRenderer, frames, 9f, true);
            }
        }

        private static void PlaceProps(Transform root, ArtCatalog art)
        {
            TryPlaceProp(root, art.dungeonChestClosed, root.position + new Vector3(-3f, 0f, 7f), 1.2f, "VaultChest");
            TryPlaceProp(root, art.dungeonShield, root.position + new Vector3(3f, 0f, 7f), 1.3f, "VaultShield");
            TryPlaceProp(root, art.dungeonBarrel, root.position + new Vector3(-7f, 0f, 0f), 1.1f, "VaultBarrel");
            TryPlaceProp(root, art.dungeonChainsMedieval, root.position + new Vector3(7f, 0f, 0f), 2.4f, "VaultChain");

            PlaceSpriteSet(root, art.dungeonBanners, root.position, 5.5f, "VaultBanner");
            PlaceSpriteSet(root, art.dungeonVases, root.position + new Vector3(2f, 0f, -4f), 2.5f, "VaultVase");
            PlaceSpriteSet(root, art.dungeonCandles, root.position + new Vector3(-2f, 0f, -4f), 2f, "VaultCandle");
            PlaceSpriteSet(root, art.dungeonBones, root.position + new Vector3(4f, 0f, -3f), 2f, "VaultBone");
        }

        private static void PlaceSpikeTraps(Transform root, ArtCatalog art)
        {
            Sprite[] frames = art.spikeTrapFrames;
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            Vector3[] trapPositions =
            {
                root.position + new Vector3(-2f, 0f, 1f),
                root.position + new Vector3(2f, 0f, 1f),
                root.position + new Vector3(0f, 0f, -1f)
            };

            for (int i = 0; i < trapPositions.Length; i++)
            {
                Sprite frame = frames[0];
                float scale = WorldPropBuilder.ComputePropScale(frame, 1.1f);
                GameObject trap = WorldPropBuilder.CreateDecorProp(root, "VaultSpikes", frame, trapPositions[i], scale);
                PixelBillboardVisual visual = trap.GetComponent<PixelBillboardVisual>();
                if (visual != null && visual.SpriteRenderer != null)
                {
                    var animator = trap.AddComponent<CatalogSpriteAnimator>();
                    animator.Configure(visual.SpriteRenderer, frames, 6f, true);
                }
            }
        }

        private static void TryPlaceProp(Transform root, Sprite sprite, Vector3 worldPosition, float height, string name)
        {
            if (sprite == null)
            {
                return;
            }

            float scale = WorldPropBuilder.ComputePropScale(sprite, height);
            WorldPropBuilder.CreateDecorProp(root, name, sprite, worldPosition, scale);
        }

        private static void TryPlaceProp(Transform root, Sprite[] sprites, Vector3 worldPosition, float height, string name)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return;
            }

            Sprite sprite = sprites[Random.Range(0, sprites.Length)];
            TryPlaceProp(root, sprite, worldPosition, height, name);
        }

        private static void PlaceSpriteSet(Transform root, Sprite[] sprites, Vector3 center, float radius, string namePrefix)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return;
            }

            int count = Mathf.Min(3, sprites.Length);
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f + 0.4f;
                Vector3 pos = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Sprite sprite = sprites[i % sprites.Length];
                float scale = WorldPropBuilder.ComputePropScale(sprite, 1.2f + i * 0.1f);
                WorldPropBuilder.CreateDecorProp(root, namePrefix + i, sprite, pos, scale);
            }
        }
    }
}
