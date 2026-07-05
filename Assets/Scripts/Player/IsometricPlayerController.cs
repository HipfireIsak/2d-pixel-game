using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Core;
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
        private Vector3 lastSentInput;
        private float inputSendAccumulator;

        private const float InputSendRateSeconds = 0.05f;
        private const float PositionSmoothing = 14f;

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

            transform.position = Vector3.Lerp(
                transform.position,
                FlatMovementUtility.SnapToGround(serverPosition),
                Time.deltaTime * PositionSmoothing);
        }

        private void LateUpdate()
        {
            transform.position = FlatMovementUtility.SnapToGround(transform.position);
        }

        private void UpdateLocalInput()
        {
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

            if (SpellGroundTargeting.Instance != null && SpellGroundTargeting.Instance.IsTargeting)
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

            Vector3 velocity = motion / Time.deltaTime;
            inputSendAccumulator += Time.deltaTime;
            if (inputSendAccumulator >= InputSendRateSeconds && (velocity - lastSentInput).sqrMagnitude > 0.001f)
            {
                inputSendAccumulator = 0f;
                lastSentInput = velocity;
                CmdSubmitMovementInput(velocity, transform.position);
            }
        }

        private void HandleLocalSpellInput()
        {
            if (networkedCombatant == null || SpellGroundTargeting.Instance == null)
            {
                return;
            }

            if (SpellGroundTargeting.Instance.IsTargeting)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SpellGroundTargeting.Instance.BeginTargeting(GameConstants.SpellChronoBlast);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SpellGroundTargeting.Instance.BeginTargeting(GameConstants.SpellTemporalBolt);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SpellGroundTargeting.Instance.BeginTargeting(GameConstants.SpellManaSurge);
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
                : (playerCamera != null ? playerCamera : Camera.main);
            if (activeCamera == null)
            {
                return transform.forward;
            }

            Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, GameConstants.GroundHeight, 0f));
            if (!groundPlane.Raycast(ray, out float distance))
            {
                return transform.forward;
            }

            Vector3 hitPoint = ray.GetPoint(distance);
            Vector3 direction = hitPoint - transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? direction : transform.forward;
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
            Vector3 motion = inputVelocity * Time.deltaTime;
            transform.position = FlatMovementUtility.MoveWithFlatCollision(
                transform.position,
                motion,
                GameConstants.PlayerCollisionRadius);
            serverPosition = transform.position;
            RpcSyncTransform(serverPosition, transform.rotation);
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
