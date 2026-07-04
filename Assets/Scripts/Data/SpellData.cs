using System;
using System.Collections.Generic;

namespace AetherEcho.Data
{
    [Serializable]
    public class SpellRegistry
    {
        public List<SpellData> spells = new List<SpellData>();
    }

    [Serializable]
    public class SpellData
    {
        public string id;
        public string name;
        public string description;
        public List<string> allowed_classes = new List<string>();
        public int required_level;
        public string icon_texture;
        public string vfx_prefab;
        public CastingRules casting_rules = new CastingRules();
        public TargetingData targeting = new TargetingData();
        public PayloadData payload = new PayloadData();
    }

    [Serializable]
    public class CastingRules
    {
        public float cast_time_seconds;
        public float cooldown_seconds;
        public string resource_type = "Mana";
        public int resource_cost;
        public bool requires_line_of_sight;
        public bool can_cast_while_moving;
    }

    [Serializable]
    public class TargetingData
    {
        public string type = "Point";
        public float range_meters = 8f;
        public float radius_meters = 1.5f;
        public int max_targets = 1;
        public List<string> valid_relations = new List<string>();
    }

    [Serializable]
    public class PayloadData
    {
        public int instant_damage_magical;
        public int instant_damage_physical;
        public string damage_scaling_stat = "Intelligence";
        public float scaling_factor = 1f;
        public List<StatusEffectReference> applied_status_effects = new List<StatusEffectReference>();
        public SpawnEntityConfig spawn_entity_on_impact;
    }

    [Serializable]
    public class StatusEffectReference
    {
        public string effect_id;
        public float duration_seconds;
        public float potency;
    }

    [Serializable]
    public class SpawnEntityConfig
    {
        public string blueprint_id;
        public float duration_seconds;
    }

    [Serializable]
    public class ClassRegistry
    {
        public List<ClassData> classes = new List<ClassData>();
    }

    [Serializable]
    public class ClassData
    {
        public string class_name;
        public StatBlock base_stats = new StatBlock();
        public StatBlock stat_growth_per_level = new StatBlock();
        public List<string> weapon_proficiencies = new List<string>();
    }

    [Serializable]
    public class StatBlock
    {
        public int health;
        public int mana;
        public int strength;
        public int intelligence;
        public int agility;
    }
}
