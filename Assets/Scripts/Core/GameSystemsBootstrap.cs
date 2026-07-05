using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Content;
using AetherEcho.Quests;
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
            EnsureComponent<SpellEngine>();
            EnsureComponent<SpellVfxPlayer>();
            EnsureComponent<QuestManager>();
            EnsureComponent<WorldContentSpawner>();
            EnsureComponent<UI.GameplayHud>();
            EnsureComponent<UI.SpellGroundTargeting>();
            EnsureComponent<UI.QuestDialogUI>();
            EnsureComponent<UI.MinimapUI>();
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
