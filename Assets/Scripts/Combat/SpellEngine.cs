using System.Collections.Generic;
using Mirror;
using UnityEngine;
using AetherEcho.Content;
using AetherEcho.Core;
using AetherEcho.Data;
using AetherEcho.Player;

namespace AetherEcho.Combat
{
    public class SpellEngine : MonoBehaviour
    {
        public static SpellEngine Instance { get; private set; }

        private SpellContentManager spellContentManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            spellContentManager = SpellContentManager.Instance;
        }

        public bool CanPlayerCast(CombatantState player, string spellId, out string failureReason)
        {
            failureReason = string.Empty;
            if (player == null)
            {
                failureReason = "Invalid caster.";
                return false;
            }

            if (spellContentManager == null || !spellContentManager.TryGetSpell(spellId, out SpellData spell))
            {
                failureReason = "Spell does not exist.";
                return false;
            }

            if (!spell.allowed_classes.Contains(player.CharacterClass))
            {
                failureReason = "Class '" + player.CharacterClass + "' cannot cast this spell.";
                return false;
            }

            if (player.Level < spell.required_level)
            {
                failureReason = "Requires level " + spell.required_level + ".";
                return false;
            }

            if (player.IsSpellOnCooldown(spellId))
            {
                failureReason = "Spell is on cooldown.";
                return false;
            }

            if (!player.HasResource(spell.casting_rules.resource_type, spell.casting_rules.resource_cost))
            {
                failureReason = "Not enough " + spell.casting_rules.resource_type + ".";
                return false;
            }

            return true;
        }

        public void ServerExecuteSpell(CombatantState caster, string spellId, Vector3 targetPoint, Vector3 aimDirection, uint targetNetId = 0)
        {
            if (caster == null || !caster.isServer)
            {
                return;
            }

            if (!CanPlayerCast(caster, spellId, out _))
            {
                return;
            }

            if (!spellContentManager.TryGetSpell(spellId, out SpellData spell))
            {
                return;
            }

            caster.ConsumeResource(spell.casting_rules.resource_type, spell.casting_rules.resource_cost);
            caster.TriggerCooldown(spellId, spell.casting_rules.cooldown_seconds);

            NetworkedCombatant networkedCaster = caster.GetComponent<NetworkedCombatant>();
            if (networkedCaster != null && networkedCaster.connectionToClient != null)
            {
                networkedCaster.TargetNotifySpellCast(
                    networkedCaster.connectionToClient,
                    spellId,
                    spell.casting_rules.cooldown_seconds);
            }

            if (spell.targeting.type == "Blink")
            {
                ServerExecuteBlink(caster, spell, targetPoint, aimDirection);
                return;
            }

            if (spell.payload.duration_seconds > 0f && spell.targeting.type == "SelfAOE")
            {
                ManaSurgeZone.ServerSpawn(caster, spell);
                return;
            }

            if (spell.targeting.type == "DirectionalLine" || spell.targeting.type == "UnitTarget")
            {
                ServerLaunchProjectile(caster, spell, aimDirection, targetNetId);
                return;
            }

            Vector3 impactPoint = ResolveImpactPoint(caster.transform.position, targetPoint, aimDirection, spell);
            ApplySpellImpact(caster, spell, impactPoint, aimDirection);
        }

        [Server]
        private void ServerExecuteBlink(CombatantState caster, SpellData spell, Vector3 targetPoint, Vector3 aimDirection)
        {
            Vector3 direction = aimDirection.sqrMagnitude > 0.001f ? aimDirection : caster.transform.forward;
            direction.y = 0f;
            direction.Normalize();
            Vector3 destination = caster.transform.position + direction * spell.targeting.range_meters;
            if (targetPoint.sqrMagnitude > 0.001f)
            {
                Vector3 toPoint = targetPoint - caster.transform.position;
                toPoint.y = 0f;
                if (toPoint.magnitude > 0.5f)
                {
                    destination = caster.transform.position + toPoint.normalized * Mathf.Min(toPoint.magnitude, spell.targeting.range_meters);
                }
            }

            destination = FlatMovementUtility.SnapToGround(destination);
            NetworkedCombatant networkedCaster = caster.GetComponent<NetworkedCombatant>();
            networkedCaster?.ServerTeleport(destination);
        }

        [Server]
        public void ServerApplyProjectileImpact(
            CombatantState caster,
            SpellData spell,
            Vector3 impactPoint,
            Vector3 aimDirection,
            CombatantState directTarget)
        {
            if (directTarget != null && directTarget.IsEnemyWith(caster))
            {
                ApplyDamageToTarget(caster, spell, directTarget);
            }
            else
            {
                ApplySpellImpact(caster, spell, impactPoint, aimDirection);
            }

            if (spell.payload.spawn_entity_on_impact != null
                && !string.IsNullOrWhiteSpace(spell.payload.spawn_entity_on_impact.blueprint_id))
            {
                Vfx.TimeEchoEntity.Spawn(impactPoint, spell.payload.spawn_entity_on_impact.duration_seconds);
            }
        }

