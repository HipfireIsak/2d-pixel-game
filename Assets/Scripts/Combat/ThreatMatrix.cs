using System.Collections.Generic;
using UnityEngine;

namespace AetherEcho.Combat
{
    public enum ThreatContributionKind
    {
        Proximity,
        Damage,
        Healing
    }

    public struct ThreatSample
    {
        public CombatantState Source;
        public float Amount;
        public ThreatContributionKind Kind;
        public float Timestamp;
    }

    /// <summary>
    /// Rolling 5-second threat matrix used by monster AI instead of binary tank aggro.
    /// </summary>
    public static class ThreatMatrix
    {
        private const float WindowSeconds = 5f;
        private const float ProximityWeight = 0.15f;
        private const float DamageWeight = 1f;
        private const float HealingWeight = 0.65f;

        private static readonly Dictionary<CombatantState, List<ThreatSample>> samplesByTarget =
            new Dictionary<CombatantState, List<ThreatSample>>();

        public static void RegisterThreat(
            CombatantState source,
            CombatantState target,
            float amount,
            ThreatContributionKind kind)
        {
            if (source == null || target == null || amount <= 0f)
            {
                return;
            }

            if (!samplesByTarget.TryGetValue(target, out List<ThreatSample> samples))
            {
                samples = new List<ThreatSample>();
                samplesByTarget[target] = samples;
            }

            samples.Add(new ThreatSample
            {
                Source = source,
                Amount = amount,
                Kind = kind,
                Timestamp = Time.time
            });
        }

        public static void RegisterProximityThreat(CombatantState source, CombatantState target, float distanceMeters)
        {
            if (source == null || target == null)
            {
                return;
            }

            float proximityScore = Mathf.Clamp01(1f - distanceMeters / 12f) * 10f;
            RegisterThreat(source, target, proximityScore, ThreatContributionKind.Proximity);
        }

        public static CombatantState GetHighestThreatTarget(CombatantState aggroOwner)
        {
            if (aggroOwner == null)
            {
                return null;
            }

            if (!samplesByTarget.TryGetValue(aggroOwner, out List<ThreatSample> samples))
            {
                return null;
            }

            PruneOldSamples(samples);
            Dictionary<CombatantState, float> totals = new Dictionary<CombatantState, float>();
            foreach (ThreatSample sample in samples)
            {
                if (sample.Source == null)
                {
                    continue;
                }

                float weight = sample.Kind switch
                {
                    ThreatContributionKind.Proximity => ProximityWeight,
                    ThreatContributionKind.Damage => DamageWeight,
                    ThreatContributionKind.Healing => HealingWeight,
                    _ => 1f
                };

                if (!totals.ContainsKey(sample.Source))
                {
                    totals[sample.Source] = 0f;
                }

                totals[sample.Source] += sample.Amount * weight;
            }

            CombatantState bestTarget = null;
            float bestScore = float.MinValue;
            foreach (KeyValuePair<CombatantState, float> pair in totals)
            {
                if (pair.Value > bestScore)
                {
                    bestScore = pair.Value;
                    bestTarget = pair.Key;
                }
            }

            return bestTarget;
        }

        private static void PruneOldSamples(List<ThreatSample> samples)
        {
            float cutoff = Time.time - WindowSeconds;
            samples.RemoveAll(sample => sample.Timestamp < cutoff);
        }
    }
}
