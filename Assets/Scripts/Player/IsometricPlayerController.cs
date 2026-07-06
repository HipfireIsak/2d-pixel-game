using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Core;
using AetherEcho.Items;
using AetherEcho.Rendering;
using AetherEcho.UI;
using AetherEcho.World;

namespace AetherEcho.Player
{
    public class IsometricPlayerController : NetworkBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float rotationSpeedDegreesPerSecond = 900f;

        private NetworkedCombatant networkedCombatant;
        private PixelBillboardVisual billboardVisual;
        private LostArkCameraRig cameraRig;
        private Vector3 serverPosition;
        private Vector3 remoteSmoothVelocity;
        private Vector3 lastSentInput;
        private float inputSendAccumulator;

        private const float InputSendRateSeconds = 0.05f;
        private const float RemotePositionSmoothTime = 0.08f;
        private const float MaxMovementSpeedSlack = 1.25f;

        private void Awake()
        {
            networkedCombatant = GetComponent<NetworkedCombatant>();
            billboardVisual = GetComponent<PixelBillboardVisual>();

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
            if (isLocalPlayer)
            {
                UpdateLocalInput();
                return;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                FlatMovementUtility.SnapToGround(serverPosition),
                ref remoteSmoothVelocity,
                RemotePositionSmoothTime);
        }

        private void LateUpdate()
        {
            if (isLocalPlayer)
            {
                transform.position = FlatMovementUtility.SnapToGround(transform.position);
            }
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
                FaceAimDirection(motion / Time.deltaTime);
                if (billboardVisual != null)
                {
                    billboardVisual.SetMoveDirection(motion / Time.deltaTime);
                }
            }

            transform.position = FlatMovementUtility.MoveWithFlatCollision(
                transform.position,
                motion,
                GameConstants.PlayerCollisionRadius);
            HandleLocalSpellInput();
            HandleLootPickupInput();

            Vector3 velocity = motion / Time.deltaTime;
            inputSendAccumulator += Time.deltaTime;
            if (inputSendAccumulator >= InputSendRateSeconds)
            {
                inputSendAccumulator = 0f;
                if (velocity.sqrMagnitude > 0.001f || lastSentInput.sqrMagnitude > 0.001f)
                {
                    lastSentInput = velocity;
                    CmdSubmitMovementInput(velocity, transform.position);
                }
            }
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
                && drop.netIdentity != null)
            {
                networkedCombatant.CmdPickupGroundLoot(drop.netIdentity.netId);
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
                && drop.netIdentity != null)
            {
                networkedCombatant.CmdPickupGroundLoot(drop.netIdentity.netId);
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

        [Command]
        private void CmdSubmitMovementInput(Vector3 inputVelocity, Vector3 clientPosition)
        {
            if (networkedCombatant != null && networkedCombatant.IsCastingSpell)
            {
                return;
            }

            Vector3 validatedPosition = ValidateClientMovement(transform.position, clientPosition, inputVelocity);
            Vector3 broadcastPosition;

            if (IsHostOwnerConnection())
            {
                // Listen-server host: local client already moved this transform in Update.
                broadcastPosition = FlatMovementUtility.SnapToGround(transform.position);
            }
            else
            {
                transform.position = validatedPosition;
                broadcastPosition = validatedPosition;
            }

            serverPosition = broadcastPosition;
            RpcSyncTransform(serverPosition, transform.rotation);
        }

        private bool IsHostOwnerConnection()
        {
            return connectionToClient != null && NetworkServer.localConnection == connectionToClient;
        }

        private static Vector3 ValidateClientMovement(Vector3 serverPosition, Vector3 clientPosition, Vector3 inputVelocity)
        {
            clientPosition = FlatMovementUtility.SnapToGround(clientPosition);
            Vector3 delta = clientPosition - serverPosition;
            delta.y = 0f;

            float maxSpeed = GameConstants.PlayerMoveSpeedMetersPerSecond * GameConstants.PlayerSprintMultiplier;
            float reportedSpeed = new Vector3(inputVelocity.x, 0f, inputVelocity.z).magnitude;
            maxSpeed = Mathf.Max(maxSpeed, reportedSpeed);
            float maxDistance = maxSpeed * InputSendRateSeconds * MaxMovementSpeedSlack;

            if (delta.sqrMagnitude <= maxDistance * maxDistance)
            {
                return FlatMovementUtility.MoveWithFlatCollision(
                    serverPosition,
                    delta,
                    GameConstants.PlayerCollisionRadius);
            }

            Vector3 clampedDelta = delta.normalized * maxDistance;
            return FlatMovementUtility.MoveWithFlatCollision(
                serverPosition,
                clampedDelta,
                GameConstants.PlayerCollisionRadius);
        }

        [ClientRpc]
        private void RpcSyncTransform(Vector3 position, Quaternion rotation)
        {
            if (isLocalPlayer)
            {
                return;
            }

            serverPosition = position;
            transform.rotation = rotation;
        }
    }
}
