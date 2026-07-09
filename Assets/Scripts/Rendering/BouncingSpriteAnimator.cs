using UnityEngine;

namespace AetherEcho.Rendering
{
    public class BouncingSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Transform bounceTarget;
        [SerializeField] private float bounceHeight = 0.35f;
        [SerializeField] private float bounceSpeed = 2.2f;
        [SerializeField] private float phaseOffset;

        private Vector3 baseLocalPosition;
        private bool hasBase;

        public void Configure(Transform target, float height, float speed, float phase = 0f)
        {
            bounceTarget = target;
            bounceHeight = height;
            bounceSpeed = speed;
            phaseOffset = phase;
            CacheBase();
        }

        private void Awake()
        {
            if (bounceTarget == null)
            {
                PixelBillboardVisual visual = GetComponent<PixelBillboardVisual>();
                bounceTarget = visual != null && visual.SpriteRenderer != null
                    ? visual.SpriteRenderer.transform
                    : transform;
            }

            CacheBase();
        }

        private void CacheBase()
        {
            if (bounceTarget == null || hasBase)
            {
                return;
            }

            baseLocalPosition = bounceTarget.localPosition;
            hasBase = true;
        }

        private void LateUpdate()
        {
            if (bounceTarget == null)
            {
                return;
            }

            if (!hasBase)
            {
                CacheBase();
            }

            float offset = Mathf.Sin((Time.time + phaseOffset) * bounceSpeed) * bounceHeight;
            bounceTarget.localPosition = baseLocalPosition + new Vector3(0f, offset, 0f);
        }
    }
}
