using UnityEngine;
using UnityEngine.Tilemaps;

namespace AetherEcho.World
{
    [CreateAssetMenu(fileName = "DungeonTaleTileCatalog", menuName = "AetherEcho/Dungeon Tale Tile Catalog")]
    public class DungeonTaleTileCatalog : ScriptableObject
    {
        public Material tileMaterial;
        public Vector3 cellSize = new Vector3(1.25f, 1.4375f, 0f);

        public TileBase floorGreen;
        public TileBase floorWood;
        public TileBase floorDirt;
        public TileBase floorRed;
        public TileBase floorMetal;
        public TileBase floorFlash;
        public TileBase wall;
        public TileBase carpet;
    }

    public static class DungeonTaleTileCatalogLoader
    {
        private static DungeonTaleTileCatalog catalog;
        private static bool attempted;

        public static DungeonTaleTileCatalog Catalog
        {
            get
            {
                if (!attempted)
                {
                    attempted = true;
                    catalog = Resources.Load<DungeonTaleTileCatalog>("DungeonTaleTileCatalog");
                    if (catalog == null)
                    {
                        Debug.LogWarning("[DungeonTale] Missing Resources/DungeonTaleTileCatalog.asset — run AetherEcho/Setup Project.");
                    }
                }

                return catalog;
            }
        }
    }
}
