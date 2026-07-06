using System.Collections.Generic;
using AetherEcho.Content;
using AetherEcho.Core;
using AetherEcho.Data;
using UnityEngine;

namespace AetherEcho.Items
{
    public class ItemContentManager : MonoBehaviour
    {
        public static ItemContentManager Instance { get; private set; }

        private readonly Dictionary<string, ItemDefinition> itemDatabase = new Dictionary<string, ItemDefinition>();
        private readonly Dictionary<string, LootTableDefinition> lootTableDatabase = new Dictionary<string, LootTableDefinition>();
        private readonly Dictionary<string, VendorDefinition> vendorDatabase = new Dictionary<string, VendorDefinition>();

        private void Awake()
        {
            Instance = this;
            LoadItems(JsonContentLoader.ReadStreamingAssetText(GameConstants.DataItemsFileName));
            LoadLootTables(JsonContentLoader.ReadStreamingAssetText(GameConstants.DataLootTablesFileName));
            LoadVendors(JsonContentLoader.ReadStreamingAssetText(GameConstants.DataVendorsFileName));
        }

        public void LoadItems(string jsonText)
        {
            itemDatabase.Clear();
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return;
            }

            ItemRegistry registry = JsonUtility.FromJson<ItemRegistry>(jsonText);
            if (registry?.items == null)
            {
                return;
            }

            foreach (ItemDefinition item in registry.items)
            {
                if (item != null && !string.IsNullOrWhiteSpace(item.id))
                {
                    itemDatabase[item.id] = item;
                }
            }
        }

        public void LoadLootTables(string jsonText)
        {
            lootTableDatabase.Clear();
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return;
            }

            LootTableRegistry registry = JsonUtility.FromJson<LootTableRegistry>(jsonText);
            if (registry?.loot_tables == null)
            {
                return;
            }

            foreach (LootTableDefinition table in registry.loot_tables)
            {
                if (table != null && !string.IsNullOrWhiteSpace(table.enemy_type_id))
                {
                    lootTableDatabase[table.enemy_type_id] = table;
                }
            }
        }

        public void LoadVendors(string jsonText)
        {
            vendorDatabase.Clear();
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return;
            }

            VendorRegistry registry = JsonUtility.FromJson<VendorRegistry>(jsonText);
            if (registry?.vendors == null)
            {
                return;
            }

            foreach (VendorDefinition vendor in registry.vendors)
            {
                if (vendor != null && !string.IsNullOrWhiteSpace(vendor.id))
                {
                    vendorDatabase[vendor.id] = vendor;
                }
            }
        }

        public bool TryGetItem(string itemId, out ItemDefinition item)
        {
            return itemDatabase.TryGetValue(itemId, out item);
        }

        public bool TryGetLootTable(string enemyTypeId, out LootTableDefinition table)
        {
            return lootTableDatabase.TryGetValue(enemyTypeId, out table);
        }

        public bool TryGetVendor(string vendorId, out VendorDefinition vendor)
        {
            return vendorDatabase.TryGetValue(vendorId, out vendor);
        }
    }
}
