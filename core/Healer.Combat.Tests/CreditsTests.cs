using System.IO;
using System.Linq;
using Healer.Combat;
using Healer.Ui;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Générique et politique de licences des ressources externes (D-060).</summary>
    public class CreditsTests
    {
        private static CreditsData Data() => CreditsData.FromJson(File.ReadAllText(Path.Combine(Fixtures.ContentDir, "credits.json")));

        private static CreditEntry Valid() => new CreditEntry { Name = "Pack de test", Author = "Auteur", License = "CC0-1.0", Url = "https://exemple.org/pack", UsedFor = "Modèles", Folder = "PackTest" };

        [Test]
        public void Le_fichier_de_credits_du_jeu_est_valide()
        {
            var d = Data();
            Assert.That(d.Title, Is.Not.Empty);
            foreach (var a in d.Assets) Assert.That(CreditPolicy.Problems(a, true), Is.Empty, a.Name);
            foreach (var t in d.Tools) Assert.That(CreditPolicy.Problems(t, false), Is.Empty, t.Name);
            Assert.That(d.Tools.Any(t => t.Name.Contains("Unity")), Is.True, "le moteur est remercié");
        }

        [Test]
        public void Une_ressource_complete_sous_licence_libre_est_acceptee() => Assert.That(CreditPolicy.Problems(Valid(), true), Is.Empty);

        [TestCase("", "auteur")]
        [TestCase("GPL-3.0", "licence")]
        [TestCase("CC-BY-NC-4.0", "licence")]
        [TestCase("Propriétaire", "licence")]
        public void Une_licence_non_autorisee_ou_un_champ_manquant_est_refuse(string license, string mention)
        {
            var e = Valid();
            if (license == "") e.Author = ""; else e.License = license;
            Assert.That(CreditPolicy.Problems(e, true).Any(p => p.Contains(mention)), Is.True);
        }

        [Test]
        public void Une_url_sans_https_ou_un_dossier_manquant_est_refuse()
        {
            var e = Valid(); e.Url = "http://exemple.org"; e.Folder = null;
            var problems = CreditPolicy.Problems(e, true);
            Assert.That(problems.Any(p => p.Contains("https")), Is.True);
            Assert.That(problems.Any(p => p.Contains("dossier")), Is.True);
        }

        [Test]
        public void Chaque_dossier_importe_a_son_entree_dans_le_generique()
        {
            // Garde-fou : aucune ressource ne peut entrer dans le dépôt sans que son auteur soit remercié.
            string imported = Path.GetFullPath(Path.Combine(Fixtures.CoreDir, "..", "unity", "HealerGame", "Assets", "Art", "Imported"));
            if (!Directory.Exists(imported)) { Assert.Pass("aucune ressource importée"); return; }
            var credited = Data().Assets.Select(a => a.Folder).ToHashSet();
            foreach (var dir in Directory.GetDirectories(imported).Select(Path.GetFileName))
                Assert.That(credited, Does.Contain(dir), $"« {dir} » est importé sans entrée dans core/content/credits.json");
        }

        [Test]
        public void Le_generique_remercie_chaque_auteur_avec_sa_licence_et_sa_source()
        {
            var d = new CreditsData { Title = "Jeu" };
            d.Assets.Add(Valid());
            d.Assets.Add(new CreditEntry { Name = "Modèle B", Author = "Autrice B", License = "CC-BY-4.0", Url = "https://exemple.org/b", UsedFor = "Boss", Folder = "B", Modified = true });
            var lines = CreditsRoll.Build(d);
            var text = lines.Select(l => l.Text).ToList();
            Assert.That(text, Does.Contain("« Pack de test » — Auteur"));
            Assert.That(text, Does.Contain("« Modèle B » — Autrice B"));
            Assert.That(text.Any(t => t.Contains("licence CC-BY-4.0") && t.Contains("modifié")), Is.True);
            Assert.That(text, Does.Contain("https://exemple.org/b"));
            Assert.That(text.Any(t => t.Contains("Merci à tous les auteurs")), Is.True);
        }

        [Test]
        public void Sans_ressource_externe_le_generique_le_dit_honnetement()
        {
            var lines = CreditsRoll.Build(new CreditsData());
            Assert.That(lines.Any(l => l.Text.Contains("Aucune ressource externe")), Is.True);
            Assert.That(lines.First().Kind, Is.EqualTo(CreditLineKind.Title));
            Assert.That(CreditsRoll.TotalHeight(lines), Is.GreaterThan(0));
        }

        [Test]
        public void Le_menu_ouvre_le_generique_et_seul_le_retour_y_est_permis()
        {
            var nav = new Navigator();
            Assert.That(nav.OpenCredits(), Is.True);
            Assert.That(nav.Screen, Is.EqualTo(AppScreen.Credits));
            Assert.That(nav.OpenSettings(), Is.False, "pas d'autre écran depuis le générique");
            Assert.That(nav.StartLevel("l1", true), Is.False);
            Assert.That(InputGate.Allows(AppScreen.Credits, ScreenState.Playing, UiAction.BackToMenu), Is.True);
            foreach (UiAction a in System.Enum.GetValues(typeof(UiAction)))
                if (a != UiAction.BackToMenu) Assert.That(InputGate.Allows(AppScreen.Credits, ScreenState.Playing, a), Is.False, a.ToString());
            Assert.That(nav.BackToMenu(), Is.True);
            Assert.That(InputGate.Allows(AppScreen.MainMenu, ScreenState.Playing, UiAction.MenuCredits), Is.True);
        }

        [Test]
        public void Le_bouton_Credits_est_utilisable_au_toucher_et_ne_chevauche_aucun_bouton_du_menu()
        {
            var b = Layout.MenuCredits;
            Assert.That(b.H, Is.GreaterThanOrEqualTo(Layout.MinTouch));
            Assert.That(b.InsideScreen(), Is.True);
            foreach (var other in new[] { Layout.MenuPlay, Layout.MenuWorkshop, Layout.MenuSettings, Layout.MenuSound, Layout.MenuQuit })
                Assert.That(b.Overlaps(other), Is.False);
        }
    }
}
