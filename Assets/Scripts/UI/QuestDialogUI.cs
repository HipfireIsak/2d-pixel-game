using UnityEngine;
using AetherEcho.Player;
using AetherEcho.Quests;

namespace AetherEcho.UI
{
    public class QuestDialogUI : MonoBehaviour
    {
        public static QuestDialogUI Instance { get; private set; }

        private enum DialogMode
        {
            Offer,
            TurnIn
        }

        private QuestDefinition pendingQuest;
        private NetworkedCombatant localPlayer;
        private DialogMode mode;
        private bool isOpen;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle rewardStyle;

        private void Awake()
        {
            Instance = this;
        }

        public void ShowQuestOffer(NetworkedCombatant player, string questId)
        {
            if (QuestManager.Instance == null || !QuestManager.Instance.TryGetQuest(questId, out QuestDefinition quest))
            {
                GameplayHud.Instance?.SetToast("Quest unavailable.");
                return;
            }

            localPlayer = player;
            pendingQuest = quest;
            mode = DialogMode.Offer;
            isOpen = true;
        }

        public void ShowQuestTurnIn(NetworkedCombatant player, string questId)
        {
            if (QuestManager.Instance == null || !QuestManager.Instance.TryGetQuest(questId, out QuestDefinition quest))
            {
                GameplayHud.Instance?.SetToast("Quest unavailable.");
                return;
            }

            localPlayer = player;
            pendingQuest = quest;
            mode = DialogMode.TurnIn;
            isOpen = true;
        }

        public bool IsOpen => isOpen;

        private void Update()
        {
            if (!isOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        private void OnGUI()
        {
            if (!isOpen || pendingQuest == null)
            {
                return;
            }

            EnsureStyles();

            float width = 420f;
            float height = mode == DialogMode.TurnIn ? 280f : 320f;
            var panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(panel, string.Empty);

            float y = panel.y + 16f;
            string header = mode == DialogMode.TurnIn ? "Turn In Quest" : pendingQuest.title;
            GUI.Label(new Rect(panel.x + 16f, y, width - 32f, 28f), header, titleStyle);
            y += 32f;
            GUI.Label(new Rect(panel.x + 16f, y, width - 32f, 22f), "Chrono Sage", bodyStyle);
            y += 26f;

            string body = mode == DialogMode.TurnIn
                ? "Well done, time-weaver. Your objectives are complete. Claim your reward."
                : pendingQuest.description;
            GUI.Label(new Rect(panel.x + 16f, y, width - 32f, 48f), body, bodyStyle);
            y += 52f;

            if (mode == DialogMode.Offer)
            {
                GUI.Label(new Rect(panel.x + 16f, y, width - 32f, 22f), "Objectives", titleStyle);
                y += 24f;
                foreach (QuestObjectiveDefinition objective in pendingQuest.objectives)
                {
                    string line = "- " + (string.IsNullOrEmpty(objective.description)
                        ? objective.type + " " + objective.target_id
                        : objective.description);
                    line += " (" + objective.required_count + ")";
                    GUI.Label(new Rect(panel.x + 24f, y, width - 48f, 22f), line, bodyStyle);
                    y += 22f;
                }

                y += 8f;
            }

            QuestRewardDefinition rewards = pendingQuest.rewards;
            string rewardText = "Rewards: " + rewards.experience + " XP, " + rewards.gold + " gold";
            if (!string.IsNullOrEmpty(rewards.item_id))
            {
                rewardText += ", " + rewards.item_id;
            }

            GUI.Label(new Rect(panel.x + 16f, y, width - 32f, 22f), rewardText, rewardStyle);
            y += 36f;

            if (mode == DialogMode.TurnIn)
            {
                if (GUI.Button(new Rect(panel.x + 16f, y, 180f, 34f), "Turn In Quest"))
                {
                    TurnInQuest();
                }

                if (GUI.Button(new Rect(panel.x + width - 196f, y, 180f, 34f), "Not Yet"))
                {
                    Close();
                }
            }
            else
            {
                if (GUI.Button(new Rect(panel.x + 16f, y, 180f, 34f), "Accept Quest"))
                {
                    AcceptQuest();
                }

                if (GUI.Button(new Rect(panel.x + width - 196f, y, 180f, 34f), "Decline"))
                {
                    Close();
                }
            }
        }

        private void AcceptQuest()
        {
            if (localPlayer != null && pendingQuest != null)
            {
                localPlayer.CmdAcceptQuest(pendingQuest.id);
            }

            Close();
        }

        private void TurnInQuest()
        {
            if (localPlayer != null)
            {
                localPlayer.CmdTurnInQuest();
            }

            Close();
        }

        private void Close()
        {
            isOpen = false;
            pendingQuest = null;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            rewardStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.85f, 0.78f, 0.35f) }
            };
        }
    }
}
