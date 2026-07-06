using System;
using System.Collections.Generic;

namespace AetherEcho.Data
{
    [Serializable]
    public class ItemRegistry
    {
        public List<ItemDefinition> items = new List<ItemDefinition>();
    }

    [Serializable]
    public class ItemDefinition
    {
        public string id;
        public string name;
        public string description;
        public string item_type;
        public string equip_slot;
        public int stack_size = 1;
        public int buy_price;
        public int sell_price;
        public ItemStatModifiers stat_modifiers = new ItemStatModifiers();
        public int consumable_heal;
        public int consumable_mana;
    }

    [Serializable]
    public class ItemStatModifiers
    {
        public int health;
        public int mana;
        public int strength;
        public int intelligence;
        public int agility;
    }

    [Serializable]
    public class LootTableRegistry
    {
        public List<LootTableDefinition> loot_tables = new List<LootTableDefinition>();
    }

    [Serializable]
    public class LootTableDefinition
    {
        public string enemy_type_id;
        public int gold_min;
        public int gold_max;
        public List<LootEntryDefinition> entries = new List<LootEntryDefinition>();
    }

    [Serializable]
    public class LootEntryDefinition
    {
        public string item_id;
        public float drop_chance;
        public int quantity_min = 1;
        public int quantity_max = 1;
    }

    [Serializable]
    public class VendorRegistry
    {
        public List<VendorDefinition> vendors = new List<VendorDefinition>();
    }

    [Serializable]
    public class VendorDefinition
    {
        public string id;
        public string display_name;
        public List<VendorStockEntry> stock = new List<VendorStockEntry>();
    }

    [Serializable]
    public class VendorStockEntry
    {
        public string item_id;
        public int price;
    }

    [Serializable]
    public class InventoryEntry
    {
        public string itemId;
        public int quantity;

        public InventoryEntry() { }

        public InventoryEntry(string itemId, int quantity)
        {
            this.itemId = itemId;
            this.quantity = quantity;
        }
    }

    [Serializable]
    public class InventorySnapshot
    {
        public List<InventoryEntry> slots = new List<InventoryEntry>();
    }

    [Serializable]
    public class EquipmentSnapshot
    {
        public string head;
        public string chest;
        public string weapon;
        public string offhand;
    }
}
