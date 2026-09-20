using System.Collections.Generic;
using System.Text;
using Healer.Combat;

namespace Healer.Ui
{
    /// <summary>Un morceau de texte d'une fiche de sort ; Changed = valeur modifiée par l'équipement ou un talent (affichée en vert).</summary>
    public readonly struct SkillSpan
    {
        public string Text { get; }
        public bool Changed { get; }
        public SkillSpan(string text, bool changed = false) { Text = text; Changed = changed; }
        public override string ToString() => Changed ? "(" + Text + ")" : Text;
    }

    /// <summary>
    /// Fiche d'un sort pour le menu de pause (D-055) : nom, coût, incantation, recharge, cible et description où les
    /// valeurs du sort sont insérées. Une valeur modifiée par l'équipement ou les talents s'écrit « base(nouvelle) »,
    /// la nouvelle valeur étant marquée Changed (le client la met en vert). Pur et testé ; tous les nombres passent
    /// par Format (tronqués, jamais arrondis vers le haut).
    /// </summary>
    public sealed class SkillSheet
    {
        public string Name { get; }
        /// <summary>« 45(41) mana ».</summary>
        public IReadOnlyList<SkillSpan> Cost { get; }
        /// <summary>« Incantation : instantanée » ou « Incantation : 1s(0,9s) ».</summary>
        public IReadOnlyList<SkillSpan> Cast { get; }
        /// <summary>« CD : 5s » ou « CD : aucun ».</summary>
        public IReadOnlyList<SkillSpan> Cooldown { get; }
        public string Target { get; }
        public IReadOnlyList<SkillSpan> Description { get; }

        public SkillSheet(string name, IReadOnlyList<SkillSpan> cost, IReadOnlyList<SkillSpan> cast, IReadOnlyList<SkillSpan> cooldown, string target, IReadOnlyList<SkillSpan> description)
        {
            Name = name; Cost = cost; Cast = cast; Cooldown = cooldown; Target = target; Description = description;
        }

        /// <summary>Texte brut (sans couleurs) d'une suite de morceaux, tel qu'affiché.</summary>
        public static string Plain(IReadOnlyList<SkillSpan> spans)
        {
            var sb = new StringBuilder();
            foreach (var s in spans) sb.Append(s.ToString());
            return sb.ToString();
        }
    }

    public static class SkillDescriber
    {
        /// <summary>
        /// Les descriptions des données (skills.json) contiennent des marqueurs remplacés par les valeurs réelles :
        /// {heal} {shield} {mana} {cast} {cd}. Ex. « Soigne tous les alliés vivants de {heal} PV, mais… ».
        /// </summary>
        public static SkillSheet Sheet(SkillDef baseSkill, SkillDef current)
        {
            var cost = Pair(Format.Number(baseSkill.ManaCost), Format.Number(current.ManaCost));
            cost.Add(new SkillSpan(" mana"));
            var cast = new List<SkillSpan> { new SkillSpan("Incantation : ") };
            cast.AddRange(Pair(CastText(baseSkill), CastText(current)));
            var cd = new List<SkillSpan> { new SkillSpan("CD : ") };
            cd.AddRange(Pair(CooldownText(baseSkill), CooldownText(current)));
            string target = current.Target == "all" ? "Cible : toute l'équipe" : "Cible : 1 allié";
            return new SkillSheet(current.Name, cost, cast, cd, target, Describe(baseSkill, current));
        }

        /// <summary>« 160 » si identique, « 160 » puis « (240) » marqué Changed sinon.</summary>
        public static List<SkillSpan> Pair(string baseText, string currentText)
        {
            var list = new List<SkillSpan> { new SkillSpan(baseText) };
            if (baseText != currentText) list.Add(new SkillSpan(currentText, true));
            return list;
        }

        private static string CastText(SkillDef s) => s.CastMs > 0 ? Format.Seconds(s.CastMs) : "instantanée";
        private static string CooldownText(SkillDef s) => s.CooldownMs > 0 ? Format.Seconds(s.CooldownMs) : "aucun";

        private static List<SkillSpan> Describe(SkillDef baseSkill, SkillDef current)
        {
            var result = new List<SkillSpan>();
            string text = current.Description ?? baseSkill.Description ?? "";
            int i = 0;
            while (i < text.Length)
            {
                int open = text.IndexOf('{', i);
                int close = open < 0 ? -1 : text.IndexOf('}', open);
                if (open < 0 || close < 0) { result.Add(new SkillSpan(text.Substring(i))); break; }
                if (open > i) result.Add(new SkillSpan(text.Substring(i, open - i)));
                string key = text.Substring(open + 1, close - open - 1);
                var value = Value(key, baseSkill, current);
                if (value == null) result.Add(new SkillSpan(text.Substring(open, close - open + 1)));
                else result.AddRange(value);
                i = close + 1;
            }
            return result;
        }

        private static List<SkillSpan>? Value(string key, SkillDef b, SkillDef c) => key switch
        {
            "heal" => Pair(Format.Number(b.HealAmount ?? 0), Format.Number(c.HealAmount ?? 0)),
            "shield" => Pair(Format.Number(b.ShieldAmount ?? 0), Format.Number(c.ShieldAmount ?? 0)),
            "mana" => Pair(Format.Number(b.ManaCost), Format.Number(c.ManaCost)),
            "cast" => Pair(CastText(b), CastText(c)),
            "cd" => Pair(CooldownText(b), CooldownText(c)),
            _ => null,
        };
    }
}
