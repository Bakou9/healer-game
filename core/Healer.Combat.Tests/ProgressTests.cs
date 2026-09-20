using System;
using System.Collections.Generic;
using System.Linq;
using Healer.Combat.Progress;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Portefeuille : point d'entrée unique des gains et dépenses (D-047).</summary>
    public class WalletTests
    {
        [Test]
        public void Un_gain_augmente_le_solde_et_laisse_une_trace_avec_sa_raison()
        {
            var w = new Wallet();
            w.Grant(Wallet.Gold, 100, "test");
            Assert.That(w.Balance(Wallet.Gold), Is.EqualTo(100));
            Assert.That(w.Ledger, Has.Count.EqualTo(1));
            Assert.That(w.Ledger[0].Reason, Is.EqualTo("test"));
            Assert.That(w.Ledger[0].Amount, Is.EqualTo(100));
            Assert.That(w.Ledger[0].BalanceAfter, Is.EqualTo(100));
        }

        [Test]
        public void Un_solde_inconnu_vaut_zero() => Assert.That(new Wallet().Balance("rubis"), Is.EqualTo(0));

        [TestCase(0)]
        [TestCase(-5)]
        public void Un_gain_nul_ou_negatif_est_refuse(int amount) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new Wallet().Grant(Wallet.Gold, amount, "x"));

        [TestCase("")]
        [TestCase("   ")]
        public void Un_gain_sans_raison_est_refuse(string reason) =>
            Assert.Throws<ArgumentException>(() => new Wallet().Grant(Wallet.Gold, 10, reason));

        [Test]
        public void Une_depense_reduit_le_solde_et_est_tracee_en_negatif()
        {
            var w = new Wallet();
            w.Grant(Wallet.Gold, 100, "gain");
            Assert.That(w.TrySpend(Wallet.Gold, 40, "achat"), Is.True);
            Assert.That(w.Balance(Wallet.Gold), Is.EqualTo(60));
            Assert.That(w.Ledger.Last().Amount, Is.EqualTo(-40));
        }

        [Test]
        public void Une_depense_trop_chere_ne_change_rien()
        {
            var w = new Wallet();
            w.Grant(Wallet.Gold, 30, "gain");
            Assert.That(w.TrySpend(Wallet.Gold, 31, "achat"), Is.False);
            Assert.That(w.Balance(Wallet.Gold), Is.EqualTo(30));
            Assert.That(w.Ledger, Has.Count.EqualTo(1));
        }

        [Test]
        public void On_peut_depenser_exactement_tout_son_solde_jamais_plus()
        {
            var w = new Wallet();
            w.Grant(Wallet.Gold, 50, "gain");
            Assert.That(w.TrySpend(Wallet.Gold, 50, "tout"), Is.True);
            Assert.That(w.Balance(Wallet.Gold), Is.EqualTo(0));
            Assert.That(w.TrySpend(Wallet.Gold, 1, "encore"), Is.False);
        }

        [Test]
        public void Le_solde_est_plafonne_sans_debordement()
        {
            var w = new Wallet();
            w.Grant(Wallet.Gold, Wallet.MaxBalance, "énorme");
            w.Grant(Wallet.Gold, int.MaxValue, "encore plus");
            Assert.That(w.Balance(Wallet.Gold), Is.EqualTo(Wallet.MaxBalance));
        }

        [Test]
        public void Le_registre_garde_les_dernieres_ecritures_seulement()
        {
            var w = new Wallet();
            for (int i = 1; i <= Wallet.MaxLedgerEntries + 30; i++) w.Grant(Wallet.Gold, 1, "g" + i);
            Assert.That(w.Ledger, Has.Count.EqualTo(Wallet.MaxLedgerEntries));
            Assert.That(w.Ledger.Last().Reason, Is.EqualTo("g" + (Wallet.MaxLedgerEntries + 30)));
            Assert.That(w.Balance(Wallet.Gold), Is.EqualTo(Wallet.MaxLedgerEntries + 30), "le solde compte tout, pas seulement le registre");
        }

        [Test]
        public void Restaurer_un_solde_negatif_donne_zero()
        {
            var w = new Wallet();
            w.Restore(Wallet.Gold, -50, null);
            Assert.That(w.Balance(Wallet.Gold), Is.EqualTo(0));
        }

        [Test]
        public void Plusieurs_monnaies_sont_independantes()
        {
            var w = new Wallet();
            w.Grant(Wallet.Gold, 10, "a");
            w.Grant("gemmes", 5, "b");
            Assert.That(w.Balance(Wallet.Gold), Is.EqualTo(10));
            Assert.That(w.Balance("gemmes"), Is.EqualTo(5));
            Assert.That(w.TrySpend(Wallet.Gold, 6, "c"), Is.True);
            Assert.That(w.Balance("gemmes"), Is.EqualTo(5));
        }
    }

    internal static class ProgressFixtures
    {
        public static GameContent Content => Fixtures.FullContent();

        /// <summary>Statistiques construites à la main à partir d'événements (aucun combat à jouer).</summary>
        public static CombatStats Stats(string result, int deaths = 0, double damageTaken = 0, double durationMs = 90000)
        {
            var s = new CombatStats();
            for (int i = 0; i < deaths; i++) s.Apply(new BattleEvent { Type = "unitDied", UnitId = "dps" + i });
            if (damageTaken > 0) s.Apply(new BattleEvent { Type = "unitDamaged", UnitId = "tank", Amount = damageTaken });
            s.Apply(new BattleEvent { Type = "battleEnded", TimeMs = durationMs, Result = result });
            return s;
        }

        public static CombatStats Win(int deaths = 0, double damage = 0, double durationMs = 90000) => Stats(BattleResults.Victory, deaths, damage, durationMs);
        public static CombatStats Loss() => Stats(BattleResults.Defeat, 4, 5000, 40000);
    }

    public class PlayerProfileTests
    {
        private static readonly GameContent C = ProgressFixtures.Content;

        [Test]
        public void Une_nouvelle_partie_possede_tous_les_personnages_de_base_et_aucun_niveau_termine()
        {
            var p = PlayerProfile.NewGame(C);
            Assert.That(p.OwnedCharacters, Is.EquivalentTo(C.Characters.Select(c => c.Id)));
            Assert.That(p.Levels, Is.Empty);
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(0));
            Assert.That(p.TotalStars, Is.EqualTo(0));
            Assert.That(p.Settings.Muted, Is.False);
        }

        [Test]
        public void Seul_le_premier_niveau_est_debloque_au_depart()
        {
            var p = PlayerProfile.NewGame(C);
            Assert.That(C.Levels.Select(p.IsUnlocked), Is.EqualTo(new[] { true, false, false }));
        }

        [Test]
        public void Terminer_un_niveau_debloque_le_suivant_et_pas_celui_d_apres()
        {
            var p = PlayerProfile.NewGame(C);
            Progression.Complete(p, C, C.LevelById("l1"), ProgressFixtures.Win());
            Assert.That(C.Levels.Select(p.IsUnlocked), Is.EqualTo(new[] { true, true, false }));
        }

        [Test]
        public void Un_niveau_inconnu_n_est_pas_termine() => Assert.That(PlayerProfile.NewGame(C).RecordOf("zzz").Completed, Is.False);

        [Test]
        public void Reparer_retire_les_niveaux_et_personnages_inconnus()
        {
            var p = PlayerProfile.NewGame(C);
            p.Levels["fantome"] = new LevelRecord { Completed = true, BestStars = 3 };
            p.OwnedCharacters.Add("dragon");
            p.Repair(C);
            Assert.That(p.Levels.ContainsKey("fantome"), Is.False);
            Assert.That(p.OwnedCharacters, Does.Not.Contain("dragon"));
        }

        [Test]
        public void Reparer_garantit_la_presence_du_soigneur()
        {
            var p = PlayerProfile.NewGame(C);
            p.OwnedCharacters.Remove("healer");
            p.Repair(C);
            Assert.That(p.OwnedCharacters, Does.Contain("healer"));
        }

        [Test]
        public void Reparer_retire_les_doublons()
        {
            var p = PlayerProfile.NewGame(C);
            p.OwnedCharacters.Add("tank");
            p.OwnedCharacters.Add("tank");
            p.Repair(C);
            Assert.That(p.OwnedCharacters.Count(id => id == "tank"), Is.EqualTo(1));
        }

        [Test]
        public void Reparer_borne_les_etoiles_et_les_compteurs()
        {
            var p = PlayerProfile.NewGame(C);
            p.Levels["l1"] = new LevelRecord { Completed = true, BestStars = 9, Clears = -3, BestTimeMs = -1 };
            p.Repair(C);
            Assert.That(p.Levels["l1"].BestStars, Is.EqualTo(3));
            Assert.That(p.Levels["l1"].Clears, Is.EqualTo(0));
            Assert.That(p.Levels["l1"].BestTimeMs, Is.EqualTo(0));
        }

        [Test]
        public void Reparer_considere_termine_un_niveau_qui_a_des_etoiles()
        {
            var p = PlayerProfile.NewGame(C);
            p.Levels["l1"] = new LevelRecord { Completed = false, BestStars = 2 };
            p.Repair(C);
            Assert.That(p.Levels["l1"].Completed, Is.True);
        }

        [Test]
        public void Reparer_refuse_un_niveau_termine_dont_le_prerequis_ne_l_est_pas()
        {
            var p = PlayerProfile.NewGame(C);
            p.Levels["l3"] = new LevelRecord { Completed = true, BestStars = 3 }; // l2 jamais terminé
            p.Repair(C);
            Assert.That(p.RecordOf("l3").Completed, Is.False, "on ne saute pas un niveau par une sauvegarde bricolée");
        }

        [Test]
        public void Les_etoiles_totales_sont_la_somme_des_meilleures()
        {
            var p = PlayerProfile.NewGame(C);
            p.Levels["l1"] = new LevelRecord { Completed = true, BestStars = 3 };
            p.Levels["l2"] = new LevelRecord { Completed = true, BestStars = 2 };
            Assert.That(p.TotalStars, Is.EqualTo(5));
        }
    }

    public class ProgressionTests
    {
        private static readonly GameContent C = ProgressFixtures.Content;
        private static LevelDef L1 => C.LevelById("l1");
        private static LevelDef L2 => C.LevelById("l2");

        [Test]
        public void Une_defaite_donne_zero_etoile() => Assert.That(Progression.Stars(L1, ProgressFixtures.Loss()), Is.EqualTo(0));

        [Test]
        public void Une_victoire_avec_un_allie_K_O_donne_une_etoile() =>
            Assert.That(Progression.Stars(L1, ProgressFixtures.Win(deaths: 1, damage: 100)), Is.EqualTo(1));

        [Test]
        public void Une_victoire_sans_K_O_mais_beaucoup_de_degats_donne_deux_etoiles() =>
            Assert.That(Progression.Stars(L1, ProgressFixtures.Win(damage: L1.ThreeStarMaxDamageTaken + 1)), Is.EqualTo(2));

        [Test]
        public void Une_victoire_propre_sous_le_seuil_de_degats_donne_trois_etoiles() =>
            Assert.That(Progression.Stars(L1, ProgressFixtures.Win(damage: L1.ThreeStarMaxDamageTaken - 1)), Is.EqualTo(3));

        [Test]
        public void Le_seuil_exact_de_degats_donne_encore_trois_etoiles() =>
            Assert.That(Progression.Stars(L1, ProgressFixtures.Win(damage: L1.ThreeStarMaxDamageTaken)), Is.EqualTo(3));

        [Test]
        public void Un_K_O_empeche_les_trois_etoiles_meme_avec_peu_de_degats() =>
            Assert.That(Progression.Stars(L1, ProgressFixtures.Win(deaths: 1, damage: 1)), Is.EqualTo(1));

        [Test]
        public void Les_etoiles_ne_depassent_jamais_le_maximum()
        {
            for (int d = 0; d < 3; d++)
                Assert.That(Progression.Stars(L1, ProgressFixtures.Win(deaths: d, damage: 0)), Is.LessThanOrEqualTo(Progression.MaxStars));
        }

        [Test]
        public void La_premiere_victoire_donne_la_recompense_et_le_bonus_de_chaque_etoile()
        {
            var p = PlayerProfile.NewGame(C);
            var r = Progression.Complete(p, C, L1, ProgressFixtures.Win(damage: 100));
            Assert.That(r.Victory && r.FirstClear, Is.True);
            Assert.That(r.Stars, Is.EqualTo(3));
            Assert.That(r.NewStars, Is.EqualTo(3));
            Assert.That(r.GoldGained, Is.EqualTo(L1.RewardGold + 3 * L1.StarBonusGold));
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(r.GoldGained));
        }

        [Test]
        public void Rejouer_le_meme_niveau_avec_les_memes_etoiles_ne_donne_que_la_recompense_de_repetition()
        {
            var p = PlayerProfile.NewGame(C);
            Progression.Complete(p, C, L1, ProgressFixtures.Win(damage: 100));
            int before = p.Wallet.Balance(Wallet.Gold);
            var r = Progression.Complete(p, C, L1, ProgressFixtures.Win(damage: 100));
            Assert.That(r.FirstClear, Is.False);
            Assert.That(r.NewStars, Is.EqualTo(0));
            Assert.That(r.GoldGained, Is.EqualTo(L1.RepeatGold));
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(before + L1.RepeatGold));
        }

        [Test]
        public void S_ameliorer_ne_paie_que_les_etoiles_supplementaires()
        {
            var p = PlayerProfile.NewGame(C);
            Progression.Complete(p, C, L1, ProgressFixtures.Win(deaths: 1, damage: 100)); // 1 étoile
            var r = Progression.Complete(p, C, L1, ProgressFixtures.Win(damage: 100));    // 3 étoiles
            Assert.That(r.NewStars, Is.EqualTo(2));
            Assert.That(r.GoldGained, Is.EqualTo(L1.RepeatGold + 2 * L1.StarBonusGold));
            Assert.That(p.RecordOf("l1").BestStars, Is.EqualTo(3));
        }

        [Test]
        public void Un_moins_bon_resultat_ne_fait_jamais_baisser_le_meilleur()
        {
            var p = PlayerProfile.NewGame(C);
            Progression.Complete(p, C, L1, ProgressFixtures.Win(damage: 100));
            Progression.Complete(p, C, L1, ProgressFixtures.Win(deaths: 2, damage: 9999));
            Assert.That(p.RecordOf("l1").BestStars, Is.EqualTo(3));
        }

        [Test]
        public void Une_defaite_ne_change_rien_et_ne_coute_rien()
        {
            var p = PlayerProfile.NewGame(C);
            var r = Progression.Complete(p, C, L1, ProgressFixtures.Loss());
            Assert.That(r.Victory, Is.False);
            Assert.That(r.GoldGained, Is.EqualTo(0));
            Assert.That(p.Levels, Is.Empty);
            Assert.That(p.Wallet.Ledger, Is.Empty);
        }

        [Test]
        public void Le_meilleur_temps_est_le_plus_court_des_victoires()
        {
            var p = PlayerProfile.NewGame(C);
            Progression.Complete(p, C, L1, ProgressFixtures.Win(durationMs: 90000));
            var r = Progression.Complete(p, C, L1, ProgressFixtures.Win(durationMs: 80000));
            Assert.That(r.NewBestTime, Is.True);
            Assert.That(p.RecordOf("l1").BestTimeMs, Is.EqualTo(80000));
            var r2 = Progression.Complete(p, C, L1, ProgressFixtures.Win(durationMs: 95000));
            Assert.That(r2.NewBestTime, Is.False);
            Assert.That(p.RecordOf("l1").BestTimeMs, Is.EqualTo(80000));
        }

        [Test]
        public void La_premiere_victoire_n_annonce_pas_de_record_de_temps()
        {
            var p = PlayerProfile.NewGame(C);
            Assert.That(Progression.Complete(p, C, L1, ProgressFixtures.Win()).NewBestTime, Is.False);
        }

        [Test]
        public void Le_nombre_de_victoires_est_compte()
        {
            var p = PlayerProfile.NewGame(C);
            for (int i = 0; i < 3; i++) Progression.Complete(p, C, L1, ProgressFixtures.Win());
            Assert.That(p.RecordOf("l1").Clears, Is.EqualTo(3));
        }

        [Test]
        public void La_victoire_annonce_les_niveaux_qu_elle_debloque()
        {
            var p = PlayerProfile.NewGame(C);
            var r = Progression.Complete(p, C, L1, ProgressFixtures.Win());
            Assert.That(r.UnlockedLevelIds, Is.EqualTo(new[] { "l2" }));
            Assert.That(Progression.Complete(p, C, L1, ProgressFixtures.Win()).UnlockedLevelIds, Is.Empty, "déjà débloqué : rien de nouveau");
        }

        [Test]
        public void Toute_recompense_passe_par_le_portefeuille_avec_une_raison()
        {
            var p = PlayerProfile.NewGame(C);
            Progression.Complete(p, C, L1, ProgressFixtures.Win());
            Assert.That(p.Wallet.Ledger, Has.Count.EqualTo(1));
            Assert.That(p.Wallet.Ledger[0].Reason, Does.Contain("l1"));
        }

        [Test]
        public void Le_niveau_suivant_n_est_propose_que_s_il_est_debloque()
        {
            var p = PlayerProfile.NewGame(C);
            Assert.That(Progression.NextLevel(p, C, "l1"), Is.Null, "l1 pas encore terminé");
            Progression.Complete(p, C, L1, ProgressFixtures.Win());
            Assert.That(Progression.NextLevel(p, C, "l1")?.Id, Is.EqualTo("l2"));
        }

        [Test]
        public void Il_n_y_a_pas_de_niveau_apres_le_dernier() =>
            Assert.That(Progression.NextLevel(PlayerProfile.NewGame(C), C, C.Levels.Last().Id), Is.Null);

        [Test]
        public void Un_niveau_inconnu_n_a_pas_de_suivant() =>
            Assert.That(Progression.NextLevel(PlayerProfile.NewGame(C), C, "zzz"), Is.Null);

        [Test]
        public void La_progression_est_deterministe()
        {
            var a = PlayerProfile.NewGame(C);
            var b = PlayerProfile.NewGame(C);
            foreach (var p in new[] { a, b })
            {
                Progression.Complete(p, C, L1, ProgressFixtures.Win(deaths: 1, damage: 500, durationMs: 88000));
                Progression.Complete(p, C, L2, ProgressFixtures.Win(damage: 100, durationMs: 76000));
            }
            Assert.That(ProfileStore.ToJson(a), Is.EqualTo(ProfileStore.ToJson(b)));
        }

        [Test]
        public void Terminer_toute_la_campagne_a_trois_etoiles_donne_un_total_connu()
        {
            var p = PlayerProfile.NewGame(C);
            int expected = 0;
            foreach (var l in C.Levels)
            {
                Progression.Complete(p, C, l, ProgressFixtures.Win(damage: 1));
                expected += l.RewardGold + 3 * l.StarBonusGold;
            }
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(expected));
            Assert.That(p.TotalStars, Is.EqualTo(3 * C.Levels.Count));
        }
    }

    public class ProfileStoreTests
    {
        private static readonly GameContent C = ProgressFixtures.Content;

        private static PlayerProfile Played()
        {
            var p = PlayerProfile.NewGame(C);
            Progression.Complete(p, C, C.LevelById("l1"), ProgressFixtures.Win(damage: 100, durationMs: 85000));
            Progression.Complete(p, C, C.LevelById("l2"), ProgressFixtures.Win(deaths: 1, damage: 900, durationMs: 77000));
            p.Settings.Muted = true;
            return p;
        }

        [Test]
        public void Sauvegarder_puis_recharger_redonne_le_meme_profil()
        {
            var p = Played();
            Assert.That(ProfileStore.TryLoad(ProfileStore.ToJson(p), C, out var loaded, out var error), Is.True, error);
            Assert.That(loaded.Levels.Keys, Is.EquivalentTo(p.Levels.Keys));
            foreach (var id in p.Levels.Keys)
            {
                Assert.That(loaded.Levels[id].BestStars, Is.EqualTo(p.Levels[id].BestStars), id);
                Assert.That(loaded.Levels[id].BestTimeMs, Is.EqualTo(p.Levels[id].BestTimeMs), id);
                Assert.That(loaded.Levels[id].Clears, Is.EqualTo(p.Levels[id].Clears), id);
                Assert.That(loaded.Levels[id].Completed, Is.EqualTo(p.Levels[id].Completed), id);
            }
            Assert.That(loaded.Wallet.Balance(Wallet.Gold), Is.EqualTo(p.Wallet.Balance(Wallet.Gold)));
            Assert.That(loaded.Wallet.Ledger.Select(e => e.Reason), Is.EqualTo(p.Wallet.Ledger.Select(e => e.Reason)));
            Assert.That(loaded.OwnedCharacters, Is.EquivalentTo(p.OwnedCharacters));
            Assert.That(loaded.Settings.Muted, Is.True);
        }

        [Test]
        public void Le_texte_sauvegarde_est_stable_recharger_puis_resauvegarder_ne_change_rien()
        {
            var json = ProfileStore.ToJson(Played());
            ProfileStore.TryLoad(json, C, out var loaded, out _);
            Assert.That(ProfileStore.ToJson(loaded), Is.EqualTo(json));
        }

        [Test]
        public void Les_niveaux_sont_ecrits_dans_un_ordre_stable()
        {
            var a = PlayerProfile.NewGame(C);
            var b = PlayerProfile.NewGame(C);
            a.Levels["l1"] = new LevelRecord { Completed = true, BestStars = 1 };
            a.Levels["l2"] = new LevelRecord { Completed = true, BestStars = 1 };
            b.Levels["l2"] = new LevelRecord { Completed = true, BestStars = 1 };
            b.Levels["l1"] = new LevelRecord { Completed = true, BestStars = 1 };
            Assert.That(ProfileStore.ToJson(a), Is.EqualTo(ProfileStore.ToJson(b)));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Une_sauvegarde_absente_donne_une_partie_neuve(string? json)
        {
            Assert.That(ProfileStore.TryLoad(json, C, out var p, out var error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(p.Levels, Is.Empty);
            Assert.That(p.OwnedCharacters, Is.Not.Empty);
        }

        [TestCase("pas du json")]
        [TestCase("[1,2,3]")]
        [TestCase("{\"version\": \"abc\"}")]
        [TestCase("{\"version\": 1, \"levels\": {\"l1\": {\"bestStars\": \"beaucoup\"}}}")]
        [TestCase("{")]
        public void Une_sauvegarde_illisible_ne_plante_pas_et_donne_une_partie_neuve(string json)
        {
            Assert.That(ProfileStore.TryLoad(json, C, out var p, out var error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(p.Levels, Is.Empty);
        }

        [Test]
        public void Une_sauvegarde_d_une_version_future_est_refusee_sans_etre_ecrasee_par_erreur()
        {
            Assert.That(ProfileStore.TryLoad("{\"version\": 99}", C, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("99"));
        }

        [Test]
        public void Une_sauvegarde_sans_version_est_refusee() =>
            Assert.That(ProfileStore.TryLoad("{\"levels\": {}}", C, out _, out _), Is.False);

        [Test]
        public void Les_champs_manquants_prennent_des_valeurs_sures()
        {
            Assert.That(ProfileStore.TryLoad("{\"version\": 1}", C, out var p, out var error), Is.True, error);
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(0));
            Assert.That(p.Settings.Muted, Is.False);
            Assert.That(p.OwnedCharacters, Does.Contain("healer"), "le soigneur est toujours garanti");
        }

        [Test]
        public void Un_solde_negatif_dans_le_fichier_devient_zero()
        {
            ProfileStore.TryLoad("{\"version\": 1, \"wallet\": {\"gold\": -500}}", C, out var p, out _);
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(0));
        }

        [Test]
        public void Un_niveau_inconnu_dans_le_fichier_est_ignore()
        {
            ProfileStore.TryLoad("{\"version\": 1, \"levels\": {\"l99\": {\"completed\": true, \"bestStars\": 3}}}", C, out var p, out _);
            Assert.That(p.Levels.ContainsKey("l99"), Is.False);
        }

        [Test]
        public void Un_fichier_bricole_ne_permet_pas_de_sauter_un_niveau()
        {
            ProfileStore.TryLoad("{\"version\": 1, \"levels\": {\"l3\": {\"completed\": true, \"bestStars\": 3}}}", C, out var p, out _);
            Assert.That(p.IsUnlocked(C.LevelById("l3")), Is.False);
        }

        [Test]
        public void La_sauvegarde_ne_contient_que_du_texte_json_valide()
        {
            var json = ProfileStore.ToJson(Played());
            Assert.DoesNotThrow(() => Newtonsoft.Json.Linq.JObject.Parse(json));
            Assert.That(json, Does.Contain("\"version\": 1"));
        }
    }

    /// <summary>Inventaire de personnages : la rencontre tient compte de ce que le joueur possède (D-047).</summary>
    public class RosterTests
    {
        private static readonly GameContent C = ProgressFixtures.Content;

        private static List<string> Trace(EncounterDef enc)
        {
            var b = new Battle(enc);
            var lines = new List<string>();
            b.Subscribe(e => lines.Add(e.Format()));
            ReferenceHealerBot.Run(b, 120000);
            return lines;
        }

        [Test]
        public void Posseder_tout_le_monde_ne_change_pas_un_seul_evenement_du_combat()
        {
            var all = C.Characters.Select(c => c.Id).ToList();
            Assert.That(Trace(C.CreateEncounter("boss1", 7, all)), Is.EqualTo(Trace(C.CreateEncounter("boss1", 7))));
        }

        [Test]
        public void Un_personnage_non_possede_ne_combat_pas()
        {
            var enc = C.CreateEncounter("boss1", 7, new[] { "tank", "healer" });
            Assert.That(enc.Allies.Select(a => a.Id), Is.EqualTo(new[] { "tank", "healer" }));
        }

        [Test]
        public void Le_soigneur_combat_toujours_meme_s_il_manque_de_l_inventaire()
        {
            var enc = C.CreateEncounter("boss1", 7, new[] { "tank" });
            Assert.That(enc.Allies.Select(a => a.Id), Does.Contain("healer"));
        }

        [Test]
        public void L_ordre_de_l_equipe_est_celui_du_contenu_pas_celui_de_l_inventaire()
        {
            var enc = C.CreateEncounter("boss1", 7, new[] { "healer", "dps2", "tank" });
            Assert.That(enc.Allies.Select(a => a.Id), Is.EqualTo(new[] { "tank", "dps2", "healer" }));
        }

        [Test]
        public void Une_equipe_reduite_reste_jouable_sans_erreur()
        {
            var b = new Battle(C.CreateEncounter("boss2", 3, new[] { "tank", "healer" }));
            Assert.DoesNotThrow(() => ReferenceHealerBot.Run(b, 150000));
            // Deux personnages n'ont pas assez de dégâts pour finir le boss : le combat court simplement (pas de limite de temps, voir QUESTIONS_EN_ATTENTE).
            Assert.That(b.GetClock(), Is.GreaterThanOrEqualTo(100000));
        }

        [Test]
        public void Un_boss_inconnu_est_une_erreur_claire() =>
            Assert.That(() => C.CreateEncounter("boss99", 1), Throws.InvalidOperationException.With.Message.Contains("boss99"));
    }
}
