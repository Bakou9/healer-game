using System.Linq;
using Healer.Combat.Progress;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Outils du mode développeur (D-063) : niveaux d'équipement libres et or à 99 999.</summary>
    public class DevModeTests
    {
        private static (PlayerProfile profile, GameContent content) New()
        {
            var content = Fixtures.FullContent();
            return (PlayerProfile.NewGame(content), content);
        }

        [Test]
        public void Le_niveau_se_fixe_sans_payer_et_reste_dans_les_bornes()
        {
            var (p, c) = New();
            int gold = p.Wallet.Balance(Wallet.Gold);
            Assert.That(Workshop.DevSetEquipmentLevel(p, c, "tank_weapon", 3), Is.EqualTo(3));
            Assert.That(p.Loadout.LevelOf("tank_weapon"), Is.EqualTo(3));
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(gold), "aucun achat : l'or ne bouge pas");
            int max = c.Upgrades.Track("tank_weapon")!.MaxLevel;
            Assert.That(Workshop.DevSetEquipmentLevel(p, c, "tank_weapon", 99), Is.EqualTo(max));
            Assert.That(Workshop.DevSetEquipmentLevel(p, c, "tank_weapon", -4), Is.EqualTo(0));
            Assert.That(p.Loadout.Equipment.ContainsKey("tank_weapon"), Is.False, "niveau 0 = pas d'entrée");
        }

        [Test]
        public void Une_piste_inconnue_est_refusee_sans_effet()
        {
            var (p, c) = New();
            Assert.That(Workshop.DevSetEquipmentLevel(p, c, "inconnue", 2), Is.EqualTo(-1));
            Assert.That(p.Loadout.Equipment, Is.Empty);
        }

        [Test]
        public void On_peut_monter_puis_descendre_un_niveau_a_la_fois()
        {
            var (p, c) = New();
            foreach (int expected in new[] { 1, 2, 3, 2, 1, 0 })
            {
                int step = expected > p.Loadout.LevelOf("healer_armor") ? 1 : -1;
                Assert.That(Workshop.DevSetEquipmentLevel(p, c, "healer_armor", p.Loadout.LevelOf("healer_armor") + step), Is.EqualTo(expected));
            }
        }

        [TestCase(0)]
        [TestCase(400)]
        [TestCase(500_000)]
        public void L_or_est_amene_exactement_a_99999_par_le_portefeuille_et_journalise(int start)
        {
            var (p, _) = New();
            Workshop.DevSetGold(p, 0);
            if (start > 0) p.Wallet.Grant(Wallet.Gold, start, "test");
            Workshop.DevSetGold(p, Workshop.DevGold);
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(99_999));
            Assert.That(p.Wallet.Ledger.Any(e => e.Reason == "mode développeur"), Is.True);
        }

        [Test]
        public void Le_niveau_developpeur_est_pris_en_compte_par_l_apparence_et_les_statistiques()
        {
            var (p, c) = New();
            c.Appearance = AppearanceCatalog.FromJson(System.IO.File.ReadAllText(System.IO.Path.Combine(Fixtures.ContentDir, "appearance.json")));
            Workshop.DevSetEquipmentLevel(p, c, "tank_armor", 5);
            Assert.That(c.Appearance.Resolve("tank", p.Loadout).TierOf("armor"), Is.EqualTo(2));
            var (chars, _) = LoadoutApplier.Apply(c.Upgrades, p.Loadout, c.Characters, c.Skills);
            Assert.That(chars.First(x => x.Id == "tank").MaxHp, Is.GreaterThan(c.Characters.First(x => x.Id == "tank").MaxHp));
        }
    }
}
