using System;
using System.Collections.Generic;
using AetherEcho.Data;

namespace AetherEcho.Persistence
{
    [Serializable]
    public class QuestSaveData
    {
        public string activeQuestId;
        public int activeQuestCount;
        public bool activeQuestComplete;
        public List<string> completedQuestIds = new List<string>();
    }

    [Serializable]
    public class CharacterSaveData
    {
        public string characterId;
        public string displayName;
        public string characterClass = "Mage";
        public int level = 5;
        public int experience;
        public int gold;
        public int currentHealth = 140;
        public int currentMana = 160;
        public float posX;
        public float posY;
        public float posZ;
        public InventorySnapshot inventory = new InventorySnapshot();
        public EquipmentSnapshot equipment = new EquipmentSnapshot();
        public QuestSaveData quests = new QuestSaveData();
        public long savedAtUtcTicks;
    }
}
