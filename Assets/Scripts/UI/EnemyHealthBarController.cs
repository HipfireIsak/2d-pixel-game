using System.Collections.Generic;
using Mirror;
using UnityEngine;
using AetherEcho.Combat;

namespace AetherEcho.UI
{
    public class EnemyHealthBarController : MonoBehaviour
    {
        public static EnemyHealthBarController Instance { get; private set; }

        private readonly HashSet<uint> spellRevealedNetIds = new HashSet<uint>();
        private readonly List<uint> drawBuffer = new List<uint>();
        private uint targetedNetId;

        private void Awake()
        {
            Instance = this;
        }

        public void SetTargetedEnemy(CombatantState enemy)
        {
            targetedNetId = enemy != null && enemy.netIdentity != null ? enemy.netIdentity.netId : 0;
        }

        public void ClearTargetedEnemy()
        {
            targetedNetId = 0;
        }

        public void RevealFromSpellHit(CombatantState enemy)
        {
            if (enemy == null || enemy.netIdentity == null)
            {
                return;
            }

            spellRevealedNetIds.Add(enemy.netIdentity.netId);
        }

        private void OnGUI()
        {
            Camera camera = CombatPickUtility.ResolveGameplayCamera();
            if (camera == null)
            {
                return;
            }

            if (targetedNetId != 0)
            {
                if (TryGetCombatant(targetedNetId, out CombatantState targeted) && targeted.CurrentHealth > 0)
                {
                    DrawHealthBar(camera, targeted);
                }
                else
                {
                    targetedNetId = 0;
                }
            }

            if (spellRevealedNetIds.Count == 0)
            {
                return;
            }

            drawBuffer.Clear();
            drawBuffer.AddRange(spellRevealedNetIds);
            foreach (uint netId in drawBuffer)
            {
                if (netId == targetedNetId)
                {
                    continue;
                }

                if (!TryGetCombatant(netId, out CombatantState enemy) || enemy.CurrentHealth <= 0)
                {
                    spellRevealedNetIds.Remove(netId);
                    continue;
                }

                DrawHealthBar(camera, enemy);
            }
        }

        private static bool TryGetCombatant(uint netId, out CombatantState combatant)
        {
            combatant = null;
            if (!NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity))
            {
                return false;
            }

            combatant = identity.GetComponent<CombatantState>();
            return combatant != null;
        }

        private static void DrawHealthBar(Camera camera, CombatantState combatant)
        {
            if (!CombatPickUtility.TryGetScreenBounds(camera, combatant, out Rect screenBounds, out _))
            {
                return;
            }

            Rect guiBounds = CombatPickUtility.ToGuiRect(screenBounds);
            float barWidth = Mathf.Max(48f, guiBounds.width * 0.9f);
            float barHeight = 8f;
            var barRect = new Rect(
                guiBounds.center.x - barWidth * 0.5f,
                guiBounds.y - barHeight - 4f,
                barWidth,
                barHeight);
            GameplayHud.DrawBar(
                barRect,
                combatant.CurrentHealth,
                combatant.MaxHealth,
                new Color(0.85f, 0.2f, 0.25f));
        }
    }
}
