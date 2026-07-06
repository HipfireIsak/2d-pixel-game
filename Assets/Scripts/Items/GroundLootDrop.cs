using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Data;
using AetherEcho.Networking;
using AetherEcho.Player;
using AetherEcho.Rendering;
using AetherEcho.World;

namespace AetherEcho.Items
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class GroundLootDrop : NetworkBehaviour
    {
        public const float PickupRange = 2.75f;
        private const string PrefabResourcePath = "Items/GroundLoot";

        [SyncVar(hook = nameof(OnPickedUpChanged))]
        private bool pickedUp;

        [SyncVar] private string itemId;
        [SyncVar] private int quantity;
        [SyncVar] private int goldAmount;
        [SyncVar] private string labelText;

        private PixelBillboardVisual visual;
        private float bobTimer;
        private static GameObject cachedPrefab;

        public bool IsAvailableForPickup => !pickedUp && netId != 0;

        [Server]
        public static GroundLootDrop ServerSpawn(Vector3 position, string dropItemId, int dropQuantity, int dropGold)
        {
            GameObject prefab = GetPrefab();
            GameObject lootObject = prefab != null
                ? Instantiate(prefab, FlatMovementUtility.SnapToGround(position), Quaternion.identity)
                : CreateRuntimeLootObject(FlatMovementUtility.SnapToGround(position));

            GroundLootDrop drop = lootObject.GetComponent<GroundLootDrop>();
            if (drop == null)
            {
                drop = lootObject.AddComponent<GroundLootDrop>();
            }

            drop.ServerInitialize(dropItemId, dropQuantity, dropGold);
            NetworkServer.Spawn(lootObject);
            return drop;
        }

        private static GameObject GetPrefab()
        {
            if (cachedPrefab == null)
            {
                cachedPrefab = Resources.Load<GameObject>(PrefabResourcePath);
            }

            return cachedPrefab;
        }

        private static GameObject CreateRuntimeLootObject(Vector3 position)
        {
            var lootObject = new GameObject("GroundLoot");
            lootObject.transform.position = position;
            SphereCollider collider = lootObject.AddComponent<SphereCollider>();
            collider.radius = 0.55f;
            collider.isTrigger = true;
            lootObject.AddComponent<NetworkIdentity>();
            lootObject.AddComponent<GroundLootDrop>();
            return lootObject;
        }

        public static bool TryFindNearest(Vector3 playerPosition, float range, out GroundLootDrop nearest)
        {
            nearest = null;
            float bestDistance = range;
            GroundLootDrop[] drops = FindObjectsOfType<GroundLootDrop>();
            foreach (GroundLootDrop drop in drops)
            {
                if (drop == null || !drop.IsAvailableForPickup)
                {
                    continue;
                }

                float distance = Vector3.Distance(playerPosition, drop.transform.position);
                if (distance > bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                nearest = drop;
            }

            return nearest != null;
        }

        [Server]
        private void ServerInitialize(string dropItemId, int dropQuantity, int dropGold)
        {
            pickedUp = false;
            itemId = dropItemId ?? string.Empty;
            quantity = dropQuantity;
            goldAmount = dropGold;
            labelText = BuildLabelText();
            EnsureVisual();
        }

        private string BuildLabelText()
        {
            if (goldAmount > 0)
            {
                return goldAmount + " gold";
            }

            if (!string.IsNullOrWhiteSpace(itemId)
                && ItemContentManager.Instance != null
                && ItemContentManager.Instance.TryGetItem(itemId, out ItemDefinition item))
            {
                return item.name + (quantity > 1 ? " x" + quantity : string.Empty);
            }

            return itemId;
        }

        private void Awake()
        {
            EnsureVisual();
        }

        private void EnsureVisual()
        {
            if (visual != null)
            {
                return;
            }

            ArtCatalog art = ArtAssetResolver.Catalog;
            Sprite sprite = art != null ? art.spellBurst : null;
            visual = gameObject.AddComponent<PixelBillboardVisual>();
            float scale = 0.85f;
            float offset = FlatMovementUtility.GetSpriteGroundOffset(sprite, scale);
            visual.Configure(
                transform,
                sprite,
                directionalHero: false,
                offset: new Vector3(0f, offset + 0.2f, 0f),
                scale: scale,
                facing: SpriteFacingMode.BillboardY);
        }

        [Server]
        public bool ServerTryPickup(NetworkedCombatant player, out string failureReason)
        {
            failureReason = string.Empty;
            if (pickedUp)
            {
                failureReason = "Loot already taken.";
                return false;
            }

            if (player == null || player.CombatantState == null || player.CombatantState.IsDead)
            {
                failureReason = "Cannot pick up loot right now.";
                return false;
            }

            if (GetHorizontalDistanceToPlayer(player) > PickupRange)
            {
                failureReason = "Move closer to pick that up.";
                return false;
            }

            pickedUp = true;

            CombatantState combatant = player.CombatantState;
            string toastMessage = string.Empty;
            if (goldAmount > 0)
            {
                combatant.Gold += goldAmount;
                toastMessage = "Picked up " + goldAmount + " gold.";
            }

            if (!string.IsNullOrWhiteSpace(itemId) && quantity > 0)
            {
                PlayerInventory inventory = player.GetComponent<PlayerInventory>();
                if (inventory != null)
                {
                    if (!inventory.ServerAddItem(itemId, quantity, out string message))
                    {
                        pickedUp = false;
                        failureReason = message;
                        return false;
                    }

                    toastMessage = message;
                }
            }

            if (!string.IsNullOrEmpty(toastMessage) && player.connectionToClient != null)
            {
                player.TargetShowLootToast(player.connectionToClient, toastMessage);
            }

            Quests.QuestManager.Instance?.ServerRefreshCollectObjectives(player.netId);
            Persistence.CharacterPersistenceService.Instance?.Save(player);
            NetworkServer.Destroy(gameObject);
            return true;
        }

        private float GetHorizontalDistanceToPlayer(NetworkedCombatant player)
        {
            Vector3 playerPosition = player.transform.position;
            FlatMovementNetworkSync movementSync = player.GetComponent<FlatMovementNetworkSync>();
            if (movementSync != null)
            {
                playerPosition = movementSync.GetServerAuthorityPosition();
            }

            Vector3 lootPosition = transform.position;
            playerPosition.y = 0f;
            lootPosition.y = 0f;
            return Vector3.Distance(lootPosition, playerPosition);
        }

        private void OnPickedUpChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                labelText = string.Empty;
            }
        }

        private void OnGUI()
        {
            if (pickedUp || string.IsNullOrEmpty(labelText))
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            bobTimer += Time.deltaTime;
            Vector3 worldPos = transform.position + Vector3.up * (0.8f + Mathf.Sin(bobTimer * 3f) * 0.08f);
            Vector3 screen = camera.WorldToScreenPoint(worldPos);
            if (screen.z < 0f)
            {
                return;
            }

            screen.y = Screen.height - screen.y;
            var rect = new Rect(screen.x - 60f, screen.y - 10f, 120f, 20f);
            GUI.Label(rect, labelText, GUI.skin.label);
        }
    }
}
