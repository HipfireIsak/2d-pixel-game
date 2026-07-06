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
        public bool objectivesComplete;
    }

    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        public static readonly string[] StarterQuestIds =
        {
            "quest_fractured_slimes",
            "quest_bone_echoes",
            "quest_merchant_supplies",
            "quest_eye_of_time",
            "quest_vault_watcher"
        };

        private readonly Dictionary<string, QuestDefinition> questDatabase = new Dictionary<string, QuestDefinition>();
        private readonly Dictionary<uint, QuestProgress> activeQuestByPlayerNetId = new Dictionary<uint, QuestProgress>();
        private readonly Dictionary<uint, HashSet<string>> completedQuestsByPlayer = new Dictionary<uint, HashSet<string>>();

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
                }
            }
        }

        public bool TryGetQuest(string questId, out QuestDefinition quest)
        {
            return questDatabase.TryGetValue(questId, out quest);
        }

        public bool IsQuestCompleted(uint playerNetId, string questId)
        {
            return completedQuestsByPlayer.TryGetValue(playerNetId, out HashSet<string> completed)
                   && completed.Contains(questId);
        }

        public bool TryGetActiveProgress(uint playerNetId, out QuestProgress progress)
        {
            return activeQuestByPlayerNetId.TryGetValue(playerNetId, out progress);
        }

        public string GetSuggestedQuestId(uint playerNetId, string questGiverName)
        {
            foreach (string questId in StarterQuestIds)
            {
                if (IsQuestCompleted(playerNetId, questId))
                {
                    continue;
                }

                if (!TryGetQuest(questId, out QuestDefinition quest) || quest.quest_giver_name != questGiverName)
                {
                    continue;
                }

                if (activeQuestByPlayerNetId.TryGetValue(playerNetId, out QuestProgress active)
                    && active.questId == questId)
                {
                    continue;
                }

                return questId;
            }

            return string.Empty;
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
            if (IsQuestCompleted(netId, questId))
            {
                message = "You already completed this quest.";
                return false;
            }

            if (activeQuestByPlayerNetId.TryGetValue(netId, out QuestProgress existing))
            {
                if (!existing.objectivesComplete)
                {
                    message = "Finish or turn in your current quest first.";
                    return false;
                }

                message = "Return to the Chrono Sage to turn in your completed quest.";
                return false;
            }

            activeQuestByPlayerNetId[netId] = new QuestProgress
            {
                questId = questId,
                currentCount = 0,
                objectivesComplete = false
            };

            PushQuestUiToPlayer(player);
            message = "Quest accepted: " + quest.title;
            return true;
        }

        public bool ServerTryTurnInQuest(NetworkedCombatant player, out string message)
        {
            message = string.Empty;
            if (!NetworkServer.active || player == null)
            {
                message = "Cannot turn in quest.";
                return false;
            }

            uint netId = player.netId;
            if (!activeQuestByPlayerNetId.TryGetValue(netId, out QuestProgress progress) || !progress.objectivesComplete)
            {
                message = "You do not have a completed quest to turn in.";
                return false;
            }

            if (!questDatabase.TryGetValue(progress.questId, out QuestDefinition quest))
            {
                message = "Quest data missing.";
                return false;
            }

            if (quest.objectives.Exists(objective => objective.type == "CollectItem"))
            {
                ServerRefreshCollectObjectives(netId);
                if (!progress.objectivesComplete)
                {
                    message = "Quest objectives not complete.";
                    return false;
                }
            }

            CombatantState combatant = player.CombatantState;
            combatant.ServerGrantExperience(quest.rewards.experience);
            combatant.Gold += quest.rewards.gold;

            Items.PlayerInventory inventory = player.GetComponent<Items.PlayerInventory>();
            if (inventory != null && !string.IsNullOrWhiteSpace(quest.rewards.item_id))
            {
                inventory.ServerAddItem(quest.rewards.item_id, 1, out _);
            }

            Persistence.CharacterPersistenceService.Instance?.Save(player);
            activeQuestByPlayerNetId.Remove(netId);

            if (!completedQuestsByPlayer.TryGetValue(netId, out HashSet<string> completed))
            {
                completed = new HashSet<string>();
                completedQuestsByPlayer[netId] = completed;
            }

            completed.Add(progress.questId);
            PushQuestUiToPlayer(player);
            message = "Quest turned in: " + quest.title + " (+" + quest.rewards.experience + " XP, +" + quest.rewards.gold + " gold)";
            return true;
        }

        public void ServerRegisterEnemyKill(CombatantState killer, string enemyTypeId)
        {
            if (!NetworkServer.active || killer == null || killer.netIdentity == null)
            {
                return;
            }

            uint netId = killer.netIdentity.netId;
            if (!activeQuestByPlayerNetId.TryGetValue(netId, out QuestProgress progress) || progress.objectivesComplete)
            {
                return;
            }

            if (!questDatabase.TryGetValue(progress.questId, out QuestDefinition quest))
            {
                return;
            }

            foreach (QuestObjectiveDefinition objective in quest.objectives)
            {
                if (objective.type == "KillEnemy" && objective.target_id == enemyTypeId)
                {
                    progress.currentCount++;
                    if (progress.currentCount >= objective.required_count)
                    {
                        progress.objectivesComplete = true;
                    }
                }
            }

            CreditPartyKillProgress(killer, enemyTypeId);
        }

        [Server]
        public void ServerRefreshCollectObjectives(uint playerNetId)
        {
            if (!activeQuestByPlayerNetId.TryGetValue(playerNetId, out QuestProgress progress)
                || progress.objectivesComplete)
            {
                return;
            }

            if (!questDatabase.TryGetValue(progress.questId, out QuestDefinition quest))
            {
                return;
            }

            if (!NetworkServer.spawned.TryGetValue(playerNetId, out NetworkIdentity identity))
            {
                return;
            }

            Items.PlayerInventory inventory = identity.GetComponent<Items.PlayerInventory>();
            if (inventory == null)
            {
                return;
            }

            foreach (QuestObjectiveDefinition objective in quest.objectives)
            {
                if (objective.type != "CollectItem")
                {
                    continue;
                }

                progress.currentCount = inventory.ServerCountItem(objective.target_id);
                if (progress.currentCount >= objective.required_count)
                {
                    progress.objectivesComplete = true;
                }
            }
        }

        [Server]
        private void CreditPartyKillProgress(CombatantState killer, string enemyTypeId)
        {
            NetworkedCombatant killerPlayer = killer.GetComponent<NetworkedCombatant>();
            if (killerPlayer == null || Social.PartyManager.Instance == null)
            {
                UpdateQuestUiForNetId(killer.netIdentity.netId);
                return;
            }

            foreach (uint memberNetId in Social.PartyManager.Instance.ServerGetPartyMemberNetIds(killerPlayer.netId))
            {
                if (memberNetId == killerPlayer.netId)
                {
                    UpdateQuestUiForNetId(memberNetId);
                    continue;
                }

                if (!NetworkServer.spawned.TryGetValue(memberNetId, out NetworkIdentity memberIdentity))
                {
                    continue;
                }

                if (Vector3.Distance(killer.transform.position, memberIdentity.transform.position) > 40f)
                {
                    continue;
                }

                ServerRegisterEnemyKillForPlayer(memberNetId, enemyTypeId);
            }
        }

        [Server]
        private void ServerRegisterEnemyKillForPlayer(uint netId, string enemyTypeId)
        {
            if (!activeQuestByPlayerNetId.TryGetValue(netId, out QuestProgress progress) || progress.objectivesComplete)
            {
                return;
            }

            if (!questDatabase.TryGetValue(progress.questId, out QuestDefinition quest))
            {
                return;
            }

            foreach (QuestObjectiveDefinition objective in quest.objectives)
            {
                if (objective.type == "KillEnemy" && objective.target_id == enemyTypeId)
                {
                    progress.currentCount++;
                    if (progress.currentCount >= objective.required_count)
                    {
                        progress.objectivesComplete = true;
                    }
                }
            }

            UpdateQuestUiForNetId(netId);
        }

        [Server]
        private void UpdateQuestUiForNetId(uint netId)
        {
            if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity))
            {
                NetworkedCombatant player = identity.GetComponent<NetworkedCombatant>();
                if (player != null)
                {
                    PushQuestUiToPlayer(player);
                }
            }
        }

        public Persistence.QuestSaveData BuildSaveData(uint playerNetId)
        {
            var saveData = new Persistence.QuestSaveData();
            if (completedQuestsByPlayer.TryGetValue(playerNetId, out HashSet<string> completed))
            {
                saveData.completedQuestIds.AddRange(completed);
            }

            if (activeQuestByPlayerNetId.TryGetValue(playerNetId, out QuestProgress progress))
            {
                saveData.activeQuestId = progress.questId;
                saveData.activeQuestCount = progress.currentCount;
                saveData.activeQuestComplete = progress.objectivesComplete;
            }

            return saveData;
        }

        [Server]
        public void ServerLoadSaveData(uint playerNetId, Persistence.QuestSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            completedQuestsByPlayer[playerNetId] = new HashSet<string>(saveData.completedQuestIds ?? new List<string>());
            activeQuestByPlayerNetId.Remove(playerNetId);

            if (!string.IsNullOrWhiteSpace(saveData.activeQuestId))
            {
                activeQuestByPlayerNetId[playerNetId] = new QuestProgress
                {
                    questId = saveData.activeQuestId,
                    currentCount = saveData.activeQuestCount,
                    objectivesComplete = saveData.activeQuestComplete
                };
            }
        }

        public void PushQuestUiToPlayer(NetworkedCombatant player)
        {
            uint netId = player.netId;
            activeQuestByPlayerNetId.TryGetValue(netId, out QuestProgress progress);
            player.RpcUpdateQuestHud(BuildHudText(netId));
            player.RpcUpdateQuestTracker(BuildTrackerText(netId), progress?.objectivesComplete ?? false);
        }

        private string BuildHudText(uint playerNetId)
        {
            if (!activeQuestByPlayerNetId.TryGetValue(playerNetId, out QuestProgress progress))
            {
                return "Speak to the Chrono Sage (E) at the center hub for quests.";
            }

            if (!questDatabase.TryGetValue(progress.questId, out QuestDefinition quest))
            {
                return string.Empty;
            }

            QuestObjectiveDefinition objective = quest.objectives.Count > 0 ? quest.objectives[0] : null;
            if (progress.objectivesComplete)
            {
                return "[Turn In] " + quest.title + " — return to " + quest.quest_giver_name + " (E)";
            }

            return "[Active] " + quest.title + "  "
                   + progress.currentCount + "/" + (objective?.required_count ?? 1)
                   + "  " + (objective?.description ?? quest.description);
        }

        private string BuildTrackerText(uint playerNetId)
        {
            if (!activeQuestByPlayerNetId.TryGetValue(playerNetId, out QuestProgress progress))
            {
                return "No active quest";
            }

            if (!questDatabase.TryGetValue(progress.questId, out QuestDefinition quest))
            {
                return string.Empty;
            }

            QuestObjectiveDefinition objective = quest.objectives.Count > 0 ? quest.objectives[0] : null;
            string status = progress.objectivesComplete ? "Complete — turn in at Sage" : "In progress";
            return quest.title + "\n" + status + "\n"
                   + (objective?.description ?? quest.description) + " "
                   + progress.currentCount + "/" + (objective?.required_count ?? 1);
        }
    }
}
