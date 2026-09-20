using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Balayage de paramètres pour régler un boss (explicite : outil de conception, pas un test).</summary>
    public class ZBalayage
    {
        private sealed class M { public double Win, Death, Low, MinMs, MaxMs; }

        private static M Measure(GameContent c, string bossId, Action<Battle> drive)
        {
            int wins = 0, deaths = 0; double low = 0; double min = 1e9, max = 0;
            for (uint seed = 1; seed <= 60; seed++)
            {
                var b = new Battle(c.CreateEncounter(bossId, seed));
                double lowest = 1;
                b.Subscribe(_ => { foreach (var u in b.GetAllies()) lowest = Math.Min(lowest, u.Hp / u.MaxHp); });
                drive(b);
                if (b.GetResult() == BattleResults.Victory) wins++;
                if (b.GetAllies().Any(u => !u.Alive)) deaths++;
                low += lowest; min = Math.Min(min, b.GetClock()); max = Math.Max(max, b.GetClock());
            }
            return new M { Win = wins / 60.0, Death = deaths / 60.0, Low = low / 60, MinMs = min, MaxMs = max };
        }

        [Test, Explicit]
        public void Balayer()
        {
            string bossId = Environment.GetEnvironmentVariable("HEALER_BOSS") ?? "boss2";
            string effectId = bossId == "boss2" ? "venom" : "burn";
            var found = new List<string>();
            foreach (double atk in new[] { 34.0, 38, 42, 46 })
            foreach (double big in new[] { 2.0, 2.4, 2.8 })
            foreach (double bigMs in new[] { 1000.0, 1200, 1400 })
            foreach (double eff in new[] { 14.0, 18, 22, 26, 30, 34 })
            {
                var c = Fixtures.FullContent();
                var boss = c.BossById(bossId);
                boss.Atk = atk;
                void Patch(List<BossActionDef> p) { foreach (var a in p.Where(x => x.Type == "bigAttack")) { a.Multiplier = big; a.TelegraphMs = bigMs; } }
                Patch(boss.Pattern); foreach (var ph in boss.Phases ?? new List<BossPhaseDef>()) Patch(ph.Pattern);
                c.Effects.Single(e => e.Id == effectId).DamagePerTick = eff;

                var att = Measure(c, bossId, b => ReferenceHealerBot.Run(b, 150000));
                if (att.Win < 0.97 || att.Death > 0.08 || att.Low < 0.18 || att.Low > 0.40 || att.MinMs < 62000 || att.MaxMs > 115000) continue;
                var slow = Measure(c, bossId, b => ReferenceHealerBot.Run(b, 150000, new ReferenceHealerOptions { DecisionEveryMs = 1500 }));
                if (att.Low - slow.Low < 0.06) continue;
                var np = Measure(c, bossId, b => ReferenceHealerBot.Run(b, 150000, new ReferenceHealerOptions { Purge = false }));
                if (att.Low - np.Low < 0.06 || np.Death <= att.Death + 0.03) continue;
                found.Add($"atk={atk} big={big} bigMs={bigMs} eff={eff} | att win={att.Win:0.00} mort={att.Death:0.00} pv={att.Low:0.00} durée={att.MinMs / 1000:0}-{att.MaxMs / 1000:0} | lent pv={slow.Low:0.00} | sansPurge pv={np.Low:0.00} mort={np.Death:0.00}");
            }
            TestContext.Progress.WriteLine($"{bossId} : {found.Count} combinaison(s) valides");
            foreach (var f in found.Take(12)) TestContext.Progress.WriteLine("  " + f);
        }
    }
}
