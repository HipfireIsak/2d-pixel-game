using UnityEngine;
using AetherEcho.Rendering;

namespace AetherEcho.Vfx
{
    public static class EnemyDeathVfx
    {
        public static Color ResolveTint(string enemyTypeId, Sprite sprite, Color rendererTint)
        {
            Color sampled = SampleSpriteColor(sprite);
            if (sampled.a > 0.05f)
            {
                Color combined = sampled * rendererTint;
                combined.a = 1f;
                return combined;
            }

            Color fallback = GetFallbackTint(enemyTypeId);
            return Color.Lerp(fallback, rendererTint, 0.35f);
        }

        public static Color GetFallbackTint(string enemyTypeId)
        {
            switch (enemyTypeId)
            {
                case "slime": return new Color(0.35f, 0.85f, 0.4f);
                case "skeleton": return new Color(0.9f, 0.88f, 0.82f);
                case "bat": return new Color(0.45f, 0.35f, 0.55f);
                case "rat": return new Color(0.55f, 0.45f, 0.4f);
                case "snake": return new Color(0.35f, 0.7f, 0.35f);
                case "eye": return new Color(0.85f, 0.25f, 0.25f);
                case "sunflower": return new Color(0.95f, 0.8f, 0.2f);
                case "vault_warden": return new Color(0.75f, 0.55f, 0.95f);
                default: return new Color(0.6f, 0.75f, 0.9f);
            }
        }

        private static Color SampleSpriteColor(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return Color.clear;
            }

            Texture2D texture = sprite.texture;
            if (!texture.isReadable)
            {
                return Color.clear;
            }

            Rect rect = sprite.textureRect;
            int sampleX = Mathf.Clamp((int)(rect.x + rect.width * 0.5f), 0, texture.width - 1);
            int sampleY = Mathf.Clamp((int)(rect.y + rect.height * 0.5f), 0, texture.height - 1);
            Color center = texture.GetPixel(sampleX, sampleY);
            if (center.a > 0.1f)
            {
                return center;
            }

            float r = 0f;
            float g = 0f;
            float b = 0f;
            int count = 0;
            int step = Mathf.Max(1, (int)(Mathf.Min(rect.width, rect.height) / 8f));
            int startX = (int)rect.x;
            int startY = (int)rect.y;
            int endX = startX + (int)rect.width;
            int endY = startY + (int)rect.height;

            for (int y = startY; y < endY; y += step)
            {
                for (int x = startX; x < endX; x += step)
                {
                    Color pixel = texture.GetPixel(x, y);
                    if (pixel.a < 0.1f)
                    {
                        continue;
                    }

                    r += pixel.r;
                    g += pixel.g;
                    b += pixel.b;
                    count++;
                }
            }

            if (count == 0)
            {
                return Color.clear;
            }

            return new Color(r / count, g / count, b / count, 1f);
        }
    }
}
