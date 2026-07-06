using System.Collections.Generic;
using Mirror;
using UnityEngine;
using AetherEcho.Player;

namespace AetherEcho.Social
{
    public class Party
    {
        public uint leaderNetId;
        public readonly List<uint> memberNetIds = new List<uint>();
        public const int MaxMembers = 4;

        public bool Contains(uint netId)
        {
            return memberNetIds.Contains(netId);
        }
    }

    public class PartyManager : MonoBehaviour
    {
        public static PartyManager Instance { get; private set; }

        private readonly Dictionary<uint, Party> partyByMemberNetId = new Dictionary<uint, Party>();
        private readonly Dictionary<uint, uint> pendingInviteFromNetId = new Dictionary<uint, uint>();

        private void Awake()
        {
            Instance = this;
        }

        [Server]
        public bool ServerTryInvite(NetworkedCombatant inviter, uint targetNetId, out string message)
        {
            message = string.Empty;
            if (inviter == null)
            {
                message = "Invalid inviter.";
                return false;
            }

            if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
            {
                message = "Player not found.";
                return false;
            }

            NetworkedCombatant target = targetIdentity.GetComponent<NetworkedCombatant>();
            if (target == null || target.netId == inviter.netId)
            {
                message = "Cannot invite that player.";
                return false;
            }

            Party inviterParty = GetOrCreateParty(inviter.netId);
            if (!inviterParty.Contains(inviter.netId) || inviterParty.leaderNetId != inviter.netId)
            {
                message = "Only the party leader can invite.";
                return false;
            }

            if (inviterParty.memberNetIds.Count >= Party.MaxMembers)
            {
                message = "Party is full.";
                return false;
            }

            if (partyByMemberNetId.ContainsKey(targetNetId))
            {
                message = target.DisplayName + " is already in a party.";
                return false;
            }

            pendingInviteFromNetId[targetNetId] = inviter.netId;
            target.TargetPartyInvite(target.connectionToClient, inviter.DisplayName, inviter.netId);
            message = "Invited " + target.DisplayName + " to your party.";
            return true;
        }

        [Server]
        public bool ServerTryAcceptInvite(NetworkedCombatant target, uint inviterNetId, out string message)
        {
            message = string.Empty;
            if (target == null)
            {
                message = "Invalid player.";
                return false;
            }

            if (!pendingInviteFromNetId.TryGetValue(target.netId, out uint pendingInviter)
                || pendingInviter != inviterNetId)
            {
                message = "No pending invite.";
                return false;
            }

            pendingInviteFromNetId.Remove(target.netId);
            if (partyByMemberNetId.ContainsKey(target.netId))
            {
                message = "You are already in a party.";
                return false;
            }

            if (!partyByMemberNetId.TryGetValue(inviterNetId, out Party party))
            {
                party = new Party { leaderNetId = inviterNetId };
                party.memberNetIds.Add(inviterNetId);
                partyByMemberNetId[inviterNetId] = party;
            }

            if (party.memberNetIds.Count >= Party.MaxMembers)
            {
                message = "Party is full.";
                return false;
            }

            party.memberNetIds.Add(target.netId);
            partyByMemberNetId[target.netId] = party;
            BroadcastPartyUpdate(party);
            message = "Joined party.";
            return true;
        }

        [Server]
        public void ServerLeaveParty(NetworkedCombatant player)
        {
            if (player == null || !partyByMemberNetId.TryGetValue(player.netId, out Party party))
            {
                return;
            }

            party.memberNetIds.Remove(player.netId);
            partyByMemberNetId.Remove(player.netId);
            if (party.leaderNetId == player.netId)
            {
                if (party.memberNetIds.Count == 0)
                {
                    return;
                }

                party.leaderNetId = party.memberNetIds[0];
            }

            if (party.memberNetIds.Count <= 1)
            {
                uint lastMember = party.memberNetIds.Count == 1 ? party.memberNetIds[0] : 0;
                foreach (uint memberNetId in new List<uint>(party.memberNetIds))
                {
                    partyByMemberNetId.Remove(memberNetId);
                }

                if (lastMember != 0 && NetworkServer.spawned.TryGetValue(lastMember, out NetworkIdentity identity))
                {
                    identity.GetComponent<NetworkedCombatant>()?.TargetPartyUpdate(
                        identity.connectionToClient,
                        string.Empty,
                        0);
                }

                return;
            }

            BroadcastPartyUpdate(party);
        }

        [Server]
        public List<uint> ServerGetPartyMemberNetIds(uint playerNetId)
        {
            if (partyByMemberNetId.TryGetValue(playerNetId, out Party party))
            {
                return new List<uint>(party.memberNetIds);
            }

            return new List<uint> { playerNetId };
        }

        [Server]
        private Party GetOrCreateParty(uint leaderNetId)
        {
            if (partyByMemberNetId.TryGetValue(leaderNetId, out Party existing))
            {
                return existing;
            }

            var party = new Party { leaderNetId = leaderNetId };
            party.memberNetIds.Add(leaderNetId);
            partyByMemberNetId[leaderNetId] = party;
            return party;
        }

        [Server]
        private void BroadcastPartyUpdate(Party party)
        {
            string roster = BuildRosterText(party);
            foreach (uint memberNetId in party.memberNetIds)
            {
                if (!NetworkServer.spawned.TryGetValue(memberNetId, out NetworkIdentity identity))
                {
                    continue;
                }

                NetworkedCombatant member = identity.GetComponent<NetworkedCombatant>();
                member?.TargetPartyUpdate(member.connectionToClient, roster, party.leaderNetId);
            }
        }

        private static string BuildRosterText(Party party)
        {
            var names = new List<string>();
            foreach (uint memberNetId in party.memberNetIds)
            {
                if (NetworkServer.spawned.TryGetValue(memberNetId, out NetworkIdentity identity))
                {
                    NetworkedCombatant member = identity.GetComponent<NetworkedCombatant>();
                    if (member != null)
                    {
                        string prefix = party.leaderNetId == memberNetId ? "*" : "-";
                        names.Add(prefix + " " + member.DisplayName);
                    }
                }
            }

            return string.Join("\n", names);
        }
    }
}
