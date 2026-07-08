using System.Collections.Generic;
using Mirror;
using UnityEngine;
using AetherEcho.Core;

namespace AetherEcho.Combat
{
    public enum DamageType
    {
        Physical,
        Magical,
        True
    }

    public enum CombatRelation
    {
        Self,
        Ally,
        Enemy,
        Destructible
    }

    [RequireComponent(typeof(NetworkIdentity))]
    public class CombatantState : NetworkBehaviour
    {
        [SyncVar] public string CharacterClass = "Mage";
        [SyncVar] public int Level = 1;
        [SyncVar] public int CurrentHealth = 140;
        [SyncVar] public int CurrentMana = 160;
        [SyncVar] public int MaxHealth = 140;
        [SyncVar] public int MaxMana = 160;
        [SyncVar] public CombatRelation Relation = CombatRelation.Enemy;

        [SyncVar] public int Strength = 10;
        [SyncVar] public int Intelligence = 10;
        [SyncVar] public int Agility = 10;
        [SyncVar] public int Experience;
        [SyncVar] public int Gold;
        [SyncVar(hook = nameof(OnIsDeadChanged))] public bool IsDead;

        public int ExperienceToNextLevel => Mathf.Max(1, Level * GameConstants.ExperiencePerLevel);

        private int baseMaxHealth;
        private int baseMaxMana;
        private int baseStrength;
        private int baseIntelligence;
        private int baseAgility;
        private int equipmentHealthBonus;
        private int equipmentManaBonus;
        private int equipmentStrengthBonus;
        private int equipmentIntelligenceBonus;
        private int equipmentAgilityBonus;

        private readonly Dictionary<string, float> cooldownEndTimes = new Dictionary<string, float>();
        private readonly Dictionary<string, float> activeStatusEffects = new Dictionary<string, float>();
        private float manaRegenAccumulator;
        private float healthRegenAccumulator;

        public float GetCooldownRemainingSeconds(string spellId)
        {
            if (!cooldownEndTimes.TryGetValue(spellId, out float endTime))
            {
                return 0f;
            }

            return Mathf.Max(0f, endTime - Time.time);
        }

        [Server]
        public void ServerSetBaseStats(int health, int mana, int strength, int intelligence, int agility)
        {
            baseMaxHealth = health;
            baseMaxMana = mana;
            baseStrength = strength;
            baseIntelligence = intelligence;
            baseAgility = agility;
            MaxHealth = health + equipmentHealthBonus;
            MaxMana = mana + equipmentManaBonus;
            Strength = strength + equipmentStrengthBonus;
            Intelligence = intelligence + equipmentIntelligenceBonus;
            Agility = agility + equipmentAgilityBonus;
        }

        [Server]
        public void ServerRecalculateFromEquipment(Data.ItemStatModifiers bonuses)
        {
            equipmentHealthBonus = bonuses?.health ?? 0;
            equipmentManaBonus = bonuses?.mana ?? 0;
            equipmentStrengthBonus = bonuses?.strength ?? 0;
            equipmentIntelligenceBonus = bonuses?.intelligence ?? 0;
            equipmentAgilityBonus = bonuses?.agility ?? 0;
            MaxHealth = baseMaxHealth + equipmentHealthBonus;
            MaxMana = baseMaxMana + equipmentManaBonus;
            Strength = baseStrength + equipmentStrengthBonus;
            Intelligence = baseIntelligence + equipmentIntelligenceBonus;
            Agility = baseAgility + equipmentAgilityBonus;
            CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
            CurrentMana = Mathf.Min(CurrentMana, MaxMana);
        }

        private void Update()
        {
            if (netIdentity == null || !netIdentity.isServer)
            {
                return;
            }

            if (!IsDead)
            {
                TickManaRegeneration();
                TickHealthRegeneration();
            }
        }

        private void TickHealthRegeneration()
        {
            if (Relation == CombatRelation.Enemy || CurrentHealth >= MaxHealth)
            {
                return;
            }

            float regenPerSecond = GameConstants.BaseHealthRegenPerSecond
                                   + Strength * GameConstants.HealthRegenPerStrength;
            healthRegenAccumulator += regenPerSecond * Time.deltaTime;
            if (healthRegenAccumulator < 1f)
            {
                return;
            }

            int restored = Mathf.FloorToInt(healthRegenAccumulator);
            healthRegenAccumulator -= restored;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + restored);
        }

