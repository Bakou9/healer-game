using Healer.Ui;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Galerie de modèles du mode développeur (D-065) : navigation, entrées permises, place dans le menu.</summary>
    public class GalleryTests
    {
        [Test]
        public void Le_menu_ouvre_la_galerie_et_on_en_revient_sans_pouvoir_lancer_autre_chose()
        {
            var nav = new Navigator();
            Assert.That(nav.OpenGallery(), Is.True);
            Assert.That(nav.Screen, Is.EqualTo(AppScreen.Gallery));
            Assert.That(nav.OpenSettings(), Is.False);
            Assert.That(nav.OpenCredits(), Is.False);
            Assert.That(nav.StartLevel("l1", true), Is.False, "pas de combat depuis la galerie");
            Assert.That(nav.BackToMenu(), Is.True);
            Assert.That(nav.Screen, Is.EqualTo(AppScreen.MainMenu));
        }

        [Test]
        public void La_galerie_n_est_ouvrable_que_depuis_le_menu_principal()
        {
            var nav = new Navigator();
            nav.OpenSettings();
            Assert.That(nav.OpenGallery(), Is.False);
        }

        [Test]
        public void Seuls_ses_boutons_et_le_retour_passent_dans_la_galerie()
        {
            foreach (UiAction a in System.Enum.GetValues(typeof(UiAction)))
                Assert.That(InputGate.Allows(AppScreen.Gallery, ScreenState.Playing, a), Is.EqualTo(a == UiAction.GalleryControl || a == UiAction.BackToMenu), a.ToString());
            Assert.That(InputGate.Allows(AppScreen.MainMenu, ScreenState.Playing, UiAction.MenuGallery), Is.True);
            Assert.That(InputGate.Allows(AppScreen.Credits, ScreenState.Playing, UiAction.GalleryControl), Is.False, "les boutons de la galerie ne fuient pas ailleurs");
        }

        [Test]
        public void Le_bouton_Galerie_est_utilisable_au_toucher_et_ne_chevauche_aucun_bouton_du_menu()
        {
            var b = Layout.MenuGallery;
            Assert.That(b.H, Is.GreaterThanOrEqualTo(Layout.MinTouch));
            Assert.That(b.InsideScreen(), Is.True);
            foreach (var other in new[] { Layout.MenuPlay, Layout.MenuWorkshop, Layout.MenuSettings, Layout.MenuSound, Layout.MenuQuit, Layout.MenuCredits })
                Assert.That(b.Overlaps(other), Is.False);
        }
    }
}
