using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Content;
using AetherEcho.Core;
using AetherEcho.Data;
using AetherEcho.Player;

namespace AetherEcho.UI
{
    /// <summary>
    /// WoW-style ground targeting: press spell hotkey, move reticle, click to confirm.
    /// </summary>
    public class SpellGroundTargeting : MonoBehaviour
    {
        public static SpellGroundTargeting Instance { get; private set; }

        private NetworkedCombatant localPlayer;
        private IsometricPlayerController movementController;
        private SpellData activeSpell;
        private string activeSpellId;
        private Vector3 reticleWorldPoint;
        private bool isTargeting;

        private GUIStyle hintStyle;
        private GUIStyle spellTitleStyle;
        private Texture2D circleTexture;

        private void Awake()
        {
            Instance = this;
        }

        public void BindLocalPlayer(NetworkedCombatant player, IsometricPlayerController controller)
        {
            localPlayer = player;
            movementController = controller;
        }

        public void BeginTargeting(string spellId)
        {
            if (localPlayer == null || SpellContentManager.Instance == null)
            {
                return;
            }

            if (!SpellContentManager.Instance.TryGetSpell(spellId, out SpellData spell))
            {
                GameplayHud.Instance?.SetToast("Unknown spell.");
                return;
            }

            if (localPlayer.GetLocalCooldownRemaining(spellId) > 0.01f)
            {
                GameplayHud.Instance?.SetToast("Spell is on cooldown.");
                return;
            }

            if (!SpellEngine.Instance.CanPlayerCast(localPlayer.CombatantState, spellId, out string reason))
            {
                GameplayHud.Instance?.SetToast(reason);
                return;
            }

            activeSpellId = spellId;
            activeSpell = spell;
            isTargeting = true;
            reticleWorldPoint = GetInitialReticlePoint(spell);
        }

        public bool IsTargeting => isTargeting;

        private void Update()
        {
            if (!isTargeting || localPlayer == null || activeSpell == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelTargeting();
                return;
            }

            reticleWorldPoint = ResolveReticlePoint(activeSpell);

            if (Input.GetMouseButtonDown(0))
            {
                ConfirmCast();
            }
        }

        private void ConfirmCast()
        {
            Vector3 casterPos = localPlayer.transform.position;
            Vector3 targetPoint;
            Vector3 aimDirection;

            if (activeSpell.targeting.type == "SelfAOE")
            {
                targetPoint = casterPos;
                aimDirection = localPlayer.transform.forward;
            }
            else
            {
                targetPoint = reticleWorldPoint;
                aimDirection = targetPoint - casterPos;
                aimDirection.y = 0f;
                if (aimDirection.sqrMagnitude < 0.001f)
                {
                    aimDirection = localPlayer.transform.forward;
                }
                else
                {
                    aimDirection.Normalize();
                }
            }

            if (!localPlayer.TryLocalCast(activeSpellId, targetPoint, aimDirection, out string failureReason))
            {
                GameplayHud.Instance?.SetToast(failureReason);
            }

            CancelTargeting();
        }

        private void CancelTargeting()
        {
            isTargeting = false;
            activeSpell = null;
            activeSpellId = null;
        }

        private Vector3 GetInitialReticlePoint(SpellData spell)
        {
            if (spell.targeting.type == "SelfAOE")
            {
                return localPlayer.transform.position;
            }

            return ResolveReticlePoint(spell);
        }

        private Vector3 ResolveReticlePoint(SpellData spell)
        {
            Vector3 casterPos = localPlayer.transform.position;
            if (spell.targeting.type == "SelfAOE")
            {
                return casterPos;
            }

            if (!TryGetGroundPointUnderCursor(out Vector3 groundPoint))
            {
                return casterPos + localPlayer.transform.forward * Mathf.Min(4f, spell.targeting.range_meters);
            }

            Vector3 offset = groundPoint - casterPos;
            offset.y = 0f;
            float maxRange = spell.targeting.range_meters;
            if (offset.magnitude > maxRange)
            {
                offset = offset.normalized * maxRange;
            }

            return casterPos + offset;
        }

