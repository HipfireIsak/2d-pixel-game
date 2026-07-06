using UnityEngine;
using AetherEcho.Data;
using AetherEcho.Items;
using AetherEcho.Player;

namespace AetherEcho.UI
{
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.showInventory;

        private NetworkedCombatant localPlayer;
        private bool showInventory;
        private Vector2 scrollPosition;
        private Rect panelRect = new Rect(0, 0, 340f, 320f);
        private bool dragging;
        private Vector2 dragOffset;
        private bool panelPositionInitialized;

        private GUIStyle titleStyle;
        private GUIStyle rowStyle;
        private GUIStyle buttonStyle;
        private GUIStyle windowStyle;

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
            if (Input.GetKeyDown(KeyCode.I) && !ChatUI.BlocksGameInput)
            {
                showInventory = !showInventory;
                if (showInventory && !panelPositionInitialized)
                {
                    panelRect.x = Screen.width - panelRect.width - 24f;
                    panelRect.y = 120f;
                    panelPositionInitialized = true;
                }
            }
        }

        private void OnGUI()
        {
            if (!showInventory || localPlayer == null)
            {
                dragging = false;
                return;
            }

            EnsureStyles();
            PlayerInventory inventory = localPlayer.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                return;
            }

            panelRect = GUI.Window(91521, panelRect, DrawInventoryWindow, "Bags (drag title to move)", windowStyle);
        }

        private void DrawInventoryWindow(int windowId)
        {
            PlayerInventory inventory = localPlayer.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                return;
            }

            GUI.DragWindow(new Rect(0, 0, panelRect.width, 22));
            GUI.Label(new Rect(12, 28, panelRect.width - 24, 18), "Gold: " + localPlayer.CombatantState.Gold, rowStyle);

            scrollPosition = GUI.BeginScrollView(
                new Rect(8, 50, panelRect.width - 16, panelRect.height - 58),
                scrollPosition,
                new Rect(0, 0, panelRect.width - 32, inventory.Slots.Count * 28f));

            float y = 0f;
            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                InventoryEntry entry = inventory.Slots[i];
                ItemContentManager.Instance.TryGetItem(entry.itemId, out ItemDefinition item);
                string label = (item?.name ?? entry.itemId) + " x" + entry.quantity;
                GUI.Label(new Rect(0, y, panelRect.width - 120, 24), label, rowStyle);

                if (item != null && !string.IsNullOrWhiteSpace(item.equip_slot)
                    && GUI.Button(new Rect(panelRect.width - 110, y, 44, 22), "Equip", buttonStyle))
                {
                    localPlayer.CmdEquipItem(entry.itemId);
                }

                if (item != null && item.item_type == "Consumable"
                    && GUI.Button(new Rect(panelRect.width - 62, y, 44, 22), "Use", buttonStyle))
                {
                    localPlayer.CmdUseItem(entry.itemId);
                }

                y += 28f;
            }

            GUI.EndScrollView();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 10 };
            windowStyle = new GUIStyle(GUI.skin.window) { fontSize = 13, fontStyle = FontStyle.Bold };
        }
    }
}
