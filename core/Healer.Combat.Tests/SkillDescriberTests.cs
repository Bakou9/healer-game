using System.Linq;
using Healer.Combat;
using Healer.Ui;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Fiche de sort du menu de pause (D-055) : valeurs modifiées entre parenthèses.</summary>
    public class SkillDescriberTests
    {
        private static SkillDef Aoe(double heal = 160, double cost = 45, double cd = 5000, double cast = 0) =>
            new SkillDef { Id = "heal_aoe", Name = "Soin de zone", ManaCost = cost, CooldownMs = cd, CastMs = cast, Target = "all", HealAmount = heal,
                Description = "Soigne tous les alliés vivants de {heal} PV, mais coûteux et lent à recharger." };

        private static string Plain(System.Collections.Generic.IReadOnlyList<SkillSpan> s) => SkillSheet.Plain(s);

        [Test]
        public void Sans_bonus_rien_n_est_entre_parentheses()
        {
            var sheet = SkillDescriber.Sheet(Aoe(), Aoe());
            Assert.That(Plain(sheet.Cost), Is.EqualTo("45 mana"));
            Assert.That(Plain(sheet.Cast), Is.EqualTo("Incantation : instantanée"));
            Assert.That(Plain(sheet.Cooldown), Is.EqualTo("CD : 5s"));
            Assert.That(sheet.Target, Is.EqualTo("Cible : toute l'équipe"));
            Assert.That(Plain(sheet.Description), Is.EqualTo("Soigne tous les alliés vivants de 160 PV, mais coûteux et lent à recharger."));
            Assert.That(sheet.Cost.Concat(sheet.Cast).Concat(sheet.Cooldown).Concat(sheet.Description).Any(s => s.Changed), Is.False);
        }

        [Test]
        public void Une_valeur_modifiee_s_ecrit_base_puis_nouvelle_valeur_entre_parentheses_marquee_changee()
        {
            var sheet = SkillDescriber.Sheet(Aoe(160, 45), Aoe(240, 41));
            Assert.That(Plain(sheet.Cost), Is.EqualTo("45(41) mana"));
            Assert.That(sheet.Cost.Count(s => s.Changed), Is.EqualTo(1));
            Assert.That(sheet.Cost.First(s => s.Changed).Text, Is.EqualTo("41"));
            Assert.That(Plain(sheet.Description), Is.EqualTo("Soigne tous les alliés vivants de 160(240) PV, mais coûteux et lent à recharger."));
            Assert.That(sheet.Description.Any(s => s.Changed && s.Text == "240"), Is.True);
        }

        [Test]
        public void Incantation_et_recharge_modifiees_sont_tronquees_et_lisibles()
        {
            var sheet = SkillDescriber.Sheet(Aoe(cd: 5000, cast: 1000), Aoe(cd: 4960, cast: 1990));
            Assert.That(Plain(sheet.Cast), Is.EqualTo("Incantation : 1s(1,9s)"), "tronqué, jamais arrondi vers le haut");
            Assert.That(Plain(sheet.Cooldown), Is.EqualTo("CD : 5s(4,9s)"));
        }

        [Test]
        public void Un_bonus_peut_donner_ou_retirer_l_incantation()
        {
            Assert.That(Plain(SkillDescriber.Sheet(Aoe(cast: 0), Aoe(cast: 500)).Cast), Is.EqualTo("Incantation : instantanée(0,5s)"));
            Assert.That(Plain(SkillDescriber.Sheet(Aoe(cd: 0), Aoe(cd: 0)).Cooldown), Is.EqualTo("CD : aucun"));
        }

        [Test]
        public void Le_bouclier_et_l_incantation_du_soin_sont_inseres_dans_leur_description()
        {
            var shield = new SkillDef { Id = "shield", Name = "Bouclier", ManaCost = 28, CooldownMs = 7000, Target = "single", ShieldAmount = 260, Description = "Pose un bouclier qui absorbe {shield} points de dégâts avant les PV." };
            var boosted = new SkillDef { Id = "shield", Name = "Bouclier", ManaCost = 28, CooldownMs = 7000, Target = "single", ShieldAmount = 312, Description = shield.Description };
            Assert.That(Plain(SkillDescriber.Sheet(shield, boosted).Description), Is.EqualTo("Pose un bouclier qui absorbe 260(312) points de dégâts avant les PV."));
            var heal = new SkillDef { Id = "heal_single", Name = "Soin", ManaCost = 18, CastMs = 1000, Target = "single", HealAmount = 170, Description = "Soigne un allié ciblé de {heal} PV après une incantation de {cast}." };
            Assert.That(Plain(SkillDescriber.Sheet(heal, heal).Description), Is.EqualTo("Soigne un allié ciblé de 170 PV après une incantation de 1s."));
        }

        [Test]
        public void Un_marqueur_inconnu_est_laisse_tel_quel_et_une_description_absente_donne_un_texte_vide()
        {
            var s = new SkillDef { Id = "x", Name = "X", Description = "Fait {truc} ici." };
            Assert.That(Plain(SkillDescriber.Sheet(s, s).Description), Is.EqualTo("Fait {truc} ici."));
            var none = new SkillDef { Id = "y", Name = "Y" };
            Assert.That(Plain(SkillDescriber.Sheet(none, none).Description), Is.EqualTo(""));
        }

        [Test]
        public void Chaque_sort_du_jeu_a_une_fiche_complete_sans_marqueur_oublie()
        {
            var content = Fixtures.FullContent();
            foreach (var skill in content.Skills)
            {
                var sheet = SkillDescriber.Sheet(skill, skill);
                Assert.That(SkillSheet.Plain(sheet.Cost), Does.EndWith(" mana"), skill.Id);
                Assert.That(SkillSheet.Plain(sheet.Description), Is.Not.Empty, skill.Id);
                Assert.That(SkillSheet.Plain(sheet.Description), Does.Not.Contain("{"), skill.Id + " : marqueur non remplacé");
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
