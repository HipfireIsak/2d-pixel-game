using UnityEngine;

namespace AetherEcho.Rendering
{
    public class CatalogSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 8f;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool playOnAwake = true;

        private int frameIndex;
        private float frameTimer;
        private bool playing;

        public void Configure(SpriteRenderer renderer, Sprite[] animationFrames, float fps, bool shouldLoop = true)
        {
            targetRenderer = renderer;
            frames = animationFrames;
            framesPerSecond = Mathf.Max(0.1f, fps);
            loop = shouldLoop;
            frameIndex = 0;
            frameTimer = 0f;
            playing = frames != null && frames.Length > 0;
            ApplyFrame();
        }

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<SpriteRenderer>();
            }

            if (playOnAwake)
            {
                playing = frames != null && frames.Length > 0;
                ApplyFrame();
            }
        }

        private void Update()
        {
            if (!playing || frames == null || frames.Length == 0)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / framesPerSecond;
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                frameIndex++;
                if (frameIndex >= frames.Length)
                {
                    if (loop)
                    {
                        frameIndex = 0;
                    }
                    else
                    {
                        frameIndex = frames.Length - 1;
                        playing = false;
                        break;
                    }
                }

                ApplyFrame();
            }
        }

        private void ApplyFrame()
        {
            if (targetRenderer == null || frames == null || frames.Length == 0)
            {
                return;
            }

            int index = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
            Sprite frame = frames[index];
            if (frame != null)
            {
                targetRenderer.sprite = frame;
            }
        }
    }
}
