using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Data;

namespace AetherEcho.Items
{
    public enum EquipSlot
    {
        Head,
        Chest,
        Weapon,
        OffHand
    }

    [RequireComponent(typeof(CombatantState))]
    [RequireComponent(typeof(PlayerInventory))]
    public class PlayerEquipment : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnEquipmentJsonChanged))]
        private string equipmentJson = "{}";

        private readonly EquipmentSnapshot equipped = new EquipmentSnapshot();
        private CombatantState combatantState;
        private PlayerInventory inventory;

        public EquipmentSnapshot Equipped => equipped;

        private void Awake()
        {
            combatantState = GetComponent<CombatantState>();
            inventory = GetComponent<PlayerInventory>();
        }

        public EquipmentSnapshot GetSnapshot()
        {
            return new EquipmentSnapshot
            {
                head = equipped.head,
                chest = equipped.chest,
                weapon = equipped.weapon,
                offhand = equipped.offhand
            };
        }

        [Server]
        public void ServerLoadSnapshot(EquipmentSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            equipped.head = snapshot.head;
            equipped.chest = snapshot.chest;
            equipped.weapon = snapshot.weapon;
            equipped.offhand = snapshot.offhand;
            SyncEquipment();
            combatantState.ServerRecalculateFromEquipment(CollectEquipmentBonuses());
        }

        [Server]
        public bool ServerTryEquip(string itemId, out string message)
        {
            message = string.Empty;
            if (!ItemContentManager.Instance.TryGetItem(itemId, out ItemDefinition item)
                || string.IsNullOrWhiteSpace(item.equip_slot))
            {
                message = "Item cannot be equipped.";
                return false;
            }

            if (!inventory.ServerHasItem(itemId))
            {
                message = "You do not have that item.";
                return false;
            }

            string slotField = ResolveSlotField(item.equip_slot);
            if (slotField == null)
            {
                message = "Unknown equip slot.";
                return false;
            }

            string currentlyEquipped = GetEquippedInSlot(slotField);
            if (!string.IsNullOrWhiteSpace(currentlyEquipped))
            {
                inventory.ServerAddItem(currentlyEquipped, 1, out _);
            }

            inventory.ServerRemoveItem(itemId, 1);
            SetEquippedInSlot(slotField, itemId);
            SyncEquipment();
            combatantState.ServerRecalculateFromEquipment(CollectEquipmentBonuses());
            message = "Equipped " + item.name;
            return true;
        }

        [Server]
        public bool ServerTryUnequip(string slotName, out string message)
        {
            message = string.Empty;
            string itemId = GetEquippedInSlot(slotName);
            if (string.IsNullOrWhiteSpace(itemId))
            {
                message = "Nothing equipped in that slot.";
                return false;
            }

            if (!inventory.ServerAddItem(itemId, 1, out message))
            {
                return false;
            }

            SetEquippedInSlot(slotName, string.Empty);
            SyncEquipment();
            combatantState.ServerRecalculateFromEquipment(CollectEquipmentBonuses());
            ItemContentManager.Instance.TryGetItem(itemId, out ItemDefinition item);
            message = "Unequipped " + (item?.name ?? itemId);
            return true;
        }

        [Server]
        private ItemStatModifiers CollectEquipmentBonuses()
        {
            var total = new ItemStatModifiers();
            AddItemBonuses(equipped.head, total);
            AddItemBonuses(equipped.chest, total);
            AddItemBonuses(equipped.weapon, total);
            AddItemBonuses(equipped.offhand, total);
            return total;
        }

        private static void AddItemBonuses(string itemId, ItemStatModifiers total)
        {
            if (string.IsNullOrWhiteSpace(itemId)
                || !ItemContentManager.Instance.TryGetItem(itemId, out ItemDefinition item)
                || item.stat_modifiers == null)
            {
                return;
            }

            total.health += item.stat_modifiers.health;
            total.mana += item.stat_modifiers.mana;
            total.strength += item.stat_modifiers.strength;
            total.intelligence += item.stat_modifiers.intelligence;
            total.agility += item.stat_modifiers.agility;
        }

        [Server]
        private void SyncEquipment()
        {
            equipmentJson = JsonUtility.ToJson(equipped);
            TargetSyncEquipment(connectionToClient, equipmentJson);
        }

        [TargetRpc]
        private void TargetSyncEquipment(NetworkConnectionToClient target, string json)
        {
            ApplyEquipmentJson(json);
        }

        private void OnEquipmentJsonChanged(string oldJson, string newJson)
        {
            ApplyEquipmentJson(newJson);
        }

        private void ApplyEquipmentJson(string json)
        {
            EquipmentSnapshot snapshot = JsonUtility.FromJson<EquipmentSnapshot>(json);
            if (snapshot == null)
            {
                return;
            }

            equipped.head = snapshot.head;
            equipped.chest = snapshot.chest;
            equipped.weapon = snapshot.weapon;
            equipped.offhand = snapshot.offhand;
        }

        private static string ResolveSlotField(string equipSlot)
        {
            switch (equipSlot)
            {
                case "Head": return "head";
                case "Chest": return "chest";
                case "Weapon": return "weapon";
                case "OffHand": return "offhand";
                default: return null;
            }
        }

        private string GetEquippedInSlot(string slotField)
        {
            switch (slotField)
            {
                case "head": return equipped.head;
                case "chest": return equipped.chest;
                case "weapon": return equipped.weapon;
                case "offhand": return equipped.offhand;
                default: return string.Empty;
            }
        }

        private void SetEquippedInSlot(string slotField, string itemId)
        {
            switch (slotField)
            {
                case "head": equipped.head = itemId; break;
                case "chest": equipped.chest = itemId; break;
                case "weapon": equipped.weapon = itemId; break;
                case "offhand": equipped.offhand = itemId; break;
            }
        }
    }
}
