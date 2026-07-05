using Mirror;
using UnityEngine;
using AetherEcho.Player;
using AetherEcho.UI;

namespace AetherEcho.World
{
    public class QuestNpcInteractable : MonoBehaviour
    {
        [SerializeField] private float interactRadiusMeters = 2.8f;

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.E))
            {
                return;
            }

            if (QuestDialogUI.Instance != null && QuestDialogUI.Instance.IsOpen)
            {
                return;
            }

            NetworkedCombatant localPlayer = FindLocalPlayer();
            if (localPlayer == null)
            {
                return;
            }

            if (Vector3.Distance(transform.position, localPlayer.transform.position) > interactRadiusMeters)
            {
                return;
            }

            localPlayer.CmdInteractWithQuestNpc();
        }

        private static NetworkedCombatant FindLocalPlayer()
        {
            NetworkedCombatant[] players = FindObjectsOfType<NetworkedCombatant>();
            foreach (NetworkedCombatant player in players)
            {
                if (player.isLocalPlayer)
                {
                    return player;
                }
            }

            return null;
        }
    }
}
