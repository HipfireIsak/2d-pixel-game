using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Content;
using AetherEcho.Core;
using AetherEcho.Data;
using AetherEcho.Player;

namespace AetherEcho.UI
{
    public class GameplayHud : MonoBehaviour
    {
        public static GameplayHud Instance { get; private set; }

        private NetworkedCombatant localPlayer;
        private string questText = "Speak to the Chrono Sage (E) at the center hub for quests.";
        private string questTrackerText = "No active quest";
        private bool questReadyToTurnIn;
        private bool showQuestLog;
        private string toastText = string.Empty;
        private float toastTimer;

        private readonly string[] hotkeySpellIds =
        {
            GameConstants.SpellChronoBlast,
            GameConstants.SpellTemporalBolt,
            GameConstants.SpellManaSurge
        };

        private readonly string[] hotkeyLabels = { "1", "2", "3" };

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle toastStyle;
        private GUIStyle barBackStyle;
        private GUIStyle barFillStyle;
        private GUIStyle spellNameStyle;
        private GUIStyle spellMetaStyle;
        private GUIStyle cooldownStyle;
        private GUIStyle questLogStyle;

        private void Awake()
        {
            Instance = this;
        }

        public void BindLocalPlayer(NetworkedCombatant player)
        {
            localPlayer = player;
            MinimapUI.Instance?.BindLocalPlayer(player);
        }

        public void SetQuestText(string text)
        {
            questText = text;
        }

        public void SetQuestTracker(string trackerText, bool readyToTurnIn)
        {
            questTrackerText = trackerText;
            questReadyToTurnIn = readyToTurnIn;
        }

        public void SetToast(string text)
        {
            toastText = text;
            toastTimer = 3f;
        }

