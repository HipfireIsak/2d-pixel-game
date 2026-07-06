using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Core;
using AetherEcho.Items;
using AetherEcho.Networking;
using AetherEcho.Rendering;
using AetherEcho.UI;
using AetherEcho.World;

namespace AetherEcho.Player
{
    [DefaultExecutionOrder(100)]
    public class IsometricPlayerController : NetworkBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float rotationSpeedDegreesPerSecond = 900f;

        private NetworkedCombatant networkedCombatant;
        private PixelBillboardVisual billboardVisual;
        private LostArkCameraRig cameraRig;
        private FlatMovementNetworkSync movementSync;

        private void Awake()
        {
            networkedCombatant = GetComponent<NetworkedCombatant>();
            billboardVisual = GetComponent<PixelBillboardVisual>();
            movementSync = FlatMovementNetworkSync.Ensure(gameObject, MovementSyncMode.ClientAuthority);

            if (playerCamera != null)
            {
                playerCamera.enabled = false;
            }

            transform.position = FlatMovementUtility.SnapToGround(transform.position);
        }

        public override void OnStartLocalPlayer()
        {
            if (playerCamera != null)
            {
                playerCamera.tag = "MainCamera";
                playerCamera.enabled = true;
            }

            cameraRig = LostArkCameraRig.Ensure(playerCamera, transform);
            WorldAtmosphere.ApplyToCamera(playerCamera);
            WorldAtmosphere.ApplyDrakantosStyle();
        }

