using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Data;
using AetherEcho.Player;
using AetherEcho.Rendering;
using AetherEcho.World;

namespace AetherEcho.Items
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class GroundLootDrop : NetworkBehaviour
    {
        public const float PickupRange = 2.75f;

        [SyncVar] private string itemId;
        [SyncVar] private int quantity;
        [SyncVar] private int goldAmount;
        [SyncVar] private string labelText;

        private PixelBillboardVisual visual;
        private float bobTimer;

        [Server]
        public static GroundLootDrop ServerSpawn(Vector3 position, string dropItemId, int dropQuantity, int dropGold)
        {
            var lootObject = new GameObject("GroundLoot");
            lootObject.transform.position = FlatMovementUtility.SnapToGround(position);
            SphereCollider collider = lootObject.AddComponent<SphereCollider>();
            collider.radius = 0.55f;
            collider.isTrigger = true;

            lootObject.AddComponent<NetworkIdentity>();
            GroundLootDrop drop = lootObject.AddComponent<GroundLootDrop>();
            drop.ServerInitialize(dropItemId, dropQuantity, dropGold);
            NetworkServer.Spawn(lootObject);
            return drop;
        }

        public static bool TryFindNearest(Vector3 playerPosition, float range, out GroundLootDrop nearest)
        {
            nearest = null;
            float bestDistance = range;
            GroundLootDrop[] drops = FindObjectsOfType<GroundLootDrop>();
            foreach (GroundLootDrop drop in drops)
            {
                if (drop == null)
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
            itemId = dropItemId ?? string.Empty;
            quantity = dropQuantity;
            goldAmount = dropGold;
            if (goldAmount > 0)
            {
                labelText = goldAmount + " gold";
            }
            else if (ItemContentManager.Instance.TryGetItem(itemId, out ItemDefinition item))
            {
                labelText = item.name + (quantity > 1 ? " x" + quantity : string.Empty);
            }
            else
            {
                labelText = itemId;
            }

            EnsureVisual();
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
        public void ServerTryPickup(NetworkedCombatant player)
        {
            if (player == null || player.CombatantState == null || player.CombatantState.IsDead)
            {
                return;
            }

            if (Vector3.Distance(transform.position, player.transform.position) > PickupRange)
            {
                return;
            }

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
                    inventory.ServerAddItem(itemId, quantity, out string message);
                    toastMessage = message;
                }
            }

            if (!string.IsNullOrEmpty(toastMessage) && player.connectionToClient != null)
            {
                player.TargetShowLootToast(player.connectionToClient, toastMessage);
            }

            NetworkServer.Destroy(gameObject);
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(labelText))
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
