using UnityEngine;

namespace AetherEcho.Rendering
{
    [CreateAssetMenu(fileName = "ArtCatalog", menuName = "AetherEcho/Art Catalog")]
    public class ArtCatalog : ScriptableObject
    {
        [Header("Hero")]
        public Sprite heroSouth;
        public Sprite heroNorth;
        public Sprite heroEast;

        [Header("Enemies")]
        public Sprite slime;
        public Sprite skeleton;
        public Sprite bat;
        public Sprite rat;
        public Sprite snake;
        public Sprite eye;
        public Sprite sunflower;
        public Sprite questNpc;

        [Header("Bosses (fill vaultWarden if auto-wire missed it)")]
        [Tooltip("Stone head boss — Prop_Env_X from Atlas. Used for vault_warden.")]
        public Sprite vaultWarden;
        [Tooltip("Heart boss — Char_Heat from Atlas. Reserved for future boss encounters.")]
        public Sprite heartBoss;

        [Header("Quest UI Bubbles (sprites, not text)")]
        [Tooltip("Speech bubble with ! — Bubbles_5 from Bubbles.png")]
        public Sprite questBubbleAvailable;
        [Tooltip("Speech bubble with ? — Bubbles_8 from Bubbles.png")]
        public Sprite questBubbleTurnIn;
        [Tooltip("Optional NPC thinking dots — assign from Bubbles_10+ if desired.")]
        public Sprite[] questBubbleThinking;

        [Header("Floors")]
        public Sprite floorA;
        public Sprite floorB;
        public Sprite floorC;
        public Sprite floorD;
        public Sprite floorWood;
        public Sprite floorMetal;
        public Sprite floorRed;
        public Sprite floorDirt;
        public Sprite floorFlash;
        public Sprite carpetA;
        public Sprite carpetB;
        public Sprite tree;
        public Sprite rock;

        [Header("Water Area")]
        [Tooltip("Base tile under water pools. Uses Floor_Green tint if assigned.")]
        public Sprite waterFloorTile;
        public Sprite[] waterCausticFrames;
        public Sprite vfxWaterRipple;
        public Sprite vfxWaterDrop;

        [Header("Animated Props")]
        public Sprite[] torchFrames;
        public Sprite[] spikeTrapFrames;

        [Header("Walls & Decals")]
        public Sprite[] wallSegments;
        public Sprite[] floorDecals;

        [Header("Dungeon Tale Environment Props")]
        public Sprite[] dungeonBanners;
        public Sprite[] dungeonPaintings;
        public Sprite[] dungeonBooks;
        public Sprite[] dungeonPipes;
        public Sprite[] dungeonRoots;
        public Sprite[] dungeonVases;
        public Sprite[] dungeonTrees;
        public Sprite[] dungeonWebs;
        public Sprite[] dungeonChains;
        public Sprite[] dungeonCandles;
        public Sprite[] dungeonBones;
        public Sprite[] dungeonGravestones;
        public Sprite[] dungeonFlags;
        [Tooltip("Uncertain props from Atlas — reorder in inspector: Prop_Env_* etc.")]
        public Sprite[] dungeonMiscEnv;

        [Header("Medieval Dungeon Props (sprites — lighter than prefabs)")]
        public Sprite dungeonTorchStatic;
        public Sprite dungeonWindow;
        public Sprite dungeonDoorClosed;
        public Sprite dungeonDoorOpen;
        public Sprite dungeonDoorBarred;
        public Sprite dungeonDoorEmpty;
        public Sprite dungeonChestClosed;
        public Sprite dungeonChestOpen;
        public Sprite dungeonLeverOn;
        public Sprite dungeonLeverOff;
        public Sprite dungeonBarrel;
        public Sprite dungeonCrate;
        public Sprite dungeonShield;
        public Sprite dungeonBridgeFloor;
        public Sprite[] dungeonChainsMedieval;
        public Sprite[] dungeonLadders;
        public Sprite[] dungeonRailings;
        [Tooltip("Medieval props you want to name yourself — assign from Medieval_props_free sheet.")]
        public Sprite[] dungeonUncertain;

        [Header("VFX Sprites (lightweight)")]
        public Sprite[] vfxDustFrames;
        public Sprite[] vfxFogFrames;
        public Sprite vfxSunRay;

        [Header("VFX Prefabs (heavier — optional one-shot effects)")]
        [Tooltip("Only spawn at runtime when needed. Sprites above are preferred.")]
        public GameObject vfxPrefabDrops;
        public GameObject vfxPrefabDust;
        public GameObject vfxPrefabDust2;
        public GameObject vfxPrefabRipple;
        public GameObject vfxPrefabSunRays;
        public GameObject vfxPrefabWaterSuspension;

        [Header("Biome Props")]
        public Sprite[] propsGrass;
        public Sprite[] propsForest;
        public Sprite[] propsRock;
        public Sprite[] propsWild;
        public Sprite[] propsRuin;
        public Sprite[] propsHub;
        public Sprite[] decorGrass;
        public Sprite[] decorForest;
        public Sprite[] decorRock;
        public Sprite[] decorWild;
        public Sprite[] decorRuin;
        public Sprite[] decorHub;

        [Header("External Tilesets")]
        public Sprite[] medievalFloors;
        public Sprite[] medievalProps;
        public Sprite[] jungleGrass;
        public Sprite[] jungleTrees;
        public Sprite[] jungleRocks;
        public Sprite[] jungleVegetation;
        public Sprite[] handcraftedMoss;
        public Sprite[] gameItemDecor;
        public Sprite[] hillsBackdrop;

        [Header("Spell VFX")]
        public Sprite spellBeam;
        public Sprite spellBurst;
        public Sprite spellPulse;
        public Sprite timeEcho;

        public Sprite GetEnemySprite(string typeId)
        {
            switch (typeId)
            {
                case "skeleton": return skeleton;
                case "bat": return bat;
                case "rat": return rat;
                case "snake": return snake;
                case "eye": return eye;
                case "sunflower": return sunflower;
                case "vault_warden": return vaultWarden != null ? vaultWarden : skeleton;
                default: return slime;
            }
        }

        public Sprite GetQuestBubble(QuestBubbleKind kind)
        {
            switch (kind)
            {
                case QuestBubbleKind.Available:
                    return questBubbleAvailable;
                case QuestBubbleKind.TurnIn:
                    return questBubbleTurnIn;
                default:
                    return null;
            }
        }

        public static Sprite[] MergeSprites(params Sprite[][] groups)
        {
            int count = 0;
            foreach (Sprite[] group in groups)
            {
                if (group != null)
                {
                    count += group.Length;
                }
            }

            if (count == 0)
            {
                return System.Array.Empty<Sprite>();
            }

            var merged = new Sprite[count];
            int index = 0;
            foreach (Sprite[] group in groups)
            {
                if (group == null)
                {
                    continue;
                }

                foreach (Sprite sprite in group)
                {
                    if (sprite != null)
                    {
                        merged[index++] = sprite;
                    }
                }
            }

            if (index == merged.Length)
            {
                return merged;
            }

            var trimmed = new Sprite[index];
            System.Array.Copy(merged, trimmed, index);
            return trimmed;
        }
    }

    public enum QuestBubbleKind
    {
        Available,
        TurnIn
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
                Debug.LogWarning("[ArtAssetResolver] Missing Resources/ArtCatalog.asset — run AetherEcho/Setup Project.");
            }
        }
    }
}
