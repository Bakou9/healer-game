using System.Collections.Generic;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    public class RngTests
    {
        // Valeurs de référence calculées avec l'implémentation TypeScript (mulberry32) de la version Phaser.
        [TestCase(1u, 0.6270739405881613, 0.002735721180215478, 0.5274470399599522)]
        [TestCase(42u, 0.6011037519201636, 0.44829055899754167, 0.8524657934904099)]
        [TestCase(7u, 0.011704753153026104, 0.06195825757458806, 0.97690763277933)]
        public void Le_generateur_reproduit_exactement_la_version_TypeScript(uint seed, double a, double b, double c)
        {
            var rng = new Rng(seed);
            Assert.That(rng.Next(), Is.EqualTo(a));
            Assert.That(rng.Next(), Is.EqualTo(b));
            Assert.That(rng.Next(), Is.EqualTo(c));
        }

        [Test]
        public void Deux_generateurs_de_meme_graine_produisent_la_meme_suite()
        {
            var x = new Rng(123);
            var y = new Rng(123);
            for (int i = 0; i < 1000; i++) Assert.That(x.Next(), Is.EqualTo(y.Next()));
        }

        [Test]
        public void Les_valeurs_restent_dans_0_inclus_1_exclu()
        {
            var rng = new Rng(99);
            for (int i = 0; i < 10000; i++)
            {
                double v = rng.Next();
                Assert.That(v, Is.GreaterThanOrEqualTo(0).And.LessThan(1));
            }
        }

        [Test]
        public void PickRandom_refuse_une_liste_vide()
        {
            Assert.Throws<System.InvalidOperationException>(() => new Rng(1).PickRandom(new List<int>()));
        }
    }

    public class FixedStepperTests
    {
        [Test]
        public void Le_meme_temps_simule_quelle_que_soit_la_cadence_d_affichage()
        {
            double Run(double frameMs)
            {
                var stepper = new FixedStepper(50);
                double total = 0;
                for (double t = 0; t < 10000; t += frameMs) stepper.Advance(frameMs, dt => total += dt);
                return total;
            }
            Assert.That(System.Math.Abs(Run(1000.0 / 60) - Run(1000.0 / 30)), Is.LessThanOrEqualTo(50));
        }

        [Test]
        public void Aucun_pas_tant_que_le_temps_accumule_est_inferieur_a_un_pas()
        {
            var stepper = new FixedStepper(50);
            Assert.That(stepper.Advance(20, _ => { }), Is.EqualTo(0));
            Assert.That(stepper.Advance(20, _ => { }), Is.EqualTo(0));
            Assert.That(stepper.Advance(20, _ => { }), Is.EqualTo(1)); // 60 ms cumulés
        }

        [Test]
        public void Un_gros_ralentissement_abandonne_le_retard_au_lieu_de_le_rattraper()
        {
            var stepper = new FixedStepper(50, 5);
            int steps = 0;
            stepper.Advance(60000, _ => steps++);
            Assert.That(steps, Is.EqualTo(5));
            Assert.That(stepper.Advance(16, _ => { }), Is.EqualTo(0));
        }

        [Test]
        public void Les_delta_negatifs_sont_ignores()
        {
            Assert.That(new FixedStepper(50).Advance(-100, _ => { }), Is.EqualTo(0));
        }
    }
}
