using System.IO;
using Mirror;
using kcp2k;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using AetherEcho.Combat;
using AetherEcho.Core;
using AetherEcho.Enemies;
using AetherEcho.Networking;
using AetherEcho.Player;
using AetherEcho.Rendering;
using AetherEcho.UI;
using AetherEcho.World;

namespace AetherEcho.EditorTools
{
    public static class ProjectBootstrapUtility
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string PlayerPrefabPath = "Assets/Prefabs/PlayerAvatar.prefab";
        private const string NetworkBootstrapPath = "Assets/Prefabs/NetworkBootstrap.prefab";
        private const string ArtCatalogPath = "Assets/Resources/ArtCatalog.asset";
        private const string SlimePrefabPath = "Assets/Resources/Enemies/slime.prefab";
        private const string SkeletonPrefabPath = "Assets/Resources/Enemies/skeleton.prefab";
        private const string SpellProjectilePath = "Assets/Resources/Spells/SpellProjectile.prefab";
        private const string GroundLootPath = "Assets/Resources/Items/GroundLoot.prefab";

        [MenuItem("AetherEcho/Setup Project")]
        public static void SetupProject()
        {
            RebuildAll();
        }

        public static void RebuildAll()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Resources");
            Directory.CreateDirectory("Assets/Resources/Enemies");
            Directory.CreateDirectory("Assets/Resources/Spells");
            Directory.CreateDirectory("Assets/Resources/Items");

