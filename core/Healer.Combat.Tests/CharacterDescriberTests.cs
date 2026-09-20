using System.Linq;
using Healer.Combat;
using Healer.Ui;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Fiche des personnages du menu de pause (D-059).</summary>
    public class CharacterDescriberTests
    {
        private static string Line(CharacterSheet s, string label) => SkillSheet.Plain(s.Lines.First(l => l.Label == label).Value);

        private static CharacterDef Tank(double hp = 900, double armor = 25) =>
            new CharacterDef { Id = "tank", Name = "Garde", Role = "tank", MaxHp = hp, Atk = 35, Def = 12, ArmorPct = armor, ThreatMod = 500, CritPct = 0, CritMultPct = 150, Resist = new System.Collections.Generic.Dictionary<string, int> { ["fire"] = 15 } };

        [Test]
        public void Sans_bonus_rien_n_est_entre_parentheses_et_les_valeurs_du_moment_apparaissent()
        {
            var live = new UnitState { Hp = 840, MaxHp = 900, Shield = 66, Alive = true };
            var sheet = CharacterDescriber.Sheet(Tank(), Tank(), live);
            Assert.That(Line(sheet, "PV"), Is.EqualTo("840 / 900"));
            Assert.That(Line(sheet, "Bouclier"), Is.EqualTo("66 PV"));
            Assert.That(Line(sheet, "Armure"), Is.EqualTo("25 %"));
            Assert.That(Line(sheet, "Menace"), Is.EqualTo("500 %"));
            Assert.That(Line(sheet, "Résistances"), Is.EqualTo("feu 15 %"));
            Assert.That(sheet.Role, Is.EqualTo("Tank"));
            Assert.That(sheet.Lines.SelectMany(l => l.Value).Any(s => s.Changed), Is.False);
        }

        [Test]
        public void Un_bonus_s_ecrit_base_puis_nouvelle_valeur_entre_parentheses()
        {
            var sheet = CharacterDescriber.Sheet(Tank(900), Tank(936), new UnitState { Hp = 936, MaxHp = 936 });
            Assert.That(Line(sheet, "PV"), Is.EqualTo("936 / 900(936)"));
            Assert.That(sheet.Lines.First(l => l.Label == "PV").Value.Count(s => s.Changed), Is.EqualTo(1));
            Assert.That(Line(sheet, "Bouclier"), Is.EqualTo("aucun"));
        }

        [Test]
        public void La_critique_montre_la_chance_et_les_degats_et_le_soigneur_a_du_mana()
        {
            var dps = new CharacterDef { Id = "dps2", Name = "Mage", Role = "dps", MaxHp = 360, Atk = 78, Def = 8, CritPct = 12, CritMultPct = 200, DamageType = "magic", ThreatMod = 100 };
            var sheet = CharacterDescriber.Sheet(dps, dps);
            Assert.That(Line(sheet, "Critique"), Is.EqualTo("12 % · dégâts 200 %"));
            Assert.That(Line(sheet, "Attaque"), Is.EqualTo("78 (magique)"));
            Assert.That(sheet.Lines.Any(l => l.Label == "Mana"), Is.False);
            var healer = new CharacterDef { Id = "healer", Name = "Vous", Role = "healer", MaxHp = 480, Atk = 0, Def = 10, MaxMana = 100, ManaRegenPerSec = 6.39, CritPct = 10, CritMultPct = 150 };
            var hs = CharacterDescriber.Sheet(healer, healer);
            Assert.That(Line(hs, "Mana"), Is.EqualTo("100"));
            Assert.That(Line(hs, "Régén. mana"), Is.EqualTo("6,3/s"), "tronqué, jamais arrondi");
            Assert.That(hs.Lines.Any(l => l.Label == "Attaque"), Is.False, "le soigneur n'attaque pas");
            Assert.That(hs.Lines.Any(l => l.Label == "Critique (soins)"), Is.True);
        }

        [Test]
        public void Chaque_personnage_du_jeu_a_une_fiche_complete()
        {
            var content = Fixtures.FullContent();
            foreach (var c in content.Characters)
            {
                var sheet = CharacterDescriber.Sheet(c, c);
                Assert.That(sheet.Lines.Count, Is.GreaterThanOrEqualTo(6), c.Id);
                Assert.That(sheet.Lines.All(l => SkillSheet.Plain(l.Value).Length > 0), Is.True, c.Id);
            }
        }

        [TestCase(6.39, 1, "6,3")]
        [TestCase(6, 1, "6")]
        [TestCase(0.99, 1, "0,9")]
        public void Le_format_decimal_tronque(double value, int decimals, string expected) => Assert.That(Format.Decimal(value, decimals), Is.EqualTo(expected));
    }
}
