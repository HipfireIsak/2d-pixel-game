using UnityEngine;
using UnityEngine.Tilemaps;
using AetherEcho.Core;
using AetherEcho.Rendering;

namespace AetherEcho.World
{
    public static class DungeonTaleWorldBuilder
    {
        public struct ChunkLayout
        {
            public string name;
            public TileBase floorTile;
            public TileBase accentTile;
            public bool useCarpet;
            public Sprite[] borderTrees;
            public Sprite[] collisionProps;
            public Sprite[] decorProps;
            public string[] enemyTypes;
            public Color minimapColor;
        }

        private const int EnemiesPerChunk = 8;
        private const int WallThicknessCells = 1;
        private const int CarpetRadiusCells = 5;

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

        public static void BuildWorld(Transform worldRoot, DungeonTaleTileCatalog tiles, ArtCatalog art, int worldSeed)
        {
            if (tiles == null || art == null)
            {
                Debug.LogError("[DungeonTaleWorldBuilder] Missing tile or art catalog.");
                return;
            }

            ChunkLayout[] layouts = CreateLayouts(tiles, art);
            float chunkMeters = GameConstants.ChunkHalfExtentMeters * 2f;
            int grid = GameConstants.BiomeGridSize;
            float origin = -(grid * chunkMeters * 0.5f) + (chunkMeters * 0.5f);

            for (int z = 0; z < grid; z++)
            {
                for (int x = 0; x < grid; x++)
                {
                    int index = (z * grid) + x;
                    ChunkLayout layout = layouts[Mathf.Clamp(index, 0, layouts.Length - 1)];
                    Vector3 center = new Vector3(origin + (x * chunkMeters), 0f, origin + (z * chunkMeters));
                    BuildChunk(worldRoot, layout, center, chunkMeters, tiles, worldSeed + index * 991);
                }
            }
        }

        private static ChunkLayout[] CreateLayouts(DungeonTaleTileCatalog tiles, ArtCatalog art)
        {
            Sprite[] trees = art.propsForest;
            Sprite[] rocks = art.propsRock;
            Sprite[] grass = art.propsGrass;
            Sprite[] wild = art.propsWild;
            Sprite[] ruin = art.propsRuin;
            Sprite[] hub = art.propsHub;
            Sprite[] decor = art.decorGrass;

            return new[]
            {
                Layout("Dirt Wilds", tiles.floorDirt, tiles.floorDirt, false, trees, rocks, wild, new[] { "rat", "snake" }, MinimapBiomeColors[0]),
                Layout("Verdant Edge", tiles.floorGreen, tiles.floorGreen, false, trees, grass, decor, new[] { "slime" }, MinimapBiomeColors[1]),
                Layout("Stone Rise", tiles.floorMetal, tiles.floorMetal, false, trees, rocks, art.decorRock, new[] { "skeleton", "bat" }, MinimapBiomeColors[2]),
                Layout("Forest Path", tiles.floorGreen, tiles.floorWood, false, trees, wild, decor, new[] { "slime", "rat" }, MinimapBiomeColors[3]),
                Layout("Chrono Hub", tiles.floorGreen, tiles.floorGreen, true, trees, hub, art.decorHub, new[] { "slime" }, MinimapBiomeColors[4]),
                Layout("Woodlands", tiles.floorWood, tiles.floorWood, false, trees, rocks, decor, new[] { "skeleton" }, MinimapBiomeColors[5]),
                Layout("Crimson Moor", tiles.floorRed, tiles.floorFlash, false, trees, wild, art.decorWild, new[] { "bat", "snake" }, MinimapBiomeColors[6]),
                Layout("Metal Ruins", tiles.floorMetal, tiles.floorFlash, false, trees, ruin, art.decorRuin, new[] { "skeleton", "eye" }, MinimapBiomeColors[7]),
                Layout("Flash Fields", tiles.floorFlash, tiles.floorGreen, false, trees, grass, decor, new[] { "slime", "sunflower" }, MinimapBiomeColors[8])
            };
        }

        private static ChunkLayout Layout(
            string name,
            TileBase floor,
            TileBase accent,
            bool carpet,
            Sprite[] borderTrees,
            Sprite[] collisionProps,
            Sprite[] decorProps,
            string[] enemies,
            Color minimapColor)
        {
            return new ChunkLayout
            {
                name = name,
                floorTile = floor,
                accentTile = accent,
                useCarpet = carpet,
                borderTrees = borderTrees,
                collisionProps = collisionProps,
                decorProps = decorProps,
                enemyTypes = enemies,
                minimapColor = minimapColor
            };
        }

