using System.Collections.Generic;
using Mirror;
using UnityEngine;
using AetherEcho.Combat;
using AetherEcho.Core;
using AetherEcho.Enemies;
using AetherEcho.Player;
using AetherEcho.Rendering;
using AetherEcho.Social;
using AetherEcho.World;

namespace AetherEcho.World
{
    public class DungeonInstanceManager : MonoBehaviour
    {
        public static DungeonInstanceManager Instance { get; private set; }

        private static readonly Vector3 DungeonOrigin = new Vector3(600f, 0f, 600f);

        private bool dungeonBuilt;
        private bool bossAlive;
        private uint activePartyLeaderNetId;

        private void Awake()
        {
            Instance = this;
        }

        [Server]
        public void ServerTryEnterDungeon(NetworkedCombatant player, out string message)
        {
            message = string.Empty;
            if (player == null)
            {
                message = "Invalid player.";
                return;
            }

            List<uint> members = PartyManager.Instance != null
                ? PartyManager.Instance.ServerGetPartyMemberNetIds(player.netId)
                : new List<uint> { player.netId };

            if (!dungeonBuilt)
            {
                BuildDungeonShell();
            }

            activePartyLeaderNetId = members.Count > 0 ? members[0] : player.netId;
            bossAlive = true;
            SpawnBoss();

            int index = 0;
            foreach (uint memberNetId in members)
            {
                if (!NetworkServer.spawned.TryGetValue(memberNetId, out NetworkIdentity identity))
                {
                    continue;
                }

                NetworkedCombatant member = identity.GetComponent<NetworkedCombatant>();
                if (member == null)
                {
                    continue;
                }

                Vector3 spawn = DungeonOrigin + new Vector3(-4f + index * 2f, 0f, -6f);
                member.ServerTeleport(spawn);
                member.TargetShowToast(member.connectionToClient, "Entered Echo Vault. Defeat the Vault Warden!");
                index++;
            }

            message = "Dungeon entered.";
        }

        [Server]
        public void ServerExitDungeon(NetworkedCombatant player)
        {
            if (player == null)
            {
                return;
            }

            player.ServerTeleport(GameConstants.HubSpawnPosition);
            player.TargetShowToast(player.connectionToClient, "Returned to the Chrono Hub.");
        }

        [Server]
        public void ServerNotifyEnemyKilled(string enemyTypeId, CombatantState killer)
        {
            if (enemyTypeId != "vault_warden")
            {
                return;
            }

            bossAlive = false;
            NetworkedCombatant player = killer?.GetComponent<NetworkedCombatant>();
            if (player != null)
            {
                List<uint> members = PartyManager.Instance != null
                    ? PartyManager.Instance.ServerGetPartyMemberNetIds(player.netId)
                    : new List<uint> { player.netId };

                foreach (uint memberNetId in members)
                {
                    if (!NetworkServer.spawned.TryGetValue(memberNetId, out NetworkIdentity identity))
                    {
                        continue;
                    }

                    NetworkedCombatant member = identity.GetComponent<NetworkedCombatant>();
                    member?.TargetShowToast(member.connectionToClient, "Vault Warden defeated! Press E at the exit crystal to leave.");
                }
            }
        }

        public bool IsBossAlive => bossAlive;

        [Server]
        private void BuildDungeonShell()
        {
            dungeonBuilt = true;
            var root = new GameObject("EchoVaultInstance");
            root.transform.position = DungeonOrigin;

            CreateFloorTile(root.transform, new Vector3(0f, 0f, 0f), 16f);
            CreateFloorTile(root.transform, new Vector3(0f, 0f, 8f), 12f);

            ArtCatalog art = ArtAssetResolver.Catalog;
            Sprite exitSprite = art != null ? (art.spellPulse != null ? art.spellPulse : art.timeEcho) : null;
            var exitCrystal = WorldPropBuilder.CreateBillboardProp(
                root.transform,
                "VaultExitCrystal",
                exitSprite,
                new Vector3(0f, 0f, 10f),
                GameConstants.PlayerVisualScale * 1.1f,
                addObstacleCollider: false,
                isTree: false,
                facingMode: Rendering.SpriteFacingMode.FixedSouth);
            PixelBillboardVisual exitVisual = exitCrystal.GetComponent<PixelBillboardVisual>();
            if (exitVisual != null && exitVisual.SpriteRenderer != null)
            {
                exitVisual.SpriteRenderer.color = new Color(0.45f, 0.95f, 1f, 1f);
            }

            exitCrystal.AddComponent<DungeonExitInteractable>();
            InteractableWorldHint exitHint = exitCrystal.AddComponent<InteractableWorldHint>();
            exitHint.Configure("[E] Return to Hub");
            SphereCollider exitCollider = exitCrystal.AddComponent<SphereCollider>();
            exitCollider.radius = 1.2f;
            exitCollider.isTrigger = true;

            ArtCatalog artForDecor = ArtAssetResolver.Catalog;
            if (artForDecor != null)
            {
                DungeonDecorBuilder.BuildEchoVaultDecor(root.transform, artForDecor);
            }
        }

        [Server]
        private void SpawnBoss()
        {
            GameObject existing = GameObject.Find("VaultWarden");
            if (existing != null)
            {
                NetworkServer.Destroy(existing);
            }

            Vector3 bossPosition = DungeonOrigin + new Vector3(0f, 0f, 4f);
            NetworkedEnemy boss = WorldContentSpawner.SpawnEnemyPublic("vault_warden", bossPosition, 8);
            if (boss != null)
            {
                boss.gameObject.name = "VaultWarden";
                VaultWardenBossController controller = boss.gameObject.AddComponent<VaultWardenBossController>();
                controller.ServerInitialize();
            }
        }

        private static void CreateFloorTile(Transform parent, Vector3 localPosition, float size)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "VaultFloor";
            floor.transform.SetParent(parent, false);
            floor.transform.localPosition = localPosition;
            floor.transform.localScale = new Vector3(size, 0.1f, size);
            Object.Destroy(floor.GetComponent<Collider>());
            Renderer renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.25f, 0.15f, 0.35f);
            }
        }
    }
}
