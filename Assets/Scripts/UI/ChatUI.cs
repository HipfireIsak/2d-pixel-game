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

        private bool chatUnlocked;
        private Rect chatPanelRect;
        private bool chatPanelRectInitialized;
        private int chatOpenedFrame = -1;
        private int chatSubmittedFrame = -1;
        private bool pendingChatSubmit;

        private const int ChatDragControlId = 81003;

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
                    ChatDebug.Log("Channel switched to " + activeChannel);
                    return;
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ChatDebug.Log("Enter pressed while chat closed -> opening input");
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
            chatOpenedFrame = Time.frameCount;
            ChatDebug.Log("OpenChatInput (channel=" + activeChannel + ")");
        }

        private void CloseChatInput()
        {
            chatFocused = false;
            chatInput = string.Empty;
            GUI.FocusControl(string.Empty);
            ChatDebug.Log("CloseChatInput");
        }

        private void SendCurrentInput()
        {
            if (string.IsNullOrWhiteSpace(chatInput))
            {
                ChatDebug.LogWarning("SendCurrentInput skipped: chatInput is empty or whitespace.");
                return;
            }

            string text = chatInput.Trim();
            NetworkedCombatant localPlayer = ResolveLocalPlayer();
            if (localPlayer == null)
            {
                ChatDebug.LogWarning(
                    "SendCurrentInput failed: local player not found. "
                    + "NetworkClient.active=" + NetworkClient.active
                    + ", localPlayerObject=" + (NetworkClient.localPlayer != null)
                    + ", isConnected=" + NetworkClient.isConnected);
                return;
            }

            ChatDebug.Log("SendCurrentInput -> CmdSendChatMessage channel=" + activeChannel + ", text=\"" + text + "\"");
            localPlayer.CmdSendChatMessage(activeChannel, text);
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

        private void CaptureEnterBeforeTextField()
        {
            if (Time.frameCount == chatOpenedFrame)
            {
                return;
            }

            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyDown
                || (currentEvent.keyCode != KeyCode.Return && currentEvent.keyCode != KeyCode.KeypadEnter))
            {
                return;
            }

            string focusedControl = GUI.GetNameOfFocusedControl();
            if (focusedControl != "ChatInput" && !string.IsNullOrEmpty(focusedControl))
            {
                return;
            }

            currentEvent.Use();
            pendingChatSubmit = true;
        }

        private void SubmitChatInput(string reason)
        {
            if (Time.frameCount == chatSubmittedFrame)
            {
                return;
            }

            chatSubmittedFrame = Time.frameCount;
            ChatDebug.Log(reason + ", chatInput=\"" + chatInput + "\"");

            if (!string.IsNullOrWhiteSpace(chatInput))
            {
                SendCurrentInput();
            }
            else
            {
                ChatDebug.LogWarning("Enter submit ignored: chatInput empty.");
            }

            CloseChatInput();
            GUIUtility.keyboardControl = 0;
        }

        private void OnGUI()
        {
            bool hasMessages = ChatManager.Instance != null && ChatManager.Instance.Messages.Count > 0;
            if (!chatFocused && !chatUnlocked && (!chatLogVisible || !hasMessages))
            {
                return;
            }

            EnsureStyles();
            DrawChatPanel();
            HudPanelCustomization.DrawContextMenu();
        }

        private void DrawChatPanel()
        {
            float width = 420f;
            float inputAreaHeight = chatFocused ? 52f : 0f;
            float messageAreaHeight = Mathf.Max(72f, GetMessageHeight());
            float height = messageAreaHeight + inputAreaHeight + 16f;
            UpdateChatPanelRect(width, height);
            Rect panel = chatPanelRect;
            GUI.Box(panel, string.Empty);
            if (chatUnlocked)
            {
                GUI.Label(new Rect(panel.x + 8f, panel.y + 2f, panel.width - 16f, 14f), "Game chat (drag to move)", channelStyle);
            }

            float contentTop = panel.y + (chatUnlocked ? 18f : 8f);
            float contentHeight = panel.height - (chatUnlocked ? 26f : 16f) - inputAreaHeight;
            scrollPosition = GUI.BeginScrollView(
                new Rect(panel.x + 8f, contentTop, panel.width - 16f, contentHeight),

                scrollPosition,
                new Rect(0f, 0f, panel.width - 32f, Mathf.Max(contentHeight, GetMessageHeight())));

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
            }
            else
            {
                float inputY = panel.y + panel.height - 44f;
                GUI.Label(new Rect(panel.x + 8, inputY, 60, 20), activeChannel, channelStyle);
                GUI.Label(new Rect(panel.x + 8, inputY - 18f, width - 16, 18), "Enter send | Tab channel | Esc close", channelStyle);

                CaptureEnterBeforeTextField();

                GUI.SetNextControlName("ChatInput");
                chatInput = GUI.TextField(
                    new Rect(panel.x + 70, inputY + 2f, panel.width - 80, 22),
                    chatInput,
                    256,
                    inputStyle);

                if (pendingChatSubmit)
                {
                    pendingChatSubmit = false;
                    SubmitChatInput("Enter pressed while typing in chat");
                }

                if (focusInputNextFrame)
                {
                    GUI.FocusControl("ChatInput");
                    focusInputNextFrame = false;
                }
            }

            HudPanelCustomization.TryOpenContextMenu(panel, GetChatMenuItems());
            HudPanelCustomization.HandleDrag(ref chatPanelRect, chatUnlocked, ChatDragControlId);
        }

        private void UpdateChatPanelRect(float width, float height)
        {
            if (!chatPanelRectInitialized)
            {
                chatPanelRect = new Rect(24f, Screen.height - height - 140f, width, height);
                chatPanelRectInitialized = true;
            }

            chatPanelRect.width = width;
            chatPanelRect.height = height;

            if (!chatUnlocked)
            {
                chatPanelRect.x = 24f;
                chatPanelRect.y = Screen.height - height - 140f;
            }
        }

        private HudPanelCustomization.MenuItem[] GetChatMenuItems()
        {
            return new[]
            {
                new HudPanelCustomization.MenuItem(
                    chatUnlocked ? "Lock Game chat" : "Unlock Game chat",
                    () => chatUnlocked = !chatUnlocked)
            };
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