        [Server]
        private void ServerLaunchProjectile(CombatantState caster, SpellData spell, Vector3 aimDirection, uint targetNetId = 0)
        {
            GameObject prefab = Resources.Load<GameObject>("Spells/SpellProjectile");
            if (prefab == null)
            {
                Vector3 fallbackPoint = caster.transform.position + aimDirection.normalized * spell.targeting.range_meters;
                ApplySpellImpact(caster, spell, fallbackPoint, aimDirection);
                return;
            }

            GameObject projectileObject = Instantiate(prefab, caster.transform.position, Quaternion.identity);
            SpellProjectile projectile = projectileObject.GetComponent<SpellProjectile>();
            if (projectile == null)
            {
                Destroy(projectileObject);
                return;
            }

            if (targetNetId != 0)
            {
                projectile.ServerInitializeHoming(caster, spell, aimDirection, targetNetId);
            }
            else
            {
                projectile.ServerInitialize(caster, spell, aimDirection);
            }

            NetworkServer.Spawn(projectileObject);
        }

        private static Vector3 ResolveImpactPoint(
            Vector3 casterPosition,
            Vector3 targetPoint,
            Vector3 aimDirection,
            SpellData spell)
        {
            string targetingType = spell.targeting.type;
            if (targetingType == "DirectionalLine")
            {
                Vector3 direction = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : Vector3.forward;
                return casterPosition + direction * spell.targeting.range_meters;
            }

            if (targetingType == "Cone" || targetingType == "SelfAOE")
            {
                return casterPosition;
            }

            float distance = Vector3.Distance(casterPosition, targetPoint);
            if (distance > spell.targeting.range_meters)
            {
                Vector3 direction = (targetPoint - casterPosition).normalized;
                return casterPosition + direction * spell.targeting.range_meters;
            }

            return targetPoint;
        }

        private void ApplySpellImpact(CombatantState caster, SpellData spell, Vector3 centerPoint, Vector3 aimDirection)
        {
            if (spell.targeting.type == "SelfAOE")
            {
                centerPoint = caster.transform.position;
            }

            List<CombatantState> candidates = QueryTargets(caster, spell, centerPoint, aimDirection);
            int targetsHit = 0;

            foreach (CombatantState target in candidates)
            {
                if (targetsHit >= spell.targeting.max_targets)
                {
                    break;
                }

                if (!IsValidTarget(caster, target, spell))
                {
                    continue;
                }

                if (target.IsEnemyWith(caster))
                {
                    ApplyDamageToTarget(caster, spell, target);
                }
                else if (spell.targeting.type == "SelfAOE")
                {
                    int healAmount = Mathf.RoundToInt(20f + caster.GetStatValue("Intelligence") * 0.5f);
                    target.ServerHeal(healAmount, caster);
                }

                targetsHit++;
            }
        }

        private static void ApplyDamageToTarget(CombatantState caster, SpellData spell, CombatantState target)
        {
            int totalDamage = CalculateDamage(caster, spell);
            if (totalDamage > 0)
            {
                target.TakeDamage(totalDamage, DamageType.Magical, caster);
            }

            if (spell.payload.applied_status_effects == null)
            {
                return;
            }

            foreach (StatusEffectReference effect in spell.payload.applied_status_effects)
            {
                target.ApplyStatusEffect(effect.effect_id, effect.duration_seconds, effect.potency);
            }
        }

        private static int CalculateDamage(CombatantState caster, SpellData spell)
        {
            float baseDamage = spell.payload.instant_damage_magical + spell.payload.instant_damage_physical;
            float statModifier = caster.GetStatValue(spell.payload.damage_scaling_stat) * spell.payload.scaling_factor;
            return Mathf.RoundToInt(baseDamage + statModifier);
        }

        private static bool IsValidTarget(CombatantState caster, CombatantState target, SpellData spell)
        {
            if (target == null)
            {
                return false;
            }

            if (target == caster)
            {
                foreach (string relationName in spell.targeting.valid_relations)
                {
                    if (relationName == "Self")
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (string relationName in spell.targeting.valid_relations)
            {
                if (relationName == "Enemy" && target.IsEnemyWith(caster))
                {
                    return true;
                }

                if (relationName == "Ally" && !target.IsEnemyWith(caster))
                {
                    return true;
                }

                if (relationName == "Destructible" && target.MatchesRelation("Destructible"))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<CombatantState> QueryTargets(
            CombatantState caster,
            SpellData spell,
            Vector3 centerPoint,
            Vector3 aimDirection)
        {
            List<CombatantState> results = new List<CombatantState>();
            Vector3 queryCenter = spell.targeting.type == "SelfAOE" ? caster.transform.position : centerPoint;
            Collider[] hitColliders = Physics.OverlapSphere(queryCenter, spell.targeting.radius_meters);
            Vector3 forward = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : caster.transform.forward;

            if (spell.targeting.type == "SelfAOE")
            {
                results.Add(caster);
            }

            foreach (Collider collider in hitColliders)
            {
                CombatantState combatant = collider.GetComponentInParent<CombatantState>();
                if (combatant == null || results.Contains(combatant))
                {
                    continue;
                }

                if (spell.targeting.type == "Cone")
                {
                    Vector3 toTarget = combatant.transform.position - caster.transform.position;
                    toTarget.y = 0f;
                    if (toTarget.magnitude > spell.targeting.range_meters)
                    {
                        continue;
                    }

                    float halfAngle = spell.targeting.radius_meters * 0.5f;
                    float angle = Vector3.Angle(forward, toTarget.normalized);
                    if (angle > halfAngle)
                    {
                        continue;
                    }
                }

                results.Add(combatant);
            }

            return results;
        }
    }
}
