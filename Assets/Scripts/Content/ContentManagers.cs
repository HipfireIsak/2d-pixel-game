using System.Collections.Generic;
using System.IO;
using AetherEcho.Core;
using AetherEcho.Data;
using UnityEngine;

namespace AetherEcho.Content
{
    public static class JsonContentLoader
    {
        public static string ReadStreamingAssetText(string fileName)
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!File.Exists(path))
            {
                Debug.LogError("[JsonContentLoader] Missing data file: " + path);
                return string.Empty;
            }

            return File.ReadAllText(path);
        }
    }

    public class SpellContentManager : MonoBehaviour
    {
        public static SpellContentManager Instance { get; private set; }

        private readonly Dictionary<string, SpellData> spellDatabase = new Dictionary<string, SpellData>();

        public IReadOnlyDictionary<string, SpellData> Spells => spellDatabase;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSpellsFromJson(JsonContentLoader.ReadStreamingAssetText(GameConstants.DataSpellsFileName));
        }

        public void LoadSpellsFromJson(string jsonText)
        {
            spellDatabase.Clear();
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return;
            }

            SpellRegistry registry = JsonUtility.FromJson<SpellRegistry>(jsonText);
            if (registry?.spells == null)
            {
                Debug.LogError("[SpellContentManager] Failed to parse spell registry.");
                return;
            }

            foreach (SpellData spell in registry.spells)
            {
                if (spell == null || string.IsNullOrWhiteSpace(spell.id))
                {
                    continue;
                }

                spellDatabase[spell.id] = spell;
                Debug.Log("[SpellContentManager] Registered spell: " + spell.name + " [" + spell.id + "]");
            }
        }

        public bool TryGetSpell(string spellId, out SpellData spellData)
        {
            return spellDatabase.TryGetValue(spellId, out spellData);
        }
    }

    public class ClassContentManager : MonoBehaviour
    {
        public static ClassContentManager Instance { get; private set; }

        private readonly Dictionary<string, ClassData> classDatabase = new Dictionary<string, ClassData>();

        public IReadOnlyDictionary<string, ClassData> Classes => classDatabase;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadClassesFromJson(JsonContentLoader.ReadStreamingAssetText(GameConstants.DataClassesFileName));
        }

        public void LoadClassesFromJson(string jsonText)
        {
            classDatabase.Clear();
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return;
            }

            ClassRegistry registry = JsonUtility.FromJson<ClassRegistry>(jsonText);
            if (registry?.classes == null)
            {
                Debug.LogError("[ClassContentManager] Failed to parse class registry.");
                return;
            }

            foreach (ClassData classData in registry.classes)
            {
                if (classData == null || string.IsNullOrWhiteSpace(classData.class_name))
                {
                    continue;
                }

                classDatabase[classData.class_name] = classData;
                Debug.Log("[ClassContentManager] Registered class: " + classData.class_name);
            }
        }

        public bool TryGetClass(string className, out ClassData classData)
        {
            return classDatabase.TryGetValue(className, out classData);
        }

        public StatBlock ResolveStatsForLevel(ClassData classData, int level)
        {
            StatBlock resolved = new StatBlock
            {
                health = classData.base_stats.health,
                mana = classData.base_stats.mana,
                strength = classData.base_stats.strength,
                intelligence = classData.base_stats.intelligence,
                agility = classData.base_stats.agility
            };

            int levelsAboveOne = Mathf.Max(0, level - 1);
            resolved.health += Mathf.RoundToInt(classData.stat_growth_per_level.health * levelsAboveOne);
            resolved.mana += Mathf.RoundToInt(classData.stat_growth_per_level.mana * levelsAboveOne);
            resolved.strength += Mathf.RoundToInt(classData.stat_growth_per_level.strength * levelsAboveOne);
            resolved.intelligence += Mathf.RoundToInt(classData.stat_growth_per_level.intelligence * levelsAboveOne);
            resolved.agility += Mathf.RoundToInt(classData.stat_growth_per_level.agility * levelsAboveOne);
            return resolved;
        }
    }
}
