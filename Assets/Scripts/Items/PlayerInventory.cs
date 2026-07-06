using System.Collections.Generic;
using Mirror;
using UnityEngine;
using AetherEcho.Core;
using AetherEcho.Data;

namespace AetherEcho.Items
{
    [RequireComponent(typeof(Player.NetworkedCombatant))]
    public class PlayerInventory : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnInventoryJsonChanged))]
        private string inventoryJson = "[]";

        private readonly List<InventoryEntry> slots = new List<InventoryEntry>();

        public IReadOnlyList<InventoryEntry> Slots => slots;
        public int MaxSlots => GameConstants.InventoryMaxSlots;

        public InventorySnapshot GetSnapshot()
        {
            return new InventorySnapshot { slots = new List<InventoryEntry>(slots) };
        }

        [Server]
        public void ServerLoadSnapshot(InventorySnapshot snapshot)
        {
            slots.Clear();
            if (snapshot?.slots != null)
            {
                slots.AddRange(snapshot.slots);
            }

            SyncInventoryToClients();
        }

        [Server]
        public bool ServerAddItem(string itemId, int quantity, out string message)
        {
            message = string.Empty;
            if (quantity <= 0 || !ItemContentManager.Instance.TryGetItem(itemId, out ItemDefinition item))
            {
                message = "Invalid item.";
                return false;
            }

            int remaining = quantity;
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                if (slots[i].itemId != itemId)
                {
                    continue;
                }

                int canAdd = item.stack_size - slots[i].quantity;
                if (canAdd <= 0)
                {
                    continue;
                }

                int added = Mathf.Min(canAdd, remaining);
                slots[i].quantity += added;
                remaining -= added;
            }

            while (remaining > 0)
            {
                if (slots.Count >= MaxSlots)
                {
                    message = "Inventory full.";
                    SyncInventoryToClients();
                    return false;
                }

                int stackAmount = Mathf.Min(item.stack_size, remaining);
                slots.Add(new InventoryEntry(itemId, stackAmount));
                remaining -= stackAmount;
            }

            SyncInventoryToClients();
            message = "Received " + quantity + "x " + item.name;
            return true;
        }

        [Server]
        public bool ServerRemoveItem(string itemId, int quantity)
        {
            int remaining = quantity;
            for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (slots[i].itemId != itemId)
                {
                    continue;
                }

                int removed = Mathf.Min(slots[i].quantity, remaining);
                slots[i].quantity -= removed;
                remaining -= removed;
                if (slots[i].quantity <= 0)
                {
                    slots.RemoveAt(i);
                }
            }

            if (remaining > 0)
            {
                return false;
            }

            SyncInventoryToClients();
            return true;
        }

        [Server]
        public int ServerCountItem(string itemId)
        {
            int total = 0;
            foreach (InventoryEntry entry in slots)
            {
                if (entry.itemId == itemId)
                {
                    total += entry.quantity;
                }
            }

            return total;
        }

        [Server]
        public bool ServerHasItem(string itemId, int quantity = 1)
        {
            return ServerCountItem(itemId) >= quantity;
        }

        [Server]
        private void SyncInventoryToClients()
        {
            inventoryJson = JsonUtility.ToJson(new InventorySnapshot { slots = slots });
            TargetSyncInventory(connectionToClient, inventoryJson);
        }

        [TargetRpc]
        private void TargetSyncInventory(NetworkConnectionToClient target, string json)
        {
            ApplyInventoryJson(json);
        }

        private void OnInventoryJsonChanged(string oldJson, string newJson)
        {
            ApplyInventoryJson(newJson);
        }

        private void ApplyInventoryJson(string json)
        {
            slots.Clear();
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            InventorySnapshot snapshot = JsonUtility.FromJson<InventorySnapshot>(json);
            if (snapshot?.slots != null)
            {
                slots.AddRange(snapshot.slots);
            }
        }
    }
}
