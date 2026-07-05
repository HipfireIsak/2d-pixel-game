using Mirror;
using UnityEngine;
using AetherEcho.Combat;

namespace AetherEcho.Enemies
{
    public class EnemyDeathNotifier : NetworkBehaviour
    {
        private CombatantState combatantState;
        private NetworkedEnemy networkedEnemy;
        private bool deathHandled;

        private void Awake()
        {
            combatantState = GetComponent<CombatantState>();
            networkedEnemy = GetComponent<NetworkedEnemy>();
        }

        private void Update()
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
            CombatantState killer = ThreatMatrix.GetHighestThreatTarget(combatantState);
            if (killer != null && networkedEnemy != null)
            {
                networkedEnemy.ServerNotifyKilledBy(killer);
            }

            Invoke(nameof(DestroyEnemy), 1.5f);
        }

        [Server]
        private void DestroyEnemy()
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}