            ArtCatalog artCatalog = CreateOrUpdateArtCatalog();
            CreateOrUpdateTileCatalog();
            GameObject slimePrefab = CreateEnemyPrefab("slime", SlimePrefabPath, artCatalog.slime);
            GameObject skeletonPrefab = CreateEnemyPrefab("skeleton", SkeletonPrefabPath, artCatalog.skeleton);
            GameObject batPrefab = CreateEnemyPrefab("bat", "Assets/Resources/Enemies/bat.prefab", artCatalog.bat);
            GameObject ratPrefab = CreateEnemyPrefab("rat", "Assets/Resources/Enemies/rat.prefab", artCatalog.rat);
            GameObject snakePrefab = CreateEnemyPrefab("snake", "Assets/Resources/Enemies/snake.prefab", artCatalog.snake);
            GameObject eyePrefab = CreateEnemyPrefab("eye", "Assets/Resources/Enemies/eye.prefab", artCatalog.eye);
            GameObject sunflowerPrefab = CreateEnemyPrefab("sunflower", "Assets/Resources/Enemies/sunflower.prefab", artCatalog.sunflower);
            GameObject vaultWardenPrefab = CreateEnemyPrefab("vault_warden", "Assets/Resources/Enemies/vault_warden.prefab", artCatalog.vaultWarden != null ? artCatalog.vaultWarden : artCatalog.skeleton);
            GameObject spellProjectilePrefab = CreateSpellProjectilePrefab();
            GameObject groundLootPrefab = CreateGroundLootPrefab();
            GameObject playerPrefab = CreatePlayerPrefab(artCatalog);
            GameObject bootstrapPrefab = CreateNetworkBootstrapPrefab(
                playerPrefab,
                new[] { slimePrefab, skeletonPrefab, batPrefab, ratPrefab, snakePrefab, eyePrefab, sunflowerPrefab, vaultWardenPrefab },
                spellProjectilePrefab,
                groundLootPrefab);
            CreateBootstrapScene(bootstrapPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static ArtCatalog CreateOrUpdateArtCatalog()
        {
            ArtCatalog catalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>(ArtCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ArtCatalog>();
                AssetDatabase.CreateAsset(catalog, ArtCatalogPath);
            }

            catalog.heroSouth = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Hero.png", "Hero_0");
            catalog.heroNorth = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Hero.png", "Hero_4");
            catalog.heroEast = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Hero.png", "Hero_8");
            catalog.slime = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Char_Slime");
            catalog.skeleton = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Char_Skeletone");
            catalog.bat = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Char_Bat");
            catalog.rat = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Char_Rat");
            catalog.snake = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Char_Snake");
            catalog.eye = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Char_Eye");
            catalog.sunflower = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Char_Sunflower");
            catalog.questNpc = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Char_Trader");
            catalog.floorA = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Floor_A");
            catalog.floorB = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Floor_B");
            catalog.floorC = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Floor_C");
            catalog.floorD = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Floor_D");
            catalog.floorWood = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Floor_Wood");
            catalog.floorMetal = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Floor_Metal");
            catalog.floorRed = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Floor_Red");
            catalog.floorDirt = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Floor_Dirt");
            catalog.floorFlash = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Floor_Flash");
            catalog.carpetA = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Carpet_A");
            catalog.carpetB = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Carpet_B");
            catalog.tree = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Prop_TreeA");
            catalog.rock = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Prop_Skull");
            const string atlas = "Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png";
            catalog.propsGrass = LoadSprites(atlas, "Prop_GrassA", "Prop_GrassB", "Prop_Green", "Prop_SA");
            catalog.propsForest = LoadSprites(atlas, "Prop_TreeA", "Prop_TreeB", "Prop_TreeC");
            catalog.propsRock = LoadSprites(atlas, "Prop_Skull", "Prop_Bone", "Prop_Dirt", "Prop_Vase_A", "Prop_Vase_B");
            catalog.propsWild = LoadSprites(atlas, "Prop_Shrooms", "Prop_Web", "Prop_Root_C", "Prop_Root_D", "Prop_Hand");
            catalog.propsRuin = LoadSprites(atlas, "Prop_Chain_A", "Prop_Chain_B", "Prop_Pipe_a", "Prop_Candles", "Prop_Vase_C");
            catalog.propsHub = LoadSprites(atlas, "Prop_Candles", "Prop_Vase_D", "Prop_GrassB");
            catalog.decorGrass = LoadSprites(atlas, "Prop_GrassA", "Prop_GrassB", "Prop_Green", "Prop_SB", "Prop_SC");
            catalog.decorForest = LoadSprites(atlas, "Prop_SB", "Prop_SC", "Prop_Root_E", "Prop_Root_F");
            catalog.decorRock = LoadSprites(atlas, "Prop_Bone", "Prop_Skull", "Prop_Dirt");
            catalog.decorWild = LoadSprites(atlas, "Prop_Shrooms", "Prop_Web");
            catalog.decorRuin = LoadSprites(atlas, "Prop_Pipe_B", "Prop_Pipe_C", "Prop_Env_A", "Prop_Env_B");
            catalog.decorHub = LoadSprites(atlas, "Prop_GrassB", "Prop_Candles");
            catalog.spellBeam = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Fx.png", "Fx_5");
            catalog.spellBurst = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Fx.png", "Fx_10");
            catalog.spellPulse = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Fx.png", "Fx_0");
            catalog.timeEcho = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Fx.png", "Fx_15");

            WireBossAndQuestUi(catalog, atlas);
            WireAnimatedProps(catalog);
            WireWaterAndVfx(catalog);
            WireWallsAndDecals(catalog, atlas);
            WireDungeonTaleEnvironment(catalog, atlas);
            WireMedievalDungeonProps(catalog);
            WireExternalTilesets(catalog);

            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void WireBossAndQuestUi(ArtCatalog catalog, string atlas)
        {
            catalog.vaultWarden = LoadSprite(atlas, "Prop_Env_X");
            catalog.heartBoss = LoadSprite(atlas, "Char_Heat");
            catalog.questBubbleAvailable = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Bubbles.png", "Bubbles_5");
            catalog.questBubbleTurnIn = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Bubbles.png", "Bubbles_8");
            catalog.questBubbleThinking = LoadSprites(
                "Assets/Tileset/Dungeon Tale/Assets/Sprites/Bubbles.png",
                "Bubbles_10",
                "Bubbles_11",
                "Bubbles_12",
                "Bubbles_13");
        }

        private static void WireAnimatedProps(ArtCatalog catalog)
        {
            catalog.torchFrames = LoadSpriteSequence("Assets/Tileset/Dungeon Tale/Assets/Sprites/Torch.png", "Torch_", 1, 7);
            catalog.spikeTrapFrames = LoadAllSpritesFromTexture("Assets/Tileset/Dungeon Tale/Assets/Sprites/Spikes.png");
        }

        private static void WireWaterAndVfx(ArtCatalog catalog)
        {
            const string atlas = "Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png";
            catalog.waterFloorTile = LoadSprite(atlas, "Floor_C");
            catalog.waterCausticFrames = LoadAllSpritesFromTexture("Assets/Free Asset - 2D Handcrafted Art/Sprite/WaterCausticSprite.psd");
            catalog.vfxWaterDrop = LoadSprite("Assets/Free Asset - 2D Handcrafted Art/Sprite/WaterDrop.png");
            catalog.vfxWaterRipple = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Fx.png", "Fx_2");
            catalog.vfxDustFrames = LoadAllSpritesFromTexture("Assets/Free Asset - 2D Handcrafted Art/Sprite/Dust1.psd");
            catalog.vfxFogFrames = LoadAllSpritesFromTexture("Assets/Free Asset - 2D Handcrafted Art/Sprite/Fog.psd");
            catalog.vfxSunRay = LoadSprite("Assets/Free Asset - 2D Handcrafted Art/Sprite/SunRays.psd");

            const string vfxPrefabRoot = "Assets/Free Asset - 2D Handcrafted Art/VFXPrefab/";
            catalog.vfxPrefabDrops = LoadPrefab(vfxPrefabRoot + "Drops.prefab");
            catalog.vfxPrefabDust = LoadPrefab(vfxPrefabRoot + "Dust.prefab");
            catalog.vfxPrefabDust2 = LoadPrefab(vfxPrefabRoot + "Dust2.prefab");
            catalog.vfxPrefabRipple = LoadPrefab(vfxPrefabRoot + "Ripple.prefab");
            catalog.vfxPrefabSunRays = LoadPrefab(vfxPrefabRoot + "Sun_rays.prefab");
            catalog.vfxPrefabWaterSuspension = LoadPrefab(vfxPrefabRoot + "WaterSuspension.prefab");
        }

        private static void WireWallsAndDecals(ArtCatalog catalog, string atlas)
        {
            catalog.wallSegments = LoadSprites(
                atlas,
                "Wall_A", "Wall_B", "Wall_C", "Wall_D", "Wall_E", "Wall_F",
                "Wall_G", "Wall_H", "Wall_L", "Wall_N", "Wall_R", "Wall_T");
            catalog.floorDecals = LoadSprites(
                atlas,
                "Decal_A", "Decal_B", "Decal_C", "Decal_D", "Decal_E", "Decal_F",
                "Decal_G", "Decal_H", "Decal_J", "Decal_K", "Decal_L", "Decal_Star",
                "Decal_AyeA", "Decal_AyeB", "Decal_Spot", "Decal_ShadeA", "Decal_ShadeB");
        }

        private static void WireDungeonTaleEnvironment(ArtCatalog catalog, string atlas)
        {
            catalog.dungeonBanners = LoadSprites(atlas, "Prop_Env_C", "Prop_Env_C_D", "Prop_Env_C_L", "Prop_Env_C_R", "Prop_Env_C_R_B", "Prop_Env_I");
            catalog.dungeonPaintings = LoadSprites(atlas, "Prop_Env_J", "Prop_Env_K");
            catalog.dungeonBooks = LoadSprites(atlas, "Prop_Env_U");
            catalog.dungeonPipes = LoadSprites(atlas, "Prop_Pipe_a", "Prop_Pipe_B", "Prop_Pipe_C");
            catalog.dungeonRoots = LoadSprites(atlas, "Prop_Root_A", "Prop_Root_B", "Prop_Root_C", "Prop_Root_D", "Prop_Root_E", "Prop_Root_F", "Prop_Root_G", "Prop_Root_K");
            catalog.dungeonVases = LoadSprites(atlas, "Prop_Vase_A", "Prop_Vase_B", "Prop_Vase_C", "Prop_Vase_D", "Prop_Vase_E");
            catalog.dungeonTrees = LoadSprites(atlas, "Prop_TreeA", "Prop_TreeB", "Prop_TreeC");
            catalog.dungeonWebs = LoadSprites(atlas, "Prop_Web");
            catalog.dungeonChains = LoadSprites(atlas, "Prop_Chain_A", "Prop_Chain_B");
            catalog.dungeonCandles = LoadSprites(atlas, "Prop_Candles");
            catalog.dungeonBones = LoadSprites(atlas, "Prop_Bone", "Prop_Skull");
            catalog.dungeonGravestones = LoadSprites(atlas, "Prop_Env_Z", "Prop_Env_L");
            catalog.dungeonFlags = LoadSprites(atlas, "Prop_Env_C_L", "Prop_Env_C_R");
            catalog.dungeonMiscEnv = LoadSprites(
                atlas,
                "Prop_Env_A", "Prop_Env_B", "Prop_Env_D", "Prop_Env_E", "Prop_Env_F",
                "Prop_Env_G", "Prop_Env_H", "Prop_Env_M", "Prop_Env_O", "Prop_Env_P",
                "Prop_Hand", "Prop_GrassA", "Prop_GrassB", "Prop_Green", "Prop_Dirt", "Prop_Shrooms");
        }

        private static void WireMedievalDungeonProps(ArtCatalog catalog)
        {
            const string medieval = "Assets/Medieval_pixel_art_asset_FREE/Textures/Medieval_props_free.png";
            Sprite[] all = LoadAllSpritesFromTexture(medieval);
            catalog.dungeonLeverOn = GetSpriteAt(all, 0);
            catalog.dungeonWindow = GetSpriteAt(all, 2);
            catalog.dungeonDoorClosed = GetSpriteAt(all, 3);
            catalog.dungeonDoorOpen = GetSpriteAt(all, 4);
            catalog.dungeonDoorBarred = GetSpriteAt(all, 5);
            catalog.dungeonDoorEmpty = GetSpriteAt(all, 6);
            catalog.dungeonChestClosed = GetSpriteAt(all, 7);
            catalog.dungeonChestOpen = GetSpriteAt(all, 8);
            catalog.dungeonBarrel = GetSpriteAt(all, 12);
            catalog.dungeonCrate = GetSpriteAt(all, 11);
            catalog.dungeonLeverOff = GetSpriteAt(all, 14);
            catalog.dungeonTorchStatic = GetSpriteAt(all, 23);
            catalog.dungeonShield = GetSpriteAt(all, 22);
            catalog.dungeonBridgeFloor = GetSpriteAt(all, 19);
            catalog.dungeonChainsMedieval = SliceSprites(all, 15, 2);
            catalog.dungeonLadders = SliceSprites(all, 17, 2);
            catalog.dungeonRailings = SliceSprites(all, 19, 5);
            catalog.dungeonUncertain = SliceSprites(all, 1, 1);
        }

        private static void WireExternalTilesets(ArtCatalog catalog)
        {
            catalog.medievalFloors = SliceSprites(LoadAllSpritesFromTexture("Assets/Medieval_pixel_art_asset_FREE/Textures/Medieval_tiles_free2.png"), 0, 12);
            catalog.medievalProps = LoadAllSpritesFromTexture("Assets/Medieval_pixel_art_asset_FREE/Textures/Medieval_props_free.png");
            catalog.jungleGrass = LoadTextureSprites(
                "Assets/Skipan's Jungle Sprites/Grass/Grass_01.png",
                "Assets/Skipan's Jungle Sprites/Grass/Grass_02.png",
                "Assets/Skipan's Jungle Sprites/Grass/Grass_03.png",
                "Assets/Skipan's Jungle Sprites/Grass/Grass_04.png",
                "Assets/Skipan's Jungle Sprites/Grass/Grass_05.png");
            catalog.jungleTrees = LoadTextureSprites(
                "Assets/Skipan's Jungle Sprites/Trees/Tree_01.png",
                "Assets/Skipan's Jungle Sprites/Trees/Tree_02.png",
                "Assets/Skipan's Jungle Sprites/Trees/Tree_03.png",
                "Assets/Skipan's Jungle Sprites/Trees/BigTree_01.png",
                "Assets/Skipan's Jungle Sprites/Trees/BigTree_02.png");
            catalog.jungleRocks = LoadTextureSprites(
                "Assets/Skipan's Jungle Sprites/Rocks/Rock_01.png",
                "Assets/Skipan's Jungle Sprites/Rocks/Rock_02.png",
                "Assets/Skipan's Jungle Sprites/Rocks/Rock_03.png",
                "Assets/Skipan's Jungle Sprites/Rocks/RockGroup_01.png",
                "Assets/Skipan's Jungle Sprites/Rocks/RockGroup_02.png",
                "Assets/Skipan's Jungle Sprites/Rocks/RockPlatform_01.png");
            catalog.jungleVegetation = LoadTextureSprites(
                "Assets/Skipan's Jungle Sprites/Vegetation/Moss_01.png",
                "Assets/Skipan's Jungle Sprites/Vegetation/Moss_02.png",
                "Assets/Skipan's Jungle Sprites/Vegetation/Moss_03.png",
                "Assets/Skipan's Jungle Sprites/Vegetation/Mushroom_01.png",
                "Assets/Skipan's Jungle Sprites/Vegetation/Mushroom_02.png",
                "Assets/Skipan's Jungle Sprites/Vegetation/Mushroom_03.png",
                "Assets/Skipan's Jungle Sprites/Vegetation/Bush_01.png",
                "Assets/Skipan's Jungle Sprites/Vegetation/Flower_01.png",
                "Assets/Skipan's Jungle Sprites/Vegetation/Flower_02.png",
                "Assets/Skipan's Jungle Sprites/Vegetation/Reed_01.png");
            catalog.handcraftedMoss = LoadTextureSprites(
                "Assets/Free Asset - 2D Handcrafted Art/Sprite/Moss1/_1.png",
                "Assets/Free Asset - 2D Handcrafted Art/Sprite/Moss1/_2.png",
                "Assets/Free Asset - 2D Handcrafted Art/Sprite/Moss1/_3.png",
                "Assets/Free Asset - 2D Handcrafted Art/Sprite/Moss1/_4.png",
                "Assets/Free Asset - 2D Handcrafted Art/Sprite/Moss1/_5.png");
            catalog.gameItemDecor = LoadSprites(
                "Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png",
                "Char_Trader", "Prop_Vase_A", "Prop_Candles", "Prop_Bone", "Prop_Skull");
            catalog.hillsBackdrop = LoadTextureSprites(
                "Assets/Skipan's Jungle Sprites/Water Miscs/Lake_01.png",
                "Assets/Skipan's Jungle Sprites/Misc/RaySparkles_01.png",
                "Assets/Skipan's Jungle Sprites/Dirt/GrassPlatform_01.png",
                "Assets/Skipan's Jungle Sprites/Dirt/GrassPlatform_02.png");
        }

        private static Sprite GetSpriteAt(Sprite[] sprites, int index)
        {
            if (sprites == null || index < 0 || index >= sprites.Length)
            {
                return null;
            }

            return sprites[index];
        }

        private static Sprite[] SliceSprites(Sprite[] sprites, int start, int count)
        {
            if (sprites == null || sprites.Length == 0 || count <= 0)
            {
                return System.Array.Empty<Sprite>();
            }

            int end = Mathf.Min(sprites.Length, start + count);
            if (start >= end)
            {
                return System.Array.Empty<Sprite>();
            }

            var slice = new Sprite[end - start];
            for (int i = start; i < end; i++)
            {
                slice[i - start] = sprites[i];
            }

            return slice;
        }

        private static Sprite[] LoadSpriteSequence(string texturePath, string prefix, int startIndex, int count)
        {
            var sprites = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                sprites[i] = LoadSprite(texturePath, prefix + (startIndex + i));
            }

            return sprites;
        }

        private static Sprite[] LoadAllSpritesFromTexture(string texturePath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            int spriteCount = 0;
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite)
                {
                    spriteCount++;
                }
            }

