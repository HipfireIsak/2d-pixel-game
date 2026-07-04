using UnityEngine;
using AetherEcho.Core;

namespace AetherEcho.Player
{
    /// <summary>
    /// Fixed 45° top-down camera: euler (45, 0, 0), orthographic, smooth follow.
    /// </summary>
    public class LostArkCameraRig : MonoBehaviour
    {
        public static LostArkCameraRig Instance { get; private set; }

        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 0f, 0f);

        private Vector3 followVelocity;
        private readonly Quaternion fixedRotation = Quaternion.Euler(
            GameConstants.CameraPitchDegrees,
            GameConstants.CameraYawDegrees,
            0f);

        public Camera TargetCamera => targetCamera;
        public Vector3 ForwardOnGround => GetGroundForward();
        public Vector3 RightOnGround => GetGroundRight();

        public static LostArkCameraRig Ensure(Camera camera, Transform playerTransform)
        {
            if (camera == null)
            {
                return null;
            }

            LostArkCameraRig rig = camera.GetComponent<LostArkCameraRig>();
            if (rig == null)
            {
                rig = camera.gameObject.AddComponent<LostArkCameraRig>();
            }

            rig.Initialize(camera, playerTransform);
            Instance = rig;
            return rig;
        }

        private void Initialize(Camera camera, Transform playerTransform)
        {
            targetCamera = camera;
            followTarget = playerTransform;
            camera.transform.SetParent(null, true);
            camera.orthographic = true;
            camera.orthographicSize = GameConstants.CameraOrthographicSize;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 250f;
            SnapToTargetImmediate();
        }

        private void LateUpdate()
        {
            if (followTarget == null || targetCamera == null)
            {
                return;
            }

            Vector3 focusPoint = followTarget.position + lookOffset;
            Vector3 desiredPosition = focusPoint + new Vector3(
                0f,
                GameConstants.CameraHeightMeters,
                -GameConstants.CameraBackOffsetMeters);

            targetCamera.transform.position = Vector3.SmoothDamp(
                targetCamera.transform.position,
                desiredPosition,
                ref followVelocity,
                GameConstants.CameraFollowSmoothTime);
            targetCamera.transform.rotation = fixedRotation;
        }

        private void SnapToTargetImmediate()
        {
            Vector3 focusPoint = followTarget.position + lookOffset;
            targetCamera.transform.position = focusPoint + new Vector3(
                0f,
                GameConstants.CameraHeightMeters,
                -GameConstants.CameraBackOffsetMeters);
            targetCamera.transform.rotation = fixedRotation;
            followVelocity = Vector3.zero;
        }

        private Vector3 GetGroundForward()
        {
            Vector3 forward = targetCamera.transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        }

        private Vector3 GetGroundRight()
        {
            Vector3 right = targetCamera.transform.right;
            right.y = 0f;
            return right.sqrMagnitude > 0.001f ? right.normalized : Vector3.right;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
