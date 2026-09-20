using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Mesure de l'attaque ciblée (D-056) sur les boss avancés : outil de conception explicite.</summary>
    public class ZMesureFocus
    {
        public static void AddFocus(BossDef boss, double multiplier, double telegraphMs = 1500, int replaceEvery = 1)
        {
            void Patch(List<BossActionDef> pattern)
            {
                int replaced = 0;
                for (int i = 0; i < pattern.Count; i++)
                    if (pattern[i].Type == "attack" && replaced++ % replaceEvery == 0 && replaced <= 1)
                        pattern[i] = new BossActionDef { Type = "focusAttack", TelegraphMs = telegraphMs, Multiplier = multiplier };
            }
            Patch(boss.Pattern);
            if (boss.Phases != null) foreach (var p in boss.Phases) Patch(p.Pattern);
        }

        [Test, Explicit]
        public void Balayer_l_attaque_ciblee()
        {
            foreach (var (bossId, mult, atkAbs) in new (string, double, double)[]
            {
                ("boss2", 15, 20), ("boss2", 18, 18), ("boss2", 20, 16), ("boss2", 16, 20), ("boss2", 20, 18),
                ("boss3", 13, 19), ("boss3", 15, 17), ("boss3", 12, 20), ("boss3", 16, 16), ("boss3", 14, 18),
            })
            {
                {
                    var c = Fixtures.FullContent();
                    var boss = c.Bosses.First(b => b.Id == bossId);
                    if (mult > 0) AddFocus(boss, mult);
                    boss.Atk = atkAbs;
                    const int n = 100;
                    int attWin = 0, attKo = 0, spamWin = 0, lazyKo = 0, blindKo = 0, slowKo = 0; double low = 0, dur = 0;
                    for (uint seed = 1; seed <= n; seed++)
                    {
                        var a = new Battle(c.CreateEncounter(bossId, seed)); double lowest = 1;
                        a.Subscribe(_ => { foreach (var u in a.GetAllies()) lowest = Math.Min(lowest, u.Hp / u.MaxHp); });
                        ReferenceHealerBot.Run(a, 150000);
                        if (a.GetResult() == BattleResults.Victory) attWin++;
                        if (a.GetAllies().Any(u => !u.Alive)) attKo++;
                        low += lowest; dur += a.GetClock();

                        var s = new Battle(c.CreateEncounter(bossId, seed)); ZMesureSpamZone.ZoneSpam(s, true);
                        if (s.GetResult() == BattleResults.Victory) spamWin++;

                        var l = new Battle(c.CreateEncounter(bossId, seed)); ZMesureStrategies.Limited(l, new[] { "heal_aoe", "heal_single" }, 0.75);
                        if (l.GetAllies().Any(u => !u.Alive)) lazyKo++;

                        var bl = new Battle(c.CreateEncounter(bossId, seed)); ReferenceHealerBot.Run(bl, 150000, new ReferenceHealerOptions { ProtectFocusTarget = false });
                        if (bl.GetAllies().Any(u => !u.Alive)) blindKo++;

                        var sl = new Battle(c.CreateEncounter(bossId, seed)); ReferenceHealerBot.Run(sl, 150000, new ReferenceHealerOptions { DecisionEveryMs = 1500 });
                        if (sl.GetAllies().Any(u => !u.Alive)) slowKo++;
                    }
                    TestContext.Progress.WriteLine($"{bossId} focus x{mult,2} atk{boss.Atk,3} | attentif gagne {attWin,3}% KO {attKo,3}% PV {low / n:0.00} {dur / n / 1000:0}s | lent KO {slowKo,3}% | sans protéger la cible KO {blindKo,3}% | zone+purge gagne {spamWin,3}% | paresseux KO {lazyKo,3}%");
                }
            }
        }

        [Test, Explicit]
        public void Tracer_un_combat()
        {
            var c = Fixtures.FullContent();
            var boss = c.Bosses.First(b => b.Id == "boss2");
            AddFocus(boss, 5);
            var a = new Battle(c.CreateEncounter("boss2", 1));
            a.Subscribe(e => { if (e.Type == "unitDied" || e.Type == "bossPhaseChanged" || e.Type == "battleEnded" || (e.Type == "unitDamaged" && e.Amount >= 100)) TestContext.Progress.WriteLine(e.Format()); });
            ReferenceHealerBot.Run(a, 150000);
        }
    }
}
