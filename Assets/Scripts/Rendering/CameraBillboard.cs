using UnityEngine;
using AetherEcho.Player;

namespace AetherEcho.Rendering
{
    /// <summary>
    /// Keeps sprites upright on the ground while facing the camera (Y-axis billboard).
    /// </summary>
    public class CameraBillboard : MonoBehaviour
    {
        [SerializeField] private bool yAxisOnly = true;
        [SerializeField] private Transform targetTransform;

        private void Awake()
        {
            if (targetTransform == null)
            {
                targetTransform = transform;
            }
        }

        public static Camera ResolveCamera()
        {
            if (LostArkCameraRig.Instance != null && LostArkCameraRig.Instance.TargetCamera != null)
            {
                return LostArkCameraRig.Instance.TargetCamera;
            }

            return Camera.main;
        }

        public static void Apply(Transform billboardTransform, bool lockYAxis = false)
        {
            Camera camera = ResolveCamera();
            if (camera == null || billboardTransform == null)
            {
                return;
            }

            if (lockYAxis)
            {
                Vector3 toCamera = billboardTransform.position - camera.transform.position;
                toCamera.y = 0f;
                if (toCamera.sqrMagnitude > 0.001f)
                {
                    billboardTransform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
                }

                return;
            }

            billboardTransform.rotation = camera.transform.rotation;
        }

        public static Vector3 ToCameraRelativeFlatDirection(Vector3 worldDirection)
        {
            Camera camera = ResolveCamera();
            if (camera == null || worldDirection.sqrMagnitude < 0.001f)
            {
                return worldDirection;
            }

            Vector3 flatForward = camera.transform.forward;
            Vector3 flatRight = camera.transform.right;
            flatForward.y = 0f;
            flatRight.y = 0f;
            flatForward.Normalize();
            flatRight.Normalize();
            float forwardAmount = Vector3.Dot(worldDirection.normalized, flatForward);
            float rightAmount = Vector3.Dot(worldDirection.normalized, flatRight);
            return new Vector3(rightAmount, 0f, forwardAmount);
        }

        private void LateUpdate()
        {
            Apply(targetTransform, yAxisOnly);
        }
    }
}