        private void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            UpdateLocalInput();
        }

        private void LateUpdate()
        {
            if (!isLocalPlayer || movementSync == null)
            {
                return;
            }

            movementSync.SubmitLocalTransform(transform.position, transform.rotation, Input.GetKey(KeyCode.LeftShift));
        }

        private void UpdateLocalInput()
        {
            if (ChatUI.BlocksGameInput)
            {
                return;
            }

            if (networkedCombatant != null
                && networkedCombatant.CombatantState != null
                && networkedCombatant.CombatantState.IsDead)
            {
                return;
            }

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 input = new Vector3(horizontal, 0f, vertical);
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            Vector3 worldInput = TransformInputRelativeToCamera(input);
            bool sprinting = Input.GetKey(KeyCode.LeftShift);
            float speed = GameConstants.PlayerMoveSpeedMetersPerSecond
                          * (sprinting ? GameConstants.PlayerSprintMultiplier : 1f);
            Vector3 motion = worldInput * speed * Time.deltaTime;

            if (networkedCombatant != null && networkedCombatant.IsCastingSpell)
            {
                motion = Vector3.zero;
            }

            if (motion.sqrMagnitude > 0.001f)
            {
                Vector3 velocity = motion / Time.deltaTime;
                FaceAimDirection(velocity);
                if (billboardVisual != null)
                {
                    billboardVisual.SetMoveDirection(velocity);
                }
            }
            else if (billboardVisual != null)
            {
                billboardVisual.SetMoving(false);
            }

            Vector3 positionBeforeMove = transform.position;

            transform.position = FlatMovementUtility.MoveWithFlatCollision(
                transform.position,
                motion,
                GameConstants.PlayerCollisionRadius);

            float frameDelta = MovementDebugLogger.HorizontalDistance(positionBeforeMove, transform.position);
            if (motion.sqrMagnitude > 0.0001f || frameDelta > 0.0001f)
            {
                MovementDebugLogger.Log(
                    "LocalMove",
                    "sprint=" + sprinting
                    + " speed=" + speed.ToString("F2")
                    + " frameDelta=" + frameDelta.ToString("F4")
                    + " input=(" + horizontal.ToString("F2") + "," + vertical.ToString("F2") + ")"
                    + " before=" + MovementDebugLogger.FormatVector(positionBeforeMove)
                    + " after=" + MovementDebugLogger.FormatVector(transform.position)
                    + " dt=" + Time.deltaTime.ToString("F4"));
            }

            HandleLocalSpellInput();
            HandleLootPickupInput();
            HandleRecallInput();
        }

        private void HandleRecallInput()
        {
            if (networkedCombatant == null || !Input.GetKeyDown(KeyCode.H))
            {
                return;
            }

            networkedCombatant.CmdRecallToHub();
        }

        private void HandleLootPickupInput()
        {
            if (networkedCombatant == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                TryPickupNearestLoot();
                return;
            }

            if (Input.GetMouseButtonDown(1)
                && (SpellGroundTargeting.Instance == null || !SpellGroundTargeting.Instance.IsTargeting))
            {
                TryPickupLootUnderCursor();
            }
        }

        private void TryPickupNearestLoot()
        {
            if (GroundLootDrop.TryFindNearest(transform.position, GroundLootDrop.PickupRange, out GroundLootDrop drop)
                && drop.netId != 0)
            {
                networkedCombatant.CmdPickupGroundLoot(drop.netId);
            }
        }

        private void TryPickupLootUnderCursor()
        {
            Camera camera = CombatPickUtility.ResolveGameplayCamera();
            if (camera == null)
            {
                return;
            }

            if (CombatPickUtility.TryPickGroundLootAtScreen(
                    camera,
                    Input.mousePosition,
                    transform.position,
                    GroundLootDrop.PickupRange,
                    out GroundLootDrop drop)
                && drop.netId != 0)
            {
                networkedCombatant.CmdPickupGroundLoot(drop.netId);
            }
        }

        private void HandleLocalSpellInput()
        {
            if (networkedCombatant == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                TryCastBlink();
                return;
            }

            if (SpellGroundTargeting.Instance != null && SpellGroundTargeting.Instance.IsTargeting)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                TryCastChronoBlastOnTarget();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SpellGroundTargeting.Instance?.BeginTargeting(GameConstants.SpellTemporalBolt);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                TryCastSelfSpell(GameConstants.SpellManaSurge);
            }
        }

        private void TryCastChronoBlastOnTarget()
        {
            if (TargetSelectionController.Instance == null || !TargetSelectionController.Instance.HasTarget)
            {
                GameplayHud.Instance?.SetToast("Click an enemy to target, then press 1.");
                return;
            }

            CombatantState target = TargetSelectionController.Instance.SelectedTarget;
            Vector3 targetPoint = target.transform.position;
            Vector3 aimDirection = targetPoint - transform.position;
            aimDirection.y = 0f;
            if (aimDirection.sqrMagnitude < 0.001f)
            {
                aimDirection = transform.forward;
            }

            uint targetNetId = TargetSelectionController.Instance.SelectedTargetNetId;
            if (!networkedCombatant.TryLocalCast(
                    GameConstants.SpellChronoBlast,
                    targetPoint,
                    aimDirection.normalized,
                    out string failureReason,
                    targetNetId))
            {
                GameplayHud.Instance?.SetToast(failureReason);
            }
        }

        private void TryCastSelfSpell(string spellId)
        {
            if (!networkedCombatant.TryLocalCast(
                    spellId,
                    transform.position,
                    transform.forward,
                    out string failureReason,
                    0))
            {
                GameplayHud.Instance?.SetToast(failureReason);
            }
        }

        private void TryCastBlink()
        {
            Vector3 aimDirection = GetCursorWorldDirection();
            Vector3 targetPoint = transform.position + aimDirection.normalized * GameConstants.PlayerMoveSpeedMetersPerSecond;
            if (!networkedCombatant.TryLocalCast(
                    GameConstants.SpellChronoBlink,
                    targetPoint,
                    aimDirection,
                    out string failureReason,
                    0))
            {
                GameplayHud.Instance?.SetToast(failureReason);
            }
        }

        public void FaceAimDirection(Vector3 aimDirection)
        {
            aimDirection.y = 0f;
            if (aimDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(aimDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeedDegreesPerSecond * Time.deltaTime);
        }

        private Vector3 GetCursorWorldDirection()
        {
            Camera activeCamera = cameraRig != null && cameraRig.TargetCamera != null
                ? cameraRig.TargetCamera
                : (playerCamera != null ? playerCamera : CombatPickUtility.ResolveGameplayCamera());
            if (activeCamera == null)
            {
                return transform.forward;
            }

            if (CombatPickUtility.TryGetGroundPointUnderCursor(activeCamera, Input.mousePosition, out Vector3 groundPoint))
            {
                Vector3 direction = groundPoint - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    return direction.normalized;
                }
            }

            return transform.forward;
        }

        private Vector3 TransformInputRelativeToCamera(Vector3 input)
        {
            if (cameraRig != null)
            {
                return cameraRig.RightOnGround * input.x + cameraRig.ForwardOnGround * input.z;
            }

            Quaternion yaw = Quaternion.Euler(0f, GameConstants.CameraYawDegrees, 0f);
            return yaw * input;
        }
    }
}
