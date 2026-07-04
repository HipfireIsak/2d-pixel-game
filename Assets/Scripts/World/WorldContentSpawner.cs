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
            SpawnEnemies();
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

            if (art != null && art.floorA != null)
            {
                float extent = GameConstants.WorldHalfExtentMeters * 2f;
                WorldPropBuilder.CreateSeamlessFloorGrid(
                    worldRoot.transform,
                    art.floorA,
                    art.floorB,
                    extent,
                    extent);
            }

            ScatterEnvironmentProps(worldRoot.transform, art);
            CreateDirectionalLightIfMissing();
        }

        private static void ScatterEnvironmentProps(Transform parent, ArtCatalog art)
        {
            if (art == null)
            {
                return;
            }

            float playerHeight = art.heroSouth != null
                ? art.heroSouth.bounds.size.y * GameConstants.PlayerVisualScale
                : 1.2f;
            float treeScaleBase = WorldPropBuilder.ComputePropScale(art.tree, playerHeight * GameConstants.TreeHeightMultiplier);
            float rockScaleBase = WorldPropBuilder.ComputePropScale(art.rock, playerHeight * GameConstants.RockHeightMultiplier);

            Random.InitState(WorldSeed);
            float extent = GameConstants.WorldHalfExtentMeters - 2f;

            ScatterProps(parent, GetSpriteArray(art.trees, art.tree), GameConstants.ProceduralTreeCount, extent, treeScaleBase, 0.85f, 1.1f, true);
            ScatterProps(parent, GetSpriteArray(art.rocks, art.rock), GameConstants.ProceduralRockCount, extent, rockScaleBase, 0.75f, 1.15f, false);
            ScatterDecor(parent, art.bushes, GameConstants.ProceduralBushCount, extent, playerHeight * 0.35f, 0.8f, 1.2f, "Bush");
            ScatterDecor(parent, art.mushrooms, GameConstants.ProceduralMushroomCount, extent, playerHeight * 0.18f, 0.85f, 1.15f, "Mushroom");
            ScatterDecor(parent, art.reeds, 35, extent, playerHeight * 0.55f, 0.9f, 1.1f, "Reed");
        }

        private static Sprite[] GetSpriteArray(Sprite[] sprites, Sprite fallback)
        {
            if (sprites != null && sprites.Length > 0)
            {
                return sprites;
            }

            return fallback != null ? new[] { fallback } : System.Array.Empty<Sprite>();
        }

        private static void ScatterProps(
            Transform parent,
            Sprite[] sprites,
            int count,
            float extent,
            float baseScale,
            float minScaleMultiplier,
            float maxScaleMultiplier,
            bool isTree)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 position = RandomGroundPoint(extent);
                Sprite sprite = sprites[Random.Range(0, sprites.Length)];
                float targetHeight = (sprite.bounds.size.y * baseScale) * Random.Range(minScaleMultiplier, maxScaleMultiplier);
                float scale = WorldPropBuilder.ComputePropScale(sprite, targetHeight);
                WorldPropBuilder.CreateBillboardProp(
                    parent,
                    (isTree ? "Tree_" : "Rock_") + i,
                    sprite,
                    position,
                    scale,
                    addObstacleCollider: true,
                    isTree: isTree);
            }
        }

        private static void ScatterDecor(
            Transform parent,
            Sprite[] sprites,
            int count,
            float extent,
            float targetHeight,
            float minScaleMultiplier,
            float maxScaleMultiplier,
            string prefix)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 position = RandomGroundPoint(extent);
                Sprite sprite = sprites[Random.Range(0, sprites.Length)];
                float scale = WorldPropBuilder.ComputePropScale(sprite, targetHeight * Random.Range(minScaleMultiplier, maxScaleMultiplier));
                WorldPropBuilder.CreateDecorProp(parent, prefix + "_" + i, sprite, position, scale);
            }
        }

        private static Vector3 RandomGroundPoint(float extent)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float x = Random.Range(-extent, extent);
                float z = Random.Range(-extent, extent);
                if (new Vector2(x, z).magnitude < GameConstants.SpawnSafeRadiusMeters)
                {
                    continue;
                }

                return new Vector3(x, GameConstants.GroundHeight, z);
            }

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(GameConstants.SpawnSafeRadiusMeters + 2f, extent);
            return new Vector3(Mathf.Cos(angle) * radius, GameConstants.GroundHeight, Mathf.Sin(angle) * radius);
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

        private void SpawnEnemies()
        {
            Random.InitState(WorldSeed + 17);
            float extent = GameConstants.WorldHalfExtentMeters - 6f;

            for (int i = 0; i < 28; i++)
            {
                Vector3 position = RandomGroundPoint(extent * 0.85f);
                string typeId = Random.value > 0.45f ? "slime" : "skeleton";
                SpawnEnemy(typeId, position, 2 + (i % 4));
            }
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
                isTree: false);
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
