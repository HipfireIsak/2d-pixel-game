using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Core;
using AetherEcho.Rendering;
using AetherEcho.World;

namespace AetherEcho.Enemies
{
    [RequireComponent(typeof(CombatantState))]
    public class NetworkedEnemy : NetworkBehaviour
    {
        [SyncVar] private string enemyTypeId = "slime";
        [SyncVar] private int maxHealth = 80;

        private CombatantState combatantState;
        private PixelBillboardVisual billboardVisual;
        private float attackCooldownSeconds;

        public string EnemyTypeId => enemyTypeId;
        public CombatantState Combatant => combatantState;

        private void Awake()
        {
            combatantState = GetComponent<CombatantState>();
            billboardVisual = GetComponent<PixelBillboardVisual>();
            transform.position = FlatMovementUtility.SnapToGround(transform.position);
        }

        [Server]
        public void ServerInitialize(string typeId, int level)
        {
            enemyTypeId = typeId;
            ArtCatalog art = ArtAssetResolver.Catalog;
            int health = typeId == "skeleton" ? 120 : 80;
            maxHealth = health + level * 10;
            combatantState.CharacterClass = typeId;
            combatantState.Level = level;
            combatantState.MaxHealth = maxHealth;
            combatantState.CurrentHealth = maxHealth;
            combatantState.CurrentMana = 0;
            combatantState.Strength = typeId == "skeleton" ? 12 : 8;
            combatantState.Intelligence = 4;
            combatantState.Agility = 6;
            combatantState.Relation = CombatRelation.Enemy;

            if (billboardVisual != null && art != null)
            {
                Sprite sprite = typeId == "skeleton" ? art.skeleton : art.slime;
                float scale = 1.1f;
                float groundOffset = FlatMovementUtility.GetSpriteGroundOffset(sprite, scale);
                billboardVisual.Configure(
                    transform,
                    sprite,
                    directionalHero: false,
                    offset: new Vector3(0f, groundOffset, 0f),
                    scale: scale);
            }
        }

        private void Update()
        {
            if (!isServer)
            {
                return;
            }

            attackCooldownSeconds -= Time.deltaTime;
            CombatantState target = ThreatMatrix.GetHighestThreatTarget(combatantState);
            if (target == null)
            {
                return;
            }

            ThreatMatrix.RegisterProximityThreat(target, combatantState, Vector3.Distance(transform.position, target.transform.position));
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude > 1.2f)
            {
                Vector3 step = toTarget.normalized * 2.8f * Time.deltaTime;
                transform.position = FlatMovementUtility.MoveWithFlatCollision(
                    transform.position,
                    step,
                    GameConstants.EnemyCollisionRadius);
                if (billboardVisual != null)
                {
                    billboardVisual.SetMoveDirection(toTarget);
                }
            }
            else if (attackCooldownSeconds <= 0f)
            {
                target.TakeDamage(8, DamageType.Physical, combatantState);
                attackCooldownSeconds = 1.4f;
            }
        }

        private void LateUpdate()
        {
            transform.position = FlatMovementUtility.SnapToGround(transform.position);
        }

        [Server]
        public void ServerNotifyKilledBy(CombatantState killer)
        {
            Quests.QuestManager.Instance?.ServerRegisterEnemyKill(killer, enemyTypeId);
        }
    }
}
