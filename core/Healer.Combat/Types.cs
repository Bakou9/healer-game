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
        /// <summary>Intervalle entre deux attaques automatiques, en ms (D-082). Absent = Battle.AllyAttackIntervalMs (comportement historique).
        /// Chaque personnage a sa propre cadence ; à cadence différente, l'attaque (Atk) est recalculée pour garder le même dps moyen.</summary>
        public double? AttackIntervalMs { get; set; }
        /// <summary>Seul le soigneur en a besoin.</summary>
        public double? MaxMana { get; set; }
        public double? ManaRegenPerSec { get; set; }

        /// <summary>Armure en pourcentage : réduit les dégâts PHYSIQUES reçus (0 à 80). S'ajoute à la défense fixe, qui s'applique ensuite.</summary>
        public double ArmorPct { get; set; }
        /// <summary>Chance d'esquiver un coup direct, en % (0 à 60) : aucun dégât, aucun effet appliqué.</summary>
        public double DodgePct { get; set; }
        /// <summary>Chance de coup critique en % (0 à 100) : pour un allié, ses attaques ; pour le soigneur, ses soins.</summary>
        public double CritPct { get; set; }
        /// <summary>Multiplicateur d'un critique en % (150 = +50 %).</summary>
        public double CritMultPct { get; set; } = 150;
        /// <summary>Menace générée en % (100 = normale) : le tank en génère beaucoup plus, ce qui attire les coups du boss.</summary>
        public double ThreatMod { get; set; } = 100;
        /// <summary>Type des dégâts infligés au boss (« physical » ou « magic » aujourd'hui).</summary>
        public string DamageType { get; set; } = "physical";
        /// <summary>Résistances par type de dégât, en % (négatif = vulnérable) : -100 à 90.</summary>
        public Dictionary<string, int>? Resist { get; set; }
    }

    public class SkillDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public double ManaCost { get; set; }
        public double CooldownMs { get; set; }
        /// <summary>Temps d'incantation en ms : 0 = instantané. Le sort se lance au bout de ce temps (mana et recharge à l'achèvement).</summary>
        public double CastMs { get; set; }
        /// <summary>"single" ou "all".</summary>
        public string Target { get; set; } = "single";
        public double? HealAmount { get; set; }
        public double? ShieldAmount { get; set; }
        public bool? Cleanse { get; set; }
        /// <summary>Sort de capstone (E02-T05) : absent de la barre de base, ajouté seulement si le talent qui le
        /// débloque (TalentOptionDef.UnlocksSkill) est choisi. Jamais vrai pour heal_single/heal_aoe/shield/purge.</summary>
        public bool Capstone { get; set; }
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
        /// <summary>Type de dégâts de l'effet (« poison », « fire »…) : les résistances s'y appliquent. Vide = aucune résistance.</summary>
        public string DamageType { get; set; } = "";
    }

    public class BossActionDef
    {
        /// <summary>"attack", "bigAttack" (zone), "focusAttack" (un allié fragile, annoncé avec sa cible) ou "poison".</summary>
        public string Type { get; set; } = "";
        public double TelegraphMs { get; set; }
        /// <summary>ATK du boss × Multiplier. 0 = aucun dégât, seulement l'effet.</summary>
        public double? Multiplier { get; set; }
        public bool? HitsAll { get; set; }
        public string? EffectId { get; set; }
        /// <summary>Type de dégâts de l'action ; vide = celui du boss.</summary>
        public string? DamageType { get; set; }
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

        /// <summary>Enrage optionnel : passé un certain temps, les dégâts directs du boss montent par paliers. Absent = aucun.</summary>
        public EnrageDef? Enrage { get; set; }

        /// <summary>Type de dégâts de ses attaques (« physical » par défaut).</summary>
        public string DamageType { get; set; } = "physical";
        public double CritPct { get; set; }
        public double CritMultPct { get; set; } = 150;
        /// <summary>Résistances du boss par type de dégât reçu, en % (négatif = vulnérable).</summary>
        public Dictionary<string, int>? Resist { get; set; }
        /// <summary>« random » (défaut) : cible au hasard ; « threat » : cible tirée au sort en proportion de la menace.</summary>
        public string Targeting { get; set; } = "random";
    }

    /// <summary>
    /// Enrage d'un boss : à AfterMs, puis toutes les EveryMs, ses attaques infligent Pct % de dégâts de plus (cumulés :
    /// au n-ième palier, +n × Pct %). Pousse à finir le combat plutôt qu'à survivre indéfiniment (D-050).
    /// </summary>
    public class EnrageDef
    {
        public double AfterMs { get; set; }
        public double EveryMs { get; set; }
        public double Pct { get; set; }
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
        /// <summary>Menace accumulée (attaques et soins) : plus elle est haute, plus le boss vise cette unité.</summary>
        public double Threat { get; set; }
        public double ArmorPct { get; set; }
        public double DodgePct { get; set; }
        public double CritPct { get; set; }
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
        /// <summary>Allié visé par une attaque ciblée (« focusAttack »), connu dès le début du télégraphe ; null pour les autres attaques.</summary>
        public string? TargetId { get; set; }
    }

    public static class BattleResults
    {
        public const string Ongoing = "ongoing";
        public const string Victory = "victory";
        public const string Defeat = "defeat";
    }
}

namespace Healer.Combat
{
    /// <summary>Niveau de la campagne : un boss, un déblocage, des récompenses (core/content/levels.json).</summary>
    public class LevelDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string BossId { get; set; } = "";
        /// <summary>Niveau à terminer avant celui-ci ; null = disponible dès le début.</summary>
        public string? Requires { get; set; }
        /// <summary>Or gagné à la première victoire.</summary>
        public int RewardGold { get; set; }
        /// <summary>Or gagné à chaque victoire suivante.</summary>
        public int RepeatGold { get; set; }
        /// <summary>Or gagné pour chaque étoile obtenue pour la première fois.</summary>
        public int StarBonusGold { get; set; }
        /// <summary>3ᵉ étoile : victoire sans allié K.O. ET au plus ce total de dégâts encaissés (boucliers et purges comptent).</summary>
        public double ThreeStarMaxDamageTaken { get; set; }
        /// <summary>XP du Soigneur gagnée à la première victoire (E02-T06, D-084). Même logique que l'or.</summary>
        public int RewardXp { get; set; }
        /// <summary>XP du Soigneur gagnée à chaque victoire suivante (E02-T06, D-084).</summary>
        public int RepeatXp { get; set; }
    }
}

namespace Healer.Combat
{
    /// <summary>Incantation en cours du soigneur (lecture seule, pour l'interface).</summary>
    public class CastState
    {
        public string SkillId { get; set; } = "";
        public string? TargetId { get; set; }
        public double TotalMs { get; set; }
        public double ElapsedMs { get; set; }
        /// <summary>0 à 1.</summary>
        public double Progress => TotalMs <= 0 ? 1 : System.Math.Max(0, System.Math.Min(1, ElapsedMs / TotalMs));
    }
}
