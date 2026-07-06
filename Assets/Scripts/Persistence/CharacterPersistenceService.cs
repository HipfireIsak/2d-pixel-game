using System;
using System.Collections.Generic;
using System.IO;
using AetherEcho.Data;
using AetherEcho.Player;
using AetherEcho.Quests;
using UnityEngine;

namespace AetherEcho.Persistence
{
    public class CharacterPersistenceService : MonoBehaviour
    {
        public static CharacterPersistenceService Instance { get; private set; }

        private string saveRootPath;

        private void Awake()
        {
            Instance = this;
            saveRootPath = Path.Combine(Application.persistentDataPath, "AetherEcho", "characters");
            Directory.CreateDirectory(saveRootPath);
        }

        public static string SanitizeCharacterId(string rawId)
        {
            if (string.IsNullOrWhiteSpace(rawId))
            {
                return "adventurer";
            }

            char[] buffer = rawId.ToLowerInvariant().ToCharArray();
            for (int i = 0; i < buffer.Length; i++)
            {
                char c = buffer[i];
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    buffer[i] = '_';
                }
            }

            return new string(buffer, 0, Mathf.Min(32, buffer.Length));
        }

        public bool TryLoad(string characterId, out CharacterSaveData saveData)
        {
            saveData = null;
            string path = GetSavePath(characterId);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                saveData = JsonUtility.FromJson<CharacterSaveData>(json);
                return saveData != null;
            }
            catch (Exception exception)
            {
                Debug.LogError("[CharacterPersistenceService] Failed to load " + path + ": " + exception.Message);
                return false;
            }
        }

        public void Save(NetworkedCombatant player)
        {
            if (player == null || !player.isServer)
            {
                return;
            }

            CharacterSaveData saveData = BuildSaveData(player);
            string path = GetSavePath(saveData.characterId);
            try
            {
                Directory.CreateDirectory(saveRootPath);
                File.WriteAllText(path, JsonUtility.ToJson(saveData, true));
                Debug.Log("[CharacterPersistenceService] Saved character: " + saveData.characterId);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CharacterPersistenceService] Failed to save " + path + ": " + exception.Message);
            }
        }

        public CharacterSaveData BuildSaveData(NetworkedCombatant player)
        {
            Combat.CombatantState combatant = player.CombatantState;
            Vector3 position = player.transform.position;
            var saveData = new CharacterSaveData
            {
                characterId = SanitizeCharacterId(player.DisplayName),
                displayName = player.DisplayName,
                characterClass = combatant.CharacterClass,
                level = combatant.Level,
                experience = combatant.Experience,
                gold = combatant.Gold,
                currentHealth = combatant.CurrentHealth,
                currentMana = combatant.CurrentMana,
                posX = position.x,
                posY = position.y,
                posZ = position.z,
                savedAtUtcTicks = DateTime.UtcNow.Ticks
            };

            Items.PlayerInventory inventory = player.GetComponent<Items.PlayerInventory>();
            if (inventory != null)
            {
                saveData.inventory = inventory.GetSnapshot();
            }

            Items.PlayerEquipment equipment = player.GetComponent<Items.PlayerEquipment>();
            if (equipment != null)
            {
                saveData.equipment = equipment.GetSnapshot();
            }

            if (QuestManager.Instance != null)
            {
                saveData.quests = QuestManager.Instance.BuildSaveData(player.netId);
            }

            return saveData;
        }

        public void ApplySaveData(NetworkedCombatant player, CharacterSaveData saveData)
        {
            if (player == null || saveData == null || !player.isServer)
            {
                return;
            }

            Combat.CombatantState combatant = player.CombatantState;
            player.ServerApplyLoadedCharacter(
                saveData.displayName,
                saveData.characterClass,
                saveData.level,
                saveData.experience,
                saveData.gold,
                saveData.currentHealth,
                saveData.currentMana);

            player.transform.position = new Vector3(saveData.posX, saveData.posY, saveData.posZ);

            Items.PlayerInventory inventory = player.GetComponent<Items.PlayerInventory>();
            inventory?.ServerLoadSnapshot(saveData.inventory);

            Items.PlayerEquipment equipment = player.GetComponent<Items.PlayerEquipment>();
            equipment?.ServerLoadSnapshot(saveData.equipment);

            QuestManager.Instance?.ServerLoadSaveData(player.netId, saveData.quests);
            QuestManager.Instance?.PushQuestUiToPlayer(player);
        }

        private string GetSavePath(string characterId)
        {
            return Path.Combine(saveRootPath, SanitizeCharacterId(characterId) + ".json");
        }
    }
}
