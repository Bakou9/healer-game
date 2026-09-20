using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Enrage du boss : ses dégâts directs montent par paliers passé un certain temps (D-050).</summary>
    public class EnrageTests
    {
        private static GameContent Content() => Fixtures.FullContent();

        /// <summary>
        /// Combat qui ne peut pas se terminer (alliés et boss quasi immortels) : on observe l'enrage dans le temps
        /// sans que la partie s'arrête avant. Une simulation terminée n'avance plus : ne jamais attendre son horloge.
        /// </summary>
        private static EncounterDef Encounter(EnrageDef? enrage, string boss = "boss1", uint seed = 1)
        {
            var c = Content();
            c.BossById(boss).Enrage = enrage;
            c.BossById(boss).MaxHp = 1e12;
            foreach (var ch in c.Characters) ch.MaxHp = 1e9;
            return c.CreateEncounter(boss, seed);
        }

        [Test]
        public void Sans_enrage_le_multiplicateur_reste_a_un_pour_toujours()
        {
            var b = new Battle(Encounter(null));
            foreach (double t in new[] { 0.0, 30000, 90000, 200000 })
            {
                while (b.GetClock() < t) b.Step(100);
                Assert.That(b.GetEnrageMultiplier(), Is.EqualTo(1));
                Assert.That(b.GetEnrageLevel(), Is.EqualTo(0));
            }
        }

        [Test]
        public void Avant_le_delai_le_boss_n_est_pas_enrage()
        {
            var b = new Battle(Encounter(new EnrageDef { AfterMs = 20000, EveryMs = 10000, Pct = 10 }));
            while (b.GetClock() < 19900) b.Step(100);
            Assert.That(b.GetEnrageLevel(), Is.EqualTo(0));
            Assert.That(b.GetEnrageMultiplier(), Is.EqualTo(1));
        }

        [Test]
        public void Au_delai_le_premier_palier_s_applique_puis_un_de_plus_toutes_les_periodes()
        {
            var b = new Battle(Encounter(new EnrageDef { AfterMs = 20000, EveryMs = 10000, Pct = 10 }));
            var expected = new Dictionary<double, (int level, double mult)>
            {
                [20000] = (1, 1.1), [29900] = (1, 1.1), [30000] = (2, 1.2), [40000] = (3, 1.3),
            };
            foreach (var kv in expected.OrderBy(k => k.Key))
            {
                while (b.GetClock() < kv.Key) b.Step(100);
                Assert.That(b.GetEnrageLevel(), Is.EqualTo(kv.Value.level), "à " + kv.Key);
                Assert.That(b.GetEnrageMultiplier(), Is.EqualTo(kv.Value.mult).Within(1e-9), "à " + kv.Key);
            }
        }

        [Test]
        public void Chaque_palier_emet_un_seul_evenement_avec_le_niveau_et_le_pourcentage_cumule()
        {
            var b = new Battle(Encounter(new EnrageDef { AfterMs = 20000, EveryMs = 10000, Pct = 5 }));
            var events = new List<BattleEvent>();
            b.Subscribe(e => { if (e.Type == "bossEnraged") events.Add(e); });
            while (b.GetClock() < 50000) b.Step(100);
            Assert.That(events.Select(e => e.Phase), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(events.Select(e => e.Amount), Is.EqualTo(new[] { 5.0, 10.0, 15.0, 20.0 }));
            Assert.That(events.Select(e => e.TimeMs), Is.EqualTo(new[] { 20000.0, 30000, 40000, 50000 }));
        }

        [Test]
        public void Un_boss_sans_enrage_n_emet_jamais_l_evenement()
        {
            foreach (var boss in new[] { "boss1", "boss2" })
            {
                var b = new Battle(Content().CreateEncounter(boss, 3));
                bool seen = false;
                b.Subscribe(e => { if (e.Type == "bossEnraged") seen = true; });
                ReferenceHealerBot.Run(b, 150000);
                Assert.That(seen, Is.False, boss);
            }
        }

        [Test]
        public void L_enrage_augmente_les_degats_directs_du_boss()
        {
            double FirstHit(EnrageDef? enrage, double atMs)
            {
                var c = Content();
                var boss = c.BossById("boss1");
                boss.Enrage = enrage;
                boss.MaxHp = 1e12;
                foreach (var ch in c.Characters) ch.MaxHp = 1e9;
                boss.Pattern = new List<BossActionDef> { new BossActionDef { Type = "attack", TelegraphMs = 0 } };
                boss.Phases = null;
                var b = new Battle(c.CreateEncounter("boss1", 1));
                double hit = -1;
                b.Subscribe(e => { if (e.Type == "unitDamaged" && e.TimeMs >= atMs && hit < 0 && e.UnitId == "tank") hit = e.Amount + e.Absorbed; });
                while (hit < 0 && b.GetClock() < 120000) b.Step(100);
                return hit;
            }
            double calm = FirstHit(null, 30000);
            double angry = FirstHit(new EnrageDef { AfterMs = 10000, EveryMs = 10000, Pct = 100 }, 30000);
            Assert.That(angry, Is.GreaterThan(calm));
        }

        [Test]
        public void L_enrage_ne_change_pas_les_degats_des_effets_sur_la_duree()
        {
            var calm = new List<double>(); var angry = new List<double>();
            void Run(EnrageDef? enrage, List<double> into)
            {
                var cc = Content();
                cc.BossById("boss2").Enrage = enrage;
                cc.BossById("boss2").MaxHp = 1e12;
                foreach (var ch in cc.Characters) ch.MaxHp = 1e9;
                var b = new Battle(cc.CreateEncounter("boss2", 5));
                b.Subscribe(e => { if (e.Type == "effectTick") into.Add(e.Amount + e.Absorbed); });
                b.Run(60000);
            }
            Run(null, calm);
            Run(new EnrageDef { AfterMs = 1000, EveryMs = 1000, Pct = 50 }, angry);
            Assert.That(calm.Distinct().Count(), Is.EqualTo(1));
            Assert.That(angry.Distinct().Single(), Is.EqualTo(calm.Distinct().Single()), "les brûlures et venins ne dépendent pas de l'enrage");
        }

        [Test]
        public void L_enrage_est_deterministe()
        {
            List<string> Trace()
            {
                var b = new Battle(Content().CreateEncounter("boss3", 9));
                var l = new List<string>(); b.Subscribe(e => l.Add(e.Format()));
                ReferenceHealerBot.Run(b, 150000);
                return l;
            }
            Assert.That(Trace(), Is.EqualTo(Trace()));
        }

        [Test]
        public void Une_periode_nulle_ou_negative_ne_provoque_ni_division_par_zero_ni_enrage()
        {
            var b = new Battle(Encounter(new EnrageDef { AfterMs = 1000, EveryMs = 0, Pct = 10 }));
            Assert.DoesNotThrow(() => b.Run(20000));
            Assert.That(b.GetEnrageLevel(), Is.EqualTo(0));
        }

        [Test]
        public void Les_donnees_d_enrage_sont_valides_et_les_boss_avances_en_ont_un()
        {
            var c = Content();
            foreach (var boss in c.Bosses.Where(b => b.Enrage != null))
            {
                Assert.That(boss.Enrage!.AfterMs, Is.InRange(10000, 120000), boss.Id);
                Assert.That(boss.Enrage.EveryMs, Is.GreaterThanOrEqualTo(1000), boss.Id);
                Assert.That(boss.Enrage.Pct, Is.InRange(1, 50), boss.Id);
            }
            Assert.That(c.BossById("boss3").Enrage, Is.Not.Null, "le dernier boss de la campagne doit enrager");
            Assert.That(c.BossById("boss1").Enrage, Is.Null, "le premier boss est un tutoriel : pas d'enrage");
        }

        [Test]
        public void Le_golden_du_boss_3_contient_les_paliers_d_enrage_et_ceux_des_autres_boss_aucun()
        {
            string Golden(string n) => System.IO.File.ReadAllText(System.IO.Path.Combine(Fixtures.GoldenDir, n + ".txt"));
            Assert.That(Golden("boss3-bot-seed7"), Does.Contain("bossEnraged"));
            foreach (var n in new[] { "bot-seed7", "bot-seed42", "boss2-bot-seed7", "bot-lent-seed7", "bot-sans-purge-seed7" })
                Assert.That(Golden(n), Does.Not.Contain("bossEnraged"), n);
        }
    }

    /// <summary>
    /// Une seule action ne doit pas suffire à gagner tranquillement les boss avancés. Ces tests existent depuis le
    /// retour d'un joueur : « je n'ai qu'à faire des soins de zone de temps en temps et je gagne » (Seigneur de Cendre).
    /// Le premier boss est un tutoriel volontairement indulgent : exempté.
    /// </summary>
    public class StrategyDiversityTests
    {
        private const int Seeds = 100;
        private static readonly GameContent C = Fixtures.FullContent();
        private static readonly Dictionary<string, (double win, double death)> Cache = new Dictionary<string, (double, double)>();

        private static (double win, double death) Play(string boss, string label, Action<Battle> drive)
        {
            var key = boss + "|" + label;
            if (Cache.TryGetValue(key, out var v)) return v;
            int wins = 0, deaths = 0;
            for (uint seed = 1; seed <= Seeds; seed++)
            {
                var b = new Battle(C.CreateEncounter(boss, seed));
                drive(b);
                if (b.GetResult() == BattleResults.Victory) wins++;
                if (b.GetAllies().Any(u => !u.Alive)) deaths++;
            }
            return Cache[key] = ((double)wins / Seeds, (double)deaths / Seeds);
        }

        public static IEnumerable<string> AdvancedBosses() => Fixtures.FullContent().Bosses.Skip(1).Select(b => b.Id);

        [TestCaseSource(nameof(AdvancedBosses))]
        public void Ne_faire_que_du_soin_de_zone_reactif_perd_souvent(string boss)
        {
            var zoneOnly = Play(boss, "zone", b => ZMesureStrategies.Limited(b, new[] { "heal_aoe" }, 0.75));
            Assert.That(zoneOnly.win, Is.LessThanOrEqualTo(0.75), $"{boss} : le soin de zone seul gagne {zoneOnly.win:0%} des combats");
        }

        [TestCaseSource(nameof(AdvancedBosses))]
        public void Soigner_sans_jamais_poser_de_bouclier_ni_purger_coute_des_allies(string boss)
        {
            var lazy = Play(boss, "paresseux", b => ZMesureStrategies.Limited(b, new[] { "heal_aoe", "heal_single" }, 0.75));
            Assert.That(lazy.death, Is.GreaterThanOrEqualTo(0.25), $"{boss} : sans bouclier ni purge, seuls {lazy.death:0%} des combats voient un allié tomber");
        }

        [TestCaseSource(nameof(AdvancedBosses))]
        public void Le_joueur_attentif_fait_nettement_mieux_que_le_joueur_paresseux(string boss)
        {
            var attentive = Play(boss, "attentif", b => ReferenceHealerBot.Run(b, 150000));
            var lazy = Play(boss, "paresseux", b => ZMesureStrategies.Limited(b, new[] { "heal_aoe", "heal_single" }, 0.75));
            Assert.That(lazy.death - attentive.death, Is.GreaterThanOrEqualTo(0.15), boss);
        }

        [Test]
        public void Le_dernier_boss_est_le_plus_exigeant_contre_les_strategies_paresseuses()
        {
            var ids = AdvancedBosses().ToList();
            double LazyLoss(string b) => 1 - Play(b, "zone", x => ZMesureStrategies.Limited(x, new[] { "heal_aoe" }, 0.75)).win;
            Assert.That(LazyLoss(ids.Last()), Is.GreaterThanOrEqualTo(0.25), "au moins 25 % de défaites en soin de zone seul");
        }

        // Retour de playtest (D-056) : « il suffit de spammer le Soin de zone, avec quelques purges ». Cette stratégie
        // n'était couverte par aucune borne (zone seul = sans purge ; paresseux = sans purge non plus).
        [TestCaseSource(nameof(AdvancedBosses))]
        public void Le_soin_de_zone_en_boucle_avec_des_purges_ne_suffit_pas_a_gagner_presque_tout_le_temps(string boss)
        {
            var spam = Play(boss, "zone-boucle-purge", b => ZMesureSpamZone.ZoneSpam(b, true));
            Assert.That(spam.win, Is.LessThanOrEqualTo(0.60), $"{boss} : le soin de zone en boucle avec purges gagne {spam.win:0%} des combats");
        }

        // Attaque ciblée (D-056) : l'annonce de la victime doit compter. Ne pas protéger la victime annoncée coûte des alliés.
        [TestCaseSource(nameof(AdvancedBosses))]
        public void Ne_pas_proteger_la_victime_annoncee_d_une_attaque_ciblee_n_est_jamais_avantageux(string boss)
        {
            var attentive = Play(boss, "attentif", b => ReferenceHealerBot.Run(b, 150000));
            var ignoring = Play(boss, "ignore-cible", b => ReferenceHealerBot.Run(b, 150000, new ReferenceHealerOptions { ProtectFocusTarget = false }));
            // Borne modeste, mesurée : +2 à +3 points (seuil 1 point contre les erreurs d arrondi) de morts (7 % → 9 % Reine, 5 % → 8 % Seigneur). Le levier est faible dans cette première version (D-056).
            Assert.That(ignoring.death - attentive.death, Is.GreaterThanOrEqualTo(0.01), $"{boss} : attentif {attentive.death:0%} de morts, sans protéger la cible {ignoring.death:0%}");
        }

        [Test]
        public void Le_dernier_boss_punit_nettement_le_soin_de_zone_en_boucle_avec_purges()
        {
            var last = AdvancedBosses().Last();
            var spam = Play(last, "zone-boucle-purge", b => ZMesureSpamZone.ZoneSpam(b, true));
            Assert.That(spam.win, Is.LessThanOrEqualTo(0.40), $"{last} : {spam.win:0%} de victoires");
        }
    }
}
