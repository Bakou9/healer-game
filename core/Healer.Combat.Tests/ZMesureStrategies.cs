using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Mesure de stratégies « limitées » (explicite : outil de conception).</summary>
    public class ZMesureStrategies
    {
        /// <summary>Joueur qui n'utilise qu'un sous-ensemble de sorts, décidant toutes les 500 ms.</summary>
        public static void Limited(Battle battle, string[] skills, double hurtBelow, double durationMs = 150000)
        {
            for (double t = 0; t < durationMs && battle.GetResult() == BattleResults.Ongoing; t += 100)
            {
                if (t % 500 == 0)
                {
                    var allies = battle.GetAllies().Where(a => a.Alive).ToList();
                    var lowest = allies.OrderBy(a => a.Hp / a.MaxHp).First();
                    int hurt = allies.Count(a => a.Hp / a.MaxHp < hurtBelow);
                    if (skills.Contains("heal_aoe") && hurt >= 1 && battle.CanUseSkillNow("healer", "heal_aoe"))
                        battle.IssueCommand(new Command { TimeMs = t, SkillId = "heal_aoe" });
                    else if (skills.Contains("heal_single") && lowest.Hp / lowest.MaxHp < hurtBelow && battle.CanUseSkillNow("healer", "heal_single"))
                        battle.IssueCommand(new Command { TimeMs = t, SkillId = "heal_single", TargetId = lowest.Id });
                }
                battle.Step(100);
            }
        }

        [Test, Explicit]
        public void Mesurer()
        {
            var c = Fixtures.FullContent();
            var profiles = new (string, string[], double)[]
            {
                ("zone seul (<75%)", new[] { "heal_aoe" }, 0.75),
                ("zone seul (<90%)", new[] { "heal_aoe" }, 0.90),
                ("soin seul (<75%)", new[] { "heal_single" }, 0.75),
                ("zone+soin sans bouclier/purge", new[] { "heal_aoe", "heal_single" }, 0.75),
            };
            foreach (var boss in new[] { "boss1", "boss2", "boss3" })
                foreach (var (label, skills, below) in profiles)
                {
                    int wins = 0, deaths = 0; double low = 0, dur = 0;
                    for (uint seed = 1; seed <= 100; seed++)
                    {
                        var b = new Battle(c.CreateEncounter(boss, seed));
                        double lowest = 1;
                        b.Subscribe(_ => { foreach (var u in b.GetAllies()) lowest = Math.Min(lowest, u.Hp / u.MaxHp); });
                        Limited(b, skills, below);
                        if (b.GetResult() == BattleResults.Victory) wins++;
                        if (b.GetAllies().Any(u => !u.Alive)) deaths++;
                        low += lowest; dur += b.GetClock();
                    }
                    TestContext.Progress.WriteLine($"{boss} {label,-30} gagne={wins,3}% mort={deaths,3}% pv={low / 100:0.00} durée={dur / 100000:0}s");
                }
        }
    }
}
