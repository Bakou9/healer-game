using System;
using System.Collections.Generic;
using System.Globalization;

namespace Healer.Combat
{
    /// <summary>
    /// Événements émis par la simulation (patron Observer). La simulation ne connaît pas ses
    /// observateurs : l'UI, les sons, les tests golden et un futur replay serveur s'y abonnent.
    /// Le format texte (<see cref="Format"/>) est STABLE : il fonde les références golden.
    /// </summary>
    public sealed class BattleEvent
    {
        public string Type { get; set; } = "";
        public double TimeMs { get; set; }

        public string UnitId { get; set; } = "";
        public string CasterId { get; set; } = "";
        public string SourceId { get; set; } = "";
        public string SkillId { get; set; } = "";
        public string EffectId { get; set; } = "";
        public string Action { get; set; } = "";
        public string Reason { get; set; } = "";
        public string Result { get; set; } = "";
        public string Name { get; set; } = "";
        public IReadOnlyList<string> TargetIds { get; set; } = Array.Empty<string>();
        public double Amount { get; set; }
        public double Absorbed { get; set; }
        /// <summary>Soin excédentaire (événement healed) : la part du soin qui dépasse les PV manquants. Non écrit dans la trace des golden.</summary>
        public double Overheal { get; set; }
        public int Phase { get; set; }
        public bool HitsAll { get; set; }
        /// <summary>Coup ou soin critique.</summary>
        public bool Crit { get; set; }
        /// <summary>Type des dégâts (« physical » si non précisé).</summary>
        public string DamageType { get; set; } = "";

        /// <summary>Ajouts au format : présents seulement si la mécanique intervient (les combats sans elle gardent leur texte d'origine).</summary>
        private string Suffix() => (Crit ? " CRIT" : "") + (!string.IsNullOrEmpty(DamageType) && DamageType != "physical" ? " [" + DamageType + "]" : "");

        /// <summary>Représentation texte identique à la version TypeScript (formatEvent).</summary>
        public string Format()
        {
            string head = $"{Num(TimeMs)}ms {Type}";
            switch (Type)
            {
                case "skillUsed":
                    return $"{head} {CasterId} {SkillId} -> [{string.Join(",", TargetIds)}]";
                case "healed":
                case "shielded":
                    return $"{head} {UnitId} {Num(Amount)}{Suffix()}";
                case "unitDamaged":
                    return $"{head} {UnitId} {Num(Amount)} (absorbé {Num(Absorbed)}){Suffix()}";
                case "bossDamaged":
                    return $"{head} par {SourceId} {Num(Amount)}{Suffix()}";
                case "unitDodged":
                case "focusMarked":
                    return $"{head} {UnitId}";
                case "castStarted":
                    return $"{head} {CasterId} {SkillId} {Num(Amount)}ms";
                case "castFailed":
                    return $"{head} {CasterId} {SkillId} {Reason}";
                case "unitDied":
                    return $"{head} {UnitId}";
                case "bossAction":
                    return $"{head} {Action}{(HitsAll ? " zone" : "")}";
                case "effectApplied":
                    return $"{head} {UnitId} {EffectId}";
                case "effectTick":
                    return $"{head} {UnitId} {EffectId} {Num(Amount)} (absorbé {Num(Absorbed)}){Suffix()}";
                case "effectEnded":
                    return $"{head} {UnitId} {EffectId} {Reason}";
                case "bossEnraged":
                    return $"{head} {Phase} +{Num(Amount)}%";
                case "bossPhaseChanged":
                    return $"{head} {Phase} {Name}";
                case "battleEnded":
                    return $"{head} {Result}";
                default:
                    throw new InvalidOperationException($"Type d'événement inconnu : {Type}");
            }
        }

        /// <summary>Nombre affiché comme en JavaScript : entier sans décimales, sinon valeur exacte.</summary>
        public static string Num(double value)
        {
            if (value == Math.Floor(value) && Math.Abs(value) < 1e15)
                return ((long)value).ToString(CultureInfo.InvariantCulture);
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
