using System;
using Mirror;
using UnityEngine;
using AetherEcho.World;

namespace AetherEcho.Networking
{
    public enum MovementSyncMode
    {
        ClientAuthority,
        ServerAuthority
    }

    [DefaultExecutionOrder(200)]
    public class FlatMovementNetworkSync : NetworkBehaviour
    {
        [NonSerialized] private MovementSyncMode movementSyncMode = MovementSyncMode.ClientAuthority;
        [NonSerialized] private float remoteSmoothTime = 0.08f;

        [NonSerialized] private Vector3 remotePosition;
        [NonSerialized] private Vector3 remoteVelocity;
        [NonSerialized] private Quaternion remoteRotation;
        [NonSerialized] private Vector3 serverPosition;

        [NonSerialized] private Vector3 lastSubmittedPosition;
        [NonSerialized] private bool hasSubmittedPosition;
        [NonSerialized] private bool lastSubmitWasSprint;

        public static FlatMovementNetworkSync Ensure(GameObject target, MovementSyncMode mode)
        {
            FlatMovementNetworkSync sync = target.GetComponent<FlatMovementNetworkSync>();
            if (sync == null)
            {
                sync = target.AddComponent<FlatMovementNetworkSync>();
            }

            sync.Configure(mode);
            return sync;
        }

        public void Configure(MovementSyncMode mode)
        {
            movementSyncMode = mode;
        }

        public override void OnStartServer()
        {
            serverPosition = FlatMovementUtility.SnapToGround(transform.position);
            remotePosition = serverPosition;
            remoteRotation = transform.rotation;
            MovementDebugLogger.Log(
                "SpawnServer",
                "serverPos=" + MovementDebugLogger.FormatVector(serverPosition)
                + " transform=" + MovementDebugLogger.FormatVector(transform.position));
        }

        public override void OnStartClient()
        {
            remotePosition = FlatMovementUtility.SnapToGround(transform.position);
            remoteRotation = transform.rotation;
            MovementDebugLogger.Log(
                "SpawnClient",
                "local=" + isLocalPlayer
                + " remotePos=" + MovementDebugLogger.FormatVector(remotePosition)
                + " transform=" + MovementDebugLogger.FormatVector(transform.position));
        }

        private void Update()
        {
            if (!IsNetworkedAndSpawned())
            {
                return;
            }

            if (movementSyncMode == MovementSyncMode.ServerAuthority)
            {
                if (isClient && !isServer)
                {
                    ApplyRemoteInterpolation(true);
                }

                return;
            }

            if (isLocalPlayer)
            {
                return;
            }

            if (isClient)
            {
                ApplyRemoteInterpolation(false);
            }
        }

        public void SubmitServerTransform(Vector3 position, Quaternion rotation, string entityLabel)
        {
            if (!IsNetworkedAndSpawned() || !isServer || movementSyncMode != MovementSyncMode.ServerAuthority)
            {
                return;
            }

            position = FlatMovementUtility.SnapToGround(position);
            float serverDelta = MovementDebugLogger.HorizontalDistance(serverPosition, position);
            float transformDelta = MovementDebugLogger.HorizontalDistance(transform.position, position);
            serverPosition = position;

            MovementDebugLogger.LogEnemy(
                entityLabel,
                netId,
                "ServerSubmit",
                "serverDelta=" + serverDelta.ToString("F4")
                + " transformDelta=" + transformDelta.ToString("F4")
                + " submitted=" + MovementDebugLogger.FormatVector(position)
                + " transformNow=" + MovementDebugLogger.FormatVector(transform.position));

            RpcApplyTransform(position, rotation, false, "ServerSubmit");
        }

        public void SubmitLocalTransform(Vector3 position, Quaternion rotation, bool isSprinting)
        {
            if (!IsNetworkedAndSpawned() || !isLocalPlayer || movementSyncMode != MovementSyncMode.ClientAuthority)
            {
                return;
            }

            position = FlatMovementUtility.SnapToGround(position);
            float submitDelta = hasSubmittedPosition
                ? MovementDebugLogger.HorizontalDistance(lastSubmittedPosition, position)
                : 0f;
            float transformMismatch = MovementDebugLogger.HorizontalDistance(transform.position, position);

            MovementDebugLogger.Log(
                "SubmitLocal",
                "sprint=" + isSprinting
                + " submitDelta=" + submitDelta.ToString("F4")
                + " transformMismatch=" + transformMismatch.ToString("F4")
                + " submitted=" + MovementDebugLogger.FormatVector(position)
                + " transformNow=" + MovementDebugLogger.FormatVector(transform.position)
                + " serverPos=" + MovementDebugLogger.FormatVector(serverPosition)
                + " host=" + NetworkServer.activeHost);

            lastSubmittedPosition = position;
            hasSubmittedPosition = true;
            lastSubmitWasSprint = isSprinting;

            if (NetworkServer.activeHost)
            {
                ServerRelayOwnerTransform(position, rotation, isSprinting);
                return;
            }

            CmdSubmitClientTransform(position, rotation, isSprinting);
        }

