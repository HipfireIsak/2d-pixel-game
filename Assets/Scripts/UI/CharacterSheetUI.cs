using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Data;
using AetherEcho.Items;
using AetherEcho.Player;

namespace AetherEcho.UI
{
    public class CharacterSheetUI : MonoBehaviour
    {
        public static CharacterSheetUI Instance { get; private set; }

        private NetworkedCombatant localPlayer;
        private bool showSheet;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;

        private void Awake()
        {
            Instance = this;
        }

        public void BindLocalPlayer(NetworkedCombatant player)
        {
            localPlayer = player;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                showSheet = !showSheet;
            }
        }

        private void OnGUI()
        {
            if (!showSheet || localPlayer == null)
            {
                return;
            }

            EnsureStyles();
            CombatantState combatant = localPlayer.CombatantState;
            PlayerEquipment equipment = localPlayer.GetComponent<PlayerEquipment>();

            float width = 280f;
            float height = 360f;
            var panel = new Rect((Screen.width - width) * 0.5f, 100, width, height);
            GUI.Box(panel, string.Empty);
            GUI.Label(new Rect(panel.x + 12, panel.y + 8, width - 24, 22), "Character (C)", titleStyle);
            GUI.Label(new Rect(panel.x + 12, panel.y + 34, width - 24, 120),
                localPlayer.DisplayName + "\n"
                + combatant.CharacterClass + "  Level " + combatant.Level + "\n"
                + "STR " + combatant.Strength + "  INT " + combatant.Intelligence + "  AGI " + combatant.Agility + "\n"
                + "HP " + combatant.CurrentHealth + "/" + combatant.MaxHealth + "\n"
                + "Mana " + combatant.CurrentMana + "/" + combatant.MaxMana + "\n"
                + "Gold " + combatant.Gold,
                bodyStyle);

            GUI.Label(new Rect(panel.x + 12, panel.y + 158, width - 24, 20), "Equipment", titleStyle);
            DrawEquipSlot(panel, equipment, "head", "Head", panel.y + 182);
            DrawEquipSlot(panel, equipment, "chest", "Chest", panel.y + 210);
            DrawEquipSlot(panel, equipment, "weapon", "Weapon", panel.y + 238);
            DrawEquipSlot(panel, equipment, "offhand", "Off-Hand", panel.y + 266);
        }

        private void DrawEquipSlot(Rect panel, PlayerEquipment equipment, string slot, string label, float y)
        {
            string itemId = string.Empty;
            if (equipment != null)
            {
                switch (slot)
                {
                    case "head": itemId = equipment.Equipped.head; break;
                    case "chest": itemId = equipment.Equipped.chest; break;
                    case "weapon": itemId = equipment.Equipped.weapon; break;
                    case "offhand": itemId = equipment.Equipped.offhand; break;
                }
            }

            ItemContentManager.Instance.TryGetItem(itemId, out ItemDefinition item);
            string text = label + ": " + (string.IsNullOrWhiteSpace(itemId) ? "(empty)" : item?.name ?? itemId);
            GUI.Label(new Rect(panel.x + 12, y, panel.width - 90, 22), text, bodyStyle);
            if (!string.IsNullOrWhiteSpace(itemId)
                && GUI.Button(new Rect(panel.x + panel.width - 72, y, 60, 22), "Remove", buttonStyle))
            {
                localPlayer.CmdUnequipSlot(slot);
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 10 };
        }
    }
}
