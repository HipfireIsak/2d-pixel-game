using UnityEngine;
using AetherEcho.Player;
using AetherEcho.UI;

namespace AetherEcho.World
{
    public class QuestNpcInteractable : MonoBehaviour
    {
        [SerializeField] private string questGiverName = "Chrono Sage";
        [SerializeField] private float interactRadiusMeters = 2.8f;

        public string QuestGiverName => questGiverName;

        public void Configure(string giverName)
        {
            questGiverName = giverName;
        }

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

            if (VendorUI.Instance != null && VendorUI.Instance.IsOpen)
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

            localPlayer.CmdInteractWithQuestNpc(questGiverName);
        }
    }
}
