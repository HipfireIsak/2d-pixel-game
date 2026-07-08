using Mirror;
using UnityEngine;
using AetherEcho.Core;
using AetherEcho.Data;
using AetherEcho.Networking;
using AetherEcho.Rendering;
using AetherEcho.World;

namespace AetherEcho.Combat
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class SpellProjectile : NetworkBehaviour
    {
        [SerializeField] private float defaultSpeed = 18f;
        [SerializeField] private float hitRadius = 0.48f;

        private SpellData spellData;
        private CombatantState caster;
        private Vector3 direction;
        private Vector3 previousGroundPosition;
        private float distanceTraveled;
        private float maxDistance = 12f;
        private bool hasHit;
        private uint homingTargetNetId;
        private SpriteRenderer visualRenderer;
        private FlatMovementNetworkSync movementSync;

        private void Awake()
        {
            movementSync = FlatMovementNetworkSync.Ensure(gameObject, MovementSyncMode.ServerAuthority);
        }

        [Server]
        public void ServerInitializeHoming(CombatantState casterState, SpellData spell, Vector3 aimDirection, uint targetNetId)
        {
            homingTargetNetId = targetNetId;
            ServerInitialize(casterState, spell, aimDirection);
        }

        [Server]
        public void ServerInitialize(CombatantState casterState, SpellData spell, Vector3 aimDirection)
        {
            caster = casterState;
            spellData = spell;
            direction = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : caster.transform.forward;
            direction.y = 0f;
            direction.Normalize();
            maxDistance = spell.targeting.range_meters;
            distanceTraveled = 0f;
            hasHit = false;
            previousGroundPosition = FlatMovementUtility.SnapToGround(caster.transform.position);

            EnsureVisual(spell.id);
            Vector3 spawn = FlatMovementUtility.SnapToGround(caster.transform.position);
            transform.position = spawn + Vector3.up * 0.75f + direction * 0.55f;
        }

        private void EnsureVisual(string spellId)
        {
            if (visualRenderer != null)
            {
                return;
            }

            ArtCatalog art = ArtAssetResolver.Catalog;
            Sprite sprite = art != null ? art.spellBeam : null;
            if (spellId == GameConstants.SpellChronoBlast && art != null)
            {
                sprite = art.spellBurst;
            }

            var visualObject = new GameObject("ProjectileVisual");
            visualObject.transform.SetParent(transform, false);
            visualRenderer = visualObject.AddComponent<SpriteRenderer>();
            visualRenderer.sprite = sprite;
            visualRenderer.sortingOrder = 600;
            visualRenderer.color = spellId == GameConstants.SpellTemporalBolt
                ? new Color(0.85f, 0.45f, 1f, 1f)
                : new Color(0.35f, 0.85f, 1f, 1f);
            visualObject.transform.localScale = Vector3.one * 1.1f;
            visualObject.AddComponent<CameraBillboard>();
        }

        private void Update()
        {
            if (!isServer || hasHit || spellData == null || caster == null)
            {
                return;
            }

            float step = defaultSpeed * Time.deltaTime;
            if (homingTargetNetId != 0
                && NetworkServer.spawned.TryGetValue(homingTargetNetId, out NetworkIdentity targetIdentity)
                && targetIdentity != null)
            {
                Vector3 toTarget = targetIdentity.transform.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.05f)
                {
                    direction = Vector3.Slerp(direction, toTarget.normalized, Time.deltaTime * 10f);
                    direction.y = 0f;
                    direction.Normalize();
                }
            }

            Vector3 segmentStart = previousGroundPosition;
            Vector3 groundPos = FlatMovementUtility.SnapToGround(transform.position + direction * step);
            transform.position = groundPos + Vector3.up * 0.75f;
            distanceTraveled += step;

            if (TryHitAlongSegment(segmentStart, groundPos))
            {
                return;
            }

            previousGroundPosition = groundPos;

            if (distanceTraveled >= maxDistance)
            {
                ServerResolveImpact(transform.position);
            }
        }

        private void LateUpdate()
        {
            if (visualRenderer != null)
            {
                CameraBillboard.Apply(visualRenderer.transform, lockYAxis: true);
            }

            if (isServer && movementSync != null && !hasHit)
            {
                movementSync.SubmitServerTransform(transform.position, transform.rotation, spellData != null ? spellData.id : "projectile");
            }
        }

        [Server]
        private bool TryHitAlongSegment(Vector3 segmentStart, Vector3 segmentEnd)
        {
            float spellRadius = spellData != null && spellData.targeting.radius_meters > 0f
                ? spellData.targeting.radius_meters
                : hitRadius;
            float hitReach = spellRadius + GameConstants.EnemyCollisionRadius;
            float segmentLength = HorizontalDistance(segmentStart, segmentEnd);

            CombatantState bestTarget = null;
            float bestAlongDistance = float.MaxValue;
            CombatantState[] combatants = FindObjectsOfType<CombatantState>();
            foreach (CombatantState combatant in combatants)
            {
                if (!IsValidProjectileTarget(combatant))
                {
                    continue;
                }

                Vector3 enemyGround = FlatMovementUtility.SnapToGround(combatant.transform.position);
                float distanceToSegment = DistancePointToSegmentXZ(
                    enemyGround,
                    segmentStart,
                    segmentEnd,
                    out float tAlong);
                if (distanceToSegment > hitReach)
                {
                    continue;
                }

                float alongDistance = tAlong * segmentLength;
                if (alongDistance < bestAlongDistance)
                {
                    bestAlongDistance = alongDistance;
                    bestTarget = combatant;
                }
            }

            if (bestTarget != null)
            {
                ServerResolveImpact(bestTarget.transform.position, bestTarget);
                return true;
            }

            Vector3 castDelta = segmentEnd - segmentStart;
            float castDistance = castDelta.magnitude;
            if (castDistance > 0.001f
                && Physics.SphereCast(
                    segmentStart + Vector3.up * 0.5f,
                    spellRadius * 0.5f,
                    castDelta.normalized,
                    out RaycastHit obstacleHit,
                    castDistance,
                    1 << GameConstants.ObstacleLayerIndex,
                    QueryTriggerInteraction.Ignore))
            {
                ServerResolveImpact(obstacleHit.point);
                return true;
            }

            return false;
        }

        [Server]
        private bool IsValidProjectileTarget(CombatantState combatant)
        {
            return combatant != null
                   && combatant != caster
                   && combatant.CurrentHealth > 0
                   && combatant.IsEnemyWith(caster);
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to);
        }

        private static float DistancePointToSegmentXZ(
            Vector3 point,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            out float tAlong)
        {
            Vector2 p = new Vector2(point.x, point.z);
            Vector2 a = new Vector2(segmentStart.x, segmentStart.z);
            Vector2 b = new Vector2(segmentEnd.x, segmentEnd.z);
            Vector2 ab = b - a;
            float abSqr = ab.sqrMagnitude;
            if (abSqr < 0.0001f)
            {
                tAlong = 0f;
                return Vector2.Distance(p, a);
            }

            tAlong = Mathf.Clamp01(Vector2.Dot(p - a, ab) / abSqr);
            Vector2 closest = a + ab * tAlong;
            return Vector2.Distance(p, closest);
        }

        [Server]
        private void ServerResolveImpact(Vector3 impactPoint, CombatantState directTarget = null)
        {
            if (hasHit)
            {
                return;
            }

            hasHit = true;
            if (SpellEngine.Instance != null)
            {
                SpellEngine.Instance.ServerApplyProjectileImpact(caster, spellData, impactPoint, direction, directTarget);
            }

            RpcPlayImpactFx(spellData.id, impactPoint);
            NetworkServer.Destroy(gameObject);
        }

        [ClientRpc]
        private void RpcPlayImpactFx(string spellId, Vector3 impactPoint)
        {
            if (Vfx.SpellVfxPlayer.Instance != null)
            {
                Vfx.SpellVfxPlayer.Instance.PlayImpact(spellId, impactPoint);
            }
        }
    }
}
