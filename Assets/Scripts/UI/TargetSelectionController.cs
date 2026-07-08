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
        private GUIStyle targetLabelStyle;

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
            Camera camera = CombatPickUtility.ResolveGameplayCamera();
            if (camera == null)
            {
                return;
            }

            if (CombatPickUtility.TryPickEnemyAtScreen(
                    camera,
                    Input.mousePosition,
                    localPlayer.CombatantState,
                    out CombatantState combatant))
            {
                selectedTarget = combatant;
                selectedTargetNetId = combatant.netIdentity != null ? combatant.netIdentity.netId : 0;
                EnemyHealthBarController.Instance?.SetTargetedEnemy(combatant);
                GameplayHud.Instance?.SetToast("Target: " + combatant.CharacterClass);
            }
        }

        public void ClearTarget()
        {
            selectedTarget = null;
            selectedTargetNetId = 0;
            EnemyHealthBarController.Instance?.ClearTargetedEnemy();
        }

        private void OnGUI()
        {
            if (!HasTarget)
            {
                return;
            }

            EnsureStyles();
            Camera camera = CombatPickUtility.ResolveGameplayCamera();
            if (camera == null)
            {
                return;
            }

            if (!CombatPickUtility.TryGetScreenBounds(camera, selectedTarget, out Rect screenBounds, out _))
            {
                return;
            }

            Rect guiBounds = CombatPickUtility.ToGuiRect(screenBounds);
            GUI.Box(guiBounds, string.Empty, targetStyle);
            var labelRect = new Rect(
                guiBounds.x + guiBounds.width * 0.5f - 50f,
                guiBounds.yMax + 2f,
                100f,
                20f);
            GUI.Label(labelRect, selectedTarget.CharacterClass, targetLabelStyle);
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
            targetLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.2f) }
            };
        }
    }
}
