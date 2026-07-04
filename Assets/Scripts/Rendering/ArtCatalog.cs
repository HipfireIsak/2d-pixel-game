using UnityEngine;

namespace AetherEcho.Rendering
{
    [CreateAssetMenu(fileName = "ArtCatalog", menuName = "AetherEcho/Art Catalog")]
    public class ArtCatalog : ScriptableObject
    {
        public Sprite heroSouth;
        public Sprite heroNorth;
        public Sprite heroEast;
        public Sprite slime;
        public Sprite skeleton;
        public Sprite questNpc;
        public Sprite floorA;
        public Sprite floorB;
        public Sprite floorGrass;
        public Sprite tree;
        public Sprite rock;
        public Sprite[] trees;
        public Sprite[] rocks;
        public Sprite[] bushes;
        public Sprite[] mushrooms;
        public Sprite[] reeds;
        public Sprite spellBeam;
        public Sprite spellBurst;
        public Sprite spellPulse;
        public Sprite timeEcho;
    }

    public static class ArtAssetResolver
    {
        private static ArtCatalog catalog;
        private static bool attemptedLoad;

        public static ArtCatalog Catalog
        {
            get
            {
                EnsureLoaded();
                return catalog;
            }
        }

        public static Sprite GetHeroSprite(Vector3 cameraRelativeDirection)
        {
            ArtCatalog art = Catalog;
            if (art == null)
            {
                return null;
            }

            if (cameraRelativeDirection.sqrMagnitude < 0.01f)
            {
                return art.heroSouth != null ? art.heroSouth : art.heroEast;
            }

            cameraRelativeDirection.Normalize();
            if (Mathf.Abs(cameraRelativeDirection.z) >= Mathf.Abs(cameraRelativeDirection.x))
            {
                return cameraRelativeDirection.z >= 0f
                    ? (art.heroNorth != null ? art.heroNorth : art.heroSouth)
                    : art.heroSouth;
            }

            return art.heroEast != null ? art.heroEast : art.heroSouth;
        }

        private static void EnsureLoaded()
        {
            if (attemptedLoad)
            {
                return;
            }

            attemptedLoad = true;
            catalog = Resources.Load<ArtCatalog>("ArtCatalog");
            if (catalog == null)
            {
                Debug.LogWarning("[ArtAssetResolver] Missing Resources/ArtCatalog.asset — run AetherEcho/Rebuild World Visuals.");
            }
        }
    }
}
