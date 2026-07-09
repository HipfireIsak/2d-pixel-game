using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Content;
using AetherEcho.Items;
using AetherEcho.Persistence;
using AetherEcho.Quests;
using AetherEcho.Social;
using AetherEcho.Vfx;
using AetherEcho.World;

namespace AetherEcho.Core
{
    public class GameSystemsBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            EnsureComponent<SpellContentManager>();
            EnsureComponent<ClassContentManager>();
            EnsureComponent<ItemContentManager>();
            EnsureComponent<SpellEngine>();
            EnsureComponent<SpellVfxPlayer>();
            EnsureComponent<LootService>();
            EnsureComponent<MobSpawnZoneManager>();
            EnsureComponent<QuestManager>();
            EnsureComponent<CharacterPersistenceService>();
            EnsureComponent<ChatManager>();
            EnsureComponent<PartyManager>();
            EnsureComponent<DungeonInstanceManager>();
            EnsureComponent<WorldContentSpawner>();
            EnsureComponent<UI.GameplayHud>();
            EnsureComponent<UI.SpellGroundTargeting>();
            EnsureComponent<UI.QuestDialogUI>();
            EnsureComponent<UI.MinimapUI>();
            EnsureComponent<UI.ChatUI>();
            EnsureComponent<UI.TargetSelectionController>();
            EnsureComponent<UI.EnemyHealthBarController>();
            EnsureComponent<UI.QuestNpcIndicatorUI>();
            EnsureComponent<UI.InventoryUI>();
            EnsureComponent<UI.CharacterSheetUI>();
            EnsureComponent<UI.PartyUI>();
            EnsureComponent<UI.DeathScreenUI>();
            EnsureComponent<UI.VendorUI>();
            DontDestroyOnLoad(gameObject);
        }

        private T EnsureComponent<T>() where T : Component
        {
            T component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }
    }
}
