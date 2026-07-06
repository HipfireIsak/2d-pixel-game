using Mirror;
using UnityEngine;
using AetherEcho.Player;

namespace AetherEcho.World
{
    public class DungeonExitInteractable : MonoBehaviour
    {
        [SerializeField] private float interactRadiusMeters = 2.5f;

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.E))
            {
                return;
            }

            NetworkedCombatant localPlayer = NpcInteractUtility.FindLocalPlayer();
            if (localPlayer == null)
            {
                return;
            }

            if (Vector3.Distance(transform.position, localPlayer.transform.position) > interactRadiusMeters)
            {
                return;
            }

            localPlayer.CmdExitDungeon();
        }
    }
}
