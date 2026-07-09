using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Networking;
using AetherEcho.Rendering;
using AetherEcho.Vfx;
using AetherEcho.World;

namespace AetherEcho.Enemies
{
    [DefaultExecutionOrder(250)]
    public class EnemyDeathNotifier : NetworkBehaviour
    {
        private const float DestroyDelaySeconds = 0.1f;

        private CombatantState combatantState;
        private NetworkedEnemy networkedEnemy;
        private FlatMovementNetworkSync movementSync;
        private bool deathHandled;
        private bool visualHidden;

        private void Awake()
        {
            combatantState = GetComponent<CombatantState>();
            networkedEnemy = GetComponent<NetworkedEnemy>();
            movementSync = GetComponent<FlatMovementNetworkSync>();
        }

        private void LateUpdate()
        {
            if (netIdentity == null || !netIdentity.isServer || deathHandled || combatantState == null)
            {
                return;
            }

            if (combatantState.CurrentHealth > 0)
            {
                return;
            }

            deathHandled = true;
            Vector3 deathPosition = ResolveLatestDropPosition();
            Color deathTint = ResolveDeathTint();
            string enemyTypeId = networkedEnemy != null ? networkedEnemy.EnemyTypeId : "slime";

            CombatantState killer = ThreatMatrix.GetHighestThreatTarget(combatantState);
            if (killer != null && networkedEnemy != null)
            {
                int experienceReward = networkedEnemy.ResolveKillExperience();
                killer.ServerGrantExperience(experienceReward);
                networkedEnemy.ServerNotifyKilledBy(killer);
                Items.LootService.Instance?.ServerGrantKillLoot(killer, enemyTypeId, deathPosition);
                if (netIdentity != null)
                {
                    World.MobSpawnZoneManager.Instance?.ServerNotifyEnemyDestroyed(netIdentity.netId);
                }

                uint killerNetId = killer.netIdentity != null ? killer.netIdentity.netId : 0;
                if (killerNetId != 0)
                {
                    RpcPlayXpSoul(deathPosition, killerNetId);
                }
            }

            HideEnemyVisual();
            RpcPlayDeathExplosion(deathPosition, enemyTypeId, deathTint);
            Invoke(nameof(DestroyEnemy), DestroyDelaySeconds);
        }

        [Server]
        private void DestroyEnemy()
        {
            NetworkServer.Destroy(gameObject);
        }

        [ClientRpc]
        private void RpcPlayDeathExplosion(Vector3 position, string enemyTypeId, Color tint)
        {
            HideEnemyVisual();
            SpellVfxPlayer.Instance?.PlayEnemyDeathExplosion(position, enemyTypeId, tint);
        }

        [ClientRpc]
        private void RpcPlayXpSoul(Vector3 origin, uint recipientNetId)
        {
            SpellVfxPlayer.Instance?.PlayXpSoulCollect(origin, recipientNetId);
        }

        private void HideEnemyVisual()
        {
            if (visualHidden)
            {
                return;
            }

            visualHidden = true;

            if (networkedEnemy != null)
            {
                networkedEnemy.enabled = false;
            }

            PixelBillboardVisual visual = GetComponent<PixelBillboardVisual>();
            if (visual != null)
            {
                visual.enabled = false;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private Color ResolveDeathTint()
        {
            string enemyTypeId = networkedEnemy != null ? networkedEnemy.EnemyTypeId : "slime";
            PixelBillboardVisual visual = GetComponent<PixelBillboardVisual>();
            SpriteRenderer renderer = visual != null ? visual.SpriteRenderer : null;
            if (renderer != null && renderer.sprite != null)
            {
                return EnemyDeathVfx.ResolveTint(enemyTypeId, renderer.sprite, renderer.color);
            }

            return EnemyDeathVfx.GetFallbackTint(enemyTypeId);
        }

        private Vector3 ResolveLatestDropPosition()
        {
            Vector3 position = movementSync != null
                ? movementSync.GetServerAuthorityPosition()
                : transform.position;
            return FlatMovementUtility.SnapToGround(position);
        }
    }
}
