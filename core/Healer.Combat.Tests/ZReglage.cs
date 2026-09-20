using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Réglage d'un boss par balayage (PV, attaque, dégâts d'effet) sous toutes les bornes (explicite : outil de conception).</summary>
    public class ZReglage
    {
        private sealed class M { public double Win, Death, Low, MinMs, MaxMs; }

        private static M Measure(GameContent c, string bossId, Action<Battle> drive)
        {
            int wins = 0, deaths = 0; double low = 0, min = 1e9, max = 0;
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
        public void Regler()
        {
            string bossId = Environment.GetEnvironmentVariable("HEALER_BOSS") ?? "boss1";
            bool advanced = bossId != "boss1";
            string effectId = bossId == "boss1" ? "poison" : bossId == "boss2" ? "venom" : "burn";
            double[] hp = (Environment.GetEnvironmentVariable("HEALER_HP") ?? "1,1.15,1.3,1.45").Split(',').Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            double[] atk = (Environment.GetEnvironmentVariable("HEALER_ATK") ?? "1.2,1.5,1.8,2.1,2.4,2.8").Split(',').Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            double[] eff = (Environment.GetEnvironmentVariable("HEALER_EFF") ?? "1,1.2,1.4").Split(',').Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            var baseC = Fixtures.FullContent();
            var baseBoss = baseC.BossById(bossId);
            double baseEff = baseC.Effects.Single(e => e.Id == effectId).DamagePerTick;
            var found = new List<string>();
            bool verbose = Environment.GetEnvironmentVariable("HEALER_VERBOSE") != null;
            foreach (double h in hp) foreach (double a in atk) foreach (double f in eff)
            {
                var c = Fixtures.FullContent();
                var boss = c.BossById(bossId);
                boss.MaxHp = Math.Round(baseBoss.MaxHp * h);
                boss.Atk = Math.Round(baseBoss.Atk * a);
                c.Effects.Single(e => e.Id == effectId).DamagePerTick = Math.Round(baseEff * f);
                var att = Measure(c, bossId, x => ReferenceHealerBot.Run(x, 150000));
                if (verbose) TestContext.Progress.WriteLine($"  ? hp x{h} atk x{a} eff x{f} | win={att.Win:0.00} mort={att.Death:0.00} pv={att.Low:0.00} durée={att.MinMs / 1000:0}-{att.MaxMs / 1000:0}");
                if (att.Win < 0.97 || att.Death > 0.09 || att.Low < 0.18 || att.Low > 0.36 || att.MinMs < 62000 || att.MaxMs > 115000) continue;
                var slow = Measure(c, bossId, x => ReferenceHealerBot.Run(x, 150000, new ReferenceHealerOptions { DecisionEveryMs = 1500 }));
                if (att.Low - slow.Low < 0.05) continue;
                var np = Measure(c, bossId, x => ReferenceHealerBot.Run(x, 150000, new ReferenceHealerOptions { Purge = false }));
                if (att.Low - np.Low < 0.05 || np.Death < att.Death + 0.03) continue;
                M lazy = new M(), zone = new M();
                if (advanced)
                {
                    lazy = Measure(c, bossId, x => ZMesureStrategies.Limited(x, new[] { "heal_aoe", "heal_single" }, 0.75));
                    if (lazy.Death < 0.25) continue;
                    zone = Measure(c, bossId, x => ZMesureStrategies.Limited(x, new[] { "heal_aoe" }, 0.75));
                    if (zone.Win > 0.75) continue;
                }
                found.Add($"hp x{h} atk x{a} eff x{f} => PV {boss.MaxHp} atk {boss.Atk} eff {Math.Round(baseEff * f)} | att win={att.Win:0.00} mort={att.Death:0.00} pv={att.Low:0.00} durée={att.MinMs / 1000:0}-{att.MaxMs / 1000:0} | lent pv={slow.Low:0.00} | sansPurge pv={np.Low:0.00} mort={np.Death:0.00} | paresseux mort={lazy.Death:0.00} | zone gagne={zone.Win:0.00}");
            }
            TestContext.Progress.WriteLine($"{bossId} : {found.Count} combinaison(s) valides");
            foreach (var l in found.Take(12)) TestContext.Progress.WriteLine("  " + l);
        }
    }
}
