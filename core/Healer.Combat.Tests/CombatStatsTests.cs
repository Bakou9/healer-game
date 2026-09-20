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

        // Overheal (soins gaspillés) : affiché sur l'écran de fin.
        private static CombatStats PlayArena(EncounterDef enc, double ms, params Command[] commands)
        {
            var battle = new Battle(enc, commands);
            var stats = new CombatStats();
            stats.Attach(battle);
            while (battle.GetClock() < ms && battle.GetResult() == BattleResults.Ongoing) battle.Step(50);
            return stats;
        }

        [Test]
        public void Un_soin_sur_un_allie_en_pleine_forme_est_entierement_de_l_overheal()
        {
            var stats = PlayArena(Arena.Make(), 600, new Command { TimeMs = 100, SkillId = "heal_single", TargetId = "tank" });
            Assert.That(stats.HealingDone, Is.EqualTo(0));
            Assert.That(stats.Overheal, Is.EqualTo(170), "tout le soin de 170 dépasse les PV manquants (0)");
            Assert.That(stats.OverhealRatio, Is.EqualTo(1.0));
        }

        [Test]
        public void L_overheal_est_la_part_du_soin_qui_depasse_les_PV_manquants()
        {
            // Le boss frappe les deux alliés (100 - défense 10 = 90 chacun) à 1000 ms ; le soin de zone (140) part à 1500 ms.
            var enc = Arena.Make(boss: Arena.AoePattern);
            var stats = PlayArena(enc, 1600, new Command { TimeMs = 1500, SkillId = "heal_aoe" });
            Assert.That(stats.HealingDone, Is.EqualTo(180), "90 PV manquants x 2 alliés");
            Assert.That(stats.Overheal, Is.EqualTo(100), "(140 - 90) x 2 alliés");
            Assert.That(stats.OverhealRatio, Is.EqualTo(100.0 / 280.0).Within(1e-9));
        }

        [Test]
        public void L_overheal_ne_change_pas_la_trace_des_evenements()
        {
            var e = new BattleEvent { Type = "healed", TimeMs = 5, UnitId = "tank", Amount = 90, Overheal = 50 };
            Assert.That(e.Format(), Is.EqualTo("5ms healed tank 90"), "les golden ne bougent pas");
        }

        [Test]
        public void Sans_soin_l_overheal_est_nul_et_sa_part_aussi()
        {
            var stats = new CombatStats();
            Assert.That(stats.Overheal, Is.EqualTo(0));
            Assert.That(stats.OverhealRatio, Is.EqualTo(0));
        }

        [Test]
        public void Les_degats_avant_boucliers_sont_les_PV_perdus_plus_les_degats_absorbes()
        {
            // Le boss frappe les deux alliés à 1000 ms (90 chacun) ; un Bouclier de 260 sur le Garde à 500 ms absorbe son coup.
            var enc = Arena.Make(boss: Arena.AoePattern);
            var stats = PlayArena(enc, 1200, new Command { TimeMs = 500, SkillId = "shield", TargetId = "tank" });
            Assert.That(stats.DamageAbsorbed, Is.EqualTo(90), "le coup de 90 sur le Garde est absorbé");
            Assert.That(stats.DamageTaken, Is.EqualTo(90), "seul le soigneur perd des PV");
            Assert.That(stats.DamageBeforeShields, Is.EqualTo(180), "sans bouclier : 90 + 90");
        }
    }
}
