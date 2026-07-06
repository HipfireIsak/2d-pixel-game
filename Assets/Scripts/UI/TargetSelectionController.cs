using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Player;

namespace AetherEcho.UI
{
    public class TargetSelectionController : MonoBehaviour
    {
        public static TargetSelectionController Instance { get; private set; }

        private NetworkedCombatant localPlayer;
        private CombatantState selectedTarget;
        private uint selectedTargetNetId;

        public CombatantState SelectedTarget => selectedTarget;
        public uint SelectedTargetNetId => selectedTargetNetId;
        public bool HasTarget => selectedTarget != null && selectedTarget.CurrentHealth > 0;

        private GUIStyle targetStyle;

        private void Awake()
        {
            Instance = this;
        }

        public void BindLocalPlayer(NetworkedCombatant player)
        {
            localPlayer = player;
        }

        private void Update()
        {
            if (localPlayer == null || ChatUI.BlocksGameInput || InventoryUI.IsOpen)
            {
                return;
            }

            if (SpellGroundTargeting.Instance != null && SpellGroundTargeting.Instance.IsTargeting)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) && !IsPointerOverBlockingUi())
            {
                TrySelectUnderCursor();
            }
            if (selectedTarget != null && selectedTarget.CurrentHealth <= 0)
            {
                ClearTarget();
            }
        }

        private bool IsPointerOverBlockingUi()
        {
            return VendorUI.Instance != null && VendorUI.Instance.IsOpen;
        }

        private void TrySelectUnderCursor()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 120f))
            {
                return;
            }

            CombatantState combatant = hit.collider.GetComponentInParent<CombatantState>();
            if (combatant == null || !combatant.IsEnemyWith(localPlayer.CombatantState))
            {
                return;
            }

            selectedTarget = combatant;
            selectedTargetNetId = combatant.netIdentity != null ? combatant.netIdentity.netId : 0;
            GameplayHud.Instance?.SetToast("Target: " + combatant.CharacterClass);
        }

        public void ClearTarget()
        {
            selectedTarget = null;
            selectedTargetNetId = 0;
        }

        private void OnGUI()
        {
            if (!HasTarget)
            {
                return;
            }

            EnsureStyles();
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 screen = camera.WorldToScreenPoint(selectedTarget.transform.position + Vector3.up * 1.4f);
            if (screen.z < 0f)
            {
                return;
            }

            screen.y = Screen.height - screen.y;
            var rect = new Rect(screen.x - 24f, screen.y - 24f, 48f, 48f);
            GUI.Box(rect, string.Empty, targetStyle);
            GUI.Label(new Rect(screen.x - 50f, screen.y + 28f, 100f, 20), selectedTarget.CharacterClass, targetStyle);
        }

        private void EnsureStyles()
        {
            if (targetStyle != null)
            {
                return;
            }

            targetStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { textColor = new Color(1f, 0.85f, 0.2f) }
            };
        }
    }
}
