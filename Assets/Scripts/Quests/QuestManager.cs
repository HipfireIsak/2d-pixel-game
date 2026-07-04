using System.Collections.Generic;
using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Content;
using AetherEcho.Core;
using AetherEcho.Player;
using AetherEcho.UI;

namespace AetherEcho.Quests
{
    public class QuestProgress
    {
        public string questId;
        public int currentCount;
        public bool isCompleted;
    }

    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        private readonly Dictionary<string, QuestDefinition> questDatabase = new Dictionary<string, QuestDefinition>();
        private readonly Dictionary<uint, QuestProgress> activeQuestByPlayerNetId = new Dictionary<uint, QuestProgress>();

        private void Awake()
        {
            Instance = this;
            LoadQuestsFromJson(JsonContentLoader.ReadStreamingAssetText(GameConstants.DataQuestsFileName));
        }

        public void LoadQuestsFromJson(string jsonText)
        {
            questDatabase.Clear();
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return;
            }

            QuestRegistry registry = JsonUtility.FromJson<QuestRegistry>(jsonText);
            if (registry?.quests == null)
            {
                return;
            }

            foreach (QuestDefinition quest in registry.quests)
            {
                if (quest != null && !string.IsNullOrWhiteSpace(quest.id))
                {
                    questDatabase[quest.id] = quest;
                    Debug.Log("[QuestManager] Registered quest: " + quest.title);
                }
            }
        }

        public bool TryGetQuest(string questId, out QuestDefinition quest)
        {
            return questDatabase.TryGetValue(questId, out quest);
        }

        public bool ServerTryAcceptQuest(NetworkedCombatant player, string questId, out string message)
        {
            message = string.Empty;
            if (!NetworkServer.active || player == null || !questDatabase.TryGetValue(questId, out QuestDefinition quest))
            {
                message = "Quest unavailable.";
                return false;
            }

            uint netId = player.netId;
            if (activeQuestByPlayerNetId.TryGetValue(netId, out QuestProgress existing) && !existing.isCompleted)
            {
                message = "You already have an active quest.";
                return false;
            }

            activeQuestByPlayerNetId[netId] = new QuestProgress
            {
                questId = questId,
                currentCount = 0,
                isCompleted = false
            };

            PushHudToPlayer(player, BuildHudText(player.netId));
            message = "Quest accepted: " + quest.title;
            return true;
        }

        public void ServerRegisterEnemyKill(CombatantState killer, string enemyTypeId)
        {
            if (!NetworkServer.active || killer == null || killer.netIdentity == null)
            {
                return;
            }

            uint netId = killer.netIdentity.netId;
            if (!activeQuestByPlayerNetId.TryGetValue(netId, out QuestProgress progress) || progress.isCompleted)
            {
                return;
            }

            if (!questDatabase.TryGetValue(progress.questId, out QuestDefinition quest))
            {
                return;
            }

            foreach (QuestObjectiveDefinition objective in quest.objectives)
            {
                if (objective.type != "KillEnemy" || objective.target_id != enemyTypeId)
                {
                    continue;
                }

                progress.currentCount++;
                if (progress.currentCount >= objective.required_count)
                {
                    progress.isCompleted = true;
                    killer.CurrentHealth = Mathf.Min(killer.MaxHealth, killer.CurrentHealth + 25);
                    killer.CurrentMana = Mathf.Min(killer.MaxMana, killer.CurrentMana + 20);
                }
            }

            NetworkedCombatant player = killer.GetComponent<NetworkedCombatant>();
            if (player != null)
            {
                PushHudToPlayer(player, BuildHudText(player.netId));
            }
        }

        private string BuildHudText(uint playerNetId)
        {
            if (!activeQuestByPlayerNetId.TryGetValue(playerNetId, out QuestProgress progress))
            {
                return "Speak to the Chrono Sage (E) to begin your trial.";
            }

            if (!questDatabase.TryGetValue(progress.questId, out QuestDefinition quest))
            {
                return string.Empty;
            }

            QuestObjectiveDefinition objective = quest.objectives.Count > 0 ? quest.objectives[0] : null;
            if (progress.isCompleted)
            {
                return "Quest complete: " + quest.title;
            }

            return quest.title + "  "
                   + progress.currentCount + "/" + (objective?.required_count ?? 1)
                   + "  " + (objective?.description ?? quest.description);
        }

        private static void PushHudToPlayer(NetworkedCombatant player, string hudText)
        {
            player.RpcUpdateQuestHud(hudText);
        }
    }
}
