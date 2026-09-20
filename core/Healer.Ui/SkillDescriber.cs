using System.Collections.Generic;
using Healer.Combat;

namespace Healer.Ui
{
    /// <summary>Une ligne de la fiche d'un sort : sa valeur de base et sa valeur avec l'équipement et les talents.</summary>
    public sealed class SkillStat
    {
        public string Label { get; }
        /// <summary>Valeur sans aucun bonus (données brutes du sort).</summary>
        public string Base { get; }
        /// <summary>Valeur avec les bonus d'équipement et de talents du joueur.</summary>
        public string Current { get; }
        public bool Changed => Base != Current;

        public SkillStat(string label, string baseValue, string currentValue)
        {
            Label = label;
            Base = baseValue;
            Current = currentValue;
        }
    }

    /// <summary>
    /// Fiche détaillée d'un sort pour le menu de pause : effet, coût, incantation, recharge, cible, avec et sans
    /// bonus. Pur et testé ; tous les nombres passent par Format (tronqués, jamais arrondis vers le haut).
    /// </summary>
    public static class SkillDescriber
    {
        public static IReadOnlyList<SkillStat> Stats(SkillDef baseSkill, SkillDef current)
        {
            var list = new List<SkillStat>();
            if (baseSkill.HealAmount.HasValue || current.HealAmount.HasValue)
                list.Add(new SkillStat("Soin", Heal(baseSkill), Heal(current)));
            if (baseSkill.ShieldAmount.HasValue || current.ShieldAmount.HasValue)
                list.Add(new SkillStat("Bouclier", Shield(baseSkill), Shield(current)));
            if (baseSkill.Cleanse == true || current.Cleanse == true)
                list.Add(new SkillStat("Effet", "Retire les effets négatifs", "Retire les effets négatifs"));
            list.Add(new SkillStat("Coût", Mana(baseSkill), Mana(current)));
            list.Add(new SkillStat("Incantation", Cast(baseSkill), Cast(current)));
            list.Add(new SkillStat("Recharge", Cooldown(baseSkill), Cooldown(current)));
            list.Add(new SkillStat("Cible", Target(baseSkill), Target(current)));
            return list;
        }

        private static string Heal(SkillDef s) => s.HealAmount.HasValue ? Format.Number(s.HealAmount.Value) + " PV" : "—";
        private static string Shield(SkillDef s) => s.ShieldAmount.HasValue ? Format.Number(s.ShieldAmount.Value) + " PV" : "—";
        private static string Mana(SkillDef s) => Format.Number(s.ManaCost) + " mana";
        private static string Cast(SkillDef s) => s.CastMs > 0 ? Format.Seconds(s.CastMs) : "Instantané";
        private static string Cooldown(SkillDef s) => s.CooldownMs > 0 ? Format.Seconds(s.CooldownMs) : "Aucune";
        private static string Target(SkillDef s) => s.Target == "all" ? "Toute l'équipe" : "1 allié";
    }
}
