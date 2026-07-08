using Mirror;

using UnityEngine;



namespace AetherEcho.Networking

{

    /// <summary>

    /// Ensures the player uses client-authority FlatMovementNetworkSync (not NetworkTransform).

    /// </summary>

    public static class PlayerMovementSyncSetup

    {

        public static FlatMovementNetworkSync Configure(GameObject playerRoot)

        {

            NetworkTransformUnreliable networkTransform = playerRoot.GetComponent<NetworkTransformUnreliable>();

            if (networkTransform != null)

            {

                Object.DestroyImmediate(networkTransform);

            }



            return FlatMovementNetworkSync.Ensure(playerRoot, MovementSyncMode.ClientAuthority);

        }

    }

}


