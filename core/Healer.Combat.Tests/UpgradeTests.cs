using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Healer.Combat.Progress;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Intégrité du catalogue d'améliorations (core/content/upgrades.json).</summary>
    public class UpgradeCatalogTests
    {
        private static readonly string[] Stats = { "maxHp", "atk", "def", "maxMana", "manaRegen" };
        private static readonly string[] Fields = { "healAmount", "shieldAmount", "manaCost", "cooldownMs" };
        private static GameContent C => Fixtures.FullContent();
        private static UpgradeCatalog Cat => C.Upgrades;

        private static IEnumerable<UpgradeEffect> AllEffects(UpgradeCatalog c) =>
            c.Equipment.SelectMany(t => t.PerLevel).Concat(c.TalentTiers.SelectMany(t => t.Options).SelectMany(o => o.Effects));

        [Test]
        public void Le_catalogue_est_charge_avec_de_l_equipement_et_des_talents()
        {
            Assert.That(Cat.Equipment, Is.Not.Empty);
            Assert.That(Cat.TalentTiers, Is.Not.Empty);
        }

        [Test]
        public void Chaque_personnage_a_une_arme_et_une_armure()
        {
            foreach (var c in C.Characters)
            {
                Assert.That(Cat.Equipment.Count(t => t.CharacterId == c.Id), Is.EqualTo(2), c.Id);
            }
        }

        [Test]
        public void Les_ids_des_pistes_sont_uniques_et_visent_un_personnage_existant()
        {
            Assert.That(Cat.Equipment.Select(t => t.Id).Distinct().Count(), Is.EqualTo(Cat.Equipment.Count));
            foreach (var t in Cat.Equipment) Assert.That(C.Characters.Select(c => c.Id), Does.Contain(t.CharacterId), t.Id);
        }

        [Test]
        public void Chaque_piste_a_un_prix_par_niveau_strictement_croissant_et_entier_positif()
        {
            foreach (var t in Cat.Equipment)
            {
                Assert.That(t.MaxLevel, Is.GreaterThan(0), t.Id);
                Assert.That(t.Costs, Has.Count.EqualTo(t.MaxLevel), t.Id);
                Assert.That(t.Costs, Is.Ordered.Ascending.And.All.GreaterThan(0), t.Id);
                Assert.That(t.Costs.Distinct().Count(), Is.EqualTo(t.Costs.Count), t.Id + " : chaque niveau coûte plus cher");
            }
        }

        [Test]
        public void Chaque_piste_a_un_nom_et_au_moins_un_effet()
        {
            foreach (var t in Cat.Equipment)
            {
                Assert.That(t.Name, Is.Not.Empty, t.Id);
                Assert.That(t.PerLevel, Is.Not.Empty, t.Id);
            }
        }

        [Test]
        public void Chaque_effet_est_valide_une_statistique_ou_un_champ_de_sort_connu_et_un_pourcentage_raisonnable()
        {
            var skillIds = C.Skills.Select(s => s.Id).ToHashSet();
            foreach (var e in AllEffects(Cat))
            {
                bool isStat = e.Stat != null, isSkill = e.Skill != null;
                Assert.That(isStat ^ isSkill, Is.True, "un effet vise soit une statistique, soit un sort");
                if (isStat) Assert.That(Stats, Does.Contain(e.Stat));
                else
                {
                    Assert.That(Fields, Does.Contain(e.Field));
                    Assert.That(e.Skill == "*" || skillIds.Contains(e.Skill!), Is.True, "sort inconnu : " + e.Skill);
                }
                Assert.That(e.Pct, Is.Not.EqualTo(0));
                Assert.That(e.Pct, Is.InRange(-100, 200));
            }
        }

        [Test]
        public void Les_paliers_de_talent_se_suivent_de_un_a_n_avec_prix_et_etoiles_croissants()
        {
            var tiers = Cat.TalentTiers;
            Assert.That(tiers.Select(t => t.Tier), Is.EqualTo(Enumerable.Range(1, tiers.Count)));
            Assert.That(tiers.Select(t => t.Cost), Is.Ordered.Ascending.And.All.GreaterThan(0));
            Assert.That(tiers.Select(t => t.RequiresStars), Is.Ordered.Ascending);
        }

        [Test]
        public void Les_etoiles_requises_sont_atteignables_avec_la_campagne()
        {
            int max = C.Levels.Count * Progression.MaxStars;
            foreach (var t in Cat.TalentTiers) Assert.That(t.RequiresStars, Is.LessThanOrEqualTo(max), "palier " + t.Tier);
        }

        [Test]
        public void Chaque_palier_propose_exactement_deux_options_aux_ids_uniques_dans_tout_le_catalogue()
        {
            foreach (var t in Cat.TalentTiers) Assert.That(t.Options, Has.Count.EqualTo(2), "palier " + t.Tier);
            var ids = Cat.TalentTiers.SelectMany(t => t.Options).Select(o => o.Id).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
        }

        [Test]
        public void Les_descriptions_de_talents_citent_les_bons_pourcentages()
        {
            // La description est écrite à la main : ce test empêche qu'elle mente quand on change une valeur.
            foreach (var o in Cat.TalentTiers.SelectMany(t => t.Options))
            {
                Assert.That(o.Name, Is.Not.Empty, o.Id);
                var cited = Regex.Matches(o.Description, @"(\d+) %").Select(m => int.Parse(m.Groups[1].Value)).OrderBy(x => x).ToList();
                var actual = o.Effects.Select(e => Math.Abs(e.Pct)).OrderBy(x => x).ToList();
                Assert.That(cited, Is.EqualTo(actual), o.Id + " : « " + o.Description + " »");
            }
        }

        [Test]
        public void Les_talents_ne_touchent_que_le_soigneur_et_ses_sorts()
        {
            // Les statistiques de talent visent le soigneur (les sorts sont ceux du soigneur par construction).
            foreach (var o in Cat.TalentTiers.SelectMany(t => t.Options))
                foreach (var e in o.Effects.Where(e => e.Stat != null))
                    Assert.That(new[] { "manaRegen", "maxMana" }, Does.Contain(e.Stat), o.Id + " : un talent de statistique concerne le mana du soigneur");
        }

        [Test]
        public void Un_seul_boost_de_statistique_par_piste_et_par_niveau_reste_modeste()
        {
            foreach (var t in Cat.Equipment)
                foreach (var e in t.PerLevel) Assert.That(Math.Abs(e.Pct) * t.MaxLevel, Is.LessThanOrEqualTo(60), t.Id + " : un équipement au maximum ne dépasse pas +60 %");
        }
    }

    /// <summary>Application des effets aux personnages et aux sorts d'un combat.</summary>
    public class LoadoutApplierTests
    {
        private static GameContent C => Fixtures.FullContent();

        private static (List<CharacterDef> ch, List<SkillDef> sk) Apply(Loadout? l, GameContent? c = null)
        {
            c ??= C;
            return LoadoutApplier.Apply(c.Upgrades, l, c.Characters, c.Skills);
        }

        private static CharacterDef Ch(List<CharacterDef> list, string id) => list.Single(c => c.Id == id);
        private static SkillDef Sk(List<SkillDef> list, string id) => list.Single(s => s.Id == id);

        [Test]
        public void Sans_amelioration_le_resultat_est_identique_au_contenu_de_base()
        {
            var c = C;
            foreach (var l in new Loadout?[] { null, new Loadout() })
            {
                var (ch, sk) = Apply(l, c);
                for (int i = 0; i < ch.Count; i++)
                {
                    Assert.That(ch[i].MaxHp, Is.EqualTo(c.Characters[i].MaxHp));
                    Assert.That(ch[i].Atk, Is.EqualTo(c.Characters[i].Atk));
                    Assert.That(ch[i].Def, Is.EqualTo(c.Characters[i].Def));
                    Assert.That(ch[i].MaxMana, Is.EqualTo(c.Characters[i].MaxMana));
                    Assert.That(ch[i].ManaRegenPerSec, Is.EqualTo(c.Characters[i].ManaRegenPerSec));
                }
                for (int i = 0; i < sk.Count; i++)
                {
                    Assert.That(sk[i].HealAmount, Is.EqualTo(c.Skills[i].HealAmount));
                    Assert.That(sk[i].ShieldAmount, Is.EqualTo(c.Skills[i].ShieldAmount));
                    Assert.That(sk[i].ManaCost, Is.EqualTo(c.Skills[i].ManaCost));
                    Assert.That(sk[i].CooldownMs, Is.EqualTo(c.Skills[i].CooldownMs));
                }
            }
        }

        [Test]
        public void Un_combat_sans_amelioration_rejoue_exactement_les_memes_evenements_que_le_contenu_de_base()
        {
            var c = C;
            List<string> Trace(EncounterDef e) { var b = new Battle(e); var l = new List<string>(); b.Subscribe(x => l.Add(x.Format())); ReferenceHealerBot.Run(b, 120000); return l; }
            Assert.That(Trace(c.CreateEncounter("boss1", 7, null, new Loadout())), Is.EqualTo(Trace(c.CreateEncounter("boss1", 7))));
            Assert.That(Trace(c.CreateEncounter("boss2", 7, null, null)), Is.EqualTo(Trace(c.CreateEncounter("boss2", 7))));
        }

        [Test]
        public void Les_copies_sont_independantes_le_contenu_de_base_n_est_jamais_modifie()
        {
            var c = C;
            double hp = c.Characters.Single(x => x.Id == "tank").MaxHp;
            double heal = c.Skills.Single(s => s.Id == "heal_single").HealAmount!.Value;
            Apply(Loadout.Maxed(c.Upgrades), c);
            Assert.That(c.Characters.Single(x => x.Id == "tank").MaxHp, Is.EqualTo(hp));
            Assert.That(c.Skills.Single(s => s.Id == "heal_single").HealAmount, Is.EqualTo(heal));
        }

        [Test]
        public void Une_arme_niveau_un_augmente_l_attaque_du_pourcentage_prevu_arrondi_au_plus_proche()
        {
            var l = new Loadout(); l.Equipment["tank_weapon"] = 1;
            var (ch, _) = Apply(l);
            Assert.That(Ch(ch, "tank").Atk, Is.EqualTo(Math.Floor(34 * 1.04 + 0.5))); // 35,36 -> 35 (D-082 : base 34, était 35)
        }

        [Test]
        public void Les_niveaux_d_une_piste_s_additionnent_lineairement()
        {
            var l = new Loadout(); l.Equipment["tank_weapon"] = 5;
            var (ch, _) = Apply(l);
            Assert.That(Ch(ch, "tank").Atk, Is.EqualTo(Math.Floor(34 * 1.20 + 0.5))); // +20 % (D-082 : base 34, était 35)
        }

        [Test]
        public void Une_armure_augmente_les_PV_et_la_defense()
        {
            var l = new Loadout(); l.Equipment["tank_armor"] = 2;
            var (ch, _) = Apply(l);
            Assert.That(Ch(ch, "tank").MaxHp, Is.EqualTo(Math.Floor(900 * 1.08 + 0.5)));
            Assert.That(Ch(ch, "tank").Def, Is.EqualTo(Math.Floor(28 * 1.06 + 0.5)));
        }

        [Test]
        public void Un_equipement_n_agit_que_sur_son_personnage()
        {
            var l = new Loadout(); l.Equipment["tank_weapon"] = 5;
            var (ch, _) = Apply(l);
            Assert.That(Ch(ch, "dps1").Atk, Is.EqualTo(47)); // D-082 : base 47, était 70
            Assert.That(Ch(ch, "healer").Atk, Is.EqualTo(12));
        }

        [Test]
        public void Le_baton_du_soigneur_augmente_les_deux_soins_pas_le_bouclier()
        {
            var l = new Loadout(); l.Equipment["healer_weapon"] = 5;
            var (_, sk) = Apply(l);
            Assert.That(Sk(sk, "heal_single").HealAmount, Is.EqualTo(Math.Floor(170 * 1.20 + 0.5)));
            Assert.That(Sk(sk, "heal_aoe").HealAmount, Is.EqualTo(Math.Floor(140 * 1.20 + 0.5)));
            Assert.That(Sk(sk, "shield").ShieldAmount, Is.EqualTo(260));
        }

        [Test]
        public void La_robe_du_soigneur_augmente_les_PV_et_le_mana()
        {
            var l = new Loadout(); l.Equipment["healer_armor"] = 5;
            var (ch, _) = Apply(l);
            Assert.That(Ch(ch, "healer").MaxHp, Is.EqualTo(Math.Floor(480 * 1.20 + 0.5)));
            Assert.That(Ch(ch, "healer").MaxMana, Is.EqualTo(Math.Floor(100 * 1.20 + 0.5)));
        }

        [Test]
        public void Un_talent_de_soin_augmente_le_soin_et_baisse_son_cout()
        {
            var l = new Loadout(); l.Talents[1] = "quick_heal";
            var (_, sk) = Apply(l);
            Assert.That(Sk(sk, "heal_single").HealAmount, Is.EqualTo(Math.Floor(170 * 1.30 + 0.5)));
            Assert.That(Sk(sk, "heal_single").ManaCost, Is.EqualTo(Math.Floor(18 * 0.85 + 0.5)));
            Assert.That(Sk(sk, "heal_aoe").ManaCost, Is.EqualTo(60), "les autres sorts ne changent pas");
        }

        [Test]
        public void Un_talent_generique_touche_tous_les_sorts()
        {
            var l = new Loadout(); l.Talents[1] = "thrifty";
            var (_, sk) = Apply(l);
            Assert.That(Sk(sk, "heal_single").ManaCost, Is.EqualTo(Math.Floor(18 * 0.91 + 0.5)));
            Assert.That(Sk(sk, "heal_aoe").ManaCost, Is.EqualTo(Math.Floor(60 * 0.91 + 0.5)));
            Assert.That(Sk(sk, "shield").ManaCost, Is.EqualTo(Math.Floor(28 * 0.91 + 0.5)));
            Assert.That(Sk(sk, "purge").ManaCost, Is.EqualTo(Math.Floor(12 * 0.91 + 0.5)));
        }

        [Test]
        public void La_regeneration_de_mana_garde_ses_decimales()
        {
            var l = new Loadout(); l.Talents[3] = "mana_flow";
            var (ch, _) = Apply(l);
            Assert.That(Ch(ch, "healer").ManaRegenPerSec, Is.EqualTo(6 * 1.05).Within(1e-9));
        }

        [Test]
        public void La_recharge_d_un_sort_baisse_avec_un_talent()
        {
            var l = new Loadout(); l.Talents[2] = "swift_purge";
            var (_, sk) = Apply(l);
            Assert.That(Sk(sk, "purge").CooldownMs, Is.EqualTo(Math.Floor(5000 * 0.90 + 0.5)));
            Assert.That(Sk(sk, "purge").ManaCost, Is.EqualTo(Math.Floor(12 * 0.70 + 0.5)));
        }

        [Test]
        public void Les_pourcentages_de_plusieurs_sources_s_additionnent_au_lieu_de_se_multiplier()
        {
            var l = new Loadout(); l.Equipment["healer_weapon"] = 5; l.Talents[1] = "quick_heal";
            var (_, sk) = Apply(l);
            Assert.That(Sk(sk, "heal_single").HealAmount, Is.EqualTo(Math.Floor(170 * 1.50 + 0.5)), "+20 % +30 % = +50 %, pas 1,2 × 1,3");
        }

        [Test]
        public void Un_niveau_au_dela_du_maximum_compte_comme_le_maximum()
        {
            var a = new Loadout(); a.Equipment["tank_weapon"] = 5;
            var b = new Loadout(); b.Equipment["tank_weapon"] = 99;
            Assert.That(Ch(Apply(b).ch, "tank").Atk, Is.EqualTo(Ch(Apply(a).ch, "tank").Atk));
        }

        [Test]
        public void Une_piste_ou_un_talent_inconnu_est_ignore_sans_erreur()
        {
            var l = new Loadout(); l.Equipment["fantome"] = 3; l.Talents[9] = "rien"; l.Talents[1] = "inexistant";
            Assert.DoesNotThrow(() => Apply(l));
            Assert.That(Ch(Apply(l).ch, "tank").Atk, Is.EqualTo(34)); // D-082 : base 34, était 35
        }

        [Test]
        public void Le_cout_en_mana_ne_devient_jamais_negatif_ni_la_recharge_nulle()
        {
            var c = C;
            c.Upgrades.TalentTiers[0].Options[1].Effects[0].Pct = -100;
            c.Upgrades.TalentTiers[1].Options[1].Effects[0].Pct = -100;
            var l = new Loadout(); l.Talents[1] = "thrifty"; l.Talents[2] = "swift_purge";
            var (_, sk) = Apply(l, c);
            Assert.That(sk.All(s => s.ManaCost >= 0), Is.True);
            var baseSkills = c.Skills.ToDictionary(s => s.Id);
            Assert.That(sk.All(s => baseSkills[s.Id].CooldownMs == 0 ? s.CooldownMs == 0 : s.CooldownMs >= 1), Is.True);
        }

        [Test]
        public void Un_personnage_absent_de_l_equipe_ne_recoit_rien_et_ne_fait_pas_planter()
        {
            var c = C;
            var l = new Loadout(); l.Equipment["dps2_weapon"] = 5;
            var team = c.Characters.Where(x => x.Id != "dps2");
            var (ch, _) = LoadoutApplier.Apply(c.Upgrades, l, team, c.Skills);
            Assert.That(ch.Select(x => x.Id), Does.Not.Contain("dps2"));
        }

        [Test]
        public void L_amelioration_est_deterministe()
        {
            var l = Loadout.Maxed(C.Upgrades);
            var (a, sa) = Apply(l);
            var (b, sb) = Apply(l);
            Assert.That(a.Select(x => (x.Id, x.MaxHp, x.Atk, x.Def)), Is.EqualTo(b.Select(x => (x.Id, x.MaxHp, x.Atk, x.Def))));
            Assert.That(sa.Select(x => (x.Id, x.HealAmount, x.ManaCost)), Is.EqualTo(sb.Select(x => (x.Id, x.HealAmount, x.ManaCost))));
        }

        [Test]
        public void Les_ameliorations_ne_touchent_jamais_au_boss()
        {
            var c = C;
            var boss = c.CreateEncounter("boss2", 1, null, Loadout.Maxed(c.Upgrades)).Boss;
            Assert.That(boss.MaxHp, Is.EqualTo(c.BossById("boss2").MaxHp));
            Assert.That(boss.Atk, Is.EqualTo(c.BossById("boss2").Atk));
        }

        [Test]
        public void L_equipe_reduite_garde_ses_ameliorations_et_le_soigneur()
        {
            var c = C;
            var enc = c.CreateEncounter("boss1", 1, new[] { "tank" }, Loadout.Maxed(c.Upgrades));
            Assert.That(enc.Allies.Select(a => a.Id), Is.EqualTo(new[] { "tank", "healer" }));
            Assert.That(enc.Allies.Single(a => a.Id == "tank").MaxHp, Is.GreaterThan(900));
        }

        [Test]
        public void Tout_au_maximum_donne_des_valeurs_entieres_pour_les_quantites_de_jeu()
        {
            var (ch, sk) = Apply(Loadout.Maxed(C.Upgrades));
            foreach (var c in ch) { Assert.That(c.MaxHp, Is.EqualTo(Math.Floor(c.MaxHp))); Assert.That(c.Atk, Is.EqualTo(Math.Floor(c.Atk))); Assert.That(c.Def, Is.EqualTo(Math.Floor(c.Def))); }
            foreach (var s in sk) { Assert.That(s.ManaCost, Is.EqualTo(Math.Floor(s.ManaCost))); Assert.That(s.CooldownMs, Is.EqualTo(Math.Floor(s.CooldownMs))); }
        }
    }

    /// <summary>L'atelier : achats d'équipement et choix de talents.</summary>
    public class WorkshopTests
    {
        private static readonly GameContent C = Fixtures.FullContent();

        private static PlayerProfile Rich(int gold = 100000, int stars = 9)
        {
            var p = PlayerProfile.NewGame(C);
            p.Wallet.Grant(Wallet.Gold, gold, "test");
            for (int i = 0; i < C.Levels.Count && stars > 0; i++, stars -= 3)
                p.Levels[C.Levels[i].Id] = new LevelRecord { Completed = true, BestStars = Math.Min(3, stars) };
            return p;
        }

        [Test]
        public void Acheter_un_niveau_depense_le_prix_monte_le_niveau_et_laisse_une_trace()
        {
            var p = Rich(500);
            var track = C.Upgrades.Track("tank_weapon")!;
            Assert.That(Workshop.BuyEquipment(p, C, "tank_weapon"), Is.EqualTo(PurchaseResult.Ok));
            Assert.That(p.Loadout.LevelOf("tank_weapon"), Is.EqualTo(1));
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(500 - track.Costs[0]));
            Assert.That(p.Wallet.Ledger.Last().Reason, Does.Contain("tank_weapon"));
        }

        [Test]
        public void Le_prix_est_celui_du_niveau_suivant()
        {
            var p = Rich(10000);
            var track = C.Upgrades.Track("tank_weapon")!;
            for (int level = 0; level < track.MaxLevel; level++)
            {
                Assert.That(Workshop.NextCost(p, track), Is.EqualTo(track.Costs[level]));
                int before = p.Wallet.Balance(Wallet.Gold);
                Workshop.BuyEquipment(p, C, "tank_weapon");
                Assert.That(before - p.Wallet.Balance(Wallet.Gold), Is.EqualTo(track.Costs[level]));
            }
        }

        [Test]
        public void Au_niveau_maximum_on_ne_peut_plus_acheter_et_rien_n_est_depense()
        {
            var p = Rich(100000);
            var track = C.Upgrades.Track("tank_weapon")!;
            for (int i = 0; i < track.MaxLevel; i++) Workshop.BuyEquipment(p, C, "tank_weapon");
            int gold = p.Wallet.Balance(Wallet.Gold);
            Assert.That(Workshop.BuyEquipment(p, C, "tank_weapon"), Is.EqualTo(PurchaseResult.MaxLevel));
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(gold));
            Assert.That(p.Loadout.LevelOf("tank_weapon"), Is.EqualTo(track.MaxLevel));
            Assert.That(Workshop.NextCost(p, track), Is.EqualTo(0));
        }

        [Test]
        public void Sans_assez_d_or_rien_ne_change()
        {
            var p = Rich(10);
            Assert.That(Workshop.BuyEquipment(p, C, "tank_weapon"), Is.EqualTo(PurchaseResult.NotEnoughGold));
            Assert.That(p.Loadout.LevelOf("tank_weapon"), Is.EqualTo(0));
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(10));
        }

        [Test]
        public void Avec_exactement_le_prix_l_achat_reussit()
        {
            var p = PlayerProfile.NewGame(C);
            int cost = C.Upgrades.Track("tank_weapon")!.Costs[0];
            p.Wallet.Grant(Wallet.Gold, cost, "juste");
            Assert.That(Workshop.BuyEquipment(p, C, "tank_weapon"), Is.EqualTo(PurchaseResult.Ok));
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(0));
        }

        [Test]
        public void Une_piste_inconnue_est_refusee() =>
            Assert.That(Workshop.BuyEquipment(Rich(), C, "fantome"), Is.EqualTo(PurchaseResult.UnknownItem));

        [Test]
        public void On_n_achete_pas_pour_un_personnage_non_possede()
        {
            var p = Rich();
            p.OwnedCharacters.Remove("dps2");
            Assert.That(Workshop.BuyEquipment(p, C, "dps2_weapon"), Is.EqualTo(PurchaseResult.Locked));
        }

        [Test]
        public void Tout_acheter_a_fond_coute_exactement_la_somme_des_prix()
        {
            var p = Rich(1_000_000);
            foreach (var t in C.Upgrades.Equipment) for (int i = 0; i < t.MaxLevel; i++) Workshop.BuyEquipment(p, C, t.Id);
            int spent = 1_000_000 - p.Wallet.Balance(Wallet.Gold);
            Assert.That(spent, Is.EqualTo(C.Upgrades.Equipment.Sum(t => t.Costs.Sum())));
            Assert.That(C.Upgrades.Equipment.All(t => p.Loadout.LevelOf(t.Id) == t.MaxLevel), Is.True);
        }

        // ---- Talents ----

        [Test]
        public void Le_premier_choix_d_un_palier_est_payant()
        {
            var p = Rich(500);
            Assert.That(Workshop.PickTalent(p, C, 1, "thrifty"), Is.EqualTo(PurchaseResult.Ok));
            Assert.That(p.Loadout.Talents[1], Is.EqualTo("thrifty"));
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(500 - C.Upgrades.Tier(1)!.Cost));
        }

        [Test]
        public void Changer_d_option_dans_un_palier_achete_est_gratuit()
        {
            var p = Rich(500);
            Workshop.PickTalent(p, C, 1, "thrifty");
            int gold = p.Wallet.Balance(Wallet.Gold);
            Assert.That(Workshop.PickTalent(p, C, 1, "quick_heal"), Is.EqualTo(PurchaseResult.Ok));
            Assert.That(p.Loadout.Talents[1], Is.EqualTo("quick_heal"));
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(gold), "on peut se raviser sans payer");
        }

        [Test]
        public void Rechoisir_l_option_deja_active_ne_coute_rien()
        {
            var p = Rich(500);
            Workshop.PickTalent(p, C, 1, "thrifty");
            int gold = p.Wallet.Balance(Wallet.Gold);
            Workshop.PickTalent(p, C, 1, "thrifty");
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(gold));
        }

        [Test]
        public void Un_palier_exige_le_precedent()
        {
            var p = Rich();
            Assert.That(Workshop.PickTalent(p, C, 2, "strong_shield"), Is.EqualTo(PurchaseResult.NeedPreviousTier));
            Assert.That(p.Loadout.Talents, Is.Empty);
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(100000));
        }

        [Test]
        public void Un_palier_exige_assez_d_etoiles()
        {
            var p = Rich(100000, stars: 0);
            Assert.That(Workshop.PickTalent(p, C, 1, "thrifty"), Is.EqualTo(PurchaseResult.Locked));
            Assert.That(Workshop.TierAvailability(p, C, 1), Is.EqualTo(PurchaseResult.Locked));
        }

        [Test]
        public void Le_palier_s_ouvre_pile_a_son_seuil_d_etoiles()
        {
            int need = C.Upgrades.Tier(1)!.RequiresStars;
            Assert.That(Workshop.TierAvailability(Rich(1000, need - 1), C, 1), Is.EqualTo(PurchaseResult.Locked));
            Assert.That(Workshop.TierAvailability(Rich(1000, need), C, 1), Is.EqualTo(PurchaseResult.Ok));
        }

        [Test]
        public void Sans_assez_d_or_le_talent_n_est_pas_achete()
        {
            var p = Rich(10);
            Assert.That(Workshop.PickTalent(p, C, 1, "thrifty"), Is.EqualTo(PurchaseResult.NotEnoughGold));
            Assert.That(p.Loadout.Talents, Is.Empty);
        }

        [Test]
        public void Palier_ou_option_inconnus_sont_refuses()
        {
            var p = Rich();
            Assert.That(Workshop.PickTalent(p, C, 99, "thrifty"), Is.EqualTo(PurchaseResult.UnknownItem));
            Assert.That(Workshop.PickTalent(p, C, 1, "fantome"), Is.EqualTo(PurchaseResult.UnknownItem));
            Assert.That(Workshop.PickTalent(p, C, 1, "strong_shield"), Is.EqualTo(PurchaseResult.UnknownItem), "une option d'un autre palier");
        }

        [Test]
        public void Les_trois_paliers_s_achetent_dans_l_ordre()
        {
            var p = Rich(100000);
            foreach (var tier in C.Upgrades.TalentTiers)
                Assert.That(Workshop.PickTalent(p, C, tier.Tier, tier.Options[0].Id), Is.EqualTo(PurchaseResult.Ok), "palier " + tier.Tier);
            Assert.That(100000 - p.Wallet.Balance(Wallet.Gold), Is.EqualTo(C.Upgrades.TalentTiers.Sum(t => t.Cost)));
        }

        [Test]
        public void Toute_depense_de_l_atelier_passe_par_le_portefeuille_avec_une_raison()
        {
            var p = Rich(2000);
            Workshop.BuyEquipment(p, C, "healer_armor");
            Workshop.PickTalent(p, C, 1, "quick_heal");
            var atelier = p.Wallet.Ledger.Where(e => e.Reason.StartsWith("atelier:")).ToList();
            Assert.That(atelier, Has.Count.EqualTo(2));
            Assert.That(atelier.All(e => e.Amount < 0), Is.True);
        }

        // ---- Économie ----

        [Test]
        public void La_premiere_victoire_paie_deja_un_premier_niveau_d_equipement()
        {
            var p = PlayerProfile.NewGame(C);
            Progression.Complete(p, C, C.Levels[0], ProgressFixtures.Win(damage: 1));
            int cheapest = C.Upgrades.Equipment.Min(t => t.Costs[0]);
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.GreaterThanOrEqualTo(cheapest));
        }

        [Test]
        public void Apres_les_deux_premiers_niveaux_a_deux_etoiles_le_premier_talent_est_ouvert_et_abordable()
        {
            var p = PlayerProfile.NewGame(C);
            for (int i = 0; i < 2; i++) Progression.Complete(p, C, C.Levels[i], ProgressFixtures.Win(damage: C.Levels[i].ThreeStarMaxDamageTaken + 1));
            Assert.That(Workshop.TierAvailability(p, C, 1), Is.EqualTo(PurchaseResult.Ok));
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.GreaterThanOrEqualTo(C.Upgrades.Tier(1)!.Cost));
        }

        [Test]
        public void Tout_acheter_demande_un_nombre_de_parcours_raisonnable_ni_trop_court_ni_un_grind()
        {
            int total = C.Upgrades.Equipment.Sum(t => t.Costs.Sum()) + C.Upgrades.TalentTiers.Sum(t => t.Cost);
            int firstClear = C.Levels.Sum(l => l.RewardGold + 3 * l.StarBonusGold);
            int perLap = C.Levels.Sum(l => l.RepeatGold);
            double laps = (double)(total - firstClear) / perLap;
            Assert.That(laps, Is.InRange(10, 40), $"il faut {laps:0.#} parcours complets pour tout acheter");
        }

        [Test]
        public void Les_niveaux_d_equipement_plus_chers_donnent_le_meme_gain_qu_avant_le_prix_seul_augmente()
        {
            foreach (var t in C.Upgrades.Equipment)
                for (int i = 1; i < t.Costs.Count; i++) Assert.That(t.Costs[i], Is.GreaterThan(t.Costs[i - 1]), t.Id);
        }
    }

    /// <summary>Sauvegarde de l'équipement et des talents.</summary>
    public class LoadoutPersistenceTests
    {
        private static readonly GameContent C = Fixtures.FullContent();

        private static PlayerProfile Played()
        {
            var p = PlayerProfile.NewGame(C);
            foreach (var l in C.Levels) p.Levels[l.Id] = new LevelRecord { Completed = true, BestStars = 3, Clears = 1 };
            p.Wallet.Grant(Wallet.Gold, 5000, "test");
            for (int i = 0; i < 3; i++) Workshop.BuyEquipment(p, C, "tank_armor");
            Workshop.BuyEquipment(p, C, "healer_weapon");
            Workshop.PickTalent(p, C, 1, "quick_heal");
            Workshop.PickTalent(p, C, 2, "swift_purge");
            return p;
        }

        [Test]
        public void L_equipement_et_les_talents_survivent_a_une_sauvegarde()
        {
            var p = Played();
            Assert.That(ProfileStore.TryLoad(ProfileStore.ToJson(p), C, out var loaded, out var error), Is.True, error);
            Assert.That(loaded.Loadout.Equipment, Is.EquivalentTo(p.Loadout.Equipment));
            Assert.That(loaded.Loadout.Talents, Is.EquivalentTo(p.Loadout.Talents));
            Assert.That(loaded.Wallet.Balance(Wallet.Gold), Is.EqualTo(p.Wallet.Balance(Wallet.Gold)));
        }

        [Test]
        public void Le_texte_reste_stable_apres_rechargement()
        {
            var json = ProfileStore.ToJson(Played());
            ProfileStore.TryLoad(json, C, out var loaded, out _);
            Assert.That(ProfileStore.ToJson(loaded), Is.EqualTo(json));
        }

        [Test]
        public void Une_ancienne_sauvegarde_sans_equipement_se_charge_avec_un_equipement_vide()
        {
            Assert.That(ProfileStore.TryLoad("{\"version\": 1, \"levels\": {}}", C, out var p, out _), Is.True);
            Assert.That(p.Loadout.Equipment, Is.Empty);
            Assert.That(p.Loadout.Talents, Is.Empty);
        }

        [Test]
        public void Un_equipement_inconnu_ou_un_niveau_absurde_est_repare()
        {
            ProfileStore.TryLoad("{\"version\": 1, \"equipment\": {\"fantome\": 4, \"tank_weapon\": 99, \"tank_armor\": -3, \"dps1_weapon\": 2}}", C, out var p, out _);
            Assert.That(p.Loadout.Equipment.ContainsKey("fantome"), Is.False);
            Assert.That(p.Loadout.LevelOf("tank_weapon"), Is.EqualTo(C.Upgrades.Track("tank_weapon")!.MaxLevel));
            Assert.That(p.Loadout.Equipment.ContainsKey("tank_armor"), Is.False);
            Assert.That(p.Loadout.LevelOf("dps1_weapon"), Is.EqualTo(2));
        }

        [Test]
        public void Un_talent_sans_le_palier_precedent_est_retire()
        {
            var stars = "\"levels\": {\"l1\": {\"completed\": true, \"bestStars\": 3}, \"l2\": {\"completed\": true, \"bestStars\": 3}, \"l3\": {\"completed\": true, \"bestStars\": 3}}";
            ProfileStore.TryLoad("{\"version\": 1, " + stars + ", \"talents\": {\"2\": \"strong_shield\"}}", C, out var p, out _);
            Assert.That(p.Loadout.Talents, Is.Empty);
        }

        [Test]
        public void Un_talent_dont_les_etoiles_manquent_est_retire()
        {
            ProfileStore.TryLoad("{\"version\": 1, \"talents\": {\"1\": \"thrifty\"}}", C, out var p, out _);
            Assert.That(p.Loadout.Talents, Is.Empty, "aucune étoile : le palier 1 n'a pas pu être acheté");
        }

        [Test]
        public void Une_option_inconnue_ou_d_un_autre_palier_est_retiree()
        {
            var stars = "\"levels\": {\"l1\": {\"completed\": true, \"bestStars\": 3}, \"l2\": {\"completed\": true, \"bestStars\": 3}, \"l3\": {\"completed\": true, \"bestStars\": 3}}";
            ProfileStore.TryLoad("{\"version\": 1, " + stars + ", \"talents\": {\"1\": \"strong_shield\", \"2\": \"n_importe_quoi\"}}", C, out var p, out _);
            Assert.That(p.Loadout.Talents, Is.Empty);
        }

        [Test]
        public void Un_niveau_de_talent_illisible_ne_plante_pas_le_chargement()
        {
            Assert.That(ProfileStore.TryLoad("{\"version\": 1, \"talents\": {\"abc\": \"thrifty\", \"1\": 42}}", C, out var p, out _), Is.True);
            Assert.That(p.Loadout.Talents, Is.Empty);
        }

        [Test]
        public void Le_combat_utilise_l_equipement_du_profil_charge()
        {
            var p = Played();
            ProfileStore.TryLoad(ProfileStore.ToJson(p), C, out var loaded, out _);
            var enc = C.CreateEncounter("boss1", 1, loaded.OwnedCharacters, loaded.Loadout);
            Assert.That(enc.Allies.Single(a => a.Id == "tank").MaxHp, Is.GreaterThan(900));
            Assert.That(enc.Skills.Single(s => s.Id == "heal_single").ManaCost, Is.LessThan(18));
        }
    }
}
