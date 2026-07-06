using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Data;
using AetherEcho.Player;

namespace AetherEcho.Items
{
    public class LootService : MonoBehaviour
    {
        public static LootService Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        [Server]
        public void ServerGrantKillLoot(CombatantState killer, string enemyTypeId, Vector3 dropPosition)
        {
            if (killer == null || ItemContentManager.Instance == null)
            {
                return;
            }

            NetworkedCombatant player = killer.GetComponent<NetworkedCombatant>();
            if (player == null)
            {
                return;
            }

            if (ItemContentManager.Instance.TryGetLootTable(enemyTypeId, out LootTableDefinition table))
            {
                int gold = Random.Range(table.gold_min, table.gold_max + 1);
                if (gold > 0 && Random.value < 0.85f)
                {
                    Vector3 goldPos = dropPosition + Random.insideUnitSphere * 0.6f;
                    goldPos.y = dropPosition.y;
                    GroundLootDrop.ServerSpawn(goldPos, string.Empty, 0, gold);
                }

                foreach (LootEntryDefinition entry in table.entries)
                {
                    if (Random.value > entry.drop_chance)
                    {
                        continue;
                    }

                    int quantity = Random.Range(entry.quantity_min, entry.quantity_max + 1);
                    Vector3 itemPos = dropPosition + Random.insideUnitSphere * 0.8f;
                    itemPos.y = dropPosition.y;
                    GroundLootDrop.ServerSpawn(itemPos, entry.item_id, quantity, 0);
                }
            }

            World.DungeonInstanceManager.Instance?.ServerNotifyEnemyKilled(enemyTypeId, killer);
        }
    }
}
