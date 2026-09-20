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
        public double ShieldGranted { get; private set; }
        public double DamageTaken { get; private set; }
        public double DamageAbsorbed { get; private set; }
        public double DamageToBoss { get; private set; }
        public double DurationMs { get; private set; }
        public int Deaths { get; private set; }
        public int Purges { get; private set; }
        public int Casts { get; private set; }
        public string Result { get; private set; } = BattleResults.Ongoing;
        public IReadOnlyDictionary<string, int> CastsBySkill => _castsBySkill;

        private readonly Dictionary<string, int> _castsBySkill = new Dictionary<string, int>();

        /// <summary>S'abonne au combat. Renvoie l'action de désabonnement.</summary>
        public Action Attach(Battle battle) => battle.Subscribe(Apply);

        /// <summary>Prend en compte un événement (utilisé par Attach ; public pour rejouer un enregistrement ou construire un cas de test).</summary>
        public void Apply(BattleEvent e)
        {
            switch (e.Type)
            {
                case "healed":
                    HealingDone += e.Amount;
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
