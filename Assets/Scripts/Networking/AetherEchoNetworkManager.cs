using Mirror;
using kcp2k;
using UnityEngine;
using AetherEcho.Core;

namespace AetherEcho.Networking
{
    /// <summary>
    /// Self-hosted Mirror network manager. Host listens on a configurable port; clients connect by IP.
    /// Adapted from RollABall3D's RelicHuntersNetworkManager, without Steam lobby transport.
    /// </summary>
    public class AetherEchoNetworkManager : NetworkManager
    {
        public override void Awake()
        {
            base.Awake();
            maxConnections = GameConstants.MaximumPlayersPerSession;
            autoCreatePlayer = true;
            RegisterResourcePrefab("Spells/SpellProjectile");
        }

        private void RegisterResourcePrefab(string resourcePath)
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null && !spawnPrefabs.Contains(prefab))
            {
                spawnPrefabs.Add(prefab);
            }
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient connection)
        {
            Transform startPosition = GetStartPosition();
            Vector3 spawnPosition = startPosition != null
                ? startPosition.position
                : new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(-4f, 4f));
            Quaternion spawnRotation = startPosition != null ? startPosition.rotation : Quaternion.identity;

            GameObject spawnedPlayer = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            NetworkServer.AddPlayerForConnection(connection, spawnedPlayer);
        }

        public void ConfigureTransportPort(ushort port)
        {
            if (transport is KcpTransport kcpTransport)
            {
                kcpTransport.Port = port;
            }
            else if (transport is PortTransport portTransport)
            {
                portTransport.Port = port;
            }
        }
    }
}