            if (spriteCount == 0)
            {
                return System.Array.Empty<Sprite>();
            }

            var sprites = new Sprite[spriteCount];
            int index = 0;
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    sprites[index++] = sprite;
                }
            }

            System.Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
            return sprites;
        }

        private static GameObject LoadPrefab(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void CreateOrUpdateTileCatalog()
        {
            const string path = "Assets/Resources/DungeonTaleTileCatalog.asset";
            const string tileRoot = "Assets/Tileset/Dungeon Tale/Assets/Tiles/";
            const string matPath = "Assets/Tileset/Dungeon Tale/Assets/Materials/Main.mat";

            DungeonTaleTileCatalog catalog = AssetDatabase.LoadAssetAtPath<DungeonTaleTileCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DungeonTaleTileCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            catalog.tileMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            // Main.mat uses a URP shader graph. Built-in pipeline projects use Sprites/Default at runtime instead.
            catalog.floorGreen = LoadTile(tileRoot + "Floor Green.asset");
            catalog.floorWood = LoadTile(tileRoot + "Floor Wood.asset");
            catalog.floorDirt = LoadTile(tileRoot + "Floor Dirt.asset");
            catalog.floorRed = LoadTile(tileRoot + "Floor Red.asset");
            catalog.floorMetal = LoadTile(tileRoot + "Floor Metal.asset");
            catalog.floorFlash = LoadTile(tileRoot + "Floor Flash.asset");
            catalog.wall = LoadTile(tileRoot + "Wall.asset");
            catalog.carpet = LoadTile(tileRoot + "Carpet.asset");
            catalog.cellSize = new Vector3(1.25f, 1.4375f, 0f);
            EditorUtility.SetDirty(catalog);
        }

        private static TileBase LoadTile(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<TileBase>(assetPath);
        }

        private static Sprite LoadSprite(string texturePath, string spriteName = null)
        {
            if (!string.IsNullOrEmpty(spriteName))
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
                foreach (Object asset in assets)
                {
                    if (asset is Sprite sprite && sprite.name == spriteName)
                    {
                        return sprite;
                    }
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
        }

        private static Sprite[] LoadSprites(string texturePath, params string[] spriteNames)
        {
            var sprites = new Sprite[spriteNames.Length];
            for (int i = 0; i < spriteNames.Length; i++)
            {
                sprites[i] = LoadSprite(texturePath, spriteNames[i]);
            }

            return sprites;
        }

        private static Sprite[] LoadTextureSprites(params string[] texturePaths)
        {
            var sprites = new Sprite[texturePaths.Length];
            for (int i = 0; i < texturePaths.Length; i++)
            {
                sprites[i] = LoadSprite(texturePaths[i]);
            }

            return sprites;
        }

        private static GameObject CreatePlayerPrefab(ArtCatalog artCatalog)
        {
            var playerRoot = new GameObject("PlayerAvatar");
            WorldPropBuilder.AddFlatHitCollider(playerRoot, GameConstants.PlayerCollisionRadius);

            playerRoot.AddComponent<NetworkIdentity>();
            PlayerMovementSyncSetup.Configure(playerRoot);
            playerRoot.AddComponent<CombatantState>();
            playerRoot.AddComponent<NetworkedCombatant>();
            playerRoot.AddComponent<AetherEcho.Items.PlayerInventory>();
            playerRoot.AddComponent<AetherEcho.Items.PlayerEquipment>();
            playerRoot.AddComponent<IsometricPlayerController>();
            var billboard = playerRoot.AddComponent<PixelBillboardVisual>();
            float heroScale = GameConstants.PlayerVisualScale;
            float heroOffset = FlatMovementUtility.GetSpriteGroundOffset(artCatalog.heroSouth, heroScale);
            billboard.Configure(
                playerRoot.transform,
                artCatalog.heroSouth,
                directionalHero: true,
                offset: new Vector3(0f, heroOffset, 0f),
                scale: heroScale,
                facing: SpriteFacingMode.BillboardY);

            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(playerRoot.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();

            var playerController = playerRoot.GetComponent<IsometricPlayerController>();
            SerializedObject serializedController = new SerializedObject(playerController);
            serializedController.FindProperty("playerCamera").objectReferenceValue = camera;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(playerRoot, PlayerPrefabPath);
        }

        private static GameObject CreateEnemyPrefab(string typeId, string path, Sprite sprite)
        {
            var enemyRoot = new GameObject("Enemy_" + typeId);
            WorldPropBuilder.AddFlatHitCollider(enemyRoot, GameConstants.EnemyCollisionRadius);

            enemyRoot.AddComponent<NetworkIdentity>();
            FlatMovementNetworkSync.Ensure(enemyRoot, MovementSyncMode.ServerAuthority);
            enemyRoot.AddComponent<CombatantState>();
            enemyRoot.AddComponent<PixelBillboardVisual>();
            enemyRoot.AddComponent<NetworkedEnemy>();
            enemyRoot.AddComponent<EnemyDeathNotifier>();
            float scale = GameConstants.EnemyVisualScale;
            float offset = FlatMovementUtility.GetSpriteGroundOffset(sprite, scale);
            enemyRoot.GetComponent<PixelBillboardVisual>().Configure(
                enemyRoot.transform,
                sprite,
                directionalHero: false,
                offset: new Vector3(0f, offset, 0f),
                scale: scale,
                facing: SpriteFacingMode.BillboardYWhenMoving);

            return SavePrefab(enemyRoot, path);
        }

        private static GameObject CreateGroundLootPrefab()
        {
            var lootRoot = new GameObject("GroundLoot");
            lootRoot.AddComponent<NetworkIdentity>();
            SphereCollider collider = lootRoot.AddComponent<SphereCollider>();
            collider.radius = 0.55f;
            collider.isTrigger = true;
            lootRoot.AddComponent<Items.GroundLootDrop>();
            return SavePrefab(lootRoot, GroundLootPath);
        }

        private static GameObject CreateSpellProjectilePrefab()
        {
            var projectileRoot = new GameObject("SpellProjectile");
            projectileRoot.AddComponent<NetworkIdentity>();
            FlatMovementNetworkSync.Ensure(projectileRoot, MovementSyncMode.ServerAuthority);
            projectileRoot.AddComponent<SpellProjectile>();
            return SavePrefab(projectileRoot, SpellProjectilePath);
        }

        private static GameObject CreateNetworkBootstrapPrefab(
            GameObject playerPrefab,
            GameObject[] enemyPrefabs,
            GameObject spellProjectilePrefab,
            GameObject groundLootPrefab)
        {
            var bootstrapObject = new GameObject("NetworkBootstrap");
            bootstrapObject.AddComponent<GameSystemsBootstrap>();

            AetherEchoNetworkManager networkManager = bootstrapObject.AddComponent<AetherEchoNetworkManager>();
            KcpTransport kcpTransport = bootstrapObject.AddComponent<KcpTransport>();
            NetworkSessionController sessionController = bootstrapObject.AddComponent<NetworkSessionController>();
            bootstrapObject.AddComponent<NetworkMenuUI>();

            networkManager.transport = kcpTransport;
            networkManager.playerPrefab = playerPrefab;
            networkManager.autoCreatePlayer = true;
            networkManager.maxConnections = GameConstants.MaximumPlayersPerSession;
            foreach (GameObject enemyPrefab in enemyPrefabs)
            {
                RegisterSpawnPrefab(networkManager, enemyPrefab);
            }

            RegisterSpawnPrefab(networkManager, spellProjectilePrefab);
            RegisterSpawnPrefab(networkManager, groundLootPrefab);

            SerializedObject serializedSession = new SerializedObject(sessionController);
            serializedSession.FindProperty("networkManager").objectReferenceValue = networkManager;
            serializedSession.ApplyModifiedPropertiesWithoutUndo();

            NetworkMenuUI menuUi = bootstrapObject.GetComponent<NetworkMenuUI>();
            SerializedObject serializedMenu = new SerializedObject(menuUi);
            serializedMenu.FindProperty("sessionController").objectReferenceValue = sessionController;
            serializedMenu.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(bootstrapObject, NetworkBootstrapPath);
        }

        private static void RegisterSpawnPrefab(AetherEchoNetworkManager networkManager, GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            NetworkIdentity identity = prefab.GetComponent<NetworkIdentity>();
            if (identity == null)
            {
                return;
            }

            if (!networkManager.spawnPrefabs.Contains(prefab))
            {
                networkManager.spawnPrefabs.Add(prefab);
            }
        }

        private static void CreateBootstrapScene(GameObject bootstrapPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Object.Instantiate(bootstrapPrefab);

            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.position = new Vector3(0f, GameConstants.GroundHeight, 0f);
            spawnPoint.AddComponent<NetworkStartPosition>();

            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.65f);
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootstrapScenePath, true) };
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
