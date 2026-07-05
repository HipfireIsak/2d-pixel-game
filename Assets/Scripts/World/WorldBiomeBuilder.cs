using System;
using UnityEngine;
using AetherEcho.Core;
using AetherEcho.Rendering;

namespace AetherEcho.World
{
    public static class WorldBiomeBuilder
    {
        public static readonly Color[] MinimapBiomeColors =
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

        public struct BiomeChunkDefinition
        {
            public string name;
            public Sprite floorA;
            public Sprite floorB;
            public Sprite[] props;
            public Sprite[] decor;
            public string[] enemyTypes;
            public Color minimapColor;
        }

        public static void BuildChunkGrid(Transform worldRoot, ArtCatalog art, int worldSeed)
        {
            if (art == null)
            {
                return;
            }

            BiomeChunkDefinition[] chunks = CreateChunkLayout(art);
            float chunkSize = GameConstants.ChunkHalfExtentMeters * 2f;
            int grid = GameConstants.BiomeGridSize;
            float origin = -(grid * chunkSize * 0.5f) + (chunkSize * 0.5f);

            for (int z = 0; z < grid; z++)
            {
                for (int x = 0; x < grid; x++)
                {
                    int index = (z * grid) + x;
                    BiomeChunkDefinition chunk = chunks[Mathf.Clamp(index, 0, chunks.Length - 1)];
                    Vector3 chunkCenter = new Vector3(origin + (x * chunkSize), 0f, origin + (z * chunkSize));
                    BuildChunk(worldRoot, chunk, chunkCenter, chunkSize, worldSeed + index * 997);
                }
            }
        }

        private static BiomeChunkDefinition[] CreateChunkLayout(ArtCatalog art)
        {
            return new[]
            {
                Chunk("Dirt Wilds", art.floorDirt, art.floorD, art.propsWild, art.decorWild, new[] { "rat", "snake" }, new Color(0.45f, 0.35f, 0.22f)),
                Chunk("Verdant Edge", art.floorA, art.floorB, art.propsGrass, art.decorGrass, new[] { "slime" }, new Color(0.25f, 0.55f, 0.28f)),
                Chunk("Stone Rise", art.floorC, art.floorD, art.propsRock, art.decorRock, new[] { "skeleton", "bat" }, new Color(0.42f, 0.42f, 0.46f)),
                Chunk("Forest Path", art.floorB, art.floorA, art.propsForest, art.decorForest, new[] { "slime", "rat" }, new Color(0.2f, 0.48f, 0.24f)),
                Chunk("Chrono Hub", art.floorA, art.floorB, art.propsHub, art.decorHub, new[] { "slime" }, new Color(0.35f, 0.62f, 0.38f)),
                Chunk("Woodlands", art.floorWood, art.floorC, art.propsForest, art.decorForest, new[] { "skeleton" }, new Color(0.38f, 0.28f, 0.16f)),
                Chunk("Crimson Moor", art.floorRed, art.floorFlash, art.propsRock, art.decorWild, new[] { "bat", "snake" }, new Color(0.55f, 0.22f, 0.2f)),
                Chunk("Metal Ruins", art.floorMetal, art.floorD, art.propsRuin, art.decorRuin, new[] { "skeleton", "eye" }, new Color(0.5f, 0.5f, 0.55f)),
                Chunk("Flash Fields", art.floorFlash, art.floorA, art.propsGrass, art.decorGrass, new[] { "slime", "sunflower" }, new Color(0.62f, 0.58f, 0.28f))
            };
        }

        private static BiomeChunkDefinition Chunk(
            string name,
            Sprite floorA,
            Sprite floorB,
            Sprite[] props,
            Sprite[] decor,
            string[] enemies,
            Color minimapColor)
        {
            return new BiomeChunkDefinition
            {
                name = name,
                floorA = floorA,
                floorB = floorB,
                props = props,
                decor = decor,
                enemyTypes = enemies,
                minimapColor = minimapColor
            };
        }

        private static void BuildChunk(
            Transform worldRoot,
            BiomeChunkDefinition chunk,
            Vector3 center,
            float chunkSize,
            int seed)
        {
            var chunkRoot = new GameObject("Chunk_" + chunk.name);
            chunkRoot.transform.SetParent(worldRoot, false);
            chunkRoot.transform.position = center;

            if (chunk.floorA != null)
            {
                WorldPropBuilder.CreateSeamlessFloorGrid(
                    chunkRoot.transform,
                    chunk.floorA,
                    chunk.floorB,
                    chunkSize,
                    chunkSize);
            }

            Random.InitState(seed);
            float half = (chunkSize * 0.5f) - 4f;
            float playerHeight = 1.2f;
            bool isHub = chunk.name == "Chrono Hub";

            ScatterFixedProps(chunkRoot.transform, chunk.props, 18, half, playerHeight, colliders: true, isHub);
            ScatterFixedProps(chunkRoot.transform, chunk.decor, 24, half, playerHeight * 0.35f, colliders: false, isHub);

            if (!isHub)
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector3 pos = RandomPointInChunk(center, half, isHub);
                    string typeId = chunk.enemyTypes[Random.Range(0, chunk.enemyTypes.Length)];
                    WorldContentSpawner.SpawnEnemyPublic(typeId, pos, 2 + Random.Range(0, 3));
                }
            }
        }

        public static Vector3 RandomPointInChunk(Vector3 chunkCenter, float half, bool avoidCenter)
        {
            for (int attempt = 0; attempt < 16; attempt++)
            {
                float x = chunkCenter.x + Random.Range(-half, half);
                float z = chunkCenter.z + Random.Range(-half, half);
                if (avoidCenter && Vector2.Distance(new Vector2(x, z), Vector2.zero) < GameConstants.SpawnSafeRadiusMeters)
                {
                    continue;
                }

                return new Vector3(x, GameConstants.GroundHeight, z);
            }

            return chunkCenter;
        }

        private static void ScatterFixedProps(
            Transform parent,
            Sprite[] sprites,
            int count,
            float half,
            float targetHeight,
            bool colliders,
            bool isHub)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return;
            }

            Vector3 center = parent.position;
            for (int i = 0; i < count; i++)
            {
                Sprite sprite = sprites[Random.Range(0, sprites.Length)];
                float scale = WorldPropBuilder.ComputePropScale(sprite, targetHeight * Random.Range(0.85f, 1.15f));
                Vector3 pos = RandomPointInChunk(center, half, isHub);
                bool isTree = sprite.name.Contains("Tree") || sprite.name.Contains("Root");
                if (colliders)
                {
                    WorldPropBuilder.CreateFixedProp(parent, "Prop_" + i, sprite, pos, scale, isTree);
                }
                else
                {
                    WorldPropBuilder.CreateDecorProp(parent, "Decor_" + i, sprite, pos, scale);
                }
            }
        }
    }
}
