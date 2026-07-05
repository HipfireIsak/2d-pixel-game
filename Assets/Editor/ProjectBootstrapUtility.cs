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

        [MenuItem("AetherEcho/Setup Project")]
        public static void SetupProject()
        {
            RebuildAll();
            EditorUtility.DisplayDialog(
                "AetherEcho Setup Complete",
                "Open Assets/Scenes/Bootstrap.unity and press Play.\n\n"
                + "Host, then explore, talk to the Chrono Sage (E), and cast spells with 1-3.",
                "OK");
        }

        [MenuItem("AetherEcho/Rebuild World Visuals")]
        public static void RebuildAll()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Resources");
            Directory.CreateDirectory("Assets/Resources/Enemies");
            Directory.CreateDirectory("Assets/Resources/Spells");

            ArtCatalog artCatalog = CreateOrUpdateArtCatalog();
            CreateOrUpdateTileCatalog();
            GameObject slimePrefab = CreateEnemyPrefab("slime", SlimePrefabPath, artCatalog.slime);
            GameObject skeletonPrefab = CreateEnemyPrefab("skeleton", SkeletonPrefabPath, artCatalog.skeleton);
            GameObject batPrefab = CreateEnemyPrefab("bat", "Assets/Resources/Enemies/bat.prefab", artCatalog.bat);
            GameObject ratPrefab = CreateEnemyPrefab("rat", "Assets/Resources/Enemies/rat.prefab", artCatalog.rat);
            GameObject snakePrefab = CreateEnemyPrefab("snake", "Assets/Resources/Enemies/snake.prefab", artCatalog.snake);
            GameObject eyePrefab = CreateEnemyPrefab("eye", "Assets/Resources/Enemies/eye.prefab", artCatalog.eye);
            GameObject sunflowerPrefab = CreateEnemyPrefab("sunflower", "Assets/Resources/Enemies/sunflower.prefab", artCatalog.sunflower);
            GameObject spellProjectilePrefab = CreateSpellProjectilePrefab();
            GameObject playerPrefab = CreatePlayerPrefab(artCatalog);
            GameObject bootstrapPrefab = CreateNetworkBootstrapPrefab(
                playerPrefab,
                new[] { slimePrefab, skeletonPrefab, batPrefab, ratPrefab, snakePrefab, eyePrefab, sunflowerPrefab },
                spellProjectilePrefab);
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
            EditorUtility.SetDirty(catalog);
            return catalog;
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
            playerRoot.AddComponent<CombatantState>();
            playerRoot.AddComponent<NetworkedCombatant>();
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

            enemyRoot.AddComponent<CombatantState>();
            enemyRoot.AddComponent<PixelBillboardVisual>();
            enemyRoot.AddComponent<NetworkedEnemy>();
            enemyRoot.AddComponent<EnemyDeathNotifier>();
            enemyRoot.AddComponent<NetworkIdentity>();
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

        private static GameObject CreateSpellProjectilePrefab()
        {
            var projectileRoot = new GameObject("SpellProjectile");
            projectileRoot.AddComponent<NetworkIdentity>();
            projectileRoot.AddComponent<SpellProjectile>();
            return SavePrefab(projectileRoot, SpellProjectilePath);
        }

        private static GameObject CreateNetworkBootstrapPrefab(
            GameObject playerPrefab,
            GameObject[] enemyPrefabs,
            GameObject spellProjectilePrefab)
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
