using Mirror;
using UnityEngine;
using AetherEcho.Player;
using AetherEcho.UI;

namespace AetherEcho.World
{
    public class VendorNpcInteractable : MonoBehaviour
    {
        [SerializeField] private string vendorId = "chrono_merchant";
        [SerializeField] private float interactRadiusMeters = 2.8f;

        public string VendorId => vendorId;

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.B))
            {
                return;
            }

            if (VendorUI.Instance != null && VendorUI.Instance.IsOpen)
            {
                return;
            }

            if (QuestDialogUI.Instance != null && QuestDialogUI.Instance.IsOpen)
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

            VendorUI.Instance?.Open(localPlayer, vendorId);
        }
    }
}
