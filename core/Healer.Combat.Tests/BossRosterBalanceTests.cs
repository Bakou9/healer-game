using System;
using System.Collections.Generic;
using System.Linq;
using Healer.Combat.Progress;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>
    /// Bornes d'équilibrage (docs/EQUILIBRAGE.md §2, §4 et §9) appliquées à CHAQUE boss de la campagne :
    /// les mêmes exigences que pour le premier. On ne relâche jamais une borne pour faire passer un boss :
    /// on règle le boss (voir ZBalayage pour l'outil de balayage).
    /// </summary>
    public class BossRosterBalanceTests
    {
        private const int Seeds = 100;

        private sealed class Profile
        {
            public double WinRate, DeathRate, AvgLowestHp, MinMs, MaxMs, ThreeStarRate, TwoStarOrBetterRate;
        }

        private static readonly Dictionary<string, Dictionary<string, Profile>> Cache = new Dictionary<string, Dictionary<string, Profile>>();

        private static Profile Measure(string bossId, string profile, Action<Battle> drive, Func<List<Command>>? commands = null)
        {
            var content = Fixtures.FullContent();
            var level = content.Levels.Single(l => l.BossId == bossId);
            int wins = 0, deaths = 0, three = 0, twoPlus = 0;
            double low = 0, min = double.MaxValue, max = 0;
            for (uint seed = 1; seed <= Seeds; seed++)
            {
                var battle = new Battle(content.CreateEncounter(bossId, seed), commands?.Invoke());
                var stats = new CombatStats();
                stats.Attach(battle);
                double lowest = 1;
                battle.Subscribe(_ => { foreach (var u in battle.GetAllies()) lowest = Math.Min(lowest, u.Hp / u.MaxHp); });
                drive(battle);
                if (battle.GetResult() == BattleResults.Victory) wins++;
                if (battle.GetAllies().Any(u => !u.Alive)) deaths++;
                int stars = Progression.Stars(level, stats);
                if (stars == 3) three++;
                if (stars >= 2) twoPlus++;
                low += lowest; min = Math.Min(min, battle.GetClock()); max = Math.Max(max, battle.GetClock());
            }
            return new Profile
            {
                WinRate = (double)wins / Seeds, DeathRate = (double)deaths / Seeds, AvgLowestHp = low / Seeds,
                MinMs = min, MaxMs = max, ThreeStarRate = (double)three / Seeds, TwoStarOrBetterRate = (double)twoPlus / Seeds,
            };
        }

        private static Profile Get(string bossId, string profile)
        {
            if (!Cache.TryGetValue(bossId, out var byProfile)) Cache[bossId] = byProfile = new Dictionary<string, Profile>();
            if (byProfile.TryGetValue(profile, out var p)) return p;
            Action<Battle> drive = profile switch
            {
                "attentif" => b => ReferenceHealerBot.Run(b, 150000),
                "lent" => b => ReferenceHealerBot.Run(b, 150000, new ReferenceHealerOptions { DecisionEveryMs = 1500 }),
                "sans purge" => b => ReferenceHealerBot.Run(b, 150000, new ReferenceHealerOptions { Purge = false }),
                _ => b => b.Run(150000),
            };
            Func<List<Command>>? commands = null;
            if (profile == "spam")
                commands = () => Enumerable.Range(0, 100).Select(i => new Command { TimeMs = i * 1200, SkillId = "heal_single", TargetId = "tank" }).ToList();
            return byProfile[profile] = Measure(bossId, profile, drive, commands);
        }

        public static IEnumerable<string> BossIds => Fixtures.FullContent().Bosses.Select(b => b.Id);

        [TestCaseSource(nameof(BossIds))]
        public void Gagnable_un_joueur_attentif_gagne_au_moins_95_pour_cent(string boss) =>
            Assert.That(Get(boss, "attentif").WinRate, Is.GreaterThanOrEqualTo(0.95));

        [TestCaseSource(nameof(BossIds))]
        public void Juste_un_joueur_attentif_perd_rarement_un_allie(string boss) =>
            Assert.That(Get(boss, "attentif").DeathRate, Is.LessThanOrEqualTo(0.10));

        [TestCaseSource(nameof(BossIds))]
        public void Tendu_mais_pas_au_bord_du_gouffre_PV_minimum_moyen_entre_15_et_45_pour_cent(string boss)
        {
            var p = Get(boss, "attentif");
            Assert.That(p.AvgLowestHp, Is.GreaterThanOrEqualTo(0.15));
            Assert.That(p.AvgLowestHp, Is.LessThanOrEqualTo(0.45));
        }

        [TestCaseSource(nameof(BossIds))]
        public void Duree_adaptee_au_mobile_60_a_120_secondes(string boss)
        {
            var p = Get(boss, "attentif");
            Assert.That(p.MinMs, Is.GreaterThanOrEqualTo(60000));
            Assert.That(p.MaxMs, Is.LessThanOrEqualTo(120000));
        }

        [TestCaseSource(nameof(BossIds))]
        public void La_passivite_est_punie_sans_soigneur_ou_en_spammant_un_sort_on_perd_toujours(string boss)
        {
            Assert.That(Get(boss, "sans soigneur").WinRate, Is.EqualTo(0));
            Assert.That(Get(boss, "spam").WinRate, Is.EqualTo(0));
        }

        [TestCaseSource(nameof(BossIds))]
        public void La_reactivite_compte_un_joueur_lent_descend_nettement_plus_bas(string boss) =>
            Assert.That(Get(boss, "attentif").AvgLowestHp - Get(boss, "lent").AvgLowestHp, Is.GreaterThanOrEqualTo(0.05));

        [TestCaseSource(nameof(BossIds))]
        public void La_Purge_compte_ignorer_les_effets_fait_descendre_l_equipe_plus_bas(string boss)
        {
            Assert.That(Get(boss, "attentif").AvgLowestHp - Get(boss, "sans purge").AvgLowestHp, Is.GreaterThanOrEqualTo(0.05));
            Assert.That(Get(boss, "sans purge").DeathRate, Is.GreaterThan(Get(boss, "attentif").DeathRate));
        }

        // ---- Étoiles : les seuils sont réglés pour récompenser la maîtrise, sans être un mur ----

        [TestCaseSource(nameof(BossIds))]
        public void Deux_etoiles_sont_a_portee_d_un_joueur_attentif(string boss) =>
            Assert.That(Get(boss, "attentif").TwoStarOrBetterRate, Is.GreaterThanOrEqualTo(0.85));

        [TestCaseSource(nameof(BossIds))]
        public void Trois_etoiles_sont_atteignables_mais_ne_sont_pas_garanties(string boss)
        {
            var rate = Get(boss, "attentif").ThreeStarRate;
            Assert.That(rate, Is.GreaterThanOrEqualTo(0.20), "trop dur : le 3e étoile serait un mur");
            Assert.That(rate, Is.LessThanOrEqualTo(0.85), "trop facile : la 3e étoile ne récompenserait rien");
        }

        [TestCaseSource(nameof(BossIds))]
        public void Trois_etoiles_recompensent_la_maitrise_un_joueur_lent_en_obtient_moins(string boss) =>
            Assert.That(Get(boss, "lent").ThreeStarRate, Is.LessThan(Get(boss, "attentif").ThreeStarRate));

        [TestCaseSource(nameof(BossIds))]
        public void Ignorer_les_effets_fait_perdre_les_trois_etoiles(string boss) =>
            Assert.That(Get(boss, "sans purge").ThreeStarRate, Is.LessThan(Get(boss, "attentif").ThreeStarRate));

        // ---- Progression de difficulté ----

        [Test]
        public void Le_premier_boss_retrouve_toujours_les_mesures_de_reference_de_la_version_Phaser()
        {
            var p = Get("boss1", "attentif");
            Assert.That(p.AvgLowestHp, Is.EqualTo(0.28).Within(0.03));
            Assert.That(Get("boss1", "lent").AvgLowestHp, Is.EqualTo(0.16).Within(0.04));
        }

        [Test]
        public void Chaque_boss_est_different_a_jouer_sa_signature_de_PV_et_de_duree_n_est_pas_la_meme()
        {
            var ids = BossIds.ToList();
            var signatures = ids.Select(id => $"{Math.Round(Get(id, "attentif").AvgLowestHp, 1)}|{Math.Round(Get(id, "attentif").MinMs / 10000)}").ToList();
            Assert.That(signatures.Distinct().Count(), Is.GreaterThan(1), "les boss se jouent tous pareil");
        }
    }
}
