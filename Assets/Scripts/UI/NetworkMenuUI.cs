using Mirror;
using UnityEngine;
using AetherEcho.Networking;
using AetherEcho.Core;

namespace AetherEcho.UI
{
    /// <summary>
    /// Minimal host/join menu for self-hosted Mirror sessions (IP + port).
    /// </summary>
    public class NetworkMenuUI : MonoBehaviour
    {
        [SerializeField] private NetworkSessionController sessionController;

        private string addressField = GameConstants.DefaultServerAddress;
        private string portField = GameConstants.DefaultServerPort.ToString();
        private string statusMessage = "Host a session or join by IP.";
        private bool showMenu = true;

        private void Awake()
        {
            if (sessionController == null)
            {
                sessionController = NetworkSessionController.Instance;
            }
        }

        private void Update()
        {
            if (NetworkServer.active || NetworkClient.isConnected)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    showMenu = !showMenu;
                }
            }
        }

        private void OnGUI()
        {
            if (!showMenu && (NetworkServer.active || NetworkClient.isConnected))
            {
                DrawConnectedHint();
                return;
            }

            GUILayout.BeginArea(new Rect(20, 20, 420, 420), GUI.skin.box);
            GUILayout.Label("AetherEcho: Chrono-Fractures", GUI.skin.box);
            GUILayout.Label("Self-Hosted Multiplayer (Mirror + KCP)");
            GUILayout.Space(8);

            GUILayout.Label("Server Address");
            addressField = GUILayout.TextField(addressField);
            GUILayout.Label("Port");
            portField = GUILayout.TextField(portField);

            if (GUILayout.Button("Host Game"))
            {
                TryHost();
            }

            if (GUILayout.Button("Join Game"))
            {
                TryJoin();
            }

            if (NetworkServer.active || NetworkClient.isConnected)
            {
                if (GUILayout.Button("Disconnect"))
                {
                    sessionController?.StopSession();
                    statusMessage = "Disconnected.";
                }

                string role = NetworkServer.active ? "Hosting" : "Client";
                GUILayout.Label(role + " | Players: " + (sessionController?.GetConnectedPlayerCount() ?? 0));
            }

            GUILayout.Space(8);
            GUILayout.Label(statusMessage);
            GUILayout.Label("WASD move | Shift sprint | Mouse aim | 1-3 spells | E quest | Esc menu");
            GUILayout.EndArea();
        }

        private void DrawConnectedHint()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 260, Screen.height - 40, 240, 30));
            GUILayout.Label("Esc: session menu");
            GUILayout.EndArea();
        }

        private void TryHost()
        {
            if (sessionController == null)
            {
                statusMessage = "Session controller missing.";
                return;
            }

            if (!ushort.TryParse(portField, out ushort port))
            {
                statusMessage = "Invalid port.";
                return;
            }

            sessionController.SetServerAddress(addressField);
            sessionController.SetServerPort(port);
            if (sessionController.TryStartHost(out string failureReason))
            {
                statusMessage = "Hosting on port " + port + ".";
                showMenu = false;
            }
            else
            {
                statusMessage = failureReason;
            }
        }

        private void TryJoin()
        {
            if (sessionController == null)
            {
                statusMessage = "Session controller missing.";
                return;
            }

            if (!ushort.TryParse(portField, out ushort port))
            {
                statusMessage = "Invalid port.";
                return;
            }

            sessionController.SetServerAddress(addressField);
            sessionController.SetServerPort(port);
            if (sessionController.TryStartClient(out string failureReason))
            {
                statusMessage = "Connecting to " + addressField + ":" + port + "...";
                showMenu = false;
            }
            else
            {
                statusMessage = failureReason;
            }
        }
    }
}
