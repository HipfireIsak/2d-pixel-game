using UnityEngine;
using AetherEcho.Core;
using AetherEcho.Rendering;

namespace AetherEcho.Vfx
{
    public class SpellVfxPlayer : MonoBehaviour
    {
        public static SpellVfxPlayer Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void PlaySpell(string spellId, Vector3 origin, Vector3 targetPoint, Vector3 aimDirection)
        {
            if (spellId == GameConstants.SpellManaSurge)
            {
                ArtCatalog art = ArtAssetResolver.Catalog;
                if (art != null)
                {
                    StartCoroutine(PlayPulse(origin, art.spellPulse, new Color(0.4f, 1f, 0.7f, 0.85f), 3.5f));
                }
            }
        }

        public void PlayImpact(string spellId, Vector3 impactPoint)
        {
            ArtCatalog art = ArtAssetResolver.Catalog;
            if (art == null)
            {
                return;
            }

            if (spellId == GameConstants.SpellChronoBlast || spellId == GameConstants.SpellTemporalBolt)
            {
                StartCoroutine(PlayBurst(impactPoint, art.spellBurst, new Color(0.2f, 0.9f, 1f, 0.9f)));
            }
        }

        private System.Collections.IEnumerator PlayBurst(Vector3 point, Sprite sprite, Color tint)
        {
            if (sprite == null)
            {
                yield break;
            }

            GameObject burst = CreateSpriteFx("SpellBurst", sprite, tint, 2.2f);
            burst.transform.position = point + Vector3.up * 0.5f;
            burst.AddComponent<CameraBillboard>();
            float elapsed = 0f;
            while (elapsed < 0.45f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.45f;
                burst.transform.localScale = Vector3.one * Mathf.Lerp(1f, 2.8f, t);
                SetAlpha(burst, 1f - t);
                yield return null;
            }

            Destroy(burst);
        }

        private System.Collections.IEnumerator PlayPulse(Vector3 point, Sprite sprite, Color tint, float radius)
        {
            if (sprite == null)
            {
                yield break;
            }

            GameObject pulse = CreateSpriteFx("SpellPulse", sprite, tint, radius);
            pulse.transform.position = point + Vector3.up * 0.35f;
            pulse.AddComponent<CameraBillboard>();
            float elapsed = 0f;
            while (elapsed < 0.55f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.55f;
                pulse.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.4f, t);
                SetAlpha(pulse, 1f - t);
                yield return null;
            }

            Destroy(pulse);
        }

        private static GameObject CreateSpriteFx(string name, Sprite sprite, Color tint, float scale)
        {
            var fxObject = new GameObject(name);
            SpriteRenderer renderer = fxObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingOrder = 500;
            fxObject.transform.localScale = Vector3.one * scale;
            return fxObject;
        }

        private static void SetAlpha(GameObject fxObject, float alpha)
        {
            SpriteRenderer renderer = fxObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                return;
            }

            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }

    public class TimeEchoEntity : MonoBehaviour
    {
        public static TimeEchoEntity Spawn(Vector3 position, float durationSeconds)
        {
            ArtCatalog art = ArtAssetResolver.Catalog;
            var echoObject = new GameObject("TimeEcho");
            echoObject.transform.position = position + Vector3.up * 0.4f;
            SpriteRenderer renderer = echoObject.AddComponent<SpriteRenderer>();
            renderer.sprite = art != null ? art.timeEcho : null;
            renderer.color = new Color(0.45f, 0.95f, 1f, 0.85f);
            renderer.sortingOrder = 250;
            echoObject.transform.localScale = Vector3.one * 1.8f;
            echoObject.AddComponent<CameraBillboard>();
            TimeEchoEntity echo = echoObject.AddComponent<TimeEchoEntity>();
            echo.StartCoroutine(echo.FadeAndDestroy(durationSeconds));
            return echo;
        }

        private System.Collections.IEnumerator FadeAndDestroy(float durationSeconds)
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            float elapsed = 0f;
            while (elapsed < durationSeconds)
            {
                elapsed += Time.deltaTime;
                if (renderer != null)
                {
                    Color color = renderer.color;
                    color.a = Mathf.Lerp(0.85f, 0.15f, elapsed / durationSeconds);
                    renderer.color = color;
                }

                CameraBillboard.Apply(transform, lockYAxis: true);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
