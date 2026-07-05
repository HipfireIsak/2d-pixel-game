using UnityEngine;
using AetherEcho.World;

namespace AetherEcho.Rendering
{
    public class PixelBillboardVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.9f, 0f);
        [SerializeField] private float spriteScale = 1.6f;
        [SerializeField] private bool useDirectionalHeroSprites = true;
        [SerializeField] private Sprite fixedSprite;
        [SerializeField] private SpriteFacingMode facingMode = SpriteFacingMode.BillboardY;

        private Transform trackedTransform;
        private Vector3 lastMoveDirection = Vector3.back;
        private bool isMoving;

        public SpriteRenderer SpriteRenderer => spriteRenderer;

        public void Configure(
            Transform owner,
            Sprite sprite,
            bool directionalHero = false,
            Vector3? offset = null,
            float? scale = null,
            SpriteFacingMode? facing = null)
        {
            trackedTransform = owner != null ? owner : transform;
            fixedSprite = sprite;
            useDirectionalHeroSprites = directionalHero;
            if (facing.HasValue)
            {
                facingMode = facing.Value;
            }

            if (offset.HasValue)
            {
                localOffset = offset.Value;
            }

            if (scale.HasValue)
            {
                spriteScale = scale.Value;
            }

            EnsureRenderer();
            ApplyGroundAnchor(sprite);

            if (!directionalHero && sprite != null)
            {
                ApplySprite(sprite);
            }
        }

        public void SetMoveDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.01f)
            {
                lastMoveDirection = direction;
                isMoving = true;
            }
            else
            {
                isMoving = false;
            }
        }

        public void SetMoving(bool moving)
        {
            isMoving = moving;
        }

        private void Awake()
        {
            EnsureRenderer();
        }

        private void EnsureRenderer()
        {
            if (spriteRenderer != null)
            {
                return;
            }

            var child = new GameObject("Sprite");
            child.transform.SetParent(transform, false);
            child.transform.localPosition = localOffset;
            child.transform.localScale = Vector3.one * spriteScale;
            spriteRenderer = child.AddComponent<SpriteRenderer>();
            if (trackedTransform != null)
            {
                WorldPropBuilder.ApplyDepthSorting(spriteRenderer, trackedTransform.position);
            }
        }

        private void LateUpdate()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            ApplyFacing();

            if (useDirectionalHeroSprites)
            {
                Vector3 viewDirection = CameraBillboard.ToCameraRelativeFlatDirection(lastMoveDirection);
                Sprite directional = ArtAssetResolver.GetHeroSprite(viewDirection);
                if (directional != null)
                {
                    ApplySprite(directional);
                    ApplyGroundAnchor(directional);
                }
            }
            else if (fixedSprite != null)
            {
                ApplySprite(fixedSprite);
            }

            if (trackedTransform != null)
            {
                WorldPropBuilder.ApplyDepthSorting(spriteRenderer, trackedTransform.position);
            }
        }

        private void ApplyFacing()
        {
            switch (facingMode)
            {
                case SpriteFacingMode.FixedSouth:
                    spriteRenderer.transform.localRotation = Quaternion.identity;
                    return;
                case SpriteFacingMode.BillboardYWhenMoving:
                    if (!isMoving)
                    {
                        spriteRenderer.transform.localRotation = Quaternion.identity;
                        return;
                    }

                    break;
            }

            CameraBillboard.Apply(spriteRenderer.transform, lockYAxis: true);
        }

        private void ApplyGroundAnchor(Sprite sprite)
        {
            if (sprite == null || spriteRenderer == null)
            {
                return;
            }

            localOffset = new Vector3(0f, FlatMovementUtility.GetSpriteGroundOffset(sprite, spriteScale), 0f);
            spriteRenderer.transform.localPosition = localOffset;
        }

        private void ApplySprite(Sprite sprite)
        {
            if (sprite == null || spriteRenderer.sprite == sprite)
            {
                return;
            }

            spriteRenderer.sprite = sprite;
        }
    }
}
