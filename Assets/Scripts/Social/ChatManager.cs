using System.Collections.Generic;
using Mirror;
using UnityEngine;
using AetherEcho.Player;

namespace AetherEcho.Social
{
    public struct ChatMessage
    {
        public string channel;
        public string senderName;
        public string text;
        public float timestamp;
    }

    public class ChatManager : MonoBehaviour
    {
        public static ChatManager Instance { get; private set; }

        public const int MaxMessages = 50;
        public const float SayRadiusMeters = 30f;

        private readonly List<ChatMessage> messages = new List<ChatMessage>();

        public IReadOnlyList<ChatMessage> Messages => messages;

        private void Awake()
        {
            Instance = this;
        }

        [Server]
        public void ServerReceiveMessage(NetworkedCombatant sender, string channel, string text)
        {
            ChatDebug.Log(
                "ServerReceiveMessage sender=" + (sender != null ? sender.DisplayName : "null")
                + ", channel=" + channel
                + ", text=\"" + text + "\""
                + ", NetworkServer.active=" + NetworkServer.active);

            if (sender == null || string.IsNullOrWhiteSpace(text))
            {
                ChatDebug.LogWarning("ServerReceiveMessage rejected: invalid sender or empty text.");
                return;
            }

            text = text.Trim();
            if (text.Length > 200)
            {
                text = text.Substring(0, 200);
            }

            var message = new ChatMessage
            {
                channel = channel,
                senderName = sender.DisplayName,
                text = text,
                timestamp = Time.time
            };

            if (channel == "Say")
            {
                ChatDebug.Log("Broadcasting Say message from " + message.senderName);
                BroadcastSayMessage(sender.transform.position, message);
            }
            else
            {
                ChatDebug.Log("Broadcasting Global message from " + message.senderName);
                BroadcastGlobalMessage(message);
            }
        }

        [Server]
        private void BroadcastGlobalMessage(ChatMessage message)
        {
            NetworkedCombatant[] players = FindObjectsOfType<NetworkedCombatant>();
            ChatDebug.Log("BroadcastGlobalMessage to " + players.Length + " combatant(s)");
            foreach (NetworkedCombatant player in players)
            {
                if (player.connectionToClient == null)
                {
                    ChatDebug.LogWarning("Skipping player netId=" + player.netId + ": no connectionToClient");
                    continue;
                }

                ChatDebug.Log("TargetReceiveChatMessage -> netId=" + player.netId + ", name=" + player.DisplayName);
                player.TargetReceiveChatMessage(
                    player.connectionToClient,
                    message.channel,
                    message.senderName,
                    message.text);
            }
        }

        [Server]
        private void BroadcastSayMessage(Vector3 origin, ChatMessage message)
        {
            NetworkedCombatant[] players = FindObjectsOfType<NetworkedCombatant>();
            foreach (NetworkedCombatant player in players)
            {
                if (player.connectionToClient == null)
                {
                    continue;
                }

                if (Vector3.Distance(origin, player.transform.position) <= SayRadiusMeters)
                {
                    player.TargetReceiveChatMessage(
                        player.connectionToClient,
                        message.channel,
                        message.senderName,
                        message.text);
                }
            }
        }

        private void AppendMessage(ChatMessage message)
        {
            messages.Add(message);
            if (messages.Count > MaxMessages)
            {
                messages.RemoveAt(0);
            }
        }

        public void ClientAppendMessage(string channel, string senderName, string text)
        {
            ChatDebug.Log("ClientAppendMessage [" + channel + "] " + senderName + ": \"" + text + "\"");
            AppendMessage(new ChatMessage
            {
                channel = channel,
                senderName = senderName,
                text = text,
                timestamp = Time.time
            });
            UI.ChatUI.Instance?.RefreshFromManager();
        }
    }
}
