using Mirror;
using System.Collections;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Content;
using AetherEcho.Core;
using AetherEcho.Items;
using AetherEcho.Networking;
using AetherEcho.Persistence;
using AetherEcho.Rendering;
using AetherEcho.Social;
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
        [SyncVar] private string castBarSpellId = string.Empty;
        [SyncVar] private float castBarStartTime;
        [SyncVar] private float castBarEndTime;
        [SyncVar] private Vector3 replicatedAimDirection = Vector3.forward;

        private bool castRoutineActive;

        private CombatantState combatantState;
        private IsometricPlayerController movementController;
        private PixelBillboardVisual billboardVisual;
        private PlayerInventory playerInventory;
        private PlayerEquipment playerEquipment;
        private float autoSaveAccumulator;
        private float nextRecallAvailableTime;
        private readonly System.Collections.Generic.Dictionary<string, float> clientCooldownEndTimes
            = new System.Collections.Generic.Dictionary<string, float>();

        public string DisplayName => displayName;
        public bool IsCastingSpell => isCastingSpell;
        public bool HasCastBar => !string.IsNullOrEmpty(castBarSpellId) && Time.time < castBarEndTime;
        public string CastBarSpellId => castBarSpellId;

        public float GetCastBarProgress()
        {
            if (!HasCastBar || castBarEndTime <= castBarStartTime)
            {
                return 0f;
            }

            return Mathf.Clamp01((Time.time - castBarStartTime) / (castBarEndTime - castBarStartTime));
        }
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
            playerInventory = GetComponent<PlayerInventory>();
            if (playerInventory == null)
            {
                playerInventory = gameObject.AddComponent<PlayerInventory>();
            }

            playerEquipment = GetComponent<PlayerEquipment>();
            if (playerEquipment == null)
            {
                playerEquipment = gameObject.AddComponent<PlayerEquipment>();
            }
        }

        private void Update()
        {
            if (!isServer)
            {
                return;
            }

            autoSaveAccumulator += Time.deltaTime;
            if (autoSaveAccumulator >= GameConstants.AutoSaveIntervalSeconds)
            {
                autoSaveAccumulator = 0f;
                CharacterPersistenceService.Instance?.Save(this);
            }
        }

        public override void OnStartServer()
        {
            ApplyDefaultClassStats(GameConstants.DefaultPlayerClass, GameConstants.DefaultPlayerLevel);
            Quests.QuestManager.Instance?.PushQuestUiToPlayer(this);
        }

        private bool saveLoaded;

        [Server]
        private void TryLoadSavedCharacter()
        {
            if (CharacterPersistenceService.Instance == null)
            {
                return;
            }

            string characterId = CharacterPersistenceService.SanitizeCharacterId(displayName);
            if (CharacterPersistenceService.Instance.TryLoad(characterId, out CharacterSaveData saveData))
            {
                CharacterPersistenceService.Instance.ApplySaveData(this, saveData);
            }
        }

        [Server]
        public void ServerApplyLoadedCharacter(
            string name,
            string className,
            int level,
            int experience,
            int gold,
            int health,
            int mana)
        {
            displayName = string.IsNullOrWhiteSpace(name) ? displayName : name;
            ApplyDefaultClassStats(className, level);
            combatantState.Experience = experience;
            combatantState.Gold = gold;
            combatantState.CurrentHealth = Mathf.Min(health, combatantState.MaxHealth);
            combatantState.CurrentMana = Mathf.Min(mana, combatantState.MaxMana);
        }

        [Server]
        public void ServerTeleport(Vector3 position)
        {
            Vector3 snapped = FlatMovementUtility.SnapToGround(position);
            FlatMovementNetworkSync movementSync = GetComponent<FlatMovementNetworkSync>();
            if (movementSync != null)
            {
                movementSync.ApplyAuthoritativeTeleport(snapped, transform.rotation);
                return;
            }

            transform.position = snapped;
        }

        [Server]
        public void ServerNotifyDeath()
        {
            TargetShowDeathScreen(connectionToClient, true);
        }

        public override void OnStartLocalPlayer()
        {
            CmdSetDisplayName(System.Environment.MachineName);
            EnsureVisual();
            transform.position = FlatMovementUtility.SnapToGround(transform.position);
            GameplayHud.Instance?.BindLocalPlayer(this);
            MinimapUI.Instance?.BindLocalPlayer(this);
            SpellGroundTargeting.Instance?.BindLocalPlayer(this, movementController);
            InventoryUI.Instance?.BindLocalPlayer(this);
            CharacterSheetUI.Instance?.BindLocalPlayer(this);
            PartyUI.Instance?.BindLocalPlayer(this);
            DeathScreenUI.Instance?.BindLocalPlayer(this);
            TargetSelectionController.Instance?.BindLocalPlayer(this);
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
            combatantState.ServerSetBaseStats(
                stats.health,
                stats.mana,
                stats.strength,
                stats.intelligence,
                stats.agility);
            combatantState.CurrentHealth = stats.health;
            combatantState.CurrentMana = stats.mana;
            combatantState.Relation = CombatRelation.Ally;
        }

        [Command]
        private void CmdSetDisplayName(string requestedName)
        {
            displayName = string.IsNullOrWhiteSpace(requestedName)
                ? "Adventurer"
                : requestedName.Substring(0, Mathf.Min(24, requestedName.Length));

            if (!saveLoaded)
            {
                saveLoaded = true;
                TryLoadSavedCharacter();
                Quests.QuestManager.Instance?.ServerRefreshCollectObjectives(netId);
                Quests.QuestManager.Instance?.PushQuestUiToPlayer(this);
            }
        }

        [Command]
        public void CmdInteractWithQuestNpc(string questGiverName)
        {
            if (Quests.QuestManager.Instance == null)
            {
                TargetShowToast(connectionToClient, "Quest system unavailable.");
                return;
            }

            if (Quests.QuestManager.Instance.TryGetActiveProgress(netId, out Quests.QuestProgress progress))
            {
                Quests.QuestManager.Instance.TryGetQuest(progress.questId, out Quests.QuestDefinition activeQuest);
                if (progress.objectivesComplete
                    && activeQuest != null
                    && activeQuest.quest_giver_name == questGiverName)
                {
                    TargetOpenQuestTurnIn(connectionToClient, progress.questId);
                    return;
                }

                TargetShowToast(connectionToClient, "Finish your current objectives first. Press J for the quest log.");
                return;
            }

            string questId = Quests.QuestManager.Instance.GetSuggestedQuestId(netId, questGiverName);
            if (string.IsNullOrWhiteSpace(questId)
                || Quests.QuestManager.Instance.IsQuestCompleted(netId, questId))
            {
                TargetShowToast(connectionToClient, "No quests available from this NPC.");
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
            CmdInteractWithQuestNpc("Chrono Sage");
        }

        [TargetRpc]
        public void TargetShowToast(NetworkConnectionToClient target, string message)
        {
            GameplayHud.Instance?.SetToast(message);
        }

        [Command]
        public void CmdRequestCastSpell(string spellId, Vector3 targetPoint, Vector3 aimDirection, uint targetNetId)
        {
            if (castRoutineActive)
            {
                TargetShowToast(connectionToClient, "Already casting.");
                return;
            }

            StartCoroutine(ServerCastRoutine(spellId, targetPoint, aimDirection, targetNetId));
        }

        [Server]
        private IEnumerator ServerCastRoutine(string spellId, Vector3 targetPoint, Vector3 aimDirection, uint targetNetId)
        {
            castRoutineActive = true;
            if (combatantState.IsDead)
            {
                TargetShowToast(connectionToClient, "You are dead.");
                castRoutineActive = false;
                yield break;
            }

            if (SpellEngine.Instance == null)
            {
                TargetShowToast(connectionToClient, "Spell engine unavailable.");
                castRoutineActive = false;
                yield break;
            }

            if (!SpellEngine.Instance.CanPlayerCast(combatantState, spellId, out string failureReason))
            {
                TargetShowToast(connectionToClient, failureReason);
                castRoutineActive = false;
                yield break;
            }

            if (!SpellContentManager.Instance.TryGetSpell(spellId, out Data.SpellData spell))
            {
                castRoutineActive = false;
                yield break;
            }

            if (spell.targeting.type == "UnitTarget")
            {
                if (targetNetId == 0 || !NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
                {
                    TargetShowToast(connectionToClient, "Invalid target.");
                    castRoutineActive = false;
                    yield break;
                }

                CombatantState targetState = targetIdentity.GetComponent<CombatantState>();
                if (targetState == null || !targetState.IsEnemyWith(combatantState))
                {
                    TargetShowToast(connectionToClient, "Invalid target.");
                    castRoutineActive = false;
                    yield break;
                }

                float targetRange = spell.targeting.range_meters;
                if (Vector3.Distance(transform.position, targetIdentity.transform.position) > targetRange)
                {
                    TargetShowToast(connectionToClient, "Target out of range.");
                    castRoutineActive = false;
                    yield break;
                }
            }

            float castTime = Mathf.Max(0f, spell.casting_rules.cast_time_seconds);
            replicatedAimDirection = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : transform.forward;
            castBarSpellId = spellId;
            castBarStartTime = Time.time;
            castBarEndTime = Time.time + castTime;
            isCastingSpell = castTime > GameConstants.MovementBlockingCastTimeSeconds
                             && !spell.casting_rules.can_cast_while_moving;

            if (castTime > 0f)
            {
                yield return new WaitForSeconds(castTime);
            }

            if (combatantState.IsDead)
            {
                ClearCastBar();
                castRoutineActive = false;
                yield break;
            }

            if (!SpellEngine.Instance.CanPlayerCast(combatantState, spellId, out failureReason))
            {
                TargetShowToast(connectionToClient, failureReason);
                ClearCastBar();
                castRoutineActive = false;
                yield break;
            }

            SpellEngine.Instance.ServerExecuteSpell(combatantState, spellId, targetPoint, replicatedAimDirection, targetNetId);
            if (spellId == GameConstants.SpellManaSurge)
            {
                RpcPlaySpellFx(spellId, transform.position, replicatedAimDirection);
            }

            ClearCastBar();
            castRoutineActive = false;
        }

        [Server]
        private void ClearCastBar()
        {
            castBarSpellId = string.Empty;
            castBarStartTime = 0f;
            castBarEndTime = 0f;
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

        [ClientRpc]
        public void RpcSyncQuestClientState(string activeQuestId, bool objectivesComplete, string completedQuestIdsCsv)
        {
            Quests.QuestClientState.Apply(activeQuestId, objectivesComplete, completedQuestIdsCsv);
        }

        public bool TryLocalCast(string spellId, Vector3 targetPoint, Vector3 aimDirection, out string failureReason, uint targetNetId = 0)
        {
            failureReason = string.Empty;
            if (!isLocalPlayer)
            {
                failureReason = "Not local player.";
                return false;
            }

            if (ChatUI.BlocksGameInput)
            {
                failureReason = "Chat is open.";
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

            if (movementController != null && aimDirection.sqrMagnitude > 0.001f)
            {
                movementController.FaceAimDirection(aimDirection);
            }

            CmdRequestCastSpell(spellId, targetPoint, aimDirection, targetNetId);
            return true;
        }

        [TargetRpc]
        public void TargetNotifySpellCast(NetworkConnectionToClient target, string spellId, float cooldownSeconds)
        {
            clientCooldownEndTimes[spellId] = Time.time + cooldownSeconds;
        }

        [Command]
        public void CmdRecallToHub()
        {
            if (combatantState.IsDead || IsCastingSpell)
            {
                TargetShowToast(connectionToClient, "Cannot recall right now.");
                return;
            }

            if (Time.time < nextRecallAvailableTime)
            {
                float remaining = nextRecallAvailableTime - Time.time;
                TargetShowToast(connectionToClient, "Recall ready in " + Mathf.CeilToInt(remaining) + "s.");
                return;
            }

            nextRecallAvailableTime = Time.time + GameConstants.RecallCooldownSeconds;
            ServerTeleport(GameConstants.HubSpawnPosition);
            TargetShowToast(connectionToClient, "Recalled to the Chrono Hub.");
            CharacterPersistenceService.Instance?.Save(this);
        }

        [Command]
        public void CmdSendChatMessage(string channel, string text)
        {
            ChatDebug.Log(
                "CmdSendChatMessage received on server netId=" + netId
                + ", channel=" + channel
                + ", text=\"" + text + "\""
                + ", ChatManager.Instance=" + (ChatManager.Instance != null));
            if (ChatManager.Instance == null)
            {
                ChatDebug.LogWarning("CmdSendChatMessage failed: ChatManager.Instance is null on server.");
                return;
            }

            ChatManager.Instance.ServerReceiveMessage(this, channel, text);
        }

        [Command]
        public void CmdPickupGroundLoot(uint lootNetId)
        {
            if (!NetworkServer.spawned.TryGetValue(lootNetId, out NetworkIdentity identity))
            {
                TargetShowToast(connectionToClient, "That loot is no longer there.");
                return;
            }

            GroundLootDrop drop = identity.GetComponent<GroundLootDrop>();
            if (drop == null)
            {
                TargetShowToast(connectionToClient, "Cannot pick that up.");
                return;
            }

            if (!drop.ServerTryPickup(this, out string failureReason)
                && !string.IsNullOrEmpty(failureReason))
            {
                TargetShowToast(connectionToClient, failureReason);
            }
        }

        [TargetRpc]
        public void TargetReceiveChatMessage(NetworkConnectionToClient target, string channel, string senderName, string text)
        {
            ChatDebug.Log(
                "TargetReceiveChatMessage on client [" + channel + "] "
                + senderName + ": \"" + text + "\""
                + ", ChatManager.Instance=" + (ChatManager.Instance != null));
            if (ChatManager.Instance == null)
            {
                ChatDebug.LogWarning("TargetReceiveChatMessage failed: ChatManager.Instance is null on client.");
                return;
            }

            ChatManager.Instance.ClientAppendMessage(channel, senderName, text);
        }

        [Command]
        public void CmdEquipItem(string itemId)
        {
            string message = "Equipment unavailable.";
            if (playerEquipment == null || !playerEquipment.ServerTryEquip(itemId, out message))
            {
                TargetShowToast(connectionToClient, message);
                return;
            }

            TargetShowToast(connectionToClient, message);
            CharacterPersistenceService.Instance?.Save(this);
        }

        [Command]
        public void CmdUnequipSlot(string slotName)
        {
            string message = "Equipment unavailable.";
            if (playerEquipment == null || !playerEquipment.ServerTryUnequip(slotName, out message))
            {
                TargetShowToast(connectionToClient, message);
                return;
            }

            TargetShowToast(connectionToClient, message);
            CharacterPersistenceService.Instance?.Save(this);
        }

        [Command]
        public void CmdUseItem(string itemId)
        {
            if (playerInventory == null
                || !ItemContentManager.Instance.TryGetItem(itemId, out Data.ItemDefinition item))
            {
                TargetShowToast(connectionToClient, "Cannot use item.");
                return;
            }

            if (item.item_type != "Consumable")
            {
                TargetShowToast(connectionToClient, "Item is not consumable.");
                return;
            }

            if (!playerInventory.ServerRemoveItem(itemId, 1))
            {
                TargetShowToast(connectionToClient, "You do not have that item.");
                return;
            }

            if (item.consumable_heal > 0)
            {
                combatantState.ServerHeal(item.consumable_heal, combatantState);
            }

            if (item.consumable_mana > 0)
            {
                combatantState.CurrentMana = Mathf.Min(
                    combatantState.MaxMana,
                    combatantState.CurrentMana + item.consumable_mana);
            }

            Quests.QuestManager.Instance?.ServerRefreshCollectObjectives(netId);
            TargetShowToast(connectionToClient, "Used " + item.name);
            CharacterPersistenceService.Instance?.Save(this);
        }

        [Command]
        public void CmdBuyFromVendor(string vendorId, string itemId)
        {
            if (playerInventory == null
                || !ItemContentManager.Instance.TryGetVendor(vendorId, out Data.VendorDefinition vendor)
                || !ItemContentManager.Instance.TryGetItem(itemId, out Data.ItemDefinition item))
            {
                TargetShowToast(connectionToClient, "Purchase failed.");
                return;
            }

            int price = item.buy_price;
            foreach (Data.VendorStockEntry stock in vendor.stock)
            {
                if (stock.item_id == itemId)
                {
                    price = stock.price;
                    break;
                }
            }

            if (combatantState.Gold < price)
            {
                TargetShowToast(connectionToClient, "Not enough gold.");
                return;
            }

            if (!playerInventory.ServerAddItem(itemId, 1, out string message))
            {
                TargetShowToast(connectionToClient, message);
                return;
            }

            combatantState.Gold -= price;
            Quests.QuestManager.Instance?.ServerRefreshCollectObjectives(netId);
            TargetShowToast(connectionToClient, "Purchased " + item.name);
            CharacterPersistenceService.Instance?.Save(this);
        }

        [Command]
        public void CmdInviteToPartyByName(string targetName)
        {
            if (PartyManager.Instance == null || string.IsNullOrWhiteSpace(targetName))
            {
                TargetShowToast(connectionToClient, "Invalid player name.");
                return;
            }

            NetworkedCombatant[] players = FindObjectsOfType<NetworkedCombatant>();
            foreach (NetworkedCombatant candidate in players)
            {
                if (candidate.DisplayName.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (PartyManager.Instance.ServerTryInvite(this, candidate.netId, out string message))
                    {
                        TargetShowToast(connectionToClient, message);
                    }
                    else
                    {
                        TargetShowToast(connectionToClient, message);
                    }

                    return;
                }
            }

            TargetShowToast(connectionToClient, "Player not found.");
        }

        [Command]
        public void CmdAcceptPartyInvite(uint inviterNetId)
        {
            if (PartyManager.Instance == null)
            {
                return;
            }

            PartyManager.Instance.ServerTryAcceptInvite(this, inviterNetId, out string message);
            TargetShowToast(connectionToClient, message);
        }

        [Command]
        public void CmdLeaveParty()
        {
            PartyManager.Instance?.ServerLeaveParty(this);
            TargetPartyUpdate(connectionToClient, string.Empty, 0);
            TargetShowToast(connectionToClient, "Left party.");
        }

        [Command]
        public void CmdEnterDungeon()
        {
            if (DungeonInstanceManager.Instance == null)
            {
                TargetShowToast(connectionToClient, "Dungeon unavailable.");
                return;
            }

            DungeonInstanceManager.Instance.ServerTryEnterDungeon(this, out string message);
            if (!string.IsNullOrEmpty(message) && message != "Dungeon entered.")
            {
                TargetShowToast(connectionToClient, message);
            }
        }

        [Command]
        public void CmdExitDungeon()
        {
            DungeonInstanceManager.Instance?.ServerExitDungeon(this);
            CharacterPersistenceService.Instance?.Save(this);
        }

        [Command]
        public void CmdRespawn()
        {
            if (!combatantState.IsDead)
            {
                return;
            }

            Vector3 hubSpawn = GameConstants.HubSpawnPosition;
            ServerTeleport(hubSpawn);
            int respawnHealth = Mathf.Max(1, Mathf.RoundToInt(combatantState.MaxHealth * GameConstants.PlayerRespawnHealthFraction));
            int respawnMana = Mathf.Max(1, Mathf.RoundToInt(combatantState.MaxMana * GameConstants.PlayerRespawnHealthFraction));
            combatantState.ServerRespawn(respawnHealth, respawnMana);
            TargetShowDeathScreen(connectionToClient, false);
            TargetShowToast(connectionToClient, "Respawned at the Chrono Hub.");
            CharacterPersistenceService.Instance?.Save(this);
        }

        [TargetRpc]
        public void TargetShowLootToast(NetworkConnectionToClient target, string message)
        {
            GameplayHud.Instance?.SetToast(message);
        }

        [TargetRpc]
        public void TargetPartyInvite(NetworkConnectionToClient target, string inviterName, uint inviterNetId)
        {
            PartyUI.Instance?.ShowInvite(inviterName, inviterNetId);
        }

        [TargetRpc]
        public void TargetPartyUpdate(NetworkConnectionToClient target, string roster, uint leaderNetId)
        {
            PartyUI.Instance?.SetPartyRoster(roster, leaderNetId);
        }

        [TargetRpc]
        private void TargetShowDeathScreen(NetworkConnectionToClient target, bool dead)
        {
            DeathScreenUI.Instance?.SetDead(dead);
        }

        public override void OnStopServer()
        {
            CharacterPersistenceService.Instance?.Save(this);
        }

        private void OnDisplayNameChanged(string oldName, string newName)
        {
            gameObject.name = "Player_" + newName;
        }
    }
}
