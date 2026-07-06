using UnityEngine;
using AetherEcho.Player;

namespace AetherEcho.UI
{
    public class PartyUI : MonoBehaviour
    {
        public static PartyUI Instance { get; private set; }

        private NetworkedCombatant localPlayer;
        private bool showParty;
        private string partyRoster = string.Empty;
        private uint partyLeaderNetId;
        private string inviteTargetName = string.Empty;
        private uint pendingInviterNetId;

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

        public void SetPartyRoster(string roster, uint leaderNetId)
        {
            partyRoster = roster ?? string.Empty;
            partyLeaderNetId = leaderNetId;
        }

        public void ShowInvite(string inviterName, uint inviterNetId)
        {
            pendingInviterNetId = inviterNetId;
            SetToastInvite(inviterName);
        }

        private string inviteToast;
        private float inviteTimer;

        private void SetToastInvite(string inviterName)
        {
            inviteToast = inviterName + " invited you to a party. Press P to accept.";
            inviteTimer = 15f;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                showParty = !showParty;
            }

            if (inviteTimer > 0f)
            {
                inviteTimer -= Time.deltaTime;
            }
        }

        private void OnGUI()
        {
            if (localPlayer == null)
            {
                return;
            }

            EnsureStyles();

            if (inviteTimer > 0f && !string.IsNullOrEmpty(inviteToast))
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 180, 80, 360, 28), inviteToast, bodyStyle);
                if (GUI.Button(new Rect(Screen.width * 0.5f - 40, 112, 80, 24), "Accept", buttonStyle))
                {
                    localPlayer.CmdAcceptPartyInvite(pendingInviterNetId);
                    inviteTimer = 0f;
                }
            }

            if (!showParty)
            {
                return;
            }

            float width = 260f;
            var panel = new Rect(24, 360, width, 200);
            GUI.Box(panel, string.Empty);
            GUI.Label(new Rect(panel.x + 12, panel.y + 8, width - 24, 22), "Party (P)", titleStyle);
            GUI.Label(new Rect(panel.x + 12, panel.y + 34, width - 24, 80), string.IsNullOrEmpty(partyRoster) ? "Solo" : partyRoster, bodyStyle);

            inviteTargetName = GUI.TextField(new Rect(panel.x + 12, panel.y + 118, width - 24, 22), inviteTargetName);
            if (GUI.Button(new Rect(panel.x + 12, panel.y + 146, 110, 24), "Invite", buttonStyle))
            {
                localPlayer.CmdInviteToPartyByName(inviteTargetName);
            }

            if (GUI.Button(new Rect(panel.x + 130, panel.y + 146, 110, 24), "Leave", buttonStyle))
            {
                localPlayer.CmdLeaveParty();
                partyRoster = string.Empty;
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 11 };
        }
    }
}
