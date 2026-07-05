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
            SpawnQuestNpc();
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
            var worldRoot = new GameObject("WorldContent");
            WorldBiomeBuilder.BuildChunkGrid(worldRoot.transform, art, WorldSeed);
            CreateDirectionalLightIfMissing();
        }

        public static void SpawnEnemyPublic(string typeId, Vector3 position, int level)
        {
            SpawnEnemy(typeId, position, level);
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

        private void SpawnQuestNpc()
        {
            ArtCatalog art = ArtAssetResolver.Catalog;
            float npcScale = art != null && art.heroSouth != null
                ? WorldPropBuilder.ComputePropScale(art.questNpc, art.heroSouth.bounds.size.y * GameConstants.PlayerVisualScale)
                : GameConstants.PlayerVisualScale;

            GameObject npc = WorldPropBuilder.CreateBillboardProp(
                null,
                "ChronoSage",
                art != null ? art.questNpc : null,
                new Vector3(-2f, 0f, -2f),
                npcScale,
                addObstacleCollider: false,
                isTree: false,
                facingMode: SpriteFacingMode.BillboardY);
            npc.AddComponent<QuestNpcInteractable>();
            SphereCollider collider = npc.AddComponent<SphereCollider>();
            collider.radius = 0.9f;
            collider.isTrigger = true;
        }

        private static void SpawnEnemy(string typeId, Vector3 position, int level)
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
        }

        private static GameObject BuildFallbackEnemy(string typeId, Vector3 position)
        {
            var enemyObject = new GameObject("Enemy_" + typeId);
            enemyObject.transform.position = FlatMovementUtility.SnapToGround(position);
            WorldPropBuilder.AddFlatHitCollider(enemyObject, GameConstants.EnemyCollisionRadius);
            enemyObject.AddComponent<NetworkIdentity>();
            enemyObject.AddComponent<CombatantState>();
            enemyObject.AddComponent<PixelBillboardVisual>();
            enemyObject.AddComponent<NetworkedEnemy>();
            enemyObject.AddComponent<EnemyDeathNotifier>();
            return enemyObject;
        }
    }
}
