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
            "quest_bone_echoes"
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

        public string GetSuggestedQuestId(uint playerNetId)
        {
            foreach (string questId in StarterQuestIds)
            {
                if (IsQuestCompleted(playerNetId, questId))
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

            return StarterQuestIds[0];
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

            CombatantState combatant = player.CombatantState;
            combatant.ServerGrantExperience(quest.rewards.experience);
            combatant.Gold += quest.rewards.gold;
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
                if (objective.type != "KillEnemy" || objective.target_id != enemyTypeId)
                {
                    continue;
                }

                progress.currentCount++;
                if (progress.currentCount >= objective.required_count)
                {
                    progress.objectivesComplete = true;
                }
            }

            NetworkedCombatant player = killer.GetComponent<NetworkedCombatant>();
            if (player != null)
            {
                PushQuestUiToPlayer(player);
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
                return "[Turn In] " + quest.title + " — return to the Chrono Sage (E)";
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
