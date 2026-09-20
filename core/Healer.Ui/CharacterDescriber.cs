using System.Collections.Generic;
using System.Linq;
using Healer.Combat;

namespace Healer.Ui
{
    /// <summary>Une ligne de la fiche d'un personnage : un libellé et une valeur (morceaux ; ceux marqués Changed sont en vert).</summary>
    public sealed class CharacterLine
    {
        public string Label { get; }
        public IReadOnlyList<SkillSpan> Value { get; }
        public CharacterLine(string label, IReadOnlyList<SkillSpan> value) { Label = label; Value = value; }
    }

    /// <summary>Fiche d'un personnage pour le menu de pause (D-059) : nom, rôle et statistiques.</summary>
    public sealed class CharacterSheet
    {
        public string Name { get; }
        public string Role { get; }
        public IReadOnlyList<CharacterLine> Lines { get; }
        public CharacterSheet(string name, string role, IReadOnlyList<CharacterLine> lines) { Name = name; Role = role; Lines = lines; }
    }

    /// <summary>
    /// Statistiques d'un personnage avec et sans les bonus d'équipement et de talents : la valeur de base s'écrit normalement,
    /// la valeur modifiée entre parenthèses (Changed), comme pour les sorts. Les PV et le bouclier sont ceux du moment.
    /// Pur et testé ; tous les nombres passent par Format (tronqués).
    /// </summary>
    public static class CharacterDescriber
    {
        public static CharacterSheet Sheet(CharacterDef baseDef, CharacterDef current, UnitState? live = null)
        {
            var lines = new List<CharacterLine>();
            var hp = new List<SkillSpan>();
            if (live != null) hp.Add(new SkillSpan(Format.Number(live.Hp) + " / "));
            hp.AddRange(SkillDescriber.Pair(Format.Number(baseDef.MaxHp), Format.Number(current.MaxHp)));
            lines.Add(new CharacterLine("PV", hp));
            if (live != null) lines.Add(new CharacterLine("Bouclier", One(live.Shield > 0 ? Format.Number(live.Shield) + " PV" : "aucun")));
            if (current.Role != "healer" && (baseDef.Atk > 0 || current.Atk > 0))
            {
                var atk = SkillDescriber.Pair(Format.Number(baseDef.Atk), Format.Number(current.Atk));
                atk.Add(new SkillSpan(current.DamageType == "magic" ? " (magique)" : " (physique)"));
                lines.Add(new CharacterLine("Attaque", atk));
            }
            lines.Add(new CharacterLine("Défense", SkillDescriber.Pair(Format.Number(baseDef.Def), Format.Number(current.Def))));
            lines.Add(new CharacterLine("Armure", SkillDescriber.Pair(Pct(baseDef.ArmorPct), Pct(current.ArmorPct))));
            lines.Add(new CharacterLine("Esquive", SkillDescriber.Pair(Pct(baseDef.DodgePct), Pct(current.DodgePct))));
            var crit = SkillDescriber.Pair(Pct(baseDef.CritPct), Pct(current.CritPct));
            crit.Add(new SkillSpan(" · dégâts "));
            crit.AddRange(SkillDescriber.Pair(Pct(baseDef.CritMultPct), Pct(current.CritMultPct)));
            lines.Add(new CharacterLine(current.Role == "healer" ? "Critique (soins)" : "Critique", crit));
            lines.Add(new CharacterLine("Menace", SkillDescriber.Pair(Pct(baseDef.ThreatMod), Pct(current.ThreatMod))));
            if (baseDef.MaxMana.HasValue || current.MaxMana.HasValue)
            {
                lines.Add(new CharacterLine("Mana", SkillDescriber.Pair(Format.Number(baseDef.MaxMana ?? 0), Format.Number(current.MaxMana ?? 0))));
                lines.Add(new CharacterLine("Régén. mana", SkillDescriber.Pair(Format.Decimal(baseDef.ManaRegenPerSec ?? 0) + "/s", Format.Decimal(current.ManaRegenPerSec ?? 0) + "/s")));
            }
            var resist = current.Resist?.Where(kv => kv.Value != 0).Select(kv => TypeName(kv.Key) + " " + kv.Value + " %").ToList();
            if (resist != null && resist.Count > 0) lines.Add(new CharacterLine("Résistances", One(string.Join(", ", resist))));
            return new CharacterSheet(current.Name, RoleName(current.Role), lines);
        }

        private static List<SkillSpan> One(string text) => new List<SkillSpan> { new SkillSpan(text) };
        private static string Pct(double value) => Format.Number(value) + " %";

        public static string RoleName(string role) => role == "tank" ? "Tank" : role == "healer" ? "Soin" : "Dégâts";

        public static string TypeName(string type) => type switch
        {
            "physical" => "physique",
            "magic" => "magie",
            "fire" => "feu",
            "poison" => "poison",
            _ => type,
        };
    }
}