        [Server]
        private void ServerRelayOwnerTransform(Vector3 position, Quaternion rotation, bool isSprinting)
        {
            Vector3 transformBefore = transform.position;
            float serverDelta = MovementDebugLogger.HorizontalDistance(serverPosition, position);
            float transformDelta = MovementDebugLogger.HorizontalDistance(transformBefore, position);

            serverPosition = position;

            MovementDebugLogger.Log(
                "HostRelay",
                "sprint=" + isSprinting
                + " serverDelta=" + serverDelta.ToString("F4")
                + " transformDelta=" + transformDelta.ToString("F4")
                + " submitted=" + MovementDebugLogger.FormatVector(position)
                + " transformBefore=" + MovementDebugLogger.FormatVector(transformBefore)
                + " transformAfter=" + MovementDebugLogger.FormatVector(transform.position)
                + " wroteTransform=false");

            RpcApplyTransform(position, rotation, isSprinting, "HostRelay");
        }

        public void ApplyAuthoritativeTeleport(Vector3 position, Quaternion rotation)
        {
            Vector3 snapped = FlatMovementUtility.SnapToGround(position);
            transform.SetPositionAndRotation(snapped, rotation);
            serverPosition = snapped;
            remotePosition = snapped;
            remoteRotation = rotation;
            lastSubmittedPosition = snapped;
            hasSubmittedPosition = true;

            MovementDebugLogger.Log(
                "Teleport",
                "pos=" + MovementDebugLogger.FormatVector(snapped));

            if (isServer)
            {
                RpcApplyTransform(snapped, rotation, lastSubmitWasSprint, "Teleport");
            }
        }

        [Command(channel = Channels.Unreliable)]
        private void CmdSubmitClientTransform(Vector3 clientPosition, Quaternion clientRotation, bool isSprinting)
        {
            if (movementSyncMode != MovementSyncMode.ClientAuthority)
            {
                return;
            }

            Vector3 transformBefore = transform.position;
            clientPosition = FlatMovementUtility.SnapToGround(clientPosition);
            float serverDelta = MovementDebugLogger.HorizontalDistance(serverPosition, clientPosition);
            float transformDelta = MovementDebugLogger.HorizontalDistance(transformBefore, clientPosition);

            serverPosition = clientPosition;
            transform.SetPositionAndRotation(clientPosition, clientRotation);

            MovementDebugLogger.Log(
                "CmdServer",
                "sprint=" + isSprinting
                + " serverDelta=" + serverDelta.ToString("F4")
                + " transformDelta=" + transformDelta.ToString("F4")
                + " clientPos=" + MovementDebugLogger.FormatVector(clientPosition)
                + " transformBefore=" + MovementDebugLogger.FormatVector(transformBefore)
                + " transformAfter=" + MovementDebugLogger.FormatVector(transform.position)
                + " wroteTransform=true");

            RpcApplyTransform(clientPosition, clientRotation, isSprinting, "CmdServer");
        }

        [ClientRpc(channel = Channels.Unreliable)]
        private void RpcApplyTransform(Vector3 position, Quaternion rotation, bool isSprinting, string source)
        {
            if (movementSyncMode == MovementSyncMode.ClientAuthority && isLocalPlayer)
            {
                MovementDebugLogger.Log(
                    "RpcSkippedLocal",
                    "source=" + source
                    + " sprint=" + isSprinting
                    + " pos=" + MovementDebugLogger.FormatVector(position)
                    + " localTransform=" + MovementDebugLogger.FormatVector(transform.position));
                return;
            }

            if (movementSyncMode == MovementSyncMode.ServerAuthority && isServer)
            {
                return;
            }

            Vector3 before = transform.position;
            remotePosition = FlatMovementUtility.SnapToGround(position);
            remoteRotation = rotation;

            if (movementSyncMode == MovementSyncMode.ServerAuthority)
            {
                MovementDebugLogger.LogEnemy(
                    gameObject.name,
                    netId,
                    "RpcRemote",
                    "source=" + source
                    + " target=" + MovementDebugLogger.FormatVector(remotePosition)
                    + " transformBefore=" + MovementDebugLogger.FormatVector(before));
            }
            else
            {
                MovementDebugLogger.Log(
                    "RpcRemote",
                    "source=" + source
                    + " sprint=" + isSprinting
                    + " target=" + MovementDebugLogger.FormatVector(remotePosition)
                    + " transformBefore=" + MovementDebugLogger.FormatVector(before)
                    + " local=" + isLocalPlayer);
            }
        }

        private void ApplyRemoteInterpolation(bool isEnemy)
        {
            Vector3 before = transform.position;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                remotePosition,
                ref remoteVelocity,
                remoteSmoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, remoteRotation, Time.deltaTime * 14f);

            float moved = MovementDebugLogger.HorizontalDistance(before, transform.position);
            if (moved > 0.001f)
            {
                if (isEnemy)
                {
                    MovementDebugLogger.LogEnemy(
                        gameObject.name,
                        netId,
                        "RemoteInterp",
                        "moved=" + moved.ToString("F4")
                        + " target=" + MovementDebugLogger.FormatVector(remotePosition)
                        + " after=" + MovementDebugLogger.FormatVector(transform.position));
                }
                else
                {
                    MovementDebugLogger.Log(
                        "RemoteInterp",
                        "moved=" + moved.ToString("F4")
                        + " target=" + MovementDebugLogger.FormatVector(remotePosition)
                        + " after=" + MovementDebugLogger.FormatVector(transform.position));
                }
            }
        }

        public Vector3 GetServerAuthorityPosition()
        {
            return serverPosition;
        }

        private bool IsNetworkedAndSpawned()
        {
            return netIdentity != null && netId != 0;
        }
    }
}
