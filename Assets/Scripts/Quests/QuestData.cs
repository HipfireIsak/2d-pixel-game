using System;
using System.Collections.Generic;

namespace AetherEcho.Quests
{
    [Serializable]
    public class QuestRegistry
    {
        public List<QuestDefinition> quests = new List<QuestDefinition>();
    }

    [Serializable]
    public class QuestDefinition
    {
        public string id;
        public string title;
        public string description;
        public string quest_giver_name;
        public List<QuestObjectiveDefinition> objectives = new List<QuestObjectiveDefinition>();
        public QuestRewardDefinition rewards = new QuestRewardDefinition();
    }

    [Serializable]
    public class QuestObjectiveDefinition
    {
        public string type;
        public string target_id;
        public int required_count;
        public string description;
    }

    [Serializable]
    public class QuestRewardDefinition
    {
        public int experience;
        public int gold;
        public string item_id;
    }
}
