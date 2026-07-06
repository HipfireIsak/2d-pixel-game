using UnityEngine;
using AetherEcho.Data;
using AetherEcho.Items;
using AetherEcho.Player;

namespace AetherEcho.UI
{
    public class VendorUI : MonoBehaviour
    {
        public static VendorUI Instance { get; private set; }

        public bool IsOpen { get; private set; }

        private NetworkedCombatant localPlayer;
        private string vendorId;
        private Vector2 scrollPosition;

        private GUIStyle titleStyle;
        private GUIStyle rowStyle;
        private GUIStyle buttonStyle;

        private void Awake()
        {
            Instance = this;
        }

        public void Open(NetworkedCombatant player, string vendorIdValue)
        {
            localPlayer = player;
            vendorId = vendorIdValue;
            IsOpen = true;
        }

        public void Close()
        {
            IsOpen = false;
        }

        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        private void OnGUI()
        {
            if (!IsOpen || localPlayer == null || !ItemContentManager.Instance.TryGetVendor(vendorId, out VendorDefinition vendor))
            {
                return;
            }

            EnsureStyles();
            float width = 360f;
            float height = 320f;
            var panel = new Rect((Screen.width - width) * 0.5f, 120, width, height);
            GUI.Box(panel, string.Empty);
            GUI.Label(new Rect(panel.x + 12, panel.y + 8, width - 24, 22), vendor.display_name, titleStyle);
            GUI.Label(new Rect(panel.x + 12, panel.y + 30, width - 24, 18),
                "Gold: " + localPlayer.CombatantState.Gold,
                rowStyle);

            scrollPosition = GUI.BeginScrollView(
                new Rect(panel.x + 8, panel.y + 52, width - 16, height - 92),
                scrollPosition,
                new Rect(0, 0, width - 32, vendor.stock.Count * 30f));

            float y = 0f;
            foreach (VendorStockEntry stock in vendor.stock)
            {
                ItemContentManager.Instance.TryGetItem(stock.item_id, out ItemDefinition item);
                string label = (item?.name ?? stock.item_id) + " — " + stock.price + " gold";
                GUI.Label(new Rect(0, y, width - 100, 24), label, rowStyle);
                if (GUI.Button(new Rect(width - 90, y, 70, 24), "Buy", buttonStyle))
                {
                    localPlayer.CmdBuyFromVendor(vendorId, stock.item_id);
                }

                y += 30f;
            }

            GUI.EndScrollView();

            if (GUI.Button(new Rect(panel.x + width - 90, panel.y + height - 34, 78, 26), "Close", buttonStyle))
            {
                Close();
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 11 };
        }
    }
}
