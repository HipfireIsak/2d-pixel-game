using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Core;
using AetherEcho.Data;
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
        private float distanceTraveled;
        private float maxDistance = 12f;
        private bool hasHit;
        private SpriteRenderer visualRenderer;

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
            Vector3 groundPos = FlatMovementUtility.SnapToGround(transform.position + direction * step);
            transform.position = groundPos + Vector3.up * 0.75f;
            distanceTraveled += step;

            if (TryHitAlongPath(step))
            {
                return;
            }

            if (distanceTraveled >= maxDistance)
            {
                ServerResolveImpact(transform.position);
            }
        }

        private void LateUpdate()
        {
            if (visualRenderer != null)
            {
                CameraBillboard.Apply(visualRenderer.transform);
            }
        }

        [Server]
        private bool TryHitAlongPath(float stepDistance)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius);
            foreach (Collider hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                CombatantState combatant = hit.GetComponentInParent<CombatantState>();
                if (combatant != null)
                {
                    if (combatant == caster || !combatant.IsEnemyWith(caster))
                    {
                        continue;
                    }

                    ServerResolveImpact(combatant.transform.position, combatant);
                    return true;
                }

                if (hit.gameObject.layer == GameConstants.ObstacleLayerIndex)
                {
                    ServerResolveImpact(transform.position);
                    return true;
                }
            }

            return false;
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
