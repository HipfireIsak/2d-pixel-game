using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Core;
using AetherEcho.Enemies;
using AetherEcho.Rendering;
using AetherEcho.Quests;

namespace AetherEcho.World
{
    public class WorldContentSpawner : MonoBehaviour
    {
        private const int WorldSeed = 90210;

        private bool hasBuiltWorld;

        private void Update()
        {
            if (!NetworkServer.active || hasBuiltWorld)
            {
                return;
            }

            hasBuiltWorld = true;
            DestroyLegacyWorldObjects();
            BuildEnvironment();
            RegisterAreaSpawnZones();
            SpawnHubNpcs();
        }

        private void RegisterAreaSpawnZones()
        {
            if (MobSpawnZoneManager.Instance == null)
            {
                return;
            }

            float chunkSpan = GameConstants.ChunkHalfExtentMeters * 2f;
            float gridOrigin = -(GameConstants.BiomeGridSize * chunkSpan * 0.5f) + (chunkSpan * 0.5f);
            string[] slimeBiome = { "slime" };
            string[] mixedWilds = { "slime", "rat", "snake" };
            string[] stoneBiome = { "skeleton", "bat", "eye" };

            for (int z = 0; z < GameConstants.BiomeGridSize; z++)
            {
                for (int x = 0; x < GameConstants.BiomeGridSize; x++)
                {
                    Vector3 center = new Vector3(gridOrigin + x * chunkSpan, 0f, gridOrigin + z * chunkSpan);
                    if (center.magnitude < GameConstants.SpawnSafeRadiusMeters + 8f)
                    {
                        MobSpawnZoneManager.Instance.RegisterZone(
                            center + new Vector3(28f, 0f, 28f),
                            34f,
                            slimeBiome,
                            10,
                            10f,
                            2,
                            4);
                        continue;
                    }

                    string[] types = x == 1 || z == 1 ? slimeBiome : (x == 2 ? stoneBiome : mixedWilds);
                    MobSpawnZoneManager.Instance.RegisterZone(center, 58f, types, 12, GameConstants.MobRespawnSecondsDefault, 2, 5);
                }
            }
        }

        private static void DestroyLegacyWorldObjects()
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground != null)
            {
                Destroy(ground);
            }

            GameObject worldContent = GameObject.Find("WorldContent");
            if (worldContent != null)
            {
                Destroy(worldContent);
            }
        }

        private void BuildEnvironment()
        {
            ArtCatalog art = ArtAssetResolver.Catalog;
            DungeonTaleTileCatalog tiles = DungeonTaleTileCatalogLoader.Catalog;
            var worldRoot = new GameObject("WorldContent");
            WorldAtmosphere.ApplyDrakantosStyle();
            DungeonTaleWorldBuilder.BuildWorld(worldRoot.transform, tiles, art, WorldSeed);
            CreateDirectionalLightIfMissing();
        }

        public static NetworkedEnemy SpawnEnemyPublic(string typeId, Vector3 position, int level)
        {
            return SpawnEnemy(typeId, position, level);
        }

        private static void CreateDirectionalLightIfMissing()
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    light.intensity = 1.1f;
                    light.color = new Color(1f, 0.98f, 0.92f);
                    light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                    return;
                }
            }

            var lightObject = new GameObject("Sun");
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.color = new Color(1f, 0.98f, 0.92f);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private void SpawnHubNpcs()
        {
            ArtCatalog art = ArtAssetResolver.Catalog;
            float npcScale = art != null && art.heroSouth != null
                ? WorldPropBuilder.ComputePropScale(art.questNpc, art.heroSouth.bounds.size.y * GameConstants.PlayerVisualScale)
                : GameConstants.PlayerVisualScale;

            SpawnQuestNpc("ChronoSage", "Chrono Sage", new Vector3(-2f, 0f, -2f), npcScale, art);
            SpawnQuestNpc("VaultKeeper", "Vault Keeper", new Vector3(6f, 0f, 2f), npcScale * 0.95f, art);
            SpawnQuestNpc("ChronoMerchantQuest", "Chrono Merchant", new Vector3(-6f, 0f, 4f), npcScale * 0.9f, art);
            SpawnVendorNpc(new Vector3(-6f, 0f, 2f), npcScale * 0.95f, art);
            SpawnDungeonPortal(new Vector3(2f, 0f, 6f), npcScale, art);
        }

        private static void SpawnQuestNpc(string objectName, string questGiverName, Vector3 position, float scale, ArtCatalog art)
        {
            GameObject npc = WorldPropBuilder.CreateBillboardProp(
                null,
                objectName,
                art != null ? art.questNpc : null,
                position,
                scale,
                addObstacleCollider: false,
                isTree: false,
                facingMode: Rendering.SpriteFacingMode.FixedSouth);
            QuestNpcInteractable interactable = npc.AddComponent<QuestNpcInteractable>();
            interactable.Configure(questGiverName);
            SphereCollider collider = npc.AddComponent<SphereCollider>();
            collider.radius = 0.9f;
            collider.isTrigger = true;
        }

        private static void SpawnVendorNpc(Vector3 position, float scale, ArtCatalog art)
        {
            GameObject npc = WorldPropBuilder.CreateBillboardProp(
                null,
                "ChronoMerchant",
                art != null ? art.questNpc : null,
                position,
                scale,
                addObstacleCollider: false,
                isTree: false,
                facingMode: Rendering.SpriteFacingMode.FixedSouth);
            npc.AddComponent<VendorNpcInteractable>();
            SphereCollider collider = npc.AddComponent<SphereCollider>();
            collider.radius = 0.9f;
            collider.isTrigger = true;
        }

        private static void SpawnDungeonPortal(Vector3 position, float scale, ArtCatalog art)
        {
            GameObject portal = WorldPropBuilder.CreateBillboardProp(
                null,
                "EchoVaultPortal",
                art != null ? art.questNpc : null,
                position,
                scale * 1.1f,
                addObstacleCollider: false,
                isTree: false,
                facingMode: Rendering.SpriteFacingMode.FixedSouth);
            portal.AddComponent<DungeonPortalInteractable>();
            SphereCollider collider = portal.AddComponent<SphereCollider>();
            collider.radius = 1.1f;
            collider.isTrigger = true;
        }

        private static NetworkedEnemy SpawnEnemy(string typeId, Vector3 position, int level)
        {
            GameObject prefab = Resources.Load<GameObject>("Enemies/" + typeId);
            GameObject enemyObject = prefab != null
                ? Instantiate(prefab, position, Quaternion.identity)
                : BuildFallbackEnemy(typeId, position);

            NetworkedEnemy enemy = enemyObject.GetComponent<NetworkedEnemy>();
            if (enemy != null)
            {
                NetworkServer.Spawn(enemyObject);
                enemy.ServerInitialize(typeId, level);
            }

            return enemy;
        }

        private static GameObject BuildFallbackEnemy(string typeId, Vector3 position)
        {
            var enemyObject = new GameObject("Enemy_" + typeId);
            enemyObject.transform.position = FlatMovementUtility.SnapToGround(position);
            WorldPropBuilder.AddFlatHitCollider(enemyObject, GameConstants.EnemyCollisionRadius);
            enemyObject.AddComponent<CombatantState>();
            enemyObject.AddComponent<PixelBillboardVisual>();
            enemyObject.AddComponent<NetworkedEnemy>();
            enemyObject.AddComponent<EnemyDeathNotifier>();
            // NetworkIdentity must be added after all NetworkBehaviour components so Awake wires netIdentity.
            enemyObject.AddComponent<NetworkIdentity>();
            return enemyObject;
        }
    }
}
