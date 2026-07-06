using Mirror;
using UnityEngine;
using AetherEcho.Player;
using AetherEcho.Social;

namespace AetherEcho.UI
{
    public class ChatUI : MonoBehaviour
    {
        public static ChatUI Instance { get; private set; }
        public static bool BlocksGameInput => Instance != null && Instance.chatFocused;

        private bool chatFocused;
        private bool chatLogVisible;
        private bool focusInputNextFrame;
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

        private void Update()
        {
            if (chatFocused)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseChatInput();
                    return;
                }

                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    activeChannel = activeChannel == "Global" ? "Say" : "Global";
                    return;
                }

                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    if (!string.IsNullOrWhiteSpace(chatInput))
                    {
                        SendCurrentInput();
                    }

                    CloseChatInput();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OpenChatInput();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) && chatLogVisible)
            {
                chatLogVisible = false;
            }
        }

        public void RefreshFromManager()
        {
            chatLogVisible = true;
        }

        private void OpenChatInput()
        {
            chatFocused = true;
            chatLogVisible = true;
            focusInputNextFrame = true;
        }

        private void CloseChatInput()
        {
            chatFocused = false;
            chatInput = string.Empty;
            GUI.FocusControl(string.Empty);
        }

        private void SendCurrentInput()
        {
            if (string.IsNullOrWhiteSpace(chatInput))
            {
                return;
            }

            string text = chatInput.Trim();
            NetworkedCombatant localPlayer = ResolveLocalPlayer();
            localPlayer?.CmdSendChatMessage(activeChannel, text);
            chatInput = string.Empty;
        }

        private static NetworkedCombatant ResolveLocalPlayer()
        {
            if (NetworkClient.active && NetworkClient.localPlayer != null)
            {
                return NetworkClient.localPlayer.GetComponent<NetworkedCombatant>();
            }

            return World.NpcInteractUtility.FindLocalPlayer();
        }

        private void OnGUI()
        {
            bool hasMessages = ChatManager.Instance != null && ChatManager.Instance.Messages.Count > 0;
            if (!chatFocused && (!chatLogVisible || !hasMessages))
            {
                return;
            }

            EnsureStyles();
            DrawChatPanel();
        }

        private void DrawChatPanel()
        {
            float width = 420f;
            float inputAreaHeight = chatFocused ? 52f : 0f;
            float messageAreaHeight = Mathf.Max(72f, GetMessageHeight());
            float height = messageAreaHeight + inputAreaHeight + 16f;
            var panel = new Rect(24, Screen.height - height - 140, width, height);
            GUI.Box(panel, string.Empty);

            scrollPosition = GUI.BeginScrollView(
                new Rect(panel.x + 8, panel.y + 8, panel.width - 16, messageAreaHeight),
                scrollPosition,
                new Rect(0, 0, panel.width - 32, Mathf.Max(messageAreaHeight, GetMessageHeight())));

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

            if (!chatFocused)
            {
                GUI.Label(
                    new Rect(panel.x + 8, panel.y + panel.height - 18f, width - 16, 18),
                    "Enter chat | Esc hide",
                    channelStyle);
                return;
            }

            float inputY = panel.y + panel.height - 44f;
            GUI.Label(new Rect(panel.x + 8, inputY, 60, 20), activeChannel, channelStyle);
            GUI.Label(new Rect(panel.x + 8, inputY - 18f, width - 16, 18), "Enter send | Tab channel | Esc close", channelStyle);

            GUI.SetNextControlName("ChatInput");
            chatInput = GUI.TextField(
                new Rect(panel.x + 70, inputY + 2f, panel.width - 80, 22),
                chatInput,
                256,
                inputStyle);

            if (focusInputNextFrame)
            {
                GUI.FocusControl("ChatInput");
                focusInputNextFrame = false;
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
