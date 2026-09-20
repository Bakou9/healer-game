using System.Linq;
using Healer.Combat;
using Healer.Ui;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Fiche de sort du menu de pause (D-054) et grille de sorts en icônes.</summary>
    public class SkillDescriberTests
    {
        private static SkillDef Heal(double amount = 170, double cost = 18, double cast = 1000, double cd = 0) =>
            new SkillDef { Id = "heal_single", Name = "Soin", ManaCost = cost, CastMs = cast, CooldownMs = cd, Target = "single", HealAmount = amount };

        [Test]
        public void Sans_bonus_la_valeur_de_base_et_la_valeur_actuelle_sont_identiques()
        {
            var stats = SkillDescriber.Stats(Heal(), Heal());
            Assert.That(stats.Any(s => s.Changed), Is.False);
        }

        [Test]
        public void Un_bonus_de_soin_et_un_cout_reduit_apparaissent_ligne_par_ligne()
        {
            var stats = SkillDescriber.Stats(Heal(170, 18), Heal(187, 15)).ToDictionary(s => s.Label);
            Assert.That(stats["Soin"].Base, Is.EqualTo("170 PV"));
            Assert.That(stats["Soin"].Current, Is.EqualTo("187 PV"));
            Assert.That(stats["Soin"].Changed, Is.True);
            Assert.That(stats["Coût"].Base, Is.EqualTo("18 mana"));
            Assert.That(stats["Coût"].Current, Is.EqualTo("15 mana"));
            Assert.That(stats["Incantation"].Changed, Is.False);
        }

        [Test]
        public void Incantation_et_recharge_sont_lisibles_et_tronquees()
        {
            var stats = SkillDescriber.Stats(Heal(cast: 1000, cd: 0), Heal(cast: 1990, cd: 4960)).ToDictionary(s => s.Label);
            Assert.That(stats["Incantation"].Base, Is.EqualTo("1s"));
            Assert.That(stats["Incantation"].Current, Is.EqualTo("1,9s"), "tronqué, jamais arrondi vers le haut");
            Assert.That(stats["Recharge"].Base, Is.EqualTo("Aucune"));
            Assert.That(stats["Recharge"].Current, Is.EqualTo("4,9s"));
        }

        [Test]
        public void Un_sort_instantane_est_annonce_comme_tel()
        {
            var s = new SkillDef { Id = "shield", Name = "Bouclier", ManaCost = 28, CooldownMs = 7000, Target = "single", ShieldAmount = 260 };
            var stats = SkillDescriber.Stats(s, s).ToDictionary(x => x.Label);
            Assert.That(stats["Incantation"].Current, Is.EqualTo("Instantané"));
            Assert.That(stats["Bouclier"].Current, Is.EqualTo("260 PV"));
            Assert.That(stats.ContainsKey("Soin"), Is.False);
        }

        [Test]
        public void La_purge_et_le_soin_de_zone_ont_leur_ligne_d_effet_et_de_cible()
        {
            var purge = new SkillDef { Id = "purge", Name = "Purge", ManaCost = 12, CooldownMs = 5000, Target = "single", Cleanse = true };
            var aoe = new SkillDef { Id = "heal_aoe", Name = "Soin de zone", ManaCost = 45, CooldownMs = 5000, Target = "all", HealAmount = 160 };
            Assert.That(SkillDescriber.Stats(purge, purge).Any(s => s.Label == "Effet" && s.Current.Contains("négatifs")), Is.True);
            Assert.That(SkillDescriber.Stats(aoe, aoe).First(s => s.Label == "Cible").Current, Is.EqualTo("Toute l'équipe"));
        }

        [Test]
        public void Chaque_sort_du_jeu_a_une_fiche_avec_au_moins_cout_et_recharge_ou_incantation()
        {
            var content = Fixtures.FullContent();
            foreach (var skill in content.Skills)
            {
                var stats = SkillDescriber.Stats(skill, skill);
                Assert.That(stats.Any(s => s.Label == "Coût"), Is.True, skill.Id);
                Assert.That(stats.All(s => !string.IsNullOrWhiteSpace(s.Current)), Is.True, skill.Id);
            }
        }
    }

    public class SkillGridLayoutTests
    {
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(6)]
        public void Les_sorts_forment_une_grille_de_carres_dans_la_colonne_de_droite(int count)
        {
            var rects = Layout.SkillButtonRects(count);
            Assert.That(rects.Length, Is.EqualTo(count));
            foreach (var r in rects)
            {
                Assert.That(r.W, Is.EqualTo(r.H).Within(0.001), "carré");
                Assert.That(r.W, Is.GreaterThanOrEqualTo(96));
                Assert.That(r.X, Is.GreaterThanOrEqualTo(Layout.Zones.Skills.X - 0.001));
                Assert.That(r.Right, Is.LessThanOrEqualTo(Layout.Zones.Skills.Right + 0.001));
                Assert.That(r.Bottom, Is.LessThanOrEqualTo(Layout.Zones.Skills.Bottom + 0.001));
            }
            for (int i = 0; i < rects.Length; i++)
                for (int j = i + 1; j < rects.Length; j++)
                    Assert.That(rects[i].Overlaps(rects[j]), Is.False, $"{i} et {j}");
        }

        [Test]
        public void Le_premier_sort_est_en_haut_a_gauche_de_la_grille_le_deuxieme_a_sa_droite()
        {
            var r = Layout.SkillButtonRects(4);
            Assert.That(r[1].Y, Is.EqualTo(r[0].Y).Within(0.001));
            Assert.That(r[1].X, Is.GreaterThan(r[0].Right));
            Assert.That(r[2].X, Is.EqualTo(r[0].X).Within(0.001));
            Assert.That(r[2].Y, Is.GreaterThan(r[0].Bottom));
        }
    }
}
