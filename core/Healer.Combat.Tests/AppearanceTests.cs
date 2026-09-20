using System.IO;
using System.Linq;
using Healer.Combat;
using Healer.Combat.Progress;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Apparence des héros selon l'équipement (D-061) : les données sont complètes et la résolution suit le niveau acheté.</summary>
    public class AppearanceTests
    {
        private static AppearanceCatalog Catalog() => AppearanceCatalog.FromJson(File.ReadAllText(Path.Combine(Fixtures.ContentDir, "appearance.json")));

        [Test]
        public void Les_paliers_commencent_a_zero_et_croissent()
        {
            var c = Catalog();
            Assert.That(c.Tiers[0].MinLevel, Is.EqualTo(0));
            for (int i = 1; i < c.Tiers.Count; i++) Assert.That(c.Tiers[i].MinLevel, Is.GreaterThan(c.Tiers[i - 1].MinLevel));
            Assert.That(c.Tiers.All(t => t.Name.Length > 0), Is.True);
        }

        [Test]
        public void Un_niveau_d_equipement_donne_le_bon_palier()
        {
            var c = Catalog();
            Assert.That(new[] { 0, 1, 2, 3, 4, 5 }.Select(c.TierOf).ToArray(), Is.EqualTo(new[] { 0, 0, 1, 1, 2, 2 }));
            Assert.That(c.TierName(0), Is.EqualTo("Ordinaire"));
            Assert.That(c.TierName(5), Is.EqualTo("Légendaire"));
        }

        [Test]
        public void Chaque_personnage_du_jeu_a_une_version_par_palier_et_par_emplacement()
        {
            var content = Fixtures.FullContent();
            var c = Catalog();
            foreach (var ch in content.Characters)
                foreach (var slot in AppearanceCatalog.Slots)
                {
                    Assert.That(c.Characters.ContainsKey(ch.Id), Is.True, ch.Id);
                    var versions = c.Characters[ch.Id][slot];
                    Assert.That(versions.Count, Is.EqualTo(c.Tiers.Count), $"{ch.Id}/{slot} : une version par palier");
                }
        }

        [Test]
        public void Chaque_emplacement_correspond_a_une_piste_de_l_atelier()
        {
            var content = Fixtures.FullContent();
            var tracks = content.Upgrades.Equipment.Select(t => t.Id).ToHashSet();
            foreach (var ch in content.Characters)
                foreach (var slot in AppearanceCatalog.Slots)
                    Assert.That(tracks, Does.Contain(ch.Id + "_" + slot), $"aucune piste d'équipement pour {ch.Id}/{slot} : l'apparence ne pourrait jamais changer");
        }

        [Test]
        public void Les_palettes_sont_completes_et_en_hexadecimal()
        {
            string[] roles = { "primary", "secondary", "accent", "glow", "trim" };
            var hex = new System.Text.RegularExpressions.Regex("^[0-9A-Fa-f]{6}$");
            foreach (var ch in Catalog().Characters)
                foreach (var slot in ch.Value)
                    foreach (var entry in slot.Value)
                    {
                        Assert.That(entry.Part, Is.Not.Empty);
                        foreach (var role in roles)
                        {
                            Assert.That(entry.Palette.ContainsKey(role), Is.True, $"{ch.Key}/{slot.Key} : rôle {role}");
                            Assert.That(hex.IsMatch(entry.Palette[role]), Is.True, $"{ch.Key}/{slot.Key}/{role} : « {entry.Palette[role]} »");
                        }
                    }
        }

        [Test]
        public void L_equipement_achete_change_l_apparence_emplacement_par_emplacement()
        {
            var c = Catalog();
            var loadout = new Loadout();
            var base0 = c.Resolve("tank", loadout);
            Assert.That(base0.TierOf("weapon"), Is.EqualTo(0));
            loadout.Equipment["tank_weapon"] = 3;
            var after = c.Resolve("tank", loadout);
            Assert.That(after.TierOf("weapon"), Is.EqualTo(1), "l'arme passe au palier raffiné");
            Assert.That(after.TierOf("armor"), Is.EqualTo(0), "l'armure n'a pas bougé");
            Assert.That(after.Signature(), Is.Not.EqualTo(base0.Signature()));
            loadout.Equipment["tank_armor"] = 5;
            Assert.That(c.Resolve("tank", loadout).TierOf("armor"), Is.EqualTo(2));
        }

        [Test]
        public void L_equipement_d_un_heros_ne_change_pas_l_apparence_des_autres()
        {
            var c = Catalog();
            var loadout = new Loadout();
            loadout.Equipment["tank_weapon"] = 5;
            foreach (var id in new[] { "dps1", "dps2", "healer" })
                Assert.That(c.Resolve(id, loadout).Signature(), Is.EqualTo(c.Resolve(id, new Loadout()).Signature()), id);
        }

        [Test]
        public void Sans_equipement_ou_avec_un_catalogue_vide_rien_ne_plante()
        {
            Assert.That(Catalog().Resolve("tank", null).Parts.Count, Is.EqualTo(2));
            var empty = new AppearanceCatalog();
            Assert.That(empty.Resolve("tank", new Loadout()).Parts, Is.Empty);
            Assert.That(empty.Resolve("inconnu", null).Parts, Is.Empty);
        }

        [Test]
        public void Chaque_palier_change_au_moins_une_couleur_pour_que_la_progression_se_voie()
        {
            foreach (var ch in Catalog().Characters)
                foreach (var slot in ch.Value)
                    for (int i = 1; i < slot.Value.Count; i++)
                        Assert.That(slot.Value[i].Palette.Values.SequenceEqual(slot.Value[i - 1].Palette.Values), Is.False, $"{ch.Key}/{slot.Key} : palier {i} identique au précédent");
        }

        [Test]
        public void Un_modele_importe_traverse_le_catalogue_jusqu_a_la_piece_et_change_la_signature()
        {
            var json = "{\"tiers\":[{\"minLevel\":0,\"name\":\"A\"}],\"characters\":{\"tank\":{\"weapon\":[{\"part\":\"tank.weapon\",\"palette\":{},\"model\":{\"path\":\"Imported/KayKit/Sword\",\"size\":50,\"offset\":[0,10,0],\"euler\":[90,0,0]}}]}}}";
            var set = AppearanceCatalog.FromJson(json).Resolve("tank", null);
            var model = set.Parts["weapon"].Model!;
            Assert.That(model.Path, Is.EqualTo("Imported/KayKit/Sword"));
            Assert.That(model.Size, Is.EqualTo(50));
            Assert.That(model.Offset[1], Is.EqualTo(10));
            Assert.That(model.Euler[0], Is.EqualTo(90));
            Assert.That(model.ImportedFolder, Is.EqualTo("KayKit"));
            Assert.That(set.Signature(), Does.Contain("#Imported/KayKit/Sword"));
        }

        [TestCase("Imported/KayKit/Sword", "KayKit")]
        [TestCase("Imported/Quaternius/Monsters/Golem", "Quaternius")]
        [TestCase("Parts/Blade", null)]
        [TestCase("Blade", null)]
        public void Le_dossier_importe_se_deduit_du_chemin_et_une_creation_de_l_equipe_n_en_a_pas(string path, string? folder) =>
            Assert.That(new AppearanceModel { Path = path }.ImportedFolder, Is.EqualTo(folder));

        [Test]
        public void Chaque_modele_importe_du_catalogue_a_son_entree_dans_le_generique()
        {
            // Garde-fou : un modèle externe désigné par les données doit être crédité, même si les fichiers ne sont pas encore là.
            var credited = CreditsData.FromJson(File.ReadAllText(Path.Combine(Fixtures.ContentDir, "credits.json"))).Assets.Select(a => a.Folder).ToHashSet();
            foreach (var ch in Catalog().Characters)
                foreach (var slot in ch.Value)
                    foreach (var entry in slot.Value)
                        if (entry.Model?.ImportedFolder is string folder)
                            Assert.That(credited, Does.Contain(folder), $"{ch.Key}/{slot.Key} utilise « {entry.Model.Path} » sans entrée « {folder} » dans credits.json");
        }
    }
}
