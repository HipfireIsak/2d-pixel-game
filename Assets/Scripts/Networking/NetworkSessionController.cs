using System;
using Mirror;
using UnityEngine;
using AetherEcho.Core;

namespace AetherEcho.Networking
{
    /// <summary>
    /// Keeps Mirror bootstrap alive across scene loads and exposes host/join helpers.
    /// Mirrors RollABall3D's PersistentNetworkBootstrap + session controller, but for direct IP hosting.
    /// </summary>
    public class NetworkSessionController : MonoBehaviour
    {
        public static NetworkSessionController Instance { get; private set; }

        [SerializeField] private AetherEchoNetworkManager networkManager;
        [SerializeField] private ushort serverPort = GameConstants.DefaultServerPort;

        public ushort ServerPort => serverPort;
        public string ServerAddress { get; private set; } = GameConstants.DefaultServerAddress;

        public event Action HostSessionStarted;
        public event Action ClientSessionStarted;
        public event Action SessionEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (networkManager == null)
            {
                networkManager = GetComponent<AetherEchoNetworkManager>();
            }
        }

        public void SetServerAddress(string address)
        {
            ServerAddress = string.IsNullOrWhiteSpace(address)
                ? GameConstants.DefaultServerAddress
                : address.Trim();
        }

        public void SetServerPort(ushort port)
        {
            serverPort = port;
            networkManager?.ConfigureTransportPort(serverPort);
        }

        public bool TryStartHost(out string failureReason)
        {
            failureReason = string.Empty;
            if (networkManager == null)
            {
                failureReason = "Network manager is missing.";
                return false;
            }

            if (NetworkServer.active || NetworkClient.isConnected)
            {
                failureReason = "Already connected.";
                return false;
            }

            networkManager.ConfigureTransportPort(serverPort);
            networkManager.StartHost();
            HostSessionStarted?.Invoke();
            return true;
        }

        public bool TryStartClient(out string failureReason)
        {
            failureReason = string.Empty;
            if (networkManager == null)
            {
                failureReason = "Network manager is missing.";
                return false;
            }

            if (NetworkServer.active || NetworkClient.isConnected)
            {
                failureReason = "Already connected.";
                return false;
            }

            networkManager.networkAddress = ServerAddress;
            networkManager.ConfigureTransportPort(serverPort);
            networkManager.StartClient();
            ClientSessionStarted?.Invoke();
            return true;
        }

        public void StopSession()
        {
            if (networkManager == null)
            {
                return;
            }

            if (NetworkServer.active && NetworkClient.isConnected)
            {
                networkManager.StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                networkManager.StopClient();
            }
            else if (NetworkServer.active)
            {
                networkManager.StopServer();
            }

            SessionEnded?.Invoke();
        }

        public int GetConnectedPlayerCount()
        {
            if (NetworkServer.active)
            {
                return NetworkServer.connections.Count;
            }

            return NetworkClient.isConnected ? 1 : 0;
        }
    }
}
