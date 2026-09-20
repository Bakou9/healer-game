using System;
using System.Linq;
using Healer.Ui;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    public class NavigatorTests
    {
        [Test]
        public void On_demarre_sur_le_menu_principal()
        {
            var n = new Navigator();
            Assert.That(n.Screen, Is.EqualTo(AppScreen.MainMenu));
            Assert.That(n.LevelId, Is.Null);
        }

        [Test]
        public void Le_parcours_normal_menu_niveaux_combat_niveaux_menu()
        {
            var n = new Navigator();
            Assert.That(n.OpenLevelSelect(), Is.True);
            Assert.That(n.Screen, Is.EqualTo(AppScreen.LevelSelect));
            Assert.That(n.StartLevel("l1", true), Is.True);
            Assert.That(n.Screen, Is.EqualTo(AppScreen.Battle));
            Assert.That(n.LevelId, Is.EqualTo("l1"));
            Assert.That(n.LeaveBattle(), Is.True);
            Assert.That(n.Screen, Is.EqualTo(AppScreen.LevelSelect));
            Assert.That(n.LevelId, Is.Null);
            Assert.That(n.BackToMenu(), Is.True);
            Assert.That(n.Screen, Is.EqualTo(AppScreen.MainMenu));
        }

        [Test]
        public void Un_niveau_verrouille_ne_se_lance_pas()
        {
            var n = new Navigator();
            n.OpenLevelSelect();
            Assert.That(n.StartLevel("l3", false), Is.False);
            Assert.That(n.Screen, Is.EqualTo(AppScreen.LevelSelect));
            Assert.That(n.LevelId, Is.Null);
        }

        [Test]
        public void On_ne_lance_pas_un_niveau_depuis_le_menu_principal()
        {
            var n = new Navigator();
            Assert.That(n.StartLevel("l1", true), Is.False);
            Assert.That(n.Screen, Is.EqualTo(AppScreen.MainMenu));
        }

        [TestCase("")]
        [TestCase(null)]
        public void Un_niveau_sans_identifiant_ne_se_lance_pas(string? id)
        {
            var n = new Navigator();
            n.OpenLevelSelect();
            Assert.That(n.StartLevel(id!, true), Is.False);
        }

        [Test]
        public void Enchainer_sur_le_niveau_suivant_depuis_un_combat()
        {
            var n = new Navigator();
            n.OpenLevelSelect();
            n.StartLevel("l1", true);
            Assert.That(n.StartLevel("l2", true), Is.True);
            Assert.That(n.LevelId, Is.EqualTo("l2"));
            Assert.That(n.Screen, Is.EqualTo(AppScreen.Battle));
        }

        [Test]
        public void Les_transitions_hors_de_leur_ecran_sont_ignorees_sans_rien_changer()
        {
            var n = new Navigator();
            Assert.That(n.BackToMenu(), Is.False);
            Assert.That(n.LeaveBattle(), Is.False);
            n.OpenLevelSelect();
            Assert.That(n.OpenLevelSelect(), Is.False);
            Assert.That(n.LeaveBattle(), Is.False);
            n.StartLevel("l1", true);
            Assert.That(n.OpenLevelSelect(), Is.False);
            Assert.That(n.BackToMenu(), Is.False);
            Assert.That(n.Screen, Is.EqualTo(AppScreen.Battle));
        }

        [Test]
        public void Le_demarrage_direct_sert_aux_captures_et_aux_tests()
        {
            var n = new Navigator();
            n.StartAt("l2");
            Assert.That(n.Screen, Is.EqualTo(AppScreen.Battle));
            Assert.That(n.LevelId, Is.EqualTo("l2"));
        }
    }

    public class AppScreenGateTests
    {
        private static readonly UiAction[] Menu = { UiAction.MenuPlay, UiAction.MenuToggleSound, UiAction.MenuQuit };
        private static readonly UiAction[] Levels = { UiAction.PickLevel, UiAction.BackToMenu };

        [Test]
        public void Le_menu_principal_ne_laisse_passer_que_ses_boutons()
        {
            foreach (UiAction a in Enum.GetValues(typeof(UiAction)))
                Assert.That(InputGate.Allows(AppScreen.MainMenu, ScreenState.Playing, a), Is.EqualTo(Menu.Contains(a)), a.ToString());
        }

        [Test]
        public void Le_choix_du_niveau_ne_laisse_passer_que_ses_boutons()
        {
            foreach (UiAction a in Enum.GetValues(typeof(UiAction)))
                Assert.That(InputGate.Allows(AppScreen.LevelSelect, ScreenState.Playing, a), Is.EqualTo(Levels.Contains(a)), a.ToString());
        }

        [Test]
        public void Un_combat_en_arriere_plan_ne_capte_rien_hors_de_l_ecran_de_combat()
        {
            // Même si l'état du combat est « en cours », les cartes et sorts ne réagissent pas sous un menu.
            foreach (var screen in new[] { AppScreen.MainMenu, AppScreen.LevelSelect })
                foreach (var a in new[] { UiAction.TapAlly, UiAction.TapSkill, UiAction.TogglePause, UiAction.StartFight, UiAction.Restart })
                    Assert.That(InputGate.Allows(screen, ScreenState.Playing, a), Is.False, screen + " / " + a);
        }

        [Test]
        public void A_l_ecran_de_combat_la_regle_du_combat_s_applique_telle_quelle()
        {
            foreach (ScreenState s in Enum.GetValues(typeof(ScreenState)))
                foreach (UiAction a in Enum.GetValues(typeof(UiAction)))
                    Assert.That(InputGate.Allows(AppScreen.Battle, s, a), Is.EqualTo(InputGate.Allows(s, a)), s + " / " + a);
        }

        [Test]
        public void Les_gestes_du_menu_ne_sont_permis_nulle_part_ailleurs()
        {
            foreach (var a in Menu)
                foreach (ScreenState s in Enum.GetValues(typeof(ScreenState)))
                {
                    Assert.That(InputGate.Allows(AppScreen.Battle, s, a), Is.False, a + " en combat / " + s);
                    Assert.That(InputGate.Allows(AppScreen.LevelSelect, s, a), Is.False, a + " au choix du niveau");
                }
        }

        [Test]
        public void La_confirmation_en_fin_de_combat_enchaine_apres_une_victoire_s_il_y_a_un_niveau_suivant()
        {
            Assert.That(InputGate.ConfirmAction(ScreenState.Ended, true, true), Is.EqualTo(UiAction.NextLevel));
            Assert.That(InputGate.ConfirmAction(ScreenState.Ended, true, false), Is.EqualTo(UiAction.Restart));
            Assert.That(InputGate.ConfirmAction(ScreenState.Ended, false, true), Is.EqualTo(UiAction.Restart));
            Assert.That(InputGate.ConfirmAction(ScreenState.Ended, false, false), Is.EqualTo(UiAction.Restart));
        }

        [Test]
        public void La_confirmation_hors_fin_de_combat_ne_depend_ni_de_la_victoire_ni_du_suivant()
        {
            foreach (var s in new[] { ScreenState.Start, ScreenState.Playing, ScreenState.Paused })
                foreach (bool v in new[] { true, false })
                    foreach (bool h in new[] { true, false })
                        Assert.That(InputGate.ConfirmAction(s, v, h), Is.EqualTo(InputGate.ConfirmAction(s)));
        }

        [Test]
        public void La_confirmation_ne_fait_jamais_une_action_interdite_a_l_ecran_de_combat()
        {
            foreach (ScreenState s in Enum.GetValues(typeof(ScreenState)))
                foreach (bool v in new[] { true, false })
                    foreach (bool h in new[] { true, false })
                        Assert.That(InputGate.Allows(s, InputGate.ConfirmAction(s, v, h)), Is.True);
        }
    }

    /// <summary>Mise en page des écrans autres que le combat : menu, choix du niveau, fin de combat, pause.</summary>
    public class OtherScreensLayoutTests
    {
        private static void AssertValid(Rect r, string what)
        {
            Assert.That(r.InsideScreen(), Is.True, what + " dans l'écran");
            Assert.That(r.W, Is.GreaterThanOrEqualTo(Layout.MinTouch), what + " largeur");
            Assert.That(r.H, Is.GreaterThanOrEqualTo(Layout.MinTouch), what + " hauteur");
        }

        [Test]
        public void Les_boutons_du_menu_sont_valides_centres_et_ne_se_chevauchent_pas()
        {
            var buttons = new[] { Layout.MenuPlay, Layout.MenuSound, Layout.MenuQuit };
            foreach (var b in buttons)
            {
                AssertValid(b, "bouton du menu");
                Assert.That(b.X + b.W / 2, Is.EqualTo(Layout.GameW / 2).Within(0.001));
            }
            for (int i = 0; i < buttons.Length; i++)
                for (int j = i + 1; j < buttons.Length; j++) Assert.That(buttons[i].Overlaps(buttons[j]), Is.False);
        }

        [Test]
        public void Jouer_est_le_plus_grand_bouton_du_menu() =>
            Assert.That(Layout.MenuPlay.H, Is.GreaterThan(Layout.MenuSound.H));

        [Test]
        public void Le_bouton_retour_est_valide_et_ne_gene_ni_la_pause_ni_la_barre_du_boss()
        {
            AssertValid(Layout.BackButton, "retour");
            Assert.That(Layout.BackButton.Overlaps(Layout.PauseButton), Is.False);
            Assert.That(Layout.BackButton.Overlaps(Layout.BossHpBar), Is.False);
            Assert.That(Layout.BackButton.Overlaps(Layout.Zones.Team), Is.False);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(6)]
        [TestCase(8)]
        public void Les_cartes_de_niveau_tiennent_dans_l_ecran_sans_chevauchement_et_centrees(int count)
        {
            var cards = Layout.LevelCardRects(count);
            Assert.That(cards, Has.Length.EqualTo(count));
            for (int i = 0; i < cards.Length; i++)
            {
                AssertValid(cards[i], "carte de niveau " + i);
                if (i > 0) Assert.That(cards[i - 1].Overlaps(cards[i]), Is.False);
                Assert.That(cards[i].Overlaps(Layout.BackButton), Is.False);
            }
            Assert.That((cards[0].X + cards[^1].Right) / 2, Is.EqualTo(Layout.GameW / 2).Within(0.001));
        }

        [Test]
        public void Avec_peu_de_niveaux_les_cartes_gardent_leur_taille_pleine() =>
            Assert.That(Layout.LevelCardRects(3).All(c => Math.Abs(c.W - Layout.LevelCardW) < 0.001), Is.True);

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Les_boutons_de_fin_de_combat_sont_valides_centres_et_separes(int count)
        {
            var b = Layout.EndButtonRects(count);
            for (int i = 0; i < b.Length; i++)
            {
                AssertValid(b[i], "bouton de fin " + i);
                if (i > 0) Assert.That(b[i - 1].Overlaps(b[i]), Is.False);
            }
            Assert.That((b[0].X + b[^1].Right) / 2, Is.EqualTo(Layout.GameW / 2).Within(0.001));
        }

        [Test]
        public void Le_menu_de_pause_est_valide_au_centre_hors_des_colonnes()
        {
            foreach (var r in new[] { Layout.PauseResume, Layout.PauseLeave })
            {
                AssertValid(r, "bouton de pause");
                Assert.That(r.Overlaps(Layout.Zones.Team), Is.False);
                Assert.That(r.Overlaps(Layout.Zones.Skills), Is.False);
            }
            Assert.That(Layout.PauseResume.Overlaps(Layout.PauseLeave), Is.False);
        }

        [Test]
        public void Quitter_le_niveau_n_est_jamais_le_bouton_sous_le_pouce_de_Reprendre()
        {
            // Reprendre est au-dessus : un tap rapide au même endroit ne quitte pas le niveau.
            Assert.That(Layout.PauseResume.Y, Is.LessThan(Layout.PauseLeave.Y));
        }
    }
}
