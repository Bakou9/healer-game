using System;
using System.Collections.Generic;
using System.Linq;
using Healer.Combat.Progress;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Outil de mesure des améliorations (explicite : ne tourne pas par défaut).</summary>
    public class ZMesureAmelio
    {
        private static string Line(GameContent c, string boss, Loadout? l, string label, Action<Battle>? drive = null)
        {
            int wins = 0, deaths = 0; double low = 0, min = 1e9, max = 0;
            for (uint seed = 1; seed <= 100; seed++)
            {
                var b = new Battle(c.CreateEncounter(boss, seed, null, l));
                double lowest = 1;
                b.Subscribe(_ => { foreach (var u in b.GetAllies()) lowest = Math.Min(lowest, u.Hp / u.MaxHp); });
                (drive ?? (x => ReferenceHealerBot.Run(x, 150000)))(b);
                if (b.GetResult() == BattleResults.Victory) wins++;
                if (b.GetAllies().Any(u => !u.Alive)) deaths++;
                low += lowest; min = Math.Min(min, b.GetClock()); max = Math.Max(max, b.GetClock());
            }
            return $"{boss} {label,-34} gagne={wins,3}% mort={deaths,3}% pv={low / 100:0.00} durée={min / 1000:0}-{max / 1000:0}s";
        }

        [Test, Explicit]
        public void Mesurer()
        {
            var c = Fixtures.FullContent();
            var cat = c.Upgrades;
            foreach (var boss in new[] { "boss1", "boss2", "boss3" })
            {
                TestContext.Progress.WriteLine(Line(c, boss, null, "base"));
                var maxEq = new Loadout(); foreach (var t in cat.Equipment) maxEq.Equipment[t.Id] = t.MaxLevel;
                TestContext.Progress.WriteLine(Line(c, boss, maxEq, "équipement max"));
                var halfEq = new Loadout(); foreach (var t in cat.Equipment) halfEq.Equipment[t.Id] = 2;
                TestContext.Progress.WriteLine(Line(c, boss, halfEq, "équipement niveau 2"));
                foreach (var tier in cat.TalentTiers)
                    foreach (var o in tier.Options)
                    {
                        var l = new Loadout(); l.Talents[tier.Tier] = o.Id;
                        TestContext.Progress.WriteLine(Line(c, boss, l, $"talent T{tier.Tier} {o.Id}"));
                    }
                var full = Loadout.Maxed(cat);
                TestContext.Progress.WriteLine(Line(c, boss, full, "TOUT max"));
                TestContext.Progress.WriteLine(Line(c, boss, full, "TOUT max sans soigneur", b => b.Run(150000)));
                TestContext.Progress.WriteLine(Line(c, boss, full, "TOUT max spam soin", b =>
                {
                    for (int i = 0; i < 100; i++) b.IssueCommand(new Command { TimeMs = i * 1200, SkillId = "heal_single", TargetId = "tank" });
                    b.Run(150000);
                }));
                TestContext.Progress.WriteLine("");
            }
        }
    }
}
