using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>
    /// Bornes d'équilibrage (docs/EQUILIBRAGE.md §2 et §4), reprises à l'identique de la version Phaser
    /// (ticket E14-T08). Un changement de valeurs qui sort de ces bornes est une régression d'équilibrage
    /// à expliquer à l'utilisateur ; on ne relâche JAMAIS une borne pour faire passer un test.
    /// </summary>
    public class BalanceTests
    {
        private const int Seeds = 100;
        private const double MaxMs = 150000;

        private sealed class Stats
        {
            public double WinRate, DeathRate, AvgLowestHp, MinDurationMs, MaxDurationMs;
        }

        private static Stats Play(Action<Battle> drive)
        {
            int wins = 0, deaths = 0;
            double lowSum = 0;
            var durations = new List<double>();
            for (uint seed = 1; seed <= Seeds; seed++)
            {
                var battle = new Battle(Fixtures.Encounter(seed));
                double lowest = 1;
                battle.Subscribe(_ => { foreach (var u in battle.GetAllies()) lowest = Math.Min(lowest, u.Hp / u.MaxHp); });
                drive(battle);
                if (battle.GetResult() == BattleResults.Victory) wins++;
                if (battle.GetAllies().Any(u => !u.Alive)) deaths++;
                lowSum += lowest;
                durations.Add(battle.GetClock());
            }
            return new Stats
            {
                WinRate = (double)wins / Seeds,
                DeathRate = (double)deaths / Seeds,
                AvgLowestHp = lowSum / Seeds,
                MinDurationMs = durations.Min(),
                MaxDurationMs = durations.Max(),
            };
        }

        private Stats _attentive = null!, _slow = null!, _noPurge = null!, _noHealer = null!, _spam = null!;

        [OneTimeSetUp]
        public void MesurerTousLesProfils()
        {
            _attentive = Play(b => ReferenceHealerBot.Run(b, MaxMs));
            _slow = Play(b => ReferenceHealerBot.Run(b, MaxMs, new ReferenceHealerOptions { DecisionEveryMs = 1500 }));
            _noPurge = Play(b => ReferenceHealerBot.Run(b, MaxMs, new ReferenceHealerOptions { Purge = false }));
            _noHealer = Play(b => b.Run(MaxMs));
            _spam = Play(b =>
            {
                for (int i = 0; i < 100; i++) b.IssueCommand(new Command { TimeMs = i * 1200, SkillId = "heal_single", TargetId = "tank" });
                b.Run(MaxMs);
            });
        }

        [Test]
        public void Gagnable_un_joueur_attentif_gagne_au_moins_95_pour_cent_des_combats() =>
            Assert.That(_attentive.WinRate, Is.GreaterThanOrEqualTo(0.95));

        [Test]
        public void Juste_un_joueur_attentif_perd_rarement_un_allie() =>
            Assert.That(_attentive.DeathRate, Is.LessThanOrEqualTo(0.10));

        [Test]
        public void Tendu_mais_pas_au_bord_du_gouffre_PV_minimum_moyen_entre_15_et_45_pour_cent()
        {
            Assert.That(_attentive.AvgLowestHp, Is.GreaterThanOrEqualTo(0.15));
            Assert.That(_attentive.AvgLowestHp, Is.LessThanOrEqualTo(0.45));
        }

        [Test]
        public void Duree_adaptee_au_mobile_60_a_120_secondes()
        {
            Assert.That(_attentive.MinDurationMs, Is.GreaterThanOrEqualTo(60000));
            Assert.That(_attentive.MaxDurationMs, Is.LessThanOrEqualTo(120000));
        }

        [Test]
        public void La_passivite_est_punie_sans_soigneur_ou_en_spammant_un_sort_on_perd_toujours()
        {
            Assert.That(_noHealer.WinRate, Is.EqualTo(0));
            Assert.That(_spam.WinRate, Is.EqualTo(0));
        }

        [Test]
        public void La_reactivite_compte_un_joueur_lent_descend_nettement_plus_bas()
        {
            Assert.That(_attentive.AvgLowestHp - _slow.AvgLowestHp, Is.GreaterThanOrEqualTo(0.05));
        }

        [Test]
        public void La_Purge_compte_ignorer_le_poison_fait_descendre_l_equipe_plus_bas()
        {
            Assert.That(_attentive.AvgLowestHp - _noPurge.AvgLowestHp, Is.GreaterThanOrEqualTo(0.05));
            Assert.That(_noPurge.DeathRate, Is.GreaterThan(_attentive.DeathRate));
        }

        [Test]
        public void Les_mesures_de_reference_du_premier_boss_avec_les_mecaniques_actuelles()
        {
            // Mesures de référence du jeu ACTUEL (docs/EQUILIBRAGE.md §12, D-052). Elles ne sont plus celles de la version Phaser :
            // critiques, résistances, armure en %, esquive, menace et incantation ont volontairement déplacé l'équilibre.
            Assert.That(_attentive.WinRate, Is.EqualTo(1.0));
            Assert.That(_attentive.AvgLowestHp, Is.EqualTo(0.28).Within(0.03));
            Assert.That(_slow.AvgLowestHp, Is.EqualTo(0.13).Within(0.04));
            Assert.That(_noPurge.AvgLowestHp, Is.EqualTo(0.18).Within(0.04));
        }
    }
}
