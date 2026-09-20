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
        public int Phase { get; set; }
        public bool HitsAll { get; set; }

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
                    return $"{head} {UnitId} {Num(Amount)}";
                case "unitDamaged":
                    return $"{head} {UnitId} {Num(Amount)} (absorbé {Num(Absorbed)})";
                case "bossDamaged":
                    return $"{head} par {SourceId} {Num(Amount)}";
                case "unitDied":
                    return $"{head} {UnitId}";
                case "bossAction":
                    return $"{head} {Action}{(HitsAll ? " zone" : "")}";
                case "effectApplied":
                    return $"{head} {UnitId} {EffectId}";
                case "effectTick":
                    return $"{head} {UnitId} {EffectId} {Num(Amount)} (absorbé {Num(Absorbed)})";
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
