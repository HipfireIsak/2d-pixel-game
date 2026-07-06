using System.Collections;
using Mirror;
using UnityEngine;
using AetherEcho.Content;
using AetherEcho.Core;
using AetherEcho.Data;
using AetherEcho.World;

namespace AetherEcho.Combat
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class ManaSurgeZone : NetworkBehaviour
    {
        private CombatantState pendingCaster;
        private SpellData pendingSpell;

        private CombatantState caster;
        private SpellData spellData;
        private float radius;
        private float duration;
        private float tickInterval;
        private int tickDamage;
        private int tickHeal;

        [Server]
        public static void ServerSpawn(CombatantState casterState, SpellData spell)
        {
            var zoneObject = new GameObject("ManaSurgeZone");
            zoneObject.transform.position = FlatMovementUtility.SnapToGround(casterState.transform.position);
            zoneObject.AddComponent<NetworkIdentity>();
            ManaSurgeZone zone = zoneObject.AddComponent<ManaSurgeZone>();
            zone.pendingCaster = casterState;
            zone.pendingSpell = spell;
            NetworkServer.Spawn(zoneObject);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (pendingSpell == null)
            {
                return;
            }

            ServerInitialize(pendingCaster, pendingSpell);
            pendingCaster = null;
            pendingSpell = null;
        }

        [Server]
        private void ServerInitialize(CombatantState casterState, SpellData spell)
        {
            caster = casterState;
            spellData = spell;
            radius = spell.targeting.radius_meters;
            duration = spell.payload.duration_seconds > 0f ? spell.payload.duration_seconds : 4f;
            tickInterval = spell.payload.tick_interval_seconds > 0f ? spell.payload.tick_interval_seconds : 0.5f;
            tickDamage = spell.payload.tick_damage_magical > 0
                ? spell.payload.tick_damage_magical
                : Mathf.RoundToInt(18f + caster.GetStatValue("Intelligence") * 0.35f);
            tickHeal = spell.payload.tick_heal > 0
                ? spell.payload.tick_heal
                : Mathf.RoundToInt(12f + caster.GetStatValue("Intelligence") * 0.25f);
            StartCoroutine(ServerTickRoutine());
        }

        [Server]
        private IEnumerator ServerTickRoutine()
        {
            yield return null;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                ApplyTick();
                yield return new WaitForSeconds(tickInterval);
                elapsed += tickInterval;
            }

            NetworkServer.Destroy(gameObject);
        }

        [Server]
        private void ApplyTick()
        {
            if (caster == null)
            {
                return;
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider hit in hits)
            {
                CombatantState target = hit.GetComponentInParent<CombatantState>();
                if (target == null)
                {
                    continue;
                }

                if (target.IsEnemyWith(caster))
                {
                    target.TakeDamage(tickDamage, DamageType.Magical, caster);
                }
                else if (target == caster || !target.IsEnemyWith(caster))
                {
                    target.ServerHeal(tickHeal, caster);
                }
            }

            if (netIdentity != null)
            {
                RpcPulseFx(transform.position, radius);
            }
        }

        [ClientRpc]
        private void RpcPulseFx(Vector3 center, float pulseRadius)
        {
            if (Vfx.SpellVfxPlayer.Instance != null)
            {
                Vfx.SpellVfxPlayer.Instance.PlaySpell(GameConstants.SpellManaSurge, center, center, Vector3.forward);
            }
        }
    }
}