        private void TickManaRegeneration()
        {
            float regenPerSecond = GameConstants.BaseManaRegenPerSecond
                                   + Intelligence * GameConstants.ManaRegenPerIntelligence;
            manaRegenAccumulator += regenPerSecond * Time.deltaTime;
            if (manaRegenAccumulator < 1f)
            {
                return;
            }

            int restored = Mathf.FloorToInt(manaRegenAccumulator);
            manaRegenAccumulator -= restored;
            CurrentMana = Mathf.Min(MaxMana, CurrentMana + restored);
        }

        public float GetStatValue(string statName)
        {
            switch (statName)
            {
                case "Strength":
                    return Strength;
                case "Intelligence":
                    return Intelligence;
                case "Agility":
                    return Agility;
                default:
                    return 0f;
            }
        }

        public bool IsSpellOnCooldown(string spellId)
        {
            return cooldownEndTimes.TryGetValue(spellId, out float endTime) && Time.time < endTime;
        }

        public void TriggerCooldown(string spellId, float durationSeconds)
        {
            if (!isServer)
            {
                return;
            }

            cooldownEndTimes[spellId] = Time.time + durationSeconds;
        }

        public bool HasResource(string resourceType, int amount)
        {
            if (resourceType == "Mana")
            {
                return CurrentMana >= amount;
            }

            return true;
        }

        public void ConsumeResource(string resourceType, int amount)
        {
            if (!isServer)
            {
                return;
            }

            if (resourceType == "Mana")
            {
                CurrentMana = Mathf.Max(0, CurrentMana - amount);
            }
        }

        public bool IsEnemyWith(CombatantState other)
        {
            if (other == null || other == this)
            {
                return false;
            }

            return Relation == CombatRelation.Enemy || other.Relation == CombatRelation.Enemy;
        }

        public bool MatchesRelation(string relationName)
        {
            return Relation.ToString() == relationName;
        }

        [Server]
        public void TakeDamage(int damageAmount, DamageType damageType, CombatantState source)
        {
            if (damageAmount <= 0)
            {
                return;
            }

            if (IsDead)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - damageAmount);
            ThreatMatrix.RegisterThreat(source, this, damageAmount, ThreatContributionKind.Damage);
            NotifyAttackerHealthBarReveal(source);

            if (CurrentHealth <= 0)
            {
                ServerHandleDeath();
            }
        }

        [Server]
        private void NotifyAttackerHealthBarReveal(CombatantState source)
        {
            if (Relation != CombatRelation.Enemy || source == null)
            {
                return;
            }

            Player.NetworkedCombatant playerSource = source.GetComponent<Player.NetworkedCombatant>();
            if (playerSource != null && playerSource.connectionToClient != null)
            {
                TargetRevealHealthBarFromSpell(playerSource.connectionToClient);
            }
        }

        [TargetRpc]
        private void TargetRevealHealthBarFromSpell(NetworkConnectionToClient _)
        {
            UI.EnemyHealthBarController.Instance?.RevealFromSpellHit(this);
        }

        [Server]
        private void ServerHandleDeath()
        {
            IsDead = true;
            Debug.Log("[CombatantState] " + name + " defeated.");
            Player.NetworkedCombatant player = GetComponent<Player.NetworkedCombatant>();
            player?.ServerNotifyDeath();
        }

        [Server]
        public void ServerRespawn(int health, int mana)
        {
            IsDead = false;
            CurrentHealth = health;
            CurrentMana = mana;
        }

        private void OnIsDeadChanged(bool oldValue, bool newValue)
        {
            if (isLocalPlayer)
            {
                UI.DeathScreenUI.Instance?.SetDead(newValue);
            }
        }

        [Server]
        public void ApplyStatusEffect(string effectId, float durationSeconds, float potency)
        {
            activeStatusEffects[effectId] = Time.time + durationSeconds;
            Debug.Log("[CombatantState] Applied " + effectId + " to " + name + " for " + durationSeconds + "s.");
        }

        [Server]
        public void ServerGrantExperience(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Experience += amount;
            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                Level++;
                baseMaxHealth += 8;
                baseMaxMana += 10;
                MaxHealth = baseMaxHealth + equipmentHealthBonus;
                MaxMana = baseMaxMana + equipmentManaBonus;
                CurrentHealth = MaxHealth;
                CurrentMana = MaxMana;
            }
        }

        [Server]
        public void ServerHeal(int amount, CombatantState source)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            ThreatMatrix.RegisterThreat(source, this, amount, ThreatContributionKind.Healing);
        }
    }
}
