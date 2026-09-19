using System.Linq;
using System.Text.RegularExpressions;
using Healer.Ui;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Portage des tests de src/ui (format, layout, targeting) de la version Phaser (ticket E14-T12, partie logique).</summary>
    public class FormatTests
    {
        [Test]
        public void Truncate_tronque_vers_zero_au_lieu_d_arrondir()
        {
            Assert.That(Format.Truncate(7.99), Is.EqualTo(7));
            Assert.That(Format.Truncate(-7.99), Is.EqualTo(-7));
            Assert.That(Format.Truncate(4.96, 1), Is.EqualTo(4.9));
        }

        [Test]
        public void Truncate_absorbe_les_erreurs_de_flottants()
        {
            Assert.That(Format.Truncate(4.35, 2), Is.EqualTo(4.35));
            Assert.That(Format.Truncate(0.3, 1), Is.EqualTo(0.3));
            Assert.That(Format.Truncate(1.1, 1), Is.EqualTo(1.1));
        }

        [TestCase(0.0, "0")]
        [TestCase(7.9, "7")]
        [TestCase(7000.0, "7000")]
        [TestCase(9999.99, "9999")]
        [TestCase(10000.0, "10k")]
        [TestCase(12399.0, "12,3k")]
        [TestCase(999999.0, "999,9k")]
        [TestCase(2500000.0, "2,5M")]
        public void Number_affiche_un_entier_tronque_ou_abrege(double value, string expected) =>
            Assert.That(Format.Number(value), Is.EqualTo(expected));

        [Test]
        public void Number_ne_produit_jamais_de_notation_scientifique_ni_de_longues_decimales()
        {
            foreach (var n in new[] { 0.1 + 0.2, 1.0 / 3, 7.199999999, 123456789, 1e9 })
                Assert.That(Format.Number(n), Does.Match(@"^-?\d+(,\d)?[kM]?$"), n.ToString());
        }

        [TestCase(4960.0, "4,9s")]
        [TestCase(12000.0, "12s")]
        [TestCase(0.0, "0s")]
        [TestCase(-500.0, "0s")]
        public void Seconds_tronque_a_une_decimale_et_ne_descend_jamais_sous_zero(double ms, string expected) =>
            Assert.That(Format.Seconds(ms), Is.EqualTo(expected));

        [Test]
        public void Ratio_affiche_courant_sur_maximum_tronques() =>
            Assert.That(Format.Ratio(612.7, 900), Is.EqualTo("612/900"));

        [Test]
        public void Le_formatage_ne_depend_pas_de_la_culture_de_la_machine()
        {
            var previous = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("en-US");
                Assert.That(Format.Number(12399), Is.EqualTo("12,3k"));
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
                Assert.That(Format.Seconds(4960), Is.EqualTo("4,9s"));
            }
            finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
        }
    }

    public class LayoutTests
    {
        [Test]
        public void Aucune_taille_de_texte_n_est_inferieure_a_14_px()
        {
            foreach (var kv in Layout.Font.All) Assert.That(kv.Value, Is.GreaterThanOrEqualTo(14), kv.Key);
        }

        [Test]
        public void Toutes_les_zones_tiennent_dans_l_ecran_avec_les_marges_de_securite()
        {
            foreach (var (name, rect) in Layout.Zones.InOrder) Assert.That(rect.InsideScreen(), Is.True, name);
            Assert.That(Layout.Zones.Skills.Bottom, Is.LessThanOrEqualTo(Layout.GameH - Layout.SafeBottom));
            Assert.That(Layout.Zones.TopBar.Y, Is.GreaterThanOrEqualTo(Layout.SafeTop));
        }

        [Test]
        public void Les_zones_ne_se_chevauchent_pas_et_sont_dans_l_ordre_vertical()
        {
            var zones = Layout.Zones.InOrder;
            for (int i = 0; i < zones.Count; i++)
            {
                for (int j = i + 1; j < zones.Count; j++)
                    Assert.That(zones[i].rect.Overlaps(zones[j].rect), Is.False, $"{zones[i].name} / {zones[j].name}");
                if (i > 0) Assert.That(zones[i].rect.Y, Is.GreaterThanOrEqualTo(zones[i - 1].rect.Bottom));
            }
        }

        [Test]
        public void Les_sorts_sont_dans_la_moitie_basse_de_l_ecran_zone_du_pouce() =>
            Assert.That(Layout.Zones.Skills.Y, Is.GreaterThan(Layout.GameH / 2));

        [Test]
        public void La_barre_de_PV_du_boss_ne_chevauche_pas_la_pause_et_reste_dans_la_barre_haute()
        {
            Assert.That(Layout.BossHpBar.Overlaps(Layout.PauseButton), Is.False);
            Assert.That(Layout.BossHpBar.InsideScreen(), Is.True);
            Assert.That(Layout.BossHpBar.Bottom, Is.LessThanOrEqualTo(Layout.Zones.Boss.Y));
            Assert.That(Layout.BossHpBar.W, Is.GreaterThan(200));
        }

        [Test]
        public void Le_bouton_de_pause_est_une_cible_tactile_valide()
        {
            Assert.That(Layout.PauseButton.W, Is.GreaterThanOrEqualTo(Layout.MinTouch));
            Assert.That(Layout.PauseButton.H, Is.GreaterThanOrEqualTo(Layout.MinTouch));
            Assert.That(Layout.PauseButton.InsideScreen(), Is.True);
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void Cartes_d_allies_au_moins_48_px_dans_l_ecran_sans_chevauchement(int count)
        {
            var cards = Layout.TeamCardRects(count);
            Assert.That(cards, Has.Length.EqualTo(count));
            for (int i = 0; i < cards.Length; i++)
            {
                Assert.That(cards[i].W, Is.GreaterThanOrEqualTo(Layout.MinTouch));
                Assert.That(cards[i].H, Is.GreaterThanOrEqualTo(Layout.MinTouch));
                Assert.That(cards[i].InsideScreen(), Is.True);
                if (i > 0) Assert.That(cards[i - 1].Overlaps(cards[i]), Is.False);
            }
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void Boutons_de_sorts_au_moins_48_px_dans_l_ecran_sans_chevauchement(int count)
        {
            var buttons = Layout.SkillButtonRects(count);
            for (int i = 0; i < buttons.Length; i++)
            {
                Assert.That(buttons[i].W, Is.GreaterThanOrEqualTo(Layout.MinTouch), $"bouton {i}");
                Assert.That(buttons[i].H, Is.GreaterThanOrEqualTo(Layout.MinTouch));
                Assert.That(buttons[i].InsideScreen(), Is.True);
                if (i > 0) Assert.That(buttons[i - 1].Overlaps(buttons[i]), Is.False);
            }
        }

        [Test]
        public void La_barre_de_PV_passe_du_vert_a_l_orange_puis_au_rouge()
        {
            Assert.That(Layout.HpBandFor(1), Is.EqualTo(HpBand.High));
            Assert.That(Layout.HpBandFor(0.61), Is.EqualTo(HpBand.High));
            Assert.That(Layout.HpBandFor(0.6), Is.EqualTo(HpBand.Mid));
            Assert.That(Layout.HpBandFor(0.31), Is.EqualTo(HpBand.Mid));
            Assert.That(Layout.HpBandFor(0.3), Is.EqualTo(HpBand.Low));
            Assert.That(Layout.HpBandFor(0), Is.EqualTo(HpBand.Low));
        }

        [Test]
        public void L_echelle_de_rendu_est_bornee_entre_1_et_3_en_entiers_et_retombe_sur_1_si_invalide()
        {
            Assert.That(Layout.ClampRenderScale(1), Is.EqualTo(1));
            Assert.That(Layout.ClampRenderScale(1.5), Is.EqualTo(2));
            Assert.That(Layout.ClampRenderScale(2.625), Is.EqualTo(3));
            Assert.That(Layout.ClampRenderScale(4), Is.EqualTo(3));
            Assert.That(Layout.ClampRenderScale(0), Is.EqualTo(1));
            Assert.That(Layout.ClampRenderScale(double.NaN), Is.EqualTo(1));
            Assert.That(Layout.ClampRenderScale(-2), Is.EqualTo(1));
        }
    }

    public class TargetingTests
    {
        [Test]
        public void Toucher_un_allie_le_selectionne_et_en_toucher_un_autre_deplace_la_selection()
        {
            var sel = new TargetSelection();
            sel.Tap("tank", true);
            Assert.That(sel.Selected, Is.EqualTo("tank"));
            sel.Tap("dps1", true);
            Assert.That(sel.Selected, Is.EqualTo("dps1"));
        }

        [Test]
        public void Retoucher_l_allie_selectionne_annule_la_selection()
        {
            var sel = new TargetSelection();
            sel.Tap("tank", true);
            sel.Tap("tank", true);
            Assert.That(sel.Selected, Is.Null);
        }

        [Test]
        public void Un_allie_KO_ne_peut_pas_etre_selectionne()
        {
            var sel = new TargetSelection();
            sel.Tap("dps1", false);
            Assert.That(sel.Selected, Is.Null);
        }

        [Test]
        public void La_selection_est_abandonnee_a_la_mort_de_l_allie_et_conservee_tant_qu_il_est_vivant()
        {
            var sel = new TargetSelection();
            sel.Tap("dps1", true);
            sel.Sync(new[] { "dps1", "tank" });
            Assert.That(sel.Selected, Is.EqualTo("dps1"));
            sel.Sync(new[] { "tank", "healer" });
            Assert.That(sel.Selected, Is.Null);
        }

        [Test]
        public void Un_sort_de_zone_se_lance_sans_cible_meme_sans_selection()
        {
            var r = new TargetSelection().Resolve(targetsAll: true, canUseNow: true);
            Assert.That(r.Kind, Is.EqualTo(CastKind.Cast));
            Assert.That(r.TargetId, Is.Null);
        }

        [Test]
        public void Un_sort_cible_sans_selection_demande_de_choisir_une_cible()
        {
            Assert.That(new TargetSelection().Resolve(false, true).Kind, Is.EqualTo(CastKind.NeedTarget));
        }

        [Test]
        public void Un_sort_cible_s_applique_a_l_allie_selectionne()
        {
            var sel = new TargetSelection();
            sel.Tap("tank", true);
            var r = sel.Resolve(false, true);
            Assert.That(r.Kind, Is.EqualTo(CastKind.Cast));
            Assert.That(r.TargetId, Is.EqualTo("tank"));
        }

        [Test]
        public void Un_sort_indisponible_ne_lance_rien()
        {
            var sel = new TargetSelection();
            sel.Tap("tank", true);
            Assert.That(sel.Resolve(false, false).Kind, Is.EqualTo(CastKind.Unavailable));
            Assert.That(sel.Resolve(true, false).Kind, Is.EqualTo(CastKind.Unavailable));
        }

        [Test]
        public void La_selection_reste_apres_un_sort_re_soigner_la_meme_cible_en_un_seul_geste()
        {
            var sel = new TargetSelection();
            sel.Tap("tank", true);
            sel.Resolve(false, true);
            Assert.That(sel.Selected, Is.EqualTo("tank"));
            Assert.That(sel.Resolve(false, true).TargetId, Is.EqualTo("tank"));
        }

        [Test]
        public void Un_sort_de_zone_ne_modifie_pas_la_selection()
        {
            var sel = new TargetSelection();
            sel.Tap("dps2", true);
            sel.Resolve(true, true);
            Assert.That(sel.Selected, Is.EqualTo("dps2"));
        }
    }
}
