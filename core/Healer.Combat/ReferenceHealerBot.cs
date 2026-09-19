using System.Linq;

namespace Healer.Combat
{
    public sealed class ReferenceHealerOptions
    {
        /// <summary>Pas de simulation.</summary>
        public double StepMs { get; set; } = 100;
        /// <summary>
        /// Délai entre deux décisions du bot. Un humain ne réagit pas à chaque instant : ~500 ms
        /// représente un joueur attentif (voir docs/EQUILIBRAGE.md).
        /// </summary>
        public double DecisionEveryMs { get; set; } = 500;
        /// <summary>false = joueur qui ignore le poison (vérifie que la Purge compte vraiment).</summary>
        public bool Purge { get; set; } = true;
    }

    /// <summary>
    /// Bot de soin « raisonnable » utilisé pour valider l'équilibrage : il purge les alliés empoisonnés,
    /// soigne la cible la plus abîmée, bascule en soin de zone si plusieurs alliés souffrent, et pose un
    /// bouclier quand la grosse attaque du boss est télégraphiée. Ce n'est pas une IA optimale : une
    /// référence pour vérifier qu'un jeu « raisonnablement bien joué » peut gagner.
    /// </summary>
    public static class ReferenceHealerBot
    {
        public static void Run(Battle battle, double durationMs, ReferenceHealerOptions? options = null)
        {
            options ??= new ReferenceHealerOptions();
            double sinceDecisionMs = options.DecisionEveryMs; // décide dès le premier pas
            for (double t = battle.GetClock(); t < durationMs && battle.GetResult() == BattleResults.Ongoing; t += options.StepMs)
            {
                if (sinceDecisionMs >= options.DecisionEveryMs)
                {
                    sinceDecisionMs = 0;
                    Decide(battle, t, options.Purge);
                }
                sinceDecisionMs += options.StepMs;
                battle.Step(options.StepMs);
            }
        }

        private static void Decide(Battle battle, double t, bool purge)
        {
            var allies = battle.GetAllies().Where(a => a.Alive).ToList();
            var healer = allies.FirstOrDefault(a => a.Role == "healer");
            if (healer == null) return;

            // OrderBy est stable : à égalité de ratio, le premier de la liste est choisi (comme en TypeScript).
            var lowest = allies.OrderBy(a => a.Hp / a.MaxHp).First();
            int hurtCount = allies.Count(a => a.Hp / a.MaxHp < 0.75);
            var poisoned = purge ? allies.FirstOrDefault(a => a.Effects.Count > 0) : null;
            var telegraph = battle.GetTelegraph();

            if (telegraph?.Type == "bigAttack" && battle.CanUseSkillNow("healer", "shield"))
                battle.IssueCommand(new Command { TimeMs = t, SkillId = "shield", TargetId = lowest.Id });
            else if (poisoned != null && battle.CanUseSkillNow("healer", "purge"))
                battle.IssueCommand(new Command { TimeMs = t, SkillId = "purge", TargetId = poisoned.Id });
            else if (hurtCount >= 2 && battle.CanUseSkillNow("healer", "heal_aoe"))
                battle.IssueCommand(new Command { TimeMs = t, SkillId = "heal_aoe" });
            else if (lowest.Hp / lowest.MaxHp < 0.85 && battle.CanUseSkillNow("healer", "heal_single"))
                battle.IssueCommand(new Command { TimeMs = t, SkillId = "heal_single", TargetId = lowest.Id });
        }
    }
}
