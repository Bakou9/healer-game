using System;
using System.Linq;
using Healer.Ui;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>
    /// Entrées : quels gestes sont permis dans quel état, boutons modaux, ajustement à l'écran. Ces tests existent
    /// depuis le bug « le bouton Jouer ne démarre pas » (une carte d'allié dessinée avant lui avalait le clic).
    /// </summary>
    public class InputGateTests
    {
        private static readonly UiAction[] AllActions = (UiAction[])Enum.GetValues(typeof(UiAction));

        [Test]
        public void Au_demarrage_seuls_Jouer_et_le_retour_a_la_carte_reagissent()
        {
            foreach (var a in AllActions)
                Assert.That(InputGate.Allows(ScreenState.Start, a), Is.EqualTo(a == UiAction.StartFight || a == UiAction.BackToMap), a.ToString());
        }

        [Test]
        public void En_fin_de_combat_seuls_Recommencer_Niveau_suivant_et_Carte_reagissent()
        {
            foreach (var a in AllActions)
                Assert.That(InputGate.Allows(ScreenState.Ended, a), Is.EqualTo(a == UiAction.Restart || a == UiAction.NextLevel || a == UiAction.BackToMap), a.ToString());
        }

        [Test]
        public void En_combat_on_cible_lance_un_sort_et_met_en_pause_mais_ne_redemarre_pas()
        {
            Assert.That(InputGate.Allows(ScreenState.Playing, UiAction.TapAlly), Is.True);
            Assert.That(InputGate.Allows(ScreenState.Playing, UiAction.TapSkill), Is.True);
            Assert.That(InputGate.Allows(ScreenState.Playing, UiAction.TogglePause), Is.True);
            Assert.That(InputGate.Allows(ScreenState.Playing, UiAction.StartFight), Is.False);
            Assert.That(InputGate.Allows(ScreenState.Playing, UiAction.Restart), Is.False);
            Assert.That(InputGate.Allows(ScreenState.Playing, UiAction.BackToMap), Is.False, "on quitte un combat par la pause, pas d'un clic malheureux");
            Assert.That(InputGate.Allows(ScreenState.Playing, UiAction.NextLevel), Is.False);
        }

        [Test]
        public void En_pause_on_peut_reprendre_et_cibler_mais_pas_lancer_de_sort()
        {
            Assert.That(InputGate.Allows(ScreenState.Paused, UiAction.TogglePause), Is.True);
            Assert.That(InputGate.Allows(ScreenState.Paused, UiAction.TapAlly), Is.True);
            Assert.That(InputGate.Allows(ScreenState.Paused, UiAction.TapSkill), Is.False);
            Assert.That(InputGate.Allows(ScreenState.Paused, UiAction.Restart), Is.False);
            Assert.That(InputGate.Allows(ScreenState.Paused, UiAction.StartFight), Is.False);
            Assert.That(InputGate.Allows(ScreenState.Paused, UiAction.BackToMap), Is.True, "quitter le niveau depuis la pause");
        }

        [TestCase(false, false, false, ScreenState.Start)]
        [TestCase(false, true, true, ScreenState.Start)] // pas démarré : prime sur tout
        [TestCase(true, false, false, ScreenState.Playing)]
        [TestCase(true, true, false, ScreenState.Paused)]
        [TestCase(true, false, true, ScreenState.Ended)]
        [TestCase(true, true, true, ScreenState.Ended)] // fin de combat : prime sur la pause
        public void L_etat_de_l_ecran_suit_une_priorite_claire(bool started, bool paused, bool ended, ScreenState expected) =>
            Assert.That(InputGate.StateOf(started, paused, ended), Is.EqualTo(expected));

        [Test]
        public void La_touche_de_confirmation_fait_jouer_puis_pause_puis_rejouer()
        {
            Assert.That(InputGate.ConfirmAction(ScreenState.Start), Is.EqualTo(UiAction.StartFight));
            Assert.That(InputGate.ConfirmAction(ScreenState.Playing), Is.EqualTo(UiAction.TogglePause));
            Assert.That(InputGate.ConfirmAction(ScreenState.Paused), Is.EqualTo(UiAction.TogglePause));
            Assert.That(InputGate.ConfirmAction(ScreenState.Ended), Is.EqualTo(UiAction.Restart));
        }

        [Test]
        public void La_touche_de_confirmation_ne_fait_jamais_une_action_interdite()
        {
            foreach (ScreenState s in Enum.GetValues(typeof(ScreenState)))
                Assert.That(InputGate.Allows(s, InputGate.ConfirmAction(s)), Is.True, s.ToString());
        }
    }

    public class ModalButtonTests
    {
        private static readonly int[] TeamSizes = { 3, 4, 5 };

        [Test]
        public void Les_boutons_Jouer_et_Recommencer_sont_valides_au_toucher_et_dans_l_ecran()
        {
            foreach (var b in new[] { Layout.StartButton, Layout.RestartButton })
            {
                Assert.That(b.W, Is.GreaterThanOrEqualTo(Layout.MinTouch));
                Assert.That(b.H, Is.GreaterThanOrEqualTo(Layout.MinTouch));
                Assert.That(b.InsideScreen(), Is.True);
            }
        }

        [Test]
        public void Les_boutons_modaux_sont_centres_horizontalement()
        {
            foreach (var b in new[] { Layout.StartButton, Layout.RestartButton })
                Assert.That(b.X + b.W / 2, Is.EqualTo(Layout.GameW / 2).Within(0.001));
        }

        [Test]
        public void Regression_Jouer_recouvre_des_cartes_mais_aucun_clic_ne_peut_etre_capte_a_sa_place()
        {
            // Dans la mise en page paysage, « Jouer » chevauche bien la zone des cartes : la règle d'entrée
            // doit alors interdire à ces éléments de réagir tant que l'écran de démarrage est affiché.
            foreach (int n in TeamSizes)
                foreach (var card in Layout.TeamCardRects(n))
                    if (card.Overlaps(Layout.StartButton))
                        Assert.That(InputGate.Allows(ScreenState.Start, UiAction.TapAlly), Is.False, $"carte chevauchée ({n} alliés)");
            foreach (var skill in Layout.SkillButtonRects(4))
                if (skill.Overlaps(Layout.StartButton))
                    Assert.That(InputGate.Allows(ScreenState.Start, UiAction.TapSkill), Is.False);
            if (Layout.PauseButton.Overlaps(Layout.StartButton))
                Assert.That(InputGate.Allows(ScreenState.Start, UiAction.TogglePause), Is.False);
        }

        [Test]
        public void Regression_Recommencer_recouvre_des_cartes_mais_aucun_clic_ne_peut_etre_capte_a_sa_place()
        {
            foreach (var card in Layout.TeamCardRects(4))
                if (card.Overlaps(Layout.RestartButton))
                    Assert.That(InputGate.Allows(ScreenState.Ended, UiAction.TapAlly), Is.False);
            foreach (var skill in Layout.SkillButtonRects(4))
                if (skill.Overlaps(Layout.RestartButton))
                    Assert.That(InputGate.Allows(ScreenState.Ended, UiAction.TapSkill), Is.False);
            if (Layout.PauseButton.Overlaps(Layout.RestartButton))
                Assert.That(InputGate.Allows(ScreenState.Ended, UiAction.TogglePause), Is.False);
        }

        [Test]
        public void Depuis_la_disposition_en_colonnes_les_boutons_modaux_ne_chevauchent_plus_rien()
        {
            // La cause du bug (un bouton modal posé sur des cartes) a aussi disparu de la géométrie ; InputGate
            // reste le filet si une future mise en page recommence à chevaucher.
            foreach (var modal in new[] { Layout.StartButton, Layout.RestartButton })
            {
                foreach (var c in Layout.TeamCardRects(5)) Assert.That(c.Overlaps(modal), Is.False);
                foreach (var b in Layout.SkillButtonRects(6)) Assert.That(b.Overlaps(modal), Is.False);
                Assert.That(Layout.PauseButton.Overlaps(modal), Is.False);
            }
        }
    }

    public class ScreenFitTests
    {
        [Test]
        public void A_la_taille_logique_exacte_rien_n_est_decale()
        {
            var fit = new ScreenFit(Layout.GameW, Layout.GameH);
            Assert.That(fit.Scale, Is.EqualTo(1).Within(1e-9));
            Assert.That(fit.OffsetX, Is.EqualTo(0).Within(1e-9));
            Assert.That(fit.OffsetY, Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void Un_ecran_plus_large_ajoute_des_bandes_laterales_egales()
        {
            var fit = new ScreenFit(1920, 720); // 8:3
            Assert.That(fit.Scale, Is.EqualTo(1).Within(1e-9));
            Assert.That(fit.OffsetX, Is.EqualTo((1920 - Layout.GameW) / 2).Within(1e-9));
            Assert.That(fit.OffsetY, Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void Un_ecran_plus_haut_ajoute_des_bandes_en_haut_et_en_bas()
        {
            var fit = new ScreenFit(1280, 1000);
            Assert.That(fit.Scale, Is.EqualTo(1).Within(1e-9));
            Assert.That(fit.OffsetX, Is.EqualTo(0).Within(1e-9));
            Assert.That(fit.OffsetY, Is.EqualTo((1000 - Layout.GameH) / 2).Within(1e-9));
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        [TestCase(2560, 1440)]
        [TestCase(800, 600)]
        [TestCase(2340, 1080)] // téléphone en paysage
        [TestCase(1080, 2340)] // téléphone en portrait : encore utilisable, en petit
        public void La_grille_logique_tient_toujours_entiere_dans_l_ecran(double w, double h)
        {
            var fit = new ScreenFit(w, h);
            var (x0, y0) = fit.ToScreen(0, 0);
            var (x1, y1) = fit.ToScreen(Layout.GameW, Layout.GameH);
            Assert.That(x0, Is.GreaterThanOrEqualTo(-1e-6));
            Assert.That(y0, Is.GreaterThanOrEqualTo(-1e-6));
            Assert.That(x1, Is.LessThanOrEqualTo(w + 1e-6));
            Assert.That(y1, Is.LessThanOrEqualTo(h + 1e-6));
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        [TestCase(2340, 1080)]
        public void Aller_a_l_ecran_puis_revenir_au_logique_redonne_le_meme_point(double w, double h)
        {
            var fit = new ScreenFit(w, h);
            foreach (var r in new[] { Layout.StartButton, Layout.RestartButton, Layout.PauseButton })
            {
                var (sx, sy) = fit.ToScreen(r.X + r.W / 2, r.Y + r.H / 2);
                var (lx, ly) = fit.ToLogical(sx, sy);
                Assert.That(lx, Is.EqualTo(r.X + r.W / 2).Within(1e-6));
                Assert.That(ly, Is.EqualTo(r.Y + r.H / 2).Within(1e-6));
            }
        }

        [Test]
        public void Un_clic_au_centre_du_bouton_Jouer_retombe_dans_le_bouton_quelle_que_soit_la_taille_de_fenetre()
        {
            foreach (var (w, h) in new[] { (1280.0, 720.0), (1920.0, 1080.0), (1024.0, 768.0), (2340.0, 1080.0) })
            {
                var fit = new ScreenFit(w, h);
                var c = (x: Layout.StartButton.X + Layout.StartButton.W / 2, y: Layout.StartButton.Y + Layout.StartButton.H / 2);
                var (sx, sy) = fit.ToScreen(c.x, c.y);
                var (lx, ly) = fit.ToLogical(sx, sy);
                Assert.That(lx >= Layout.StartButton.X && lx <= Layout.StartButton.Right && ly >= Layout.StartButton.Y && ly <= Layout.StartButton.Bottom, Is.True, $"{w}x{h}");
            }
        }

        [TestCase(0, 720)]
        [TestCase(1280, 0)]
        [TestCase(-5, -5)]
        public void Une_taille_d_ecran_invalide_ne_produit_ni_division_par_zero_ni_valeur_infinie(double w, double h)
        {
            var fit = new ScreenFit(w, h);
            Assert.That(double.IsFinite(fit.Scale) && double.IsFinite(fit.OffsetX) && double.IsFinite(fit.OffsetY), Is.True);
            Assert.That(fit.Scale, Is.GreaterThan(0));
        }
    }

    /// <summary>Contrôles de la mise en page paysage à deux pouces (D-042, D-046).</summary>
    public class LandscapeLayoutTests
    {
        [Test]
        public void L_ecran_est_en_paysage_16_9()
        {
            Assert.That(Layout.GameW, Is.GreaterThan(Layout.GameH));
            Assert.That(Layout.GameW / Layout.GameH, Is.EqualTo(16.0 / 9.0).Within(0.01));
        }

        [Test]
        public void Les_cartes_d_allies_sont_toutes_dans_la_colonne_de_gauche()
        {
            foreach (int n in new[] { 3, 4, 5 })
                foreach (var c in Layout.TeamCardRects(n))
                {
                    Assert.That(c.X, Is.GreaterThanOrEqualTo(Layout.Zones.LeftX));
                    Assert.That(c.Right, Is.LessThanOrEqualTo(Layout.Zones.LeftX + Layout.Zones.SideW + 0.001));
                }
        }

        [Test]
        public void Les_boutons_de_sorts_sont_tous_dans_la_colonne_de_droite()
        {
            foreach (int n in new[] { 3, 4, 5, 6 })
                foreach (var b in Layout.SkillButtonRects(n))
                {
                    Assert.That(b.X, Is.GreaterThanOrEqualTo(Layout.Zones.RightX - 0.001));
                    Assert.That(b.Right, Is.LessThanOrEqualTo(Layout.GameW - Layout.SafeSide + 0.001));
                }
        }

        [Test]
        public void Une_seule_fonction_par_pouce_aucune_carte_a_droite_aucun_sort_a_gauche()
        {
            var center = Layout.GameW / 2;
            foreach (var c in Layout.TeamCardRects(4)) Assert.That(c.Right, Is.LessThan(center));
            foreach (var b in Layout.SkillButtonRects(4)) Assert.That(b.X, Is.GreaterThan(center));
            Assert.That(Layout.Zones.Strip.X, Is.GreaterThan(center), "le mana se lit avec les sorts, côté droit");
        }

        [Test]
        public void Les_deux_colonnes_sont_symetriques()
        {
            Assert.That(Layout.Zones.LeftX, Is.EqualTo(Layout.GameW - (Layout.Zones.RightX + Layout.Zones.SideW)).Within(0.001));
            Assert.That(Layout.Zones.Team.W, Is.EqualTo(Layout.Zones.Skills.W).Within(0.001));
        }

        [Test]
        public void Les_colonnes_sont_atteignables_du_pouce_depuis_le_bord_de_l_ecran()
        {
            // Un pouce couvre environ un quart de la largeur d'un téléphone en paysage depuis son bord.
            Assert.That(Layout.Zones.Team.Right, Is.LessThanOrEqualTo(Layout.GameW * 0.25));
            Assert.That(Layout.Zones.Skills.X, Is.GreaterThanOrEqualTo(Layout.GameW * 0.75));
        }

        [Test]
        public void La_scene_centrale_est_centree_et_large()
        {
            Assert.That(Layout.Zones.CenterX + Layout.Zones.CenterW / 2, Is.EqualTo(Layout.GameW / 2).Within(0.001));
            Assert.That(Layout.Zones.CenterW, Is.GreaterThanOrEqualTo(600));
            Assert.That(Layout.Zones.Boss.H, Is.GreaterThanOrEqualTo(300));
        }

        [Test]
        public void Le_mana_est_au_dessus_des_sorts_dans_la_meme_colonne()
        {
            Assert.That(Layout.Zones.Strip.X, Is.EqualTo(Layout.Zones.Skills.X).Within(0.001));
            Assert.That(Layout.Zones.Strip.Bottom, Is.LessThanOrEqualTo(Layout.Zones.Skills.Y));
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void Les_cartes_d_allies_sont_assez_hautes_et_larges_pour_un_nom_et_des_PV(int count)
        {
            foreach (var c in Layout.TeamCardRects(count))
            {
                Assert.That(c.H, Is.GreaterThanOrEqualTo(100));
                Assert.That(c.W, Is.GreaterThanOrEqualTo(200));
            }
        }

        [TestCase(4)]
        [TestCase(6)]
        public void Les_boutons_de_sorts_sont_assez_grands_pour_le_pouce(int count)
        {
            foreach (var b in Layout.SkillButtonRects(count))
            {
                // Icônes seules (D-054, demande de l'utilisateur) : carrés d'au moins 96 px, bien au-dessus des 48 px du pouce.
                Assert.That(b.H, Is.GreaterThanOrEqualTo(96));
                Assert.That(b.W, Is.GreaterThanOrEqualTo(96));
            }
        }

        [Test]
        public void La_barre_de_PV_du_boss_est_centree_et_large()
        {
            Assert.That(Layout.BossHpBar.X + Layout.BossHpBar.W / 2, Is.EqualTo(Layout.GameW / 2).Within(0.001));
            Assert.That(Layout.BossHpBar.W, Is.GreaterThanOrEqualTo(600));
        }

        [Test]
        public void La_pause_est_en_haut_a_droite_hors_des_colonnes() =>
            Assert.That(Layout.PauseButton.Bottom, Is.LessThanOrEqualTo(Layout.Zones.Skills.Y));

        [Test]
        public void Les_allies_de_la_scene_sont_alignes_au_centre_sans_se_chevaucher()
        {
            foreach (int n in new[] { 3, 4, 5 })
            {
                double prev = double.NegativeInfinity, sum = 0;
                for (int i = 0; i < n; i++)
                {
                    double x = Layout.AllyStageX(i, n);
                    Assert.That(x - prev, Is.GreaterThanOrEqualTo(150), $"{n} alliés, allié {i}");
                    Assert.That(x, Is.GreaterThan(Layout.Zones.CenterX).And.LessThan(Layout.Zones.CenterX + Layout.Zones.CenterW));
                    prev = x; sum += x;
                }
                Assert.That(sum / n, Is.EqualTo(Layout.GameW / 2).Within(0.001), $"{n} alliés centrés");
            }
        }
    }
}
