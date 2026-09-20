using System.Collections.Generic;

namespace Healer.Combat
{
    // Définitions de contenu (chargées depuis core/content/*.json) et états exposés au client.
    // Les noms de propriétés correspondent aux clés JSON sans tenir compte de la casse.

    public class CharacterDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        /// <summary>"tank", "dps" ou "healer".</summary>
        public string Role { get; set; } = "";
        public double MaxHp { get; set; }
        public double Atk { get; set; }
        public double Def { get; set; }
        /// <summary>Seul le soigneur en a besoin.</summary>
        public double? MaxMana { get; set; }
        public double? ManaRegenPerSec { get; set; }
    }

    public class SkillDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public double ManaCost { get; set; }
        public double CooldownMs { get; set; }
        /// <summary>"single" ou "all".</summary>
        public string Target { get; set; } = "single";
        public double? HealAmount { get; set; }
        public double? ShieldAmount { get; set; }
        public bool? Cleanse { get; set; }
    }

    /// <summary>Effet sur la durée appliqué à une unité (piloté par les données).</summary>
    public class EffectDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "";
        public double DamagePerTick { get; set; }
        public double TickMs { get; set; }
        public double DurationMs { get; set; }
    }

    public class BossActionDef
    {
        /// <summary>"attack", "bigAttack" ou "poison".</summary>
        public string Type { get; set; } = "";
        public double TelegraphMs { get; set; }
        /// <summary>ATK du boss × Multiplier. 0 = aucun dégât, seulement l'effet.</summary>
        public double? Multiplier { get; set; }
        public bool? HitsAll { get; set; }
        public string? EffectId { get; set; }
    }

    public class BossPhaseDef
    {
        public string Name { get; set; } = "";
        public double AtHpRatio { get; set; }
        public double TickMs { get; set; }
        public List<BossActionDef> Pattern { get; set; } = new List<BossActionDef>();
    }

    public class BossDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double MaxHp { get; set; }
        public double Atk { get; set; }
        public double Def { get; set; }
        public double TickMs { get; set; }
        public List<BossActionDef> Pattern { get; set; } = new List<BossActionDef>();
        public List<BossPhaseDef>? Phases { get; set; }
    }

    public class EncounterDef
    {
        public string Id { get; set; } = "";
        public BossDef Boss { get; set; } = new BossDef();
        public List<CharacterDef> Allies { get; set; } = new List<CharacterDef>();
        public List<EffectDef> Effects { get; set; } = new List<EffectDef>();
        public List<SkillDef> Skills { get; set; } = new List<SkillDef>();
        public uint Seed { get; set; }
    }

    /// <summary>Action du joueur, horodatée en ms depuis le début du combat (patron Command).</summary>
    public class Command
    {
        public double TimeMs { get; set; }
        public string SkillId { get; set; } = "";
        public string? TargetId { get; set; }
    }

    public class ActiveEffectState
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double MsRemaining { get; set; }
    }

    /// <summary>Vue d'une unité pour l'interface et les tests (copie : le client ne touche jamais l'état interne).</summary>
    public class UnitState
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public double MaxHp { get; set; }
        public double Hp { get; set; }
        public double MaxMana { get; set; }
        public double Mana { get; set; }
        public double Shield { get; set; }
        public bool Alive { get; set; }
        public List<ActiveEffectState> Effects { get; set; } = new List<ActiveEffectState>();
    }

    public class BossPhaseState
    {
        /// <summary>0 = phase initiale, 1 = première phase suivante, etc.</summary>
        public int Index { get; set; }
        public string Name { get; set; } = "";
    }

    public class Telegraph
    {
        public string Type { get; set; } = "";
        public double MsRemaining { get; set; }
        /// <summary>Durée totale du télégraphe (pour une jauge qui se vide : MsRemaining / TotalMs).</summary>
        public double TotalMs { get; set; }
    }

    public static class BattleResults
    {
        public const string Ongoing = "ongoing";
        public const string Victory = "victory";
        public const string Defeat = "defeat";
    }
}