        private void Update()
        {
            if (toastTimer > 0f)
            {
                toastTimer -= Time.deltaTime;
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                showQuestLog = !showQuestLog;
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (localPlayer == null)
            {
                return;
            }

            CombatantState combatant = localPlayer.CombatantState;
            DrawBar(new Rect(24, 18, 220, 18), combatant.CurrentHealth, combatant.MaxHealth, new Color(0.85f, 0.2f, 0.25f));
            DrawBar(new Rect(24, 42, 220, 18), combatant.CurrentMana, combatant.MaxMana, new Color(0.25f, 0.55f, 1f));
            DrawBar(
                new Rect(24, 66, 220, 14),
                combatant.Experience,
                combatant.ExperienceToNextLevel,
                new Color(0.55f, 0.35f, 0.95f));

            GUI.Label(new Rect(24, 84, 500, 24), combatant.CharacterClass + " Lv." + combatant.Level, bodyStyle);
            GUI.Label(
                new Rect(24, 104, 500, 20),
                "XP " + combatant.Experience + "/" + combatant.ExperienceToNextLevel
                + "  |  Gold " + combatant.Gold
                + "  |  Mana regen " + localPlayer.GetManaRegenPerSecond().ToString("0.0") + "/s",
                bodyStyle);

            Color questColor = questReadyToTurnIn ? new Color(0.95f, 0.85f, 0.35f) : Color.white;
            Color previous = GUI.color;
            GUI.color = questColor;
            GUI.Label(new Rect(24, 126, 700, 48), questText, bodyStyle);
            GUI.color = previous;

            if (showQuestLog)
            {
                DrawQuestLog();
            }

            float spellY = Screen.height - 110;
            GUI.Label(new Rect(24, spellY - 24, 500, 24), "Spells — hotkey opens ground targeting", titleStyle);
            for (int i = 0; i < hotkeySpellIds.Length; i++)
            {
                DrawSpellSlot(24 + (i * 96), spellY, hotkeyLabels[i], hotkeySpellIds[i]);
            }

            if (toastTimer > 0f && !string.IsNullOrEmpty(toastText))
            {
                float width = 420f;
                var toastRect = new Rect((Screen.width - width) * 0.5f, Screen.height - 170f, width, 36f);
                GUI.Box(toastRect, toastText, toastStyle);
            }

            GUI.Label(
                new Rect(Screen.width - 320, Screen.height - 32, 300, 24),
                "Esc: menu | E: Sage | J: quest log | 1-3: spell",
                bodyStyle);
        }

        private void DrawQuestLog()
        {
            float width = 320f;
            float height = 180f;
            var panel = new Rect(24, 178, width, height);
            GUI.Box(panel, string.Empty);
            GUI.Label(new Rect(panel.x + 12, panel.y + 8, width - 24, 22), "Quest Log", titleStyle);
            GUI.Label(new Rect(panel.x + 12, panel.y + 34, width - 24, 120), questTrackerText, questLogStyle);

            if (questReadyToTurnIn)
            {
                GUI.Label(
                    new Rect(panel.x + 12, panel.y + height - 28, width - 24, 20),
                    "Return to the Chrono Sage (E) to turn in.",
                    bodyStyle);
            }
        }

        private void DrawSpellSlot(float x, float y, string hotkey, string spellId)
        {
            SpellContentManager.Instance.TryGetSpell(spellId, out SpellData spell);
            string spellName = spell != null ? spell.name : spellId;
            int manaCost = spell?.casting_rules.resource_cost ?? 0;
            float cooldown = spell?.casting_rules.cooldown_seconds ?? 0f;
            float remaining = localPlayer.GetLocalCooldownRemaining(spellId);
            bool onCooldown = remaining > 0.01f;
            bool canAfford = spell == null || localPlayer.CombatantState.CurrentMana >= manaCost;

            Color previous = GUI.color;
            if (onCooldown || !canAfford)
            {
                GUI.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            }

            GUI.Box(new Rect(x, y, 86, 86), string.Empty);
            GUI.Label(new Rect(x + 6, y + 6, 20, 20), hotkey, spellMetaStyle);
            GUI.Label(new Rect(x + 4, y + 28, 80, 22), spellName, spellNameStyle);
            GUI.Label(new Rect(x + 4, y + 48, 80, 18), manaCost + " mana", spellMetaStyle);

            if (onCooldown)
            {
                float fill = cooldown > 0f ? remaining / cooldown : 0f;
                var overlay = new Rect(x + 2, y + 2 + (84f * (1f - fill)), 82f, 84f * fill);
                GUI.color = new Color(0f, 0f, 0f, 0.45f);
                GUI.Box(overlay, string.Empty);
                GUI.color = previous;
                GUI.Label(new Rect(x + 4, y + 66, 80, 18), remaining.ToString("0.0") + "s", cooldownStyle);
            }
            else
            {
                GUI.Label(new Rect(x + 4, y + 66, 80, 18), cooldown.ToString("0.0") + "s cd", spellMetaStyle);
            }

            GUI.color = previous;
        }

        private void DrawBar(Rect rect, int current, int max, Color fillColor)
        {
            GUI.Box(rect, string.Empty, barBackStyle);
            float fillWidth = max > 0 ? rect.width * (current / (float)max) : 0f;
            var fillRect = new Rect(rect.x + 2, rect.y + 2, Mathf.Max(0f, fillWidth - 4f), rect.height - 4f);
            Color previous = GUI.color;
            GUI.color = fillColor;
            GUI.Box(fillRect, string.Empty, barFillStyle);
            GUI.color = previous;
            GUI.Label(new Rect(rect.x + 8, rect.y + 1, rect.width - 16, rect.height), current + " / " + max, bodyStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            questLogStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            toastStyle = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            barBackStyle = new GUIStyle(GUI.skin.box);
            barFillStyle = new GUIStyle(GUI.skin.box);
            spellNameStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
            spellMetaStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.UpperCenter };
            cooldownStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.35f) }
            };
        }
    }
}
