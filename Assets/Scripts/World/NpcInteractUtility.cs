using AetherEcho.Player;

namespace AetherEcho.World
{
    public static class NpcInteractUtility
    {
        public static NetworkedCombatant FindLocalPlayer()
        {
            NetworkedCombatant[] players = UnityEngine.Object.FindObjectsOfType<NetworkedCombatant>();
            foreach (NetworkedCombatant player in players)
            {
                if (player.isLocalPlayer)
                {
                    return player;
                }
            }

            return null;
        }
    }
}
