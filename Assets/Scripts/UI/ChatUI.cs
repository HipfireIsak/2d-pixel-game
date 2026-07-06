using UnityEngine;
using AetherEcho.Social;

namespace AetherEcho.UI
{
    public class ChatUI : MonoBehaviour
    {
        public static ChatUI Instance { get; private set; }
        public static bool BlocksGameInput => Instance != null && Instance.chatFocused;

        private bool chatFocused;
        private string chatInput = string.Empty;
        private string activeChannel = "Global";
        private Vector2 scrollPosition;

        private GUIStyle inputStyle;
        private GUIStyle messageStyle;
        private GUIStyle channelStyle;

        private void Awake()
        {
            Instance = this;
        }

        public void RefreshFromManager()
        {
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!chatFocused)
                {
                    chatFocused = true;
                }
                else if (!string.IsNullOrWhiteSpace(chatInput))
                {
                    SendCurrentInput();
                    chatFocused = false;
                }
                else
                {
                    chatFocused = false;
                }
            }

            if (chatFocused && Input.GetKeyDown(KeyCode.Tab))
            {
                activeChannel = activeChannel == "Global" ? "Say" : "Global";
            }

            if (chatFocused && Input.GetKeyDown(KeyCode.Escape))
            {
                chatFocused = false;
                chatInput = string.Empty;
            }
        }

        private void SendCurrentInput()
        {
            if (string.IsNullOrWhiteSpace(chatInput))
            {
                return;
            }

            Player.NetworkedCombatant localPlayer = World.NpcInteractUtility.FindLocalPlayer();
            localPlayer?.CmdSendChatMessage(activeChannel, chatInput);
            chatInput = string.Empty;
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawChatPanel();
        }

        private void DrawChatPanel()
        {
            float width = 420f;
            float height = 160f;
            var panel = new Rect(24, Screen.height - height - 140, width, height);
            GUI.Box(panel, string.Empty);

            scrollPosition = GUI.BeginScrollView(
                new Rect(panel.x + 8, panel.y + 8, panel.width - 16, panel.height - 40),
                scrollPosition,
                new Rect(0, 0, panel.width - 32, Mathf.Max(panel.height - 40, GetMessageHeight())));

            float y = 0f;
            if (ChatManager.Instance != null)
            {
                foreach (ChatMessage message in ChatManager.Instance.Messages)
                {
                    string line = "[" + message.channel + "] " + message.senderName + ": " + message.text;
                    GUI.Label(new Rect(0, y, panel.width - 40, 18), line, messageStyle);
                    y += 18f;
                }
            }

            GUI.EndScrollView();

            GUI.Label(new Rect(panel.x + 8, panel.y + panel.height - 28, 60, 20), activeChannel, channelStyle);

            if (chatFocused)
            {
                GUI.FocusControl("ChatInput");
            }

            GUI.SetNextControlName("ChatInput");
            chatInput = GUI.TextField(
                new Rect(panel.x + 70, panel.y + panel.height - 30, panel.width - 80, 22),
                chatInput,
                256,
                inputStyle);

            if (chatFocused)
            {
                GUI.Label(new Rect(panel.x + 8, panel.y + panel.height - 48, width - 16, 18), "Enter send | Tab channel | Esc close", channelStyle);
            }
        }

        private float GetMessageHeight()
        {
            return ChatManager.Instance != null ? ChatManager.Instance.Messages.Count * 18f : 0f;
        }

        private void EnsureStyles()
        {
            if (inputStyle != null)
            {
                return;
            }

            inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 12 };
            messageStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            channelStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold };
        }
    }
}
