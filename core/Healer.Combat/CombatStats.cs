using System;
using System.Collections.Generic;

namespace Healer.Combat
{
    /// <summary>
    /// Statistiques d'un combat, calculées UNIQUEMENT à partir des événements (patron Observer) : soins
    /// effectifs, dégâts encaissés et absorbés, sorts lancés, morts. Alimente l'écran de bilan (E04-T09) et
    /// les métriques d'équilibrage (E08-T04). Pur et déterministe : mêmes événements, mêmes statistiques.
    /// </summary>
    public sealed class CombatStats
    {
        public double HealingDone { get; private set; }
        /// <summary>Soins gaspillés : la part des soins qui dépassait les PV manquants (overheal).</summary>
        public double Overheal { get; private set; }
        /// <summary>Part des soins lancés qui a été gaspillée (0 à 1) ; 0 si rien n'a été lancé.</summary>
        public double OverhealRatio => HealingDone + Overheal <= 0 ? 0 : Overheal / (HealingDone + Overheal);
        public double ShieldGranted { get; private set; }
        public double DamageTaken { get; private set; }
        public double DamageAbsorbed { get; private set; }
        /// <summary>Dégâts subis avant les boucliers (PV perdus + absorbés) : ce que l'équipe aurait encaissé sans aucun Bouclier.</summary>
        public double DamageBeforeShields => DamageTaken + DamageAbsorbed;
        public double DamageToBoss { get; private set; }
        public double DurationMs { get; private set; }
        public int Deaths { get; private set; }
        public int Purges { get; private set; }
        public int Casts { get; private set; }
        /// <summary>Coups et soins critiques (tous camps).</summary>
        public int Crits { get; private set; }
        /// <summary>Coups esquivés par les alliés.</summary>
        public int Dodges { get; private set; }
        /// <summary>Incantations qui n'ont pas abouti.</summary>
        public int FailedCasts { get; private set; }
        public string Result { get; private set; } = BattleResults.Ongoing;
        public IReadOnlyDictionary<string, int> CastsBySkill => _castsBySkill;

        /// <summary>Dégâts infligés au boss par chaque allié (identifiant → total). Un allié qui n'a rien infligé est absent.</summary>
        public IReadOnlyDictionary<string, double> DamageByAlly => _damageByAlly;

        private readonly Dictionary<string, double> _damageByAlly = new Dictionary<string, double>();

        private readonly Dictionary<string, int> _castsBySkill = new Dictionary<string, int>();

        /// <summary>Part (0 à 1) des dégâts totaux infligés par un allié ; 0 s'il n'a rien infligé ou si personne n'a rien infligé.</summary>
        public double DamageShare(string allyId) =>
            DamageToBoss <= 0 || !_damageByAlly.TryGetValue(allyId, out var d) ? 0 : d / DamageToBoss;

        /// <summary>S'abonne au combat. Renvoie l'action de désabonnement.</summary>
        public Action Attach(Battle battle) => battle.Subscribe(Apply);

        /// <summary>Prend en compte un événement (utilisé par Attach ; public pour rejouer un enregistrement ou construire un cas de test).</summary>
        public void Apply(BattleEvent e)
        {
            if (e.Crit) Crits++;
            switch (e.Type)
            {
                case "unitDodged":
                    Dodges++;
                    break;
                case "castFailed":
                    FailedCasts++;
                    break;
                case "healed":
                    HealingDone += e.Amount;
                    Overheal += e.Overheal;
                    break;
                case "shielded":
                    ShieldGranted += e.Amount;
                    break;
                case "unitDamaged":
                case "effectTick":
                    DamageTaken += e.Amount;
                    DamageAbsorbed += e.Absorbed;
                    break;
                case "bossDamaged":
                    DamageToBoss += e.Amount;
                    _damageByAlly[e.SourceId] = (_damageByAlly.TryGetValue(e.SourceId, out var d) ? d : 0) + e.Amount;
                    break;
                case "unitDied":
                    Deaths++;
                    break;
                case "effectEnded":
                    if (e.Reason == "cleansed") Purges++;
                    break;
                case "skillUsed":
                    Casts++;
                    _castsBySkill[e.SkillId] = (_castsBySkill.TryGetValue(e.SkillId, out var n) ? n : 0) + 1;
                    break;
                case "battleEnded":
                    DurationMs = e.TimeMs;
                    Result = e.Result;
                    break;
            }
        }
    }
}
