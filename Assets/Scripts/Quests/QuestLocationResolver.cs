using AetherEcho.Core;
using UnityEngine;

namespace AetherEcho.Quests
{
    public static class QuestLocationResolver
    {
        private static readonly Vector3 HubSlimeZone = new Vector3(28f, 0f, 28f);
        private static readonly Vector3 StoneBiomeSouthEast = new Vector3(160f, 0f, 160f);
        private static readonly Vector3 StoneBiomeNorthEast = new Vector3(160f, 0f, -160f);
        private static readonly Vector3 DungeonPortal = new Vector3(2f, 0f, 6f);

        public static Vector3 ResolveGiverPosition(string questGiverName)
        {
            switch (questGiverName)
            {
                case "Vault Keeper": return new Vector3(6f, 0f, 2f);
                case "Chrono Merchant": return new Vector3(-6f, 0f, 4f);
                default: return new Vector3(-2f, 0f, -2f);
            }
        }

        public static Vector3 ResolveObjectivePosition(string questId, bool objectivesComplete)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                return Vector3.zero;
            }

            if (objectivesComplete
                && QuestManager.Instance != null
                && QuestManager.Instance.TryGetQuest(questId, out QuestDefinition quest))
            {
                return ResolveGiverPosition(quest.quest_giver_name);
            }

            if (QuestManager.Instance != null
                && QuestManager.Instance.TryGetQuest(questId, out QuestDefinition activeQuest)
                && activeQuest.objectives.Count > 0)
            {
                return ResolveObjectiveTarget(activeQuest.objectives[0]);
            }

            return HubSlimeZone;
        }

        public static string ResolveLocationLabel(string questId, bool objectivesComplete)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                return string.Empty;
            }

            if (objectivesComplete
                && QuestManager.Instance != null
                && QuestManager.Instance.TryGetQuest(questId, out QuestDefinition quest))
            {
                return "Return to " + quest.quest_giver_name;
            }

            if (QuestManager.Instance != null
                && QuestManager.Instance.TryGetQuest(questId, out QuestDefinition activeQuest)
                && activeQuest.objectives.Count > 0)
            {
                return ResolveObjectiveLabel(activeQuest.objectives[0]);
            }

            return string.Empty;
        }

        public static bool TryGetBiomeGridCell(Vector3 worldPosition, out int gridX, out int gridZ)
        {
            float chunkSpan = GameConstants.ChunkHalfExtentMeters * 2f;
            float gridOrigin = GetGridOrigin(chunkSpan);
            gridX = Mathf.Clamp(
                Mathf.FloorToInt((worldPosition.x - gridOrigin) / chunkSpan),
                0,
                GameConstants.BiomeGridSize - 1);
            gridZ = Mathf.Clamp(
                Mathf.FloorToInt((worldPosition.z - gridOrigin) / chunkSpan),
                0,
                GameConstants.BiomeGridSize - 1);
            return true;
        }

        private static Vector3 ResolveObjectiveTarget(QuestObjectiveDefinition objective)
        {
            if (objective == null)
            {
                return HubSlimeZone;
            }

            if (objective.type == "CollectItem")
            {
                return objective.target_id == "slime_gel" ? HubSlimeZone : HubSlimeZone;
            }

            switch (objective.target_id)
            {
                case "slime":
                case "rat":
                case "snake":
                    return HubSlimeZone;
                case "skeleton":
                case "bat":
                    return StoneBiomeSouthEast;
                case "eye":
                    return StoneBiomeSouthEast;
                case "vault_warden":
                    return DungeonPortal;
                default:
                    return HubSlimeZone;
            }
        }

        private static string ResolveObjectiveLabel(QuestObjectiveDefinition objective)
        {
            if (objective == null)
            {
                return string.Empty;
            }

            if (objective.type == "CollectItem")
            {
                return "Go to slime fields near the hub";
            }

            switch (objective.target_id)
            {
                case "slime":
                    return "Go to slime fields southeast of hub";
                case "skeleton":
                    return "Go to southeast stone biome (bottom-right on minimap)";
                case "eye":
                    return "Go to southeast stone biome — defeat floating red eye enemies";
                case "bat":
                    return "Go to southeast stone biome (bottom-right on minimap)";
                case "vault_warden":
                    return "Enter Echo Vault via the portal near hub";
                default:
                    return "Check the yellow marker on the minimap";
            }
        }

        private static float GetGridOrigin(float chunkSpan)
        {
            return -(GameConstants.BiomeGridSize * chunkSpan * 0.5f) + (chunkSpan * 0.5f);
        }
    }
}
