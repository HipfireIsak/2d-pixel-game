using System.Collections.Generic;
using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Core;
using AetherEcho.Enemies;

namespace AetherEcho.World
{
    public struct MobSpawnZone
    {
        public int zoneId;
        public Vector3 center;
        public float radius;
        public string[] enemyTypes;
        public int maxAlive;
        public float respawnSeconds;
        public int minLevel;
        public int maxLevel;
    }

    public class MobSpawnZoneManager : MonoBehaviour
    {
        public static MobSpawnZoneManager Instance { get; private set; }

        private readonly List<MobSpawnZone> zones = new List<MobSpawnZone>();
        private readonly Dictionary<int, HashSet<uint>> aliveByZone = new Dictionary<int, HashSet<uint>>();
        private readonly Dictionary<uint, int> zoneByEnemyNetId = new Dictionary<uint, int>();
        private readonly Dictionary<int, float> respawnTimers = new Dictionary<int, float>();
        private int nextZoneId = 1;

        private void Awake()
        {
            Instance = this;
        }

        [Server]
        public int RegisterZone(Vector3 center, float radius, string[] enemyTypes, int maxAlive, float respawnSeconds, int minLevel, int maxLevel)
        {
            int zoneId = nextZoneId++;
            zones.Add(new MobSpawnZone
            {
                zoneId = zoneId,
                center = center,
                radius = radius,
                enemyTypes = enemyTypes,
                maxAlive = maxAlive,
                respawnSeconds = respawnSeconds,
                minLevel = minLevel,
                maxLevel = maxLevel
            });
            aliveByZone[zoneId] = new HashSet<uint>();
            FillZone(zoneId);
            return zoneId;
        }

        [Server]
        public void ServerNotifyEnemyDestroyed(uint enemyNetId)
        {
            if (!zoneByEnemyNetId.TryGetValue(enemyNetId, out int zoneId))
            {
                return;
            }

            zoneByEnemyNetId.Remove(enemyNetId);
            if (aliveByZone.TryGetValue(zoneId, out HashSet<uint> alive))
            {
                alive.Remove(enemyNetId);
            }

            respawnTimers[zoneId] = Time.time + GetZone(zoneId).respawnSeconds;
        }

        private void Update()
        {
            if (!NetworkServer.active)
            {
                return;
            }

            for (int i = 0; i < zones.Count; i++)
            {
                MobSpawnZone zone = zones[i];
                if (!respawnTimers.TryGetValue(zone.zoneId, out float respawnAt) || Time.time < respawnAt)
                {
                    continue;
                }

                respawnTimers.Remove(zone.zoneId);
                FillZone(zone.zoneId);
            }
        }

        [Server]
        private void FillZone(int zoneId)
        {
            MobSpawnZone zone = GetZone(zoneId);
            if (!aliveByZone.TryGetValue(zone.zoneId, out HashSet<uint> alive))
            {
                alive = new HashSet<uint>();
                aliveByZone[zone.zoneId] = alive;
            }

            while (alive.Count < zone.maxAlive)
            {
                if (!TrySpawnInZone(zone, out uint netId))
                {
                    break;
                }

                alive.Add(netId);
                zoneByEnemyNetId[netId] = zone.zoneId;
            }
        }

        [Server]
        private bool TrySpawnInZone(MobSpawnZone zone, out uint netId)
        {
            netId = 0;
            if (zone.enemyTypes == null || zone.enemyTypes.Length == 0)
            {
                return false;
            }

            Vector3 position = zone.center;
            for (int attempt = 0; attempt < 16; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * zone.radius;
                position = new Vector3(zone.center.x + offset.x, GameConstants.GroundHeight, zone.center.z + offset.y);
                if (Vector3.Distance(position, Vector3.zero) < GameConstants.SpawnSafeRadiusMeters)
                {
                    continue;
                }

                break;
            }

            string typeId = zone.enemyTypes[Random.Range(0, zone.enemyTypes.Length)];
            int level = Random.Range(zone.minLevel, zone.maxLevel + 1);
            NetworkedEnemy enemy = WorldContentSpawner.SpawnEnemyPublic(typeId, position, level);
            if (enemy != null && enemy.netIdentity != null)
            {
                netId = enemy.netId;
                return true;
            }

            return false;
        }

        private MobSpawnZone GetZone(int zoneId)
        {
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i].zoneId == zoneId)
                {
                    return zones[i];
                }
            }

            return default;
        }
    }
}
