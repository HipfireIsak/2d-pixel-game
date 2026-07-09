using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Core;
using AetherEcho.Networking;
using AetherEcho.Rendering;
using AetherEcho.World;

namespace AetherEcho.Enemies
{
    [RequireComponent(typeof(CombatantState))]
    [DefaultExecutionOrder(100)]
    public class NetworkedEnemy : NetworkBehaviour
    {
        private const float ChaseStopDistance = 1.2f;
        private const float MoveSpeed = 2.8f;

        [SyncVar] private string enemyTypeId = "slime";
        [SyncVar] private int maxHealth = 80;

        private CombatantState combatantState;
        private PixelBillboardVisual billboardVisual;
        private FlatMovementNetworkSync movementSync;
        private float attackCooldownSeconds;
        private Vector3 lastFramePosition;
        private bool hasLastFramePosition;
        private string aiState = "idle";

        public string EnemyTypeId => enemyTypeId;
        public CombatantState Combatant => combatantState;

        public int ResolveKillExperience()
        {
            int level = combatantState != null ? combatantState.Level : 1;
            return ResolveBaseKillExperience(enemyTypeId) + level * 2;
        }

        private static int ResolveBaseKillExperience(string typeId)
        {
            switch (typeId)
            {
                case "vault_warden": return 90;
                case "skeleton": return 16;
                case "bat": return 10;
                case "eye": return 12;
                case "sunflower": return 11;
                default: return 10;
            }
        }

        private void Awake()
        {
            combatantState = GetComponent<CombatantState>();
            billboardVisual = GetComponent<PixelBillboardVisual>();
            movementSync = FlatMovementNetworkSync.Ensure(gameObject, MovementSyncMode.ServerAuthority);
            transform.position = FlatMovementUtility.SnapToGround(transform.position);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyVisualFromTypeId(enemyTypeId);
        }

        [Server]
        public void ServerInitialize(string typeId, int level)
        {
            enemyTypeId = typeId;
            int health = ResolveBaseHealth(typeId);
            maxHealth = (health + level * 10) * 2;
            combatantState.CharacterClass = typeId;
            combatantState.Level = level;
            combatantState.MaxHealth = maxHealth;
            combatantState.CurrentHealth = maxHealth;
            combatantState.CurrentMana = 0;
            combatantState.Strength = typeId == "skeleton" || typeId == "vault_warden" ? 18 : 8;
            combatantState.Intelligence = typeId == "vault_warden" ? 12 : 4;
            combatantState.Agility = typeId == "vault_warden" ? 8 : 6;
            combatantState.Relation = CombatRelation.Enemy;
            ApplyVisualFromTypeId(typeId);
        }

        private void ApplyVisualFromTypeId(string typeId)
        {
            if (billboardVisual == null)
            {
                return;
            }

            ArtCatalog art = ArtAssetResolver.Catalog;
            if (art == null)
            {
                return;
            }

            Sprite sprite = art.GetEnemySprite(typeId);
            float scale = typeId == "vault_warden"
                ? GameConstants.EnemyVisualScale * 1.75f
                : GameConstants.EnemyVisualScale;
            float groundOffset = FlatMovementUtility.GetSpriteGroundOffset(sprite, scale);
            billboardVisual.Configure(
                transform,
                sprite,
                directionalHero: false,
                offset: new Vector3(0f, groundOffset, 0f),
                scale: scale,
                facing: SpriteFacingMode.BillboardYWhenMoving);
        }

        private static int ResolveBaseHealth(string typeId)
        {
            switch (typeId)
            {
                case "skeleton": return 120;
                case "vault_warden": return 800;
                case "bat": return 70;
                case "eye": return 95;
                case "sunflower": return 85;
                default: return 80;
            }
        }

        private void Update()
        {
            if (netIdentity == null || !netIdentity.isServer || combatantState == null)
            {
                return;
            }

            Vector3 positionBefore = transform.position;
            attackCooldownSeconds -= Time.deltaTime;
            CombatantState target = ThreatMatrix.GetHighestThreatTarget(combatantState);
            if (target == null)
            {
                aiState = "idle";
                billboardVisual?.SetMoving(false);
                LogAiFrame(positionBefore, target, 0f);
                return;
            }

            ThreatMatrix.RegisterProximityThreat(target, combatantState, Vector3.Distance(transform.position, target.transform.position));
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            float targetDistance = toTarget.magnitude;
            if (targetDistance > ChaseStopDistance)
            {
                aiState = "chase";
                Vector3 step = toTarget.normalized * MoveSpeed * Time.deltaTime;
                transform.position = FlatMovementUtility.MoveWithFlatCollision(
                    transform.position,
                    step,
                    GameConstants.EnemyCollisionRadius);
                billboardVisual?.SetMoveDirection(toTarget);
                LogAiFrame(positionBefore, target, targetDistance);
                return;
            }

            aiState = "attack";
            billboardVisual?.SetMoving(false);
            if (attackCooldownSeconds <= 0f)
            {
                target.TakeDamage(enemyTypeId == "vault_warden" ? 16 : 8, DamageType.Physical, combatantState);
                attackCooldownSeconds = enemyTypeId == "vault_warden" ? 1.1f : 1.4f;
            }

            LogAiFrame(positionBefore, target, targetDistance);
        }

        private void LateUpdate()
        {
            if (!isServer || movementSync == null)
            {
                return;
            }

            transform.position = FlatMovementUtility.SnapToGround(transform.position);
            movementSync.SubmitServerTransform(transform.position, transform.rotation, enemyTypeId);
        }

        private void LogAiFrame(Vector3 positionBefore, CombatantState target, float targetDistance)
        {
            float frameDelta = hasLastFramePosition
                ? MovementDebugLogger.HorizontalDistance(lastFramePosition, transform.position)
                : 0f;
            hasLastFramePosition = true;
            lastFramePosition = transform.position;

            if (frameDelta <= 0.001f && aiState != "attack")
            {
                return;
            }

            string targetLabel = target != null ? target.CharacterClass : "none";
            MovementDebugLogger.LogEnemy(
                enemyTypeId,
                netId,
                "AiTick",
                "state=" + aiState
                + " frameDelta=" + frameDelta.ToString("F4")
                + " stepDelta=" + MovementDebugLogger.HorizontalDistance(positionBefore, transform.position).ToString("F4")
                + " targetDist=" + targetDistance.ToString("F2")
                + " target=" + targetLabel
                + " pos=" + MovementDebugLogger.FormatVector(transform.position));
        }

        [Server]
        public void ServerNotifyKilledBy(CombatantState killer)
        {
            Quests.QuestManager.Instance?.ServerRegisterEnemyKill(killer, enemyTypeId);
        }
    }
}
