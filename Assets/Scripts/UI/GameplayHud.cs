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
        private string questText = "Speak to NPCs with E for quests.";
        private string questTrackerText = "No active quest";
        private bool questReadyToTurnIn;
        private bool showQuestLog;
        private string toastText = string.Empty;
        private float toastTimer;

        private readonly string[] hotkeySpellIds =
        {
            GameConstants.SpellChronoBlast,
            GameConstants.SpellTemporalBolt,
            GameConstants.SpellManaSurge,
            GameConstants.SpellChronoBlink
        };

        private readonly string[] hotkeyLabels = { "1", "2", "3", "Space" };

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle toastStyle;
        private GUIStyle barBackStyle;
        private GUIStyle barFillStyle;
        private GUIStyle spellNameStyle;
        private GUIStyle spellMetaStyle;
        private GUIStyle cooldownStyle;
        private GUIStyle questLogStyle;
        private GUIStyle frameStyle;
        private static GUIStyle sharedStatNumberStyle;

        private bool showStatNumbers;
        private bool characterDockUnlocked;
        private bool characterDockRectInitialized;
        private Rect characterDockRect = new Rect(16f, 16f, 240f, 92f);

        private const int CharacterDockDragControlId = 81001;

        private void Awake()
        {
            Instance = this;
        }

        public void BindLocalPlayer(NetworkedCombatant player)
        {
            localPlayer = player;
            MinimapUI.Instance?.BindLocalPlayer(player);
        }

        public void SetQuestText(string text) => questText = text;
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

        public void ToggleQuestLog()
        {
            showQuestLog = !showQuestLog;
        }

        private void Update()
        {
            if (toastTimer > 0f)
            {
                toastTimer -= Time.deltaTime;
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                ToggleQuestLog();
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
            DrawPlayerFrame(combatant);
            DrawQuestTrackerPanel();
            DrawCastBar();
            DrawActionBar(combatant);
            DrawExperienceBar(combatant);

            if (showQuestLog)
            {
                DrawQuestLog();
            }

            if (toastTimer > 0f && !string.IsNullOrEmpty(toastText))
            {
                float width = 420f;
                var toastRect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.35f, width, 36f);
                GUI.Box(toastRect, toastText, toastStyle);
            }

            GUI.Label(
                new Rect(Screen.width - 460, Screen.height - 24, 440, 20),
                "Click enemy to target | 1 Chrono-Blast | 2 Bolt | 3 Surge | Space Blink | H recall hub | Enter chat | B bag",
                bodyStyle);

            HudPanelCustomization.DrawContextMenu();
        }

        private void DrawPlayerFrame(CombatantState combatant)
        {
            if (!characterDockRectInitialized)
            {
                characterDockRect = new Rect(16f, 16f, 240f, 92f);
                characterDockRectInitialized = true;
            }

            characterDockRect.height = characterDockUnlocked ? 104f : 92f;

            Rect frame = characterDockRect;
            GUI.Box(frame, string.Empty, frameStyle);
            if (characterDockUnlocked)
            {
                GUI.Label(new Rect(frame.x + 8f, frame.y + 2f, frame.width - 16f, 14f), "Character dock (drag to move)", spellMetaStyle);
            }

            float contentX = frame.x + 8f;
            float titleY = frame.y + (characterDockUnlocked ? 16f : 6f);
            float barWidth = frame.width - 16f;
            float barHeight = 16f;
            float healthBarY = titleY + 24f;
            float manaBarY = healthBarY + 20f;
            float goldY = manaBarY + 20f;

            GUI.Label(new Rect(contentX, titleY, barWidth, 22f), combatant.CharacterClass + "  Lv." + combatant.Level, titleStyle);
            DrawBar(
                new Rect(contentX, healthBarY, barWidth, barHeight),
                combatant.CurrentHealth,
                combatant.MaxHealth,
                new Color(0.85f, 0.2f, 0.25f),
                showStatNumbers ? combatant.CurrentHealth + " / " + combatant.MaxHealth : null);
            DrawBar(
                new Rect(contentX, manaBarY, barWidth, barHeight),
                combatant.CurrentMana,
                combatant.MaxMana,
                new Color(0.25f, 0.55f, 1f),
                showStatNumbers ? combatant.CurrentMana + " / " + combatant.MaxMana : null);
            GUI.Label(new Rect(contentX, goldY, barWidth, 18f), "Gold " + combatant.Gold, bodyStyle);

            HudPanelCustomization.TryOpenContextMenu(frame, GetCharacterDockMenuItems());
            HudPanelCustomization.HandleDrag(ref characterDockRect, characterDockUnlocked, CharacterDockDragControlId);
        }

        private HudPanelCustomization.MenuItem[] GetCharacterDockMenuItems()
        {
            return new[]
            {
                new HudPanelCustomization.MenuItem(
                    showStatNumbers ? "Hide stat numbers" : "Show stat numbers",
                    () => showStatNumbers = !showStatNumbers),
                new HudPanelCustomization.MenuItem(
                    characterDockUnlocked ? "Lock Character dock" : "Unlock Character dock",
                    () => characterDockUnlocked = !characterDockUnlocked)
            };
        }

        private void DrawQuestTrackerPanel()
        {
            float width = 280f;
            var panel = new Rect(Screen.width - width - 16, 96, width, 72);
            GUI.Box(panel, string.Empty, frameStyle);
            Color previous = GUI.color;
            GUI.color = questReadyToTurnIn ? new Color(0.95f, 0.85f, 0.35f) : Color.white;
            GUI.Label(new Rect(panel.x + 10, panel.y + 8, width - 20, 56), questText, bodyStyle);
            GUI.color = previous;
        }

        private void DrawCastBar()
        {
            if (localPlayer == null || !localPlayer.HasCastBar)
            {
                return;
            }

            SpellContentManager.Instance.TryGetSpell(localPlayer.CastBarSpellId, out SpellData spell);
            float barWidth = 320f;
            float x = (Screen.width - barWidth) * 0.5f;
            float y = Screen.height - 154f;
            GUI.Label(new Rect(x, y - 18, barWidth, 18), spell != null ? spell.name : "Casting", spellNameStyle);
            DrawBar(new Rect(x, y, barWidth, 14), Mathf.RoundToInt(localPlayer.GetCastBarProgress() * 100f), 100, new Color(0.95f, 0.75f, 0.2f));
        }

        private void DrawActionBar(CombatantState combatant)
        {
            float slotSize = 52f;
            float spacing = 8f;
            float totalWidth = hotkeySpellIds.Length * slotSize + (hotkeySpellIds.Length - 1) * spacing;
            float startX = (Screen.width - totalWidth) * 0.5f;
            float y = Screen.height - slotSize - 28f;

            for (int i = 0; i < hotkeySpellIds.Length; i++)
            {
                DrawSpellSlot(startX + i * (slotSize + spacing), y, slotSize, hotkeyLabels[i], hotkeySpellIds[i], combatant);
            }
        }

        private void DrawExperienceBar(CombatantState combatant)
        {
            float barWidth = 420f;
            float x = (Screen.width - barWidth) * 0.5f;
            float y = Screen.height - 18f;
            DrawBar(new Rect(x, y, barWidth, 10), combatant.Experience, combatant.ExperienceToNextLevel, new Color(0.55f, 0.35f, 0.95f));
            GUI.Label(new Rect(x, y - 14, barWidth, 14), "XP " + combatant.Experience + " / " + combatant.ExperienceToNextLevel, spellMetaStyle);
        }

        private void DrawQuestLog()
        {
            float width = 320f;
            float height = 180f;
            var panel = new Rect(16, 116, width, height);
            GUI.Box(panel, string.Empty, frameStyle);
            GUI.Label(new Rect(panel.x + 12, panel.y + 8, width - 24, 22), "Quest Log (J)", titleStyle);
            GUI.Label(new Rect(panel.x + 12, panel.y + 34, width - 24, 120), questTrackerText, questLogStyle);
        }

        private void DrawSpellSlot(float x, float y, float size, string hotkey, string spellId, CombatantState combatant)
        {
            SpellContentManager.Instance.TryGetSpell(spellId, out SpellData spell);
            string spellName = spell != null ? spell.name : spellId;
            int manaCost = spell?.casting_rules.resource_cost ?? 0;
            float cooldown = spell?.casting_rules.cooldown_seconds ?? 0f;
            float remaining = localPlayer.GetLocalCooldownRemaining(spellId);
            bool onCooldown = remaining > 0.01f;
            bool canAfford = spell == null || combatant.CurrentMana >= manaCost;

            Color previous = GUI.color;
            if (onCooldown || !canAfford)
            {
                GUI.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            }

            GUI.Box(new Rect(x, y, size, size), string.Empty, frameStyle);
            GUI.Label(new Rect(x + 4, y + 4, 20, 16), hotkey, spellMetaStyle);
            GUI.Label(new Rect(x + 4, y + 18, size - 8, 18), spellName, spellNameStyle);
            GUI.Label(new Rect(x + 4, y + size - 18, size - 8, 16), manaCost + " mp", spellMetaStyle);

            if (onCooldown)
            {
                float fill = cooldown > 0f ? remaining / cooldown : 0f;
                var overlay = new Rect(x + 2, y + 2 + ((size - 4f) * (1f - fill)), size - 4f, (size - 4f) * fill);
                GUI.color = new Color(0f, 0f, 0f, 0.45f);
                GUI.Box(overlay, string.Empty);
                GUI.color = previous;
            }

            GUI.color = previous;
        }

        public static void DrawBar(Rect rect, int current, int max, Color fillColor, string statLabel = null)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            float fillWidth = max > 0 ? rect.width * (current / (float)max) : 0f;
            var fillRect = new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, fillWidth - 4f), rect.height - 4f);
            GUI.color = fillColor;
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            GUI.color = previous;

            if (!string.IsNullOrEmpty(statLabel))
            {
                GUI.Label(rect, statLabel, GetSharedStatNumberStyle());
            }
        }

        private static GUIStyle GetSharedStatNumberStyle()
        {
            if (sharedStatNumberStyle == null)
            {
                sharedStatNumberStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }

            return sharedStatNumberStyle;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            questLogStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            toastStyle = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            barBackStyle = new GUIStyle(GUI.skin.box);
            barFillStyle = new GUIStyle(GUI.skin.box);
            frameStyle = new GUIStyle(GUI.skin.box);
            spellNameStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter, wordWrap = true };
            spellMetaStyle = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.UpperCenter };
            cooldownStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.35f) }
            };
            sharedStatNumberStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }
    }
}
