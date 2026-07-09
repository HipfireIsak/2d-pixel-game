using System.Collections.Generic;
using UnityEngine;

namespace AetherEcho.Quests
{
    public enum QuestNpcMarker
    {
        None,
        Available,
        TurnIn
    }

    public static class QuestClientState
    {
        private static readonly HashSet<string> CompletedQuestIds = new HashSet<string>();

        public static string ActiveQuestId { get; private set; } = string.Empty;
        public static bool ObjectivesComplete { get; private set; }

        public static void Apply(string activeQuestId, bool objectivesComplete, string completedQuestIdsCsv)
        {
            ActiveQuestId = activeQuestId ?? string.Empty;
            ObjectivesComplete = objectivesComplete;
            CompletedQuestIds.Clear();
            if (string.IsNullOrWhiteSpace(completedQuestIdsCsv))
            {
                return;
            }

            string[] parts = completedQuestIdsCsv.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(parts[i]))
                {
                    CompletedQuestIds.Add(parts[i]);
                }
            }
        }

        public static bool HasActiveQuest => !string.IsNullOrWhiteSpace(ActiveQuestId);

        public static bool TryGetActiveQuestObjectivePosition(out Vector3 position)
        {
            position = Vector3.zero;
            if (!HasActiveQuest)
            {
                return false;
            }

            position = QuestLocationResolver.ResolveObjectivePosition(ActiveQuestId, ObjectivesComplete);
            return position != Vector3.zero;
        }

        public static QuestNpcMarker GetNpcMarker(string questGiverName)
        {
            if (string.IsNullOrWhiteSpace(questGiverName))
            {
                return QuestNpcMarker.None;
            }

            if (HasActiveQuest
                && QuestManager.Instance != null
                && QuestManager.Instance.TryGetQuest(ActiveQuestId, out QuestDefinition activeQuest)
                && activeQuest.quest_giver_name == questGiverName)
            {
                return ObjectivesComplete ? QuestNpcMarker.TurnIn : QuestNpcMarker.None;
            }

            if (HasActiveQuest)
            {
                return QuestNpcMarker.None;
            }

            return GetSuggestedQuestId(questGiverName) != string.Empty
                ? QuestNpcMarker.Available
                : QuestNpcMarker.None;
        }

        private static string GetSuggestedQuestId(string questGiverName)
        {
            if (QuestManager.Instance == null)
            {
                return string.Empty;
            }

            foreach (string questId in QuestManager.StarterQuestIds)
            {
                if (CompletedQuestIds.Contains(questId))
                {
                    continue;
                }

                if (!QuestManager.Instance.TryGetQuest(questId, out QuestDefinition quest)
                    || quest.quest_giver_name != questGiverName)
                {
                    continue;
                }

                return questId;
            }

            return string.Empty;
        }
    }
}
