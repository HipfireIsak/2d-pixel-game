using System.Collections.Generic;
using Mirror;
using UnityEngine;
using AetherEcho.Enemies;
using AetherEcho.Rendering;

namespace AetherEcho.World
{
    public class VaultWardenBossController : MonoBehaviour
    {
        private const float SkeletonSpawnIntervalSeconds = 8f;
        private const float SkeletonSpawnRadius = 4.5f;
        private const int MaxActiveSkeletons = 4;

        private readonly List<uint> minionNetIds = new List<uint>();
        private NetworkIdentity networkIdentity;
        private float skeletonSpawnTimer;

        public void ServerInitialize()
        {
            networkIdentity = GetComponent<NetworkIdentity>();
            skeletonSpawnTimer = SkeletonSpawnIntervalSeconds * 0.5f;
            minionNetIds.Clear();
            ApplyBossPresentation();
        }

        private void ApplyBossPresentation()
        {
            BouncingSpriteAnimator bounce = GetComponent<BouncingSpriteAnimator>();
            if (bounce == null)
            {
                bounce = gameObject.AddComponent<BouncingSpriteAnimator>();
            }

            bounce.Configure(null, 0.42f, 2.4f, Random.Range(0f, 1.5f));
        }

        private void Update()
        {
            if (!IsServerAuthority())
            {
                return;
            }

            skeletonSpawnTimer -= Time.deltaTime;
            PruneDeadMinions();
            if (skeletonSpawnTimer > 0f || minionNetIds.Count >= MaxActiveSkeletons)
            {
                return;
            }

            skeletonSpawnTimer = SkeletonSpawnIntervalSeconds;
            TrySpawnSkeletonMinion();
        }

        private bool IsServerAuthority()
        {
            return NetworkServer.active && networkIdentity != null && networkIdentity.isServer;
        }

        private void TrySpawnSkeletonMinion()
        {
            Vector2 offset = Random.insideUnitCircle * SkeletonSpawnRadius;
            Vector3 spawnPosition = transform.position + new Vector3(offset.x, 0f, offset.y);
            NetworkedEnemy skeleton = WorldContentSpawner.SpawnEnemyPublic("skeleton", spawnPosition, 5);
            if (skeleton == null || skeleton.netIdentity == null)
            {
                return;
            }

            minionNetIds.Add(skeleton.netIdentity.netId);
        }

        private void PruneDeadMinions()
        {
            for (int i = minionNetIds.Count - 1; i >= 0; i--)
            {
                if (!NetworkServer.spawned.ContainsKey(minionNetIds[i]))
                {
                    minionNetIds.RemoveAt(i);
                }
            }
        }
    }
}
