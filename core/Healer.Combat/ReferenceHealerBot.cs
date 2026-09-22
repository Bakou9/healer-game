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
        /// <summary>false = joueur qui ne protège pas la victime annoncée d'une attaque ciblée (vérifie que l'annonce compte vraiment).</summary>
        public bool ProtectFocusTarget { get; set; } = true;
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
                    Decide(battle, t, options.Purge, options.ProtectFocusTarget);
                }
                sinceDecisionMs += options.StepMs;
                battle.Step(options.StepMs);
            }
        }

        /// <summary>Une décision du bot (appelée à intervalle régulier par le client pour la démonstration et les captures).</summary>
        public static void Decide(Battle battle, double t, bool purge, bool protectFocusTarget = true)
        {
            var allies = battle.GetAllies().Where(a => a.Alive).ToList();
            var healer = allies.FirstOrDefault(a => a.Role == "healer");
            if (healer == null) return;

            // OrderBy est stable : à égalité de ratio, le premier de la liste est choisi (comme en TypeScript).
            var lowest = allies.OrderBy(a => a.Hp / a.MaxHp).First();
            int hurtCount = allies.Count(a => a.Hp / a.MaxHp < 0.75);
            // Miracle (E02-T05) : seuil un peu plus large que le soin de zone de base, sinon son grand mana
            // (45) ne se déclenche jamais sur les boss à dégâts plus répartis (boss2/boss3, cf. sonde).
            int hurtCountWide = allies.Count(a => a.Hp / a.MaxHp < 0.85);
            var poisoned = purge ? allies.FirstOrDefault(a => a.Effects.Count > 0) : null;
            var telegraph = battle.GetTelegraph();

            var victim = protectFocusTarget && telegraph?.Type == "focusAttack" && telegraph.TargetId != null ? allies.FirstOrDefault(a => a.Id == telegraph.TargetId) : null;
            // Capstones (E02-T05) : préférés à leur équivalent de base QUAND ils sont débloqués — CanUseSkillNow répond
            // simplement faux sinon (aucun `if` par talent), la suite de la chaîne de décision est inchangée.
            if (victim != null && victim.Shield <= 0 && battle.CanUseSkillNow("healer", "shield"))
                battle.IssueCommand(new Command { TimeMs = t, SkillId = "shield", TargetId = victim.Id }); // attaque ciblée : bouclier sur la victime annoncée
            else if (telegraph?.Type == "bigAttack" && battle.CanUseSkillNow("healer", "dome"))
                battle.IssueCommand(new Command { TimeMs = t, SkillId = "dome" }); // attaque de zone télégraphiée : bouclier sur toute l'équipe
            else if (telegraph?.Type == "bigAttack" && battle.CanUseSkillNow("healer", "shield"))
                battle.IssueCommand(new Command { TimeMs = t, SkillId = "shield", TargetId = lowest.Id });
            else if (poisoned != null && battle.CanUseSkillNow("healer", "purge"))
                battle.IssueCommand(new Command { TimeMs = t, SkillId = "purge", TargetId = poisoned.Id });
            // Urgence à un seul allié (E02-T05) : passe AVANT le soin de zone, sinon un soin de zone routinier
            // intercepterait toujours la décision dès que 2 alliés souffrent, et Renaissance ne se déclencherait jamais.
            else if (lowest.Hp / lowest.MaxHp < 0.65 && battle.CanUseSkillNow("healer", "renaissance"))
                battle.IssueCommand(new Command { TimeMs = t, SkillId = "renaissance", TargetId = lowest.Id });
            else if (hurtCountWide >= 2 && battle.CanUseSkillNow("healer", "miracle"))
                battle.IssueCommand(new Command { TimeMs = t, SkillId = "miracle" }); // plusieurs alliés mal en point : le grand soin de zone
            else if (hurtCountWide >= 2 && battle.CanUseSkillNow("healer", "seve_vitale"))
                battle.IssueCommand(new Command { TimeMs = t, SkillId = "seve_vitale" }); // capstone Vitalité (D-084) : soin + bouclier de zone
            else if (hurtCount >= 2 && battle.CanUseSkillNow("healer", "heal_aoe"))
                battle.IssueCommand(new Command { TimeMs = t, SkillId = "heal_aoe" });
            else if (lowest.Hp / lowest.MaxHp < 0.85 && battle.CanUseSkillNow("healer", "heal_single"))
                battle.IssueCommand(new Command { TimeMs = t, SkillId = "heal_single", TargetId = lowest.Id });
        }
    }
}
