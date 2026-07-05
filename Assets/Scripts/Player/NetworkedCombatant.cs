using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Content;
using AetherEcho.Core;
using AetherEcho.Rendering;
using AetherEcho.UI;
using AetherEcho.Vfx;
using AetherEcho.World;

namespace AetherEcho.Player
{
    [RequireComponent(typeof(CombatantState))]
    public class NetworkedCombatant : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnDisplayNameChanged))]
        private string displayName = "Adventurer";

        [SyncVar] private bool isCastingSpell;
        [SyncVar] private Vector3 replicatedAimDirection = Vector3.forward;

        private CombatantState combatantState;
        private IsometricPlayerController movementController;
        private PixelBillboardVisual billboardVisual;
        private readonly System.Collections.Generic.Dictionary<string, float> clientCooldownEndTimes
            = new System.Collections.Generic.Dictionary<string, float>();

        public string DisplayName => displayName;
        public bool IsCastingSpell => isCastingSpell;
        public Vector3 AimDirection => replicatedAimDirection;
        public CombatantState CombatantState => combatantState;

        public float GetLocalCooldownRemaining(string spellId)
        {
            if (clientCooldownEndTimes.TryGetValue(spellId, out float endTime))
            {
                return Mathf.Max(0f, endTime - Time.time);
            }

            if (isServer)
            {
                return combatantState.GetCooldownRemainingSeconds(spellId);
            }

            return 0f;
        }

        public float GetManaRegenPerSecond()
        {
            return GameConstants.BaseManaRegenPerSecond
                   + combatantState.Intelligence * GameConstants.ManaRegenPerIntelligence;
        }

        private void Awake()
        {
            combatantState = GetComponent<CombatantState>();
            movementController = GetComponent<IsometricPlayerController>();
            billboardVisual = GetComponent<PixelBillboardVisual>();
        }

        public override void OnStartServer()
        {
            ApplyDefaultClassStats(GameConstants.DefaultPlayerClass, GameConstants.DefaultPlayerLevel);
            Quests.QuestManager.Instance?.PushQuestUiToPlayer(this);
        }

        public override void OnStartLocalPlayer()
        {
            CmdSetDisplayName(System.Environment.MachineName);
            EnsureVisual();
            transform.position = FlatMovementUtility.SnapToGround(transform.position);
            GameplayHud.Instance?.BindLocalPlayer(this);
            MinimapUI.Instance?.BindLocalPlayer(this);
            SpellGroundTargeting.Instance?.BindLocalPlayer(this, movementController);
            CmdRequestQuestSync();
        }

        public override void OnStartClient()
        {
            EnsureVisual();
        }

        private void EnsureVisual()
        {
            if (billboardVisual == null)
            {
                billboardVisual = gameObject.AddComponent<PixelBillboardVisual>();
            }

            ArtCatalog art = ArtAssetResolver.Catalog;
            if (art != null)
            {
                float scale = GameConstants.PlayerVisualScale;
                float offset = FlatMovementUtility.GetSpriteGroundOffset(art.heroSouth, scale);
                billboardVisual.Configure(
                    transform,
                    art.heroSouth,
                    directionalHero: true,
                    offset: new Vector3(0f, offset, 0f),
                    scale: scale,
                    facing: SpriteFacingMode.BillboardY);
            }
        }

        [Server]
        public void ApplyDefaultClassStats(string className, int level)
        {
            if (ClassContentManager.Instance == null
                || !ClassContentManager.Instance.TryGetClass(className, out Data.ClassData classData))
            {
                return;
            }

            Data.StatBlock stats = ClassContentManager.Instance.ResolveStatsForLevel(classData, level);
            combatantState.CharacterClass = className;
            combatantState.Level = level;
            combatantState.MaxHealth = stats.health;
            combatantState.CurrentHealth = stats.health;
            combatantState.MaxMana = stats.mana;
            combatantState.CurrentMana = stats.mana;
            combatantState.Strength = stats.strength;
            combatantState.Intelligence = stats.intelligence;
            combatantState.Agility = stats.agility;
            combatantState.Relation = CombatRelation.Ally;
        }

        [Command]
        private void CmdSetDisplayName(string requestedName)
        {
            displayName = string.IsNullOrWhiteSpace(requestedName)
                ? "Adventurer"
                : requestedName.Substring(0, Mathf.Min(24, requestedName.Length));
        }

        [Command]
        public void CmdInteractWithQuestNpc()
        {
            if (Quests.QuestManager.Instance == null)
            {
                TargetShowToast(connectionToClient, "Quest system unavailable.");
                return;
            }

            if (Quests.QuestManager.Instance.TryGetActiveProgress(netId, out Quests.QuestProgress progress))
            {
                if (progress.objectivesComplete)
                {
                    TargetOpenQuestTurnIn(connectionToClient, progress.questId);
                    return;
                }

                TargetShowToast(connectionToClient, "Finish your current objectives first. Press J for the quest log.");
                return;
            }

            string questId = Quests.QuestManager.Instance.GetSuggestedQuestId(netId);
            if (Quests.QuestManager.Instance.IsQuestCompleted(netId, questId))
            {
                TargetShowToast(connectionToClient, "You have completed all starter quests from the Chrono Sage.");
                return;
            }

            TargetOpenQuestDialog(connectionToClient, questId);
        }

        [Command]
        public void CmdOpenQuestDialog(string questId)
        {
            if (Quests.QuestManager.Instance == null
                || !Quests.QuestManager.Instance.TryGetQuest(questId, out _))
            {
                TargetShowToast(connectionToClient, "Quest unavailable.");
                return;
            }

            TargetOpenQuestDialog(connectionToClient, questId);
        }

        [TargetRpc]
        private void TargetOpenQuestDialog(NetworkConnectionToClient target, string questId)
        {
            QuestDialogUI.Instance?.ShowQuestOffer(this, questId);
        }

        [Command]
        public void CmdAcceptQuest(string questId)
        {
            if (Quests.QuestManager.Instance == null)
            {
                return;
            }

            if (Quests.QuestManager.Instance.ServerTryAcceptQuest(this, questId, out string message))
            {
                TargetShowToast(connectionToClient, message);
            }
            else
            {
                TargetShowToast(connectionToClient, message);
            }
        }

        [TargetRpc]
        private void TargetOpenQuestTurnIn(NetworkConnectionToClient target, string questId)
        {
            QuestDialogUI.Instance?.ShowQuestTurnIn(this, questId);
        }

        [Command]
        public void CmdTurnInQuest()
        {
            if (Quests.QuestManager.Instance == null)
            {
                return;
            }

            if (Quests.QuestManager.Instance.ServerTryTurnInQuest(this, out string message))
            {
                TargetShowToast(connectionToClient, message);
            }
            else
            {
                TargetShowToast(connectionToClient, message);
            }
        }

        [Command]
        private void CmdRequestQuestSync()
        {
            Quests.QuestManager.Instance?.PushQuestUiToPlayer(this);
        }

        [Command]
        public void CmdTalkToQuestNpc(string questId)
        {
            CmdInteractWithQuestNpc();
        }

        [TargetRpc]
        private void TargetShowToast(NetworkConnectionToClient target, string message)
        {
            GameplayHud.Instance?.SetToast(message);
        }

        [Command]
        public void CmdRequestCastSpell(string spellId, Vector3 targetPoint, Vector3 aimDirection)
        {
            if (SpellEngine.Instance == null)
            {
                TargetShowToast(connectionToClient, "Spell engine unavailable.");
                return;
            }

            if (!SpellEngine.Instance.CanPlayerCast(combatantState, spellId, out string failureReason))
            {
                TargetShowToast(connectionToClient, failureReason);
                return;
            }

            isCastingSpell = true;
            replicatedAimDirection = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : transform.forward;
            SpellEngine.Instance.ServerExecuteSpell(combatantState, spellId, targetPoint, replicatedAimDirection);

            if (spellId == GameConstants.SpellManaSurge)
            {
                RpcPlaySpellFx(spellId, targetPoint, replicatedAimDirection);
            }

            isCastingSpell = false;
        }

        [ClientRpc]
        private void RpcPlaySpellFx(string spellId, Vector3 targetPoint, Vector3 aimDirection)
        {
            if (SpellVfxPlayer.Instance != null)
            {
                SpellVfxPlayer.Instance.PlaySpell(spellId, transform.position, targetPoint, aimDirection);
            }
        }

        [ClientRpc]
        public void RpcUpdateQuestHud(string hudText)
        {
            GameplayHud.Instance?.SetQuestText(hudText);
        }

        [ClientRpc]
        public void RpcUpdateQuestTracker(string trackerText, bool readyToTurnIn)
        {
            GameplayHud.Instance?.SetQuestTracker(trackerText, readyToTurnIn);
        }

        public bool TryLocalCast(string spellId, Vector3 targetPoint, Vector3 aimDirection, out string failureReason)
        {
            failureReason = string.Empty;
            if (!isLocalPlayer)
            {
                failureReason = "Not local player.";
                return false;
            }

            if (SpellEngine.Instance == null)
            {
                failureReason = "Spell engine unavailable.";
                return false;
            }

            if (GetLocalCooldownRemaining(spellId) > 0.01f)
            {
                failureReason = "Spell is on cooldown.";
                return false;
            }

            if (!SpellEngine.Instance.CanPlayerCast(combatantState, spellId, out failureReason))
            {
                return false;
            }

            if (movementController != null)
            {
                movementController.FaceAimDirection(aimDirection);
            }

            if (spellId == GameConstants.SpellManaSurge && SpellVfxPlayer.Instance != null)
            {
                SpellVfxPlayer.Instance.PlaySpell(spellId, transform.position, targetPoint, aimDirection);
            }

            CmdRequestCastSpell(spellId, targetPoint, aimDirection);
            return true;
        }

        [TargetRpc]
        public void TargetNotifySpellCast(NetworkConnectionToClient target, string spellId, float cooldownSeconds)
        {
            clientCooldownEndTimes[spellId] = Time.time + cooldownSeconds;
        }

        private void OnDisplayNameChanged(string oldName, string newName)
        {
            gameObject.name = "Player_" + newName;
        }
    }
}
