using UnityEngine;
using AetherEcho.Player;

namespace AetherEcho.UI
{
    public class DeathScreenUI : MonoBehaviour
    {
        public static DeathScreenUI Instance { get; private set; }

        private NetworkedCombatant localPlayer;
        private bool isDead;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;

        private void Awake()
        {
            Instance = this;
        }

        public void BindLocalPlayer(NetworkedCombatant player)
        {
            localPlayer = player;
        }

        public void SetDead(bool dead)
        {
            isDead = dead;
        }

        private void OnGUI()
        {
            if (!isDead || localPlayer == null)
            {
                return;
            }

            EnsureStyles();
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), string.Empty);
            GUI.color = previous;

            float width = 360f;
            var panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - 160) * 0.5f, width, 160);
            GUI.Box(panel, string.Empty);
            GUI.Label(new Rect(panel.x + 12, panel.y + 16, width - 24, 28), "You have been defeated", titleStyle);
            GUI.Label(new Rect(panel.x + 12, panel.y + 50, width - 24, 40),
                "Release your spirit to respawn at the Chrono Hub with partial health.",
                bodyStyle);

            if (GUI.Button(new Rect(panel.x + (width - 140) * 0.5f, panel.y + 100, 140, 32), "Release Spirit", buttonStyle))
            {
                localPlayer.CmdRespawn();
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 13 };
        }
    }
}
