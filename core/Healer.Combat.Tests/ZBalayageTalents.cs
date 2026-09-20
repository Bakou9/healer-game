using System;
using System.Collections.Generic;
using System.Linq;
using Healer.Combat.Progress;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Balayage des valeurs de talents pour équilibrer les deux options d'un palier (explicite : outil de conception).</summary>
    public class ZBalayageTalents
    {
        private static (double win, double death, double pv) Measure(GameContent c, string boss, Loadout l)
        {
            int wins = 0, deaths = 0; double low = 0;
            for (uint seed = 1; seed <= 60; seed++)
            {
                var b = new Battle(c.CreateEncounter(boss, seed, null, l));
                double lowest = 1;
                b.Subscribe(_ => { foreach (var u in b.GetAllies()) lowest = Math.Min(lowest, u.Hp / u.MaxHp); });
                ReferenceHealerBot.Run(b, 150000);
                if (b.GetResult() == BattleResults.Victory) wins++;
                if (b.GetAllies().Any(u => !u.Alive)) deaths++;
                low += lowest;
            }
            return (wins / 60.0, deaths / 60.0, low / 60);
        }

        [Test, Explicit]
        public void Balayer_un_palier()
        {
            int tier = int.Parse(Environment.GetEnvironmentVariable("HEALER_TIER") ?? "1");
            var aVals = (Environment.GetEnvironmentVariable("HEALER_A") ?? "20,30,40").Split(',').Select(int.Parse).ToList();
            var bVals = (Environment.GetEnvironmentVariable("HEALER_B") ?? "-5,-8,-10").Split(',').Select(int.Parse).ToList();
            string[] bosses = { "boss1", "boss2", "boss3" };
            var baseC = Fixtures.FullContent();
            var baseline = bosses.ToDictionary(b => b, b => Measure(baseC, b, new Loadout()).pv);
            var found = new List<string>();
            foreach (int a in aVals)
            foreach (int b in bVals)
            {
                var c = Fixtures.FullContent();
                var def = c.Upgrades.Tier(tier)!;
                // Option A : premier effet de l'option 0 ; option B : premier effet de l'option 1 (les effets secondaires gardent leur valeur).
                def.Options[0].Effects[0].Pct = a;
                def.Options[1].Effects[0].Pct = b;
                if (int.TryParse(Environment.GetEnvironmentVariable("HEALER_A2"), out var a2) && def.Options[0].Effects.Count > 1) def.Options[0].Effects[1].Pct = a2;
                if (int.TryParse(Environment.GetEnvironmentVariable("HEALER_B2"), out var b2) && def.Options[1].Effects.Count > 1) def.Options[1].Effects[1].Pct = b2;
                double meanDiff = 0; bool ok = true; var sb = new System.Text.StringBuilder(); int aWins = 0, bWins = 0; double gain = 0;
                foreach (var boss in bosses)
                {
                    var la = new Loadout(); la.Talents[tier] = def.Options[0].Id;
                    var lb = new Loadout(); lb.Talents[tier] = def.Options[1].Id;
                    var ra = Measure(c, boss, la); var rb = Measure(c, boss, lb);
                    if (ra.win < 0.95 || rb.win < 0.95 || ra.death > 0.12 || rb.death > 0.12) ok = false;
                    if (Math.Abs(ra.pv - rb.pv) > 0.15) ok = false;
                    meanDiff += ra.pv - rb.pv;
                    if (ra.pv > rb.pv + 0.02) aWins++; if (rb.pv > ra.pv + 0.02) bWins++;
                    gain += (ra.pv + rb.pv) / 2 - baseline[boss];
                    sb.Append($" {boss}: A={ra.pv:0.00} B={rb.pv:0.00}");
                }
                gain /= 3; meanDiff /= 3;
                if (Environment.GetEnvironmentVariable("HEALER_VERBOSE") != null) TestContext.Progress.WriteLine($"  ? A={a} B={b} ok={ok} moy={meanDiff:0.00} aW={aWins} bW={bWins} gain={gain:0.00}{sb}");
                if (ok && Math.Abs(meanDiff) <= 0.04 && aWins >= 1 && bWins >= 1 && gain >= 0.03 && gain <= 0.20) found.Add($"A={a} B={b} gain={gain:0.00}{sb}");
            }
            TestContext.Progress.WriteLine($"palier {tier} : {found.Count} combinaison(s) valides");
            foreach (var f in found.Take(15)) TestContext.Progress.WriteLine("  " + f);
        }
    }
}
