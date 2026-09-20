using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>
    /// Mesure de la stratégie « Soin de zone en boucle » (retour de playtest : « il suffit de spammer le Soin de zone,
    /// avec quelques purges »). Outil de conception explicite, comme les autres Z*.
    /// </summary>
    public class ZMesureSpamZone
    {
        /// <summary>Soin de zone dès qu'il est disponible (sans regarder les PV) ; purge dès qu'un allié a un effet négatif ; jamais de Soin ciblé ni de Bouclier.</summary>
        public static void ZoneSpam(Battle battle, bool purge, double durationMs = 150000)
        {
            for (double t = 0; t < durationMs && battle.GetResult() == BattleResults.Ongoing; t += 100)
            {
                if (t % 500 == 0)
                {
                    var allies = battle.GetAllies().Where(a => a.Alive).ToList();
                    var afflicted = allies.FirstOrDefault(a => a.Effects.Count > 0);
                    if (purge && afflicted != null && battle.CanUseSkillNow("healer", "purge"))
                        battle.IssueCommand(new Command { TimeMs = t, SkillId = "purge", TargetId = afflicted.Id });
                    else if (battle.CanUseSkillNow("healer", "heal_aoe"))
                        battle.IssueCommand(new Command { TimeMs = t, SkillId = "heal_aoe" });
                }
                battle.Step(100);
            }
        }

        [Test, Explicit]
        public void Mesurer_le_spam_de_zone()
        {
            var c = Fixtures.FullContent();
            foreach (var boss in new[] { "boss1", "boss2", "boss3" })
            {
                foreach (var (label, purge) in new[] { ("zone en boucle", false), ("zone en boucle + purge", true) })
                {
                    int wins = 0, deaths = 0; double low = 0, dur = 0, mana = 0;
                    const int n = 200;
                    for (uint seed = 1; seed <= n; seed++)
                    {
                        var b = new Battle(c.CreateEncounter(boss, seed));
                        double lowest = 1;
                        b.Subscribe(_ => { foreach (var u in b.GetAllies()) lowest = Math.Min(lowest, u.Hp / u.MaxHp); });
                        ZoneSpam(b, purge);
                        if (b.GetResult() == BattleResults.Victory) wins++;
                        if (b.GetAllies().Any(u => !u.Alive)) deaths++;
                        low += lowest; dur += b.GetClock();
                        mana += b.GetAllies().First(a => a.Id == "healer").Mana;
                    }
                    TestContext.Progress.WriteLine($"{boss} {label,-26} gagne={wins * 100 / n,3}% alliés K.O.={deaths * 100 / n,3}% PV-min={low / n:0.00} durée={dur / n / 1000:0}s mana restant={mana / n:0}");
                }
            }
        }

        [Test, Explicit]
        public void Varier_le_soin_de_zone()
        {
            var c = Fixtures.FullContent();
            var aoe = c.Skills.First(s => s.Id == "heal_aoe");
            double h0 = aoe.HealAmount!.Value, m0 = aoe.ManaCost, cd0 = aoe.CooldownMs, cast0 = aoe.CastMs;
            var variants = new (string, double, double, double, double)[]
            {
                ("H 160 / 60 / 5s", 160, 60, 5000, 0),
                ("I 150 / 60 / 6s", 150, 60, 6000, 0),
                ("J 140 / 60 / 5s", 140, 60, 5000, 0),
                ("K 150 / 65 / 5s", 150, 65, 5000, 0),
                ("L 130 / 60 / 5s", 130, 60, 5000, 0),
                ("M 160 / 70 / 5s", 160, 70, 5000, 0),
                ("N 120 / 55 / 5s", 120, 55, 5000, 0),
            };
            foreach (var (label, heal, mana, cd, cast) in variants)
            {
                aoe.HealAmount = heal; aoe.ManaCost = mana; aoe.CooldownMs = cd; aoe.CastMs = cast;
                var line = new System.Text.StringBuilder(label.PadRight(34));
                foreach (var boss in new[] { "boss1", "boss2", "boss3" })
                {
                    const int n = 100;
                    int spamWins = 0, botWins = 0, botKo = 0; double botLow = 0, botDur = 0;
                    for (uint seed = 1; seed <= n; seed++)
                    {
                        var b = new Battle(c.CreateEncounter(boss, seed)); ZoneSpam(b, true);
                        if (b.GetResult() == BattleResults.Victory) spamWins++;
                        var r = new Battle(c.CreateEncounter(boss, seed));
                        double lowest = 1;
                        r.Subscribe(_ => { foreach (var u in r.GetAllies()) lowest = Math.Min(lowest, u.Hp / u.MaxHp); });
                        ReferenceHealerBot.Run(r, 150000);
                        if (r.GetResult() == BattleResults.Victory) botWins++;
                        if (r.GetAllies().Any(u => !u.Alive)) botKo++;
                        botLow += lowest; botDur += r.GetClock();
                    }
                    line.Append($" | {boss}: spam {spamWins,3}%  bot {botWins,3}% KO {botKo,2}% PV {botLow / n:0.00} {botDur / n / 1000:0}s");
                }
                TestContext.Progress.WriteLine(line.ToString());
            }
            aoe.HealAmount = h0; aoe.ManaCost = m0; aoe.CooldownMs = cd0; aoe.CastMs = cast0;
        }
    }
}
