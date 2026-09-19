using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Statistiques de combat (ticket E01-T13) : calculées uniquement à partir des événements.</summary>
    public class CombatStatsTests
    {
        private static (Battle battle, CombatStats stats, List<BattleEvent> events) PlayBot(uint seed)
        {
            var battle = new Battle(Fixtures.Encounter(seed));
            var stats = new CombatStats();
            stats.Attach(battle);
            var events = new List<BattleEvent>();
            battle.Subscribe(events.Add);
            ReferenceHealerBot.Run(battle, 120000);
            return (battle, stats, events);
        }

        [Test]
        public void Les_totaux_correspondent_exactement_aux_evenements()
        {
            var (_, stats, events) = PlayBot(7);
            Assert.That(stats.HealingDone, Is.EqualTo(events.Where(e => e.Type == "healed").Sum(e => e.Amount)));
            Assert.That(stats.ShieldGranted, Is.EqualTo(events.Where(e => e.Type == "shielded").Sum(e => e.Amount)));
            Assert.That(stats.DamageToBoss, Is.EqualTo(events.Where(e => e.Type == "bossDamaged").Sum(e => e.Amount)));
            Assert.That(stats.DamageTaken, Is.EqualTo(events.Where(e => e.Type is "unitDamaged" or "effectTick").Sum(e => e.Amount)));
            Assert.That(stats.DamageAbsorbed, Is.EqualTo(events.Where(e => e.Type is "unitDamaged" or "effectTick").Sum(e => e.Absorbed)));
            Assert.That(stats.Deaths, Is.EqualTo(events.Count(e => e.Type == "unitDied")));
            Assert.That(stats.Purges, Is.EqualTo(events.Count(e => e.Type == "effectEnded" && e.Reason == "cleansed")));
        }

        [Test]
        public void Le_total_des_sorts_est_la_somme_par_sort()
        {
            var (_, stats, _) = PlayBot(42);
            Assert.That(stats.Casts, Is.GreaterThan(0));
            Assert.That(stats.CastsBySkill.Values.Sum(), Is.EqualTo(stats.Casts));
            Assert.That(stats.CastsBySkill.Keys, Does.Contain("heal_single"));
        }

        [Test]
        public void En_cas_de_victoire_les_degats_au_boss_couvrent_ses_PV_et_la_duree_est_celle_du_combat()
        {
            var (battle, stats, _) = PlayBot(7);
            Assert.That(stats.Result, Is.EqualTo(BattleResults.Victory));
            Assert.That(stats.DamageToBoss, Is.GreaterThanOrEqualTo(battle.GetBossMaxHp()));
            Assert.That(stats.DurationMs, Is.EqualTo(battle.GetClock()));
        }

        [Test]
        public void Un_combat_sans_soigneur_est_une_defaite_avec_des_morts_et_aucun_soin()
        {
            var battle = new Battle(Fixtures.Encounter(1));
            var stats = new CombatStats();
            stats.Attach(battle);
            battle.Run(120000);
            Assert.That(stats.Result, Is.EqualTo(BattleResults.Defeat));
            Assert.That(stats.Deaths, Is.EqualTo(4));
            Assert.That(stats.HealingDone, Is.EqualTo(0));
            Assert.That(stats.Casts, Is.EqualTo(0));
        }

        [Test]
        public void Les_statistiques_sont_deterministes()
        {
            var a = PlayBot(42).stats;
            var b = PlayBot(42).stats;
            Assert.That((a.HealingDone, a.DamageTaken, a.Casts, a.Deaths, a.DurationMs), Is.EqualTo((b.HealingDone, b.DamageTaken, b.Casts, b.Deaths, b.DurationMs)));
        }

        [Test]
        public void Le_telegraphe_expose_sa_duree_totale_pour_les_jauges()
        {
            var battle = new Battle(Fixtures.Encounter(9));
            Telegraph? seen = null;
            for (int t = 0; t < 8000 && seen == null; t += 100)
            {
                battle.Step(100);
                seen = battle.GetTelegraph();
            }
            Assert.That(seen, Is.Not.Null);
            Assert.That(seen!.TotalMs, Is.GreaterThan(0));
            Assert.That(seen.MsRemaining, Is.LessThanOrEqualTo(seen.TotalMs));
        }
    }
}