        private static void BuildChunk(
            Transform worldRoot,
            ChunkLayout layout,
            Vector3 center,
            float chunkMeters,
            DungeonTaleTileCatalog tiles,
            int seed)
        {
            var chunkRoot = new GameObject("Chunk_" + layout.name);
            chunkRoot.transform.SetParent(worldRoot, false);
            chunkRoot.transform.position = center;

            int cellsX = Mathf.CeilToInt(chunkMeters / tiles.cellSize.x);
            int cellsZ = Mathf.CeilToInt(chunkMeters / tiles.cellSize.y);

            var gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(chunkRoot.transform, false);
            gridObject.transform.localPosition = new Vector3(-chunkMeters * 0.5f, GameConstants.FloorVisualHeight, -chunkMeters * 0.5f);
            gridObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Grid grid = gridObject.AddComponent<Grid>();
            grid.cellSize = tiles.cellSize;

            Tilemap floorMap = CreateTilemapLayer(gridObject, "Floor", sortingOrder: GameConstants.GroundSortingOrder - 10, tiles.tileMaterial);
            FillFloor(floorMap, cellsX, cellsZ, layout.floorTile, layout.accentTile);

            if (tiles.wall != null)
            {
                PaintBorder(floorMap, cellsX, cellsZ, tiles.wall, WallThicknessCells);
            }

            if (layout.useCarpet && tiles.carpet != null)
            {
                Tilemap carpetMap = CreateTilemapLayer(gridObject, "Carpet", sortingOrder: GameConstants.GroundSortingOrder - 5, tiles.tileMaterial);
                PaintCarpetPatch(carpetMap, cellsX, cellsZ, tiles.carpet, CarpetRadiusCells);
            }

            bool isHub = layout.name == "Chrono Hub";
            WorldChunkDresser.DressChunk(
                chunkRoot.transform,
                center,
                chunkMeters * 0.5f - 6f,
                layout,
                seed,
                isHub);

            if (!isHub)
            {
                // Enemies are spawned by MobSpawnZoneManager area zones.
            }
        }

        private static Tilemap CreateTilemapLayer(GameObject gridObject, string layerName, int sortingOrder, Material material)
        {
            var layerObject = new GameObject(layerName);
            layerObject.transform.SetParent(gridObject.transform, false);

            var tilemap = layerObject.AddComponent<Tilemap>();
            var renderer = layerObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            Material resolved = WorldRenderMaterials.ResolveTilemapMaterial(material);
            if (resolved != null)
            {
                renderer.sharedMaterial = resolved;
            }

            return tilemap;
        }

        private static void FillFloor(Tilemap tilemap, int cellsX, int cellsZ, TileBase primary, TileBase accent)
        {
            if (primary == null)
            {
                return;
            }

            accent ??= primary;
            for (int x = 0; x < cellsX; x++)
            {
                for (int y = 0; y < cellsZ; y++)
                {
                    bool checker = ((x + y) & 1) == 0;
                    tilemap.SetTile(new Vector3Int(x, y, 0), checker ? primary : accent);
                }
            }

            tilemap.RefreshAllTiles();
            tilemap.CompressBounds();
        }

        private static void PaintBorder(Tilemap tilemap, int cellsX, int cellsZ, TileBase wallTile, int thickness)
        {
            for (int t = 0; t < thickness; t++)
            {
                for (int x = 0; x < cellsX; x++)
                {
                    tilemap.SetTile(new Vector3Int(x, t, 0), wallTile);
                    tilemap.SetTile(new Vector3Int(x, cellsZ - 1 - t, 0), wallTile);
                }

                for (int y = 0; y < cellsZ; y++)
                {
                    tilemap.SetTile(new Vector3Int(t, y, 0), wallTile);
                    tilemap.SetTile(new Vector3Int(cellsX - 1 - t, y, 0), wallTile);
                }
            }

            tilemap.RefreshAllTiles();
        }

        private static void PaintCarpetPatch(Tilemap tilemap, int cellsX, int cellsZ, TileBase carpetTile, int radius)
        {
            Vector2Int center = new Vector2Int(cellsX / 2, cellsZ / 2);
            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    if (x < 1 || y < 1 || x >= cellsX - 1 || y >= cellsZ - 1)
                    {
                        continue;
                    }

                    tilemap.SetTile(new Vector3Int(x, y, 0), carpetTile);
                }
            }

            tilemap.RefreshAllTiles();
        }

        public static Vector3 RandomPointInChunk(Vector3 chunkCenter, float half, bool avoidCenter)
        {
            for (int attempt = 0; attempt < 24; attempt++)
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
    }
}
