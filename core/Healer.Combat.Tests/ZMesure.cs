using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Outil de mesure (explicite : ne tourne pas par défaut) pour régler l'équilibrage des boss.</summary>
    public class ZMesure
    {
        [Test, Explicit]
        public void Mesurer_tous_les_boss()
        {
            var content = Fixtures.FullContent();
            foreach (var boss in content.Bosses)
            {
                foreach (var (label, drive) in new (string, Action<Battle>)[]
                {
                    ("attentif", b => ReferenceHealerBot.Run(b, 150000)),
                    ("lent", b => ReferenceHealerBot.Run(b, 150000, new ReferenceHealerOptions { DecisionEveryMs = 1500 })),
                    ("sans purge", b => ReferenceHealerBot.Run(b, 150000, new ReferenceHealerOptions { Purge = false })),
                    ("sans soigneur", b => b.Run(150000)),
                })
                {
                    int wins = 0, deaths = 0; double low = 0, dmg = 0; var durs = new List<double>(); var dmgs = new List<double>();
                    for (uint seed = 1; seed <= 100; seed++)
                    {
                        var battle = new Battle(content.CreateEncounter(boss.Id, seed));
                        var stats = new CombatStats(); stats.Attach(battle);
                        double lowest = 1;
                        battle.Subscribe(_ => { foreach (var u in battle.GetAllies()) lowest = Math.Min(lowest, u.Hp / u.MaxHp); });
                        drive(battle);
                        if (battle.GetResult() == BattleResults.Victory) wins++;
                        if (battle.GetAllies().Any(u => !u.Alive)) deaths++;
                        low += lowest; durs.Add(battle.GetClock()); dmgs.Add(stats.DamageTaken);
                    }
                    dmgs.Sort();
                    TestContext.Progress.WriteLine($"{boss.Id,-6} {label,-13} gagne={wins,3}% mort={deaths,3}% pvMin={low / 100:0.00} durée={durs.Min() / 1000:0}-{durs.Max() / 1000:0}s dégâtsMed={dmgs[50]:0} p25={dmgs[25]:0} p75={dmgs[75]:0}");
                }
            }
        }
    }
}
