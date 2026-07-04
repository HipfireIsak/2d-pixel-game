using System.IO;
using Mirror;
using kcp2k;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
            GameObject slimePrefab = CreateEnemyPrefab("slime", SlimePrefabPath, artCatalog.slime);
            GameObject skeletonPrefab = CreateEnemyPrefab("skeleton", SkeletonPrefabPath, artCatalog.skeleton);
            GameObject spellProjectilePrefab = CreateSpellProjectilePrefab();
            GameObject playerPrefab = CreatePlayerPrefab(artCatalog);
            GameObject bootstrapPrefab = CreateNetworkBootstrapPrefab(playerPrefab, slimePrefab, skeletonPrefab, spellProjectilePrefab);
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
            catalog.questNpc = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Char_Trader");
            catalog.floorA = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Floor_A");
            catalog.floorB = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Atlas.png", "Floor_B");
            catalog.floorGrass = LoadSprite("Assets/Skipan's Jungle Sprites/Grass/Grass_02.png");
            catalog.tree = LoadSprite("Assets/Skipan's Jungle Sprites/Trees/Tree_01.png");
            catalog.rock = LoadSprite("Assets/Skipan's Jungle Sprites/Rocks/Rock_02.png");
            catalog.trees = new[]
            {
                LoadSprite("Assets/Skipan's Jungle Sprites/Trees/Tree_01.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Trees/Tree_02.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Trees/Tree_03.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Trees/Twigs_01.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Trees/Twigs_02.png")
            };
            catalog.rocks = new[]
            {
                LoadSprite("Assets/Skipan's Jungle Sprites/Rocks/Rock_01.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Rocks/Rock_02.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Rocks/Rock_03.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Rocks/RockGroup_01.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Rocks/RockGroup_02.png")
            };
            catalog.bushes = new[]
            {
                LoadSprite("Assets/Skipan's Jungle Sprites/Vegetation/Bush_01.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Vegetation/Moss_01.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Vegetation/Moss_02.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Vegetation/Moss_03.png")
            };
            catalog.mushrooms = new[]
            {
                LoadSprite("Assets/Skipan's Jungle Sprites/Vegetation/Mushroom_01.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Vegetation/Mushroom_02.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Vegetation/Mushroom_03.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Vegetation/Doublemushroom.png")
            };
            catalog.reeds = new[]
            {
                LoadSprite("Assets/Skipan's Jungle Sprites/Vegetation/Reed_01.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Vegetation/ReedGroup_01.png"),
                LoadSprite("Assets/Skipan's Jungle Sprites/Vegetation/ReedGroup_02.png")
            };
            catalog.spellBeam = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Fx.png", "Fx_5");
            catalog.spellBurst = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Fx.png", "Fx_10");
            catalog.spellPulse = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Fx.png", "Fx_0");
            catalog.timeEcho = LoadSprite("Assets/Tileset/Dungeon Tale/Assets/Sprites/Fx.png", "Fx_15");
            EditorUtility.SetDirty(catalog);
            return catalog;
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
                scale: heroScale);

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
            enemyRoot.AddComponent<CombatantState>();
            enemyRoot.AddComponent<PixelBillboardVisual>();
            enemyRoot.AddComponent<NetworkedEnemy>();
            enemyRoot.AddComponent<EnemyDeathNotifier>();
            float scale = 1.1f;
            float offset = FlatMovementUtility.GetSpriteGroundOffset(sprite, scale);
            enemyRoot.GetComponent<PixelBillboardVisual>().Configure(
                enemyRoot.transform,
                sprite,
                directionalHero: false,
                offset: new Vector3(0f, offset, 0f),
                scale: scale);

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
            GameObject slimePrefab,
            GameObject skeletonPrefab,
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
            RegisterSpawnPrefab(networkManager, slimePrefab);
            RegisterSpawnPrefab(networkManager, skeletonPrefab);
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