        private static bool TryGetGroundPointUnderCursor(out Vector3 groundPoint)
        {
            groundPoint = Vector3.zero;
            Camera camera = Camera.main;
            if (camera == null)
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            var groundPlane = new Plane(Vector3.up, new Vector3(0f, GameConstants.GroundHeight, 0f));
            if (!groundPlane.Raycast(ray, out float distance))
            {
                return false;
            }

            groundPoint = ray.GetPoint(distance);
            groundPoint.y = GameConstants.GroundHeight;
            return true;
        }

        private void OnGUI()
        {
            if (!isTargeting || activeSpell == null)
            {
                return;
            }

            EnsureStyles();

            string modeLabel = activeSpell.targeting.type == "SelfAOE"
                ? "Self AOE — click to cast"
                : "Ground target — click to cast";
            GUI.Label(new Rect(Screen.width * 0.5f - 180f, 24f, 360f, 28f), activeSpell.name, spellTitleStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 180f, 50f, 360f, 24f), modeLabel, hintStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 180f, 72f, 360f, 24f), "Esc / RMB to cancel", hintStyle);

            DrawWorldReticle(reticleWorldPoint, activeSpell.targeting.radius_meters, activeSpell.targeting.type == "SelfAOE");
        }

        private void DrawWorldReticle(Vector3 worldCenter, float radiusMeters, bool filledArea)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 screenCenter = camera.WorldToScreenPoint(worldCenter);
            if (screenCenter.z < 0f)
            {
                return;
            }

            screenCenter.y = Screen.height - screenCenter.y;
            Vector3 screenEdge = camera.WorldToScreenPoint(worldCenter + Vector3.right * radiusMeters);
            screenEdge.y = Screen.height - screenEdge.y;
            float pixelRadius = Mathf.Abs(screenEdge.x - screenCenter.x);
            pixelRadius = Mathf.Clamp(pixelRadius, 18f, 220f);

            Color ringColor = filledArea
                ? new Color(0.35f, 0.85f, 1f, 0.35f)
                : new Color(0.95f, 0.85f, 0.2f, 0.85f);
            DrawCircle(new Vector2(screenCenter.x, screenCenter.y), pixelRadius, ringColor, filledArea);

            if (localPlayer != null && activeSpell.targeting.type != "SelfAOE")
            {
                Vector3 playerScreen = camera.WorldToScreenPoint(localPlayer.transform.position);
                playerScreen.y = Screen.height - playerScreen.y;
                DrawLine(new Vector2(playerScreen.x, playerScreen.y), new Vector2(screenCenter.x, screenCenter.y), ringColor);
            }
        }

        private void DrawCircle(Vector2 center, float radius, Color color, bool filled)
        {
            EnsureCircleTexture();
            Color previous = GUI.color;
            GUI.color = color;
            var rect = new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f);
            if (filled)
            {
                GUI.DrawTexture(rect, circleTexture, ScaleMode.StretchToFill, true);
            }
            else
            {
                GUI.DrawTexture(rect, circleTexture, ScaleMode.StretchToFill, true);
            }

            GUI.color = previous;
        }

        private void DrawLine(Vector2 from, Vector2 to, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 1f)
            {
                return;
            }

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Color previous = GUI.color;
            GUI.color = color;
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, from);
            GUI.DrawTexture(new Rect(from.x, from.y - 1f, length, 3f), Texture2D.whiteTexture);
            GUI.matrix = matrix;
            GUI.color = previous;
        }

        private void EnsureCircleTexture()
        {
            if (circleTexture != null)
            {
                return;
            }

            circleTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            circleTexture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[64 * 64];
            Vector2 center = new Vector2(31.5f, 31.5f);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = dist > 30f && dist < 32f ? 1f : (dist < 30f ? 0.25f : 0f);
                    pixels[y * 64 + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            circleTexture.SetPixels(pixels);
            circleTexture.Apply();
        }

        private void EnsureStyles()
        {
            if (hintStyle != null)
            {
                return;
            }

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.92f, 0.94f, 1f) }
            };
            spellTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.55f, 0.9f, 1f) }
            };
        }
    }
}
