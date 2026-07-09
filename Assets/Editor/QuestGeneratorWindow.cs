using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using AetherEcho.Quests;

namespace AetherEcho.Editor
{
    public class QuestGeneratorWindow : EditorWindow
    {
        private string questId = "quest_custom_01";
        private string questTitle = "Custom Quest";
        private string questDescription = "Defeat enemies for the Chrono Sage.";
        private string questGiver = "Chrono Sage";
        private string objectiveType = "KillEnemy";
        private string targetId = "slime";
        private int requiredCount = 3;
        private string objectiveDescription = "Slay corrupted slimes";
        private int rewardXp = 120;
        private int rewardGold = 25;
        private string rewardItem = "chrono_shard";
        private Vector2 scrollPosition;

        [MenuItem("AetherEcho/Quest Generator")]
        public static void ShowWindow()
        {
            GetWindow<QuestGeneratorWindow>("Quest Generator");
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.LabelField("Quest Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generate quest JSON entries for StreamingAssets/quests.json. "
                + "Add the output to the quests array, then restart play mode.",
                MessageType.Info);

            questId = EditorGUILayout.TextField("Quest ID", questId);
            questTitle = EditorGUILayout.TextField("Title", questTitle);
            questDescription = EditorGUILayout.TextField("Description", questDescription);
            questGiver = EditorGUILayout.TextField("Quest Giver", questGiver);
            objectiveType = EditorGUILayout.TextField("Objective Type", objectiveType);
            targetId = EditorGUILayout.TextField("Target ID", targetId);
            requiredCount = EditorGUILayout.IntField("Required Count", requiredCount);
            objectiveDescription = EditorGUILayout.TextField("Objective Text", objectiveDescription);
            rewardXp = EditorGUILayout.IntField("Reward XP", rewardXp);
            rewardGold = EditorGUILayout.IntField("Reward Gold", rewardGold);
            rewardItem = EditorGUILayout.TextField("Reward Item", rewardItem);

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Copy JSON To Clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = BuildQuestJson();
                ShowNotification(new GUIContent("Quest JSON copied."));
            }

            if (GUILayout.Button("Append To quests.json"))
            {
                AppendQuestToFile();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(BuildQuestJson(), GUILayout.MinHeight(220f));
            EditorGUILayout.EndScrollView();
        }

        private string BuildQuestJson()
        {
            return "    {\n"
                   + "      \"id\": \"" + questId + "\",\n"
                   + "      \"title\": \"" + questTitle + "\",\n"
                   + "      \"description\": \"" + questDescription + "\",\n"
                   + "      \"quest_giver_name\": \"" + questGiver + "\",\n"
                   + "      \"objectives\": [\n"
                   + "        {\n"
                   + "          \"type\": \"" + objectiveType + "\",\n"
                   + "          \"target_id\": \"" + targetId + "\",\n"
                   + "          \"required_count\": " + requiredCount + ",\n"
                   + "          \"description\": \"" + objectiveDescription + "\"\n"
                   + "        }\n"
                   + "      ],\n"
                   + "      \"rewards\": {\n"
                   + "        \"experience\": " + rewardXp + ",\n"
                   + "        \"gold\": " + rewardGold + ",\n"
                   + "        \"item_id\": \"" + rewardItem + "\"\n"
                   + "      }\n"
                   + "    }";
        }

        private void AppendQuestToFile()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "quests.json");
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("Quest Generator", "quests.json was not found.", "OK");
                return;
            }

            string json = File.ReadAllText(path);
            if (!json.Contains("\"quests\""))
            {
                EditorUtility.DisplayDialog("Quest Generator", "quests.json format is invalid.", "OK");
                return;
            }

            if (json.Contains("\"id\": \"" + questId + "\""))
            {
                EditorUtility.DisplayDialog("Quest Generator", "A quest with this ID already exists.", "OK");
                return;
            }

            int insertIndex = json.LastIndexOf(']');
            if (insertIndex < 0)
            {
                EditorUtility.DisplayDialog("Quest Generator", "Could not find quests array.", "OK");
                return;
            }

            string prefix = json.Substring(0, insertIndex).TrimEnd();
            bool needsComma = prefix.EndsWith("}");
            string updated = prefix
                             + (needsComma ? "," : string.Empty)
                             + "\n"
                             + BuildQuestJson()
                             + "\n  ]\n}";
            File.WriteAllText(path, updated);
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent("Quest appended."));
        }
    }
}
