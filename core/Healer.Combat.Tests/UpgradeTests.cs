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
        private static readonly string[] Fields = { "healAmount", "shieldAmount", "manaCost", "cooldownMs", "castMs" };
        private static readonly string[] Voies = { "lumiere", "egide", "purification", "vitalite" };
        private static GameContent C => Fixtures.FullContent();
        private static UpgradeCatalog Cat => C.Upgrades;

        private static IEnumerable<UpgradeEffect> AllEffects(UpgradeCatalog c) =>
            c.Equipment.SelectMany(t => t.PerLevel)
                .Concat(c.TalentTiers.SelectMany(t => t.Options).SelectMany(o => o.Effects))
                .Concat(c.Relics.SelectMany(r => r.Effects));

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
        public void Les_paliers_de_talent_se_suivent_de_un_a_n_avec_un_cout_en_points_croissant_ou_egal()
        {
            // D-084 : le coût est en points de talent (pas en or/étoiles) ; il reste UNIFORME (1/palier) par
            // simplicité de lecture (24 points au total = 2 voies pleines, ou réparties sur les 4) — donc
            // "croissant ou égal", pas strictement croissant.
            var tiers = Cat.TalentTiers;
            Assert.That(tiers.Select(t => t.Tier), Is.EqualTo(Enumerable.Range(1, tiers.Count)));
            Assert.That(tiers.Select(t => t.Cost), Is.Ordered.Ascending.And.All.GreaterThan(0));
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
            // La description est écrite à la main (ou générée avec les mêmes valeurs) : ce test empêche qu'elle
            // mente quand on change une valeur.
            foreach (var o in Cat.TalentTiers.SelectMany(t => t.Options))
            {
                Assert.That(o.Name, Is.Not.Empty, o.Id);
                var cited = Regex.Matches(o.Description, @"(\d+) %").Select(m => int.Parse(m.Groups[1].Value)).OrderBy(x => x).ToList();
                var actual = o.Effects.Select(e => Math.Abs(e.Pct)).OrderBy(x => x).ToList();
                Assert.That(cited, Is.EqualTo(actual), o.Id + " : « " + o.Description + " »");
            }
        }

        [Test]
        public void Les_talents_de_statistique_ne_touchent_que_le_soigneur()
        {
            // D-084 : élargi à maxHp/def (voie Vitalité) — jusqu'ici réservé à manaRegen/maxMana (D-083). Les
            // sorts modifiés sont ceux du soigneur par construction (LoadoutApplier applique les effets "skill" au
            // soigneur uniquement).
            foreach (var o in Cat.TalentTiers.SelectMany(t => t.Options))
                foreach (var e in o.Effects.Where(e => e.Stat != null))
                    Assert.That(Stats, Does.Contain(e.Stat), o.Id + " : un talent de statistique concerne le soigneur");
        }

        [Test]
        public void Un_seul_boost_de_statistique_par_piste_et_par_niveau_reste_modeste()
        {
            foreach (var t in Cat.Equipment)
                foreach (var e in t.PerLevel) Assert.That(Math.Abs(e.Pct) * t.MaxLevel, Is.LessThanOrEqualTo(60), t.Id + " : un équipement au maximum ne dépasse pas +60 %");
        }

        // ---- E02-T01/T02/T09 : voies de spécialisation, schéma et budget de puissance (D-084 : 4 voies × 12 paliers) --

        [Test]
        public void Chaque_palier_de_talent_appartient_a_une_voie_connue_avec_un_rang_de_un_a_douze()
        {
            foreach (var t in Cat.TalentTiers)
            {
                Assert.That(Voies, Does.Contain(t.Voie), "palier " + t.Tier);
                Assert.That(t.PalierDansVoie, Is.InRange(1, 12), "palier " + t.Tier);
            }
        }

        [Test]
        public void Quatre_voies_de_douze_paliers_chacune_sans_trou()
        {
            foreach (var voie in Voies)
            {
                var paliers = Cat.Voie(voie);
                Assert.That(paliers.Select(t => t.PalierDansVoie), Is.EqualTo(Enumerable.Range(1, 12)), voie);
            }
        }

        [Test]
        public void Chaque_option_de_talent_declare_un_budget_de_puissance_positif()
        {
            // E02-T09 : le budget sert à COMPARER les talents, pas à changer le jeu ; c'est l'équilibrage mesuré
            // (UpgradeBalanceTests) qui valide réellement, mais un budget absent ou nul serait un oubli de contenu.
            foreach (var o in Cat.TalentTiers.SelectMany(t => t.Options)) Assert.That(o.Power, Is.GreaterThan(0), o.Id);
        }

        [Test]
        public void Les_deux_options_d_un_meme_palier_ont_un_budget_de_puissance_proche()
        {
            // Règle de calcul (E02-T09) : les deux choix d'un palier doivent être des alternatives, pas un piège ;
            // on tolère un écart de puissance déclarée de 20 % au plus entre les deux options d'un même palier.
            foreach (var t in Cat.TalentTiers)
            {
                var (a, b) = (t.Options[0].Power, t.Options[1].Power);
                Assert.That(Math.Abs(a - b), Is.LessThanOrEqualTo(Math.Max(a, b) * 0.2), "palier " + t.Tier);
            }
        }

        [Test]
        public void Un_talent_qui_debloque_un_sort_vise_un_capstone_existant_et_reserve()
        {
            var skillIds = C.Skills.Select(s => s.Id).ToHashSet();
            foreach (var o in Cat.TalentTiers.SelectMany(t => t.Options).Where(o => o.UnlocksSkill != null))
            {
                Assert.That(skillIds, Does.Contain(o.UnlocksSkill), o.Id);
                var skill = C.Skills.Single(s => s.Id == o.UnlocksSkill);
                Assert.That(skill.Capstone, Is.True, o.Id + " : " + skill.Id + " doit être marqué capstone");
            }
        }

        [Test]
        public void Chaque_voie_a_exactement_un_capstone_au_dernier_palier()
        {
            foreach (var voie in Voies)
            {
                var paliers = Cat.Voie(voie);
                var capstoneOptions = paliers.SelectMany(t => t.Options).Where(o => o.UnlocksSkill != null).ToList();
                Assert.That(capstoneOptions, Has.Count.EqualTo(1), voie);
                Assert.That(paliers.Last().Options, Does.Contain(capstoneOptions[0]), voie + " : le capstone est au dernier palier");
            }
        }

        [Test]
        public void Aucun_sort_capstone_n_est_dans_la_barre_de_base_du_soigneur()
        {
            // Sans talent choisi, la barre du soigneur reste les 4 sorts historiques : un capstone est un bonus, jamais le point de départ.
            var (_, skills) = LoadoutApplier.Apply(Cat, null, C.Characters, C.Skills);
            Assert.That(skills.Any(s => s.Capstone), Is.False);
            Assert.That(skills.Select(s => s.Id), Is.EquivalentTo(new[] { "heal_single", "heal_aoe", "shield", "purge" }));
        }

        // ---- E02-T07 : reliques (D-084) ------------------------------------------------------------------------

        [Test]
        public void Chaque_relique_a_un_nom_un_cout_positif_et_au_moins_un_effet()
        {
            Assert.That(Cat.Relics, Is.Not.Empty);
            foreach (var r in Cat.Relics)
            {
                Assert.That(r.Name, Is.Not.Empty, r.Id);
                Assert.That(r.Cost, Is.GreaterThan(0), r.Id);
                Assert.That(r.Effects, Is.Not.Empty, r.Id);
            }
        }

        [Test]
        public void Les_ids_de_reliques_sont_uniques()
        {
            Assert.That(Cat.Relics.Select(r => r.Id).Distinct().Count(), Is.EqualTo(Cat.Relics.Count));
        }

        [Test]
        public void Il_y_a_plus_de_reliques_que_d_emplacements_equipables()
        {
            // Sinon "choisir" ses reliques n'existe pas : tout se porte en même temps.
            Assert.That(Cat.Relics.Count, Is.GreaterThan(UpgradeCatalog.MaxEquippedRelics));
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

        /// <summary>Premier palier de la voie donnée, quel que soit son numéro global (indépendant du réglage du contenu).</summary>
        private static TalentTierDef Tier1Of(GameContent c, string voie) => c.Upgrades.Voie(voie)[0];
        private static TalentTierDef TierOf(GameContent c, string voie, int palier) => c.Upgrades.Voie(voie)[palier - 1];

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
        public void Un_talent_de_soin_augmente_le_soin_et_accelere_son_incantation()
        {
            // D-084 : palier 1 de la voie Lumière — l'option "sûre" (0 = sans contrepartie) du rang 1.
            var c = C;
            var tier = Tier1Of(c, "lumiere");
            var option = tier.Options[0]; // "vif" : heal_single +healAmount, castMs négatif
            var healEffect = option.Effects.Single(e => e.Field == "healAmount");
            var castEffect = option.Effects.Single(e => e.Field == "castMs");
            var l = new Loadout(); l.Talents[tier.Tier] = option.Id;
            var (_, sk) = Apply(l, c);
            Assert.That(Sk(sk, "heal_single").HealAmount, Is.EqualTo(Math.Floor(170 * (100 + healEffect.Pct) / 100.0 + 0.5)));
            Assert.That(Sk(sk, "heal_single").CastMs, Is.EqualTo(Math.Floor(1000 * (100 + castEffect.Pct) / 100.0 + 0.5)));
            Assert.That(Sk(sk, "heal_aoe").HealAmount, Is.EqualTo(140), "les autres sorts ne changent pas");
        }

        [Test]
        public void Un_talent_generique_touche_tous_les_sorts()
        {
            // D-084 : capstone de la voie Purification (dernier palier), l'option "maîtrise" porte sur "*".
            var c = C;
            var tier = TierOf(c, "purification", 12);
            var option = tier.Options.Single(o => o.Effects.Any(e => e.Skill == "*"));
            var costEffect = option.Effects.Single(e => e.Skill == "*");
            var l = new Loadout(); l.Talents[tier.Tier] = option.Id;
            var (_, sk) = Apply(l, c);
            double Expected(double baseCost) => Math.Floor(baseCost * (100 + costEffect.Pct) / 100.0 + 0.5);
            Assert.That(Sk(sk, "heal_single").ManaCost, Is.EqualTo(Expected(18)));
            Assert.That(Sk(sk, "heal_aoe").ManaCost, Is.EqualTo(Expected(60)));
            Assert.That(Sk(sk, "shield").ManaCost, Is.EqualTo(Expected(28)));
            Assert.That(Sk(sk, "purge").ManaCost, Is.EqualTo(Expected(12)));
        }

        [Test]
        public void La_regeneration_de_mana_garde_ses_decimales()
        {
            // D-084 : un palier "régénération" de la voie Lumière (rang multiple de 3, cf. générateur).
            var c = C;
            var tier = TierOf(c, "lumiere", 3);
            var option = tier.Options.Single(o => o.Effects.Any(e => e.Stat == "manaRegen"));
            var regenEffect = option.Effects.Single(e => e.Stat == "manaRegen");
            var l = new Loadout(); l.Talents[tier.Tier] = option.Id;
            var (ch, _) = Apply(l, c);
            Assert.That(Ch(ch, "healer").ManaRegenPerSec, Is.EqualTo(6 * (100 + regenEffect.Pct) / 100.0).Within(1e-9));
        }

        [Test]
        public void La_recharge_d_un_sort_baisse_avec_un_talent()
        {
            // D-084 : palier 1 de la voie Purification (Purge Efficace).
            var c = C;
            var tier = Tier1Of(c, "purification");
            var option = tier.Options.Single(o => o.Id.Contains("efficace"));
            var cdEffect = option.Effects.Single(e => e.Field == "cooldownMs");
            var costEffect = option.Effects.Single(e => e.Field == "manaCost");
            var l = new Loadout(); l.Talents[tier.Tier] = option.Id;
            var (_, sk) = Apply(l, c);
            Assert.That(Sk(sk, "purge").CooldownMs, Is.EqualTo(Math.Floor(5000 * (100 + cdEffect.Pct) / 100.0 + 0.5)));
            Assert.That(Sk(sk, "purge").ManaCost, Is.EqualTo(Math.Floor(12 * (100 + costEffect.Pct) / 100.0 + 0.5)));
        }

        [Test]
        public void Les_pourcentages_de_plusieurs_sources_s_additionnent_au_lieu_de_se_multiplier()
        {
            var c = C;
            var tier = Tier1Of(c, "lumiere");
            var option = tier.Options[0];
            var healEffect = option.Effects.Single(e => e.Field == "healAmount");
            var l = new Loadout(); l.Equipment["healer_weapon"] = 5; l.Talents[tier.Tier] = option.Id;
            var (_, sk) = Apply(l, c);
            int total = 20 + healEffect.Pct; // +20 % (arme, 5 niveaux à 4 %) + le talent
            Assert.That(Sk(sk, "heal_single").HealAmount, Is.EqualTo(Math.Floor(170 * (100 + total) / 100.0 + 0.5)),
                $"+20 % (arme) +{healEffect.Pct} % (talent) = +{total} %, pas 1,2 × 1,{healEffect.Pct:00}");
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
            // On pousse à -100 % le premier effet manaCost et le premier effet cooldownMs trouvés dans le catalogue
            // (peu importe lesquels : c'est le clamp qu'on teste, pas un talent précis).
            var c = C;
            var allEffects = c.Upgrades.TalentTiers.SelectMany(t => t.Options.Select(o => (tier: t.Tier, opt: o.Id, effects: o.Effects))).ToList();
            var (manaTier, manaOpt, _) = allEffects.First(x => x.effects.Any(e => e.Field == "manaCost"));
            var (cdTier, cdOpt, _) = allEffects.First(x => x.effects.Any(e => e.Field == "cooldownMs"));
            allEffects.Single(x => x.tier == manaTier && x.opt == manaOpt).effects.First(e => e.Field == "manaCost").Pct = -100;
            allEffects.Single(x => x.tier == cdTier && x.opt == cdOpt).effects.First(e => e.Field == "cooldownMs").Pct = -100;
            var l = new Loadout(); l.Talents[manaTier] = manaOpt; l.Talents[cdTier] = cdOpt;
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

        // ---- E02-T07 : reliques (D-084) ------------------------------------------------------------------------

        [Test]
        public void Une_relique_equipee_applique_ses_effets()
        {
            var c = C;
            var relic = c.Upgrades.Relics[0];
            var l = new Loadout(); l.EquippedRelics.Add(relic.Id);
            var (ch, sk) = Apply(l, c);
            // Au moins un effet doit être visible (peu importe lequel : on vérifie le branchement, pas un chiffre précis).
            bool changed = relic.Effects.Any(e =>
                e.Stat != null ? true : sk.Any(s => s.Id == e.Skill || e.Skill == "*"));
            Assert.That(changed, Is.True, relic.Id);
        }

        [Test]
        public void Au_dela_de_la_limite_les_reliques_en_trop_sont_ignorees()
        {
            var c = C;
            var l = new Loadout();
            l.EquippedRelics.AddRange(c.Upgrades.Relics.Select(r => r.Id)); // toutes, au-delà de la limite équipable
            Assert.DoesNotThrow(() => Apply(l, c));
        }
    }

    /// <summary>L'atelier : achats d'équipement, choix de talents et reliques.</summary>
    public class WorkshopTests
    {
        private static readonly GameContent C = Fixtures.FullContent();

        private static PlayerProfile Rich(int gold = 100000, int talentPoints = 48)
        {
            var p = PlayerProfile.NewGame(C);
            p.Wallet.Grant(Wallet.Gold, gold, "test");
            if (talentPoints > 0) p.Wallet.Grant(Wallet.TalentPoints, talentPoints, "test");
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

        // ---- Talents (E02-T03, D-084 : points de talent, pas d'or ni d'étoiles) ----

        [Test]
        public void Le_premier_choix_d_un_palier_coute_des_points_de_talent_pas_d_or()
        {
            var p = Rich(500, talentPoints: 5);
            var tier1 = C.Upgrades.Voie("lumiere")[0];
            Assert.That(Workshop.PickTalent(p, C, tier1.Tier, tier1.Options[0].Id), Is.EqualTo(PurchaseResult.Ok));
            Assert.That(p.Loadout.Talents[tier1.Tier], Is.EqualTo(tier1.Options[0].Id));
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(500), "un talent ne coûte pas d'or");
            Assert.That(HealerLeveling.TalentPointsAvailable(p, C), Is.EqualTo(5 - tier1.Cost));
        }

        [Test]
        public void Changer_d_option_dans_un_palier_achete_est_gratuit()
        {
            var p = Rich(500, talentPoints: 5);
            var tier1 = C.Upgrades.Voie("lumiere")[0];
            Workshop.PickTalent(p, C, tier1.Tier, tier1.Options[0].Id);
            int points = HealerLeveling.TalentPointsAvailable(p, C);
            Assert.That(Workshop.PickTalent(p, C, tier1.Tier, tier1.Options[1].Id), Is.EqualTo(PurchaseResult.Ok));
            Assert.That(p.Loadout.Talents[tier1.Tier], Is.EqualTo(tier1.Options[1].Id));
            Assert.That(HealerLeveling.TalentPointsAvailable(p, C), Is.EqualTo(points), "on peut se raviser sans payer");
        }

        [Test]
        public void Rechoisir_l_option_deja_active_ne_coute_rien()
        {
            var p = Rich(500, talentPoints: 5);
            var tier1 = C.Upgrades.Voie("lumiere")[0];
            Workshop.PickTalent(p, C, tier1.Tier, tier1.Options[0].Id);
            int points = HealerLeveling.TalentPointsAvailable(p, C);
            Workshop.PickTalent(p, C, tier1.Tier, tier1.Options[0].Id);
            Assert.That(HealerLeveling.TalentPointsAvailable(p, C), Is.EqualTo(points));
        }

        [Test]
        public void Un_palier_exige_le_precedent_DE_SA_VOIE()
        {
            // D-084 : palier 2 de la voie Lumière exige le palier 1 de la voie Lumière, PAS le tier global précédent
            // (qui est le palier 1 d'une AUTRE voie).
            var p = Rich();
            var tier2 = C.Upgrades.Voie("lumiere")[1];
            Assert.That(Workshop.PickTalent(p, C, tier2.Tier, tier2.Options[0].Id), Is.EqualTo(PurchaseResult.NeedPreviousTier));
            Assert.That(p.Loadout.Talents, Is.Empty);
        }

        [Test]
        public void Un_palier_ouvert_de_sa_voie_n_exige_pas_les_autres_voies()
        {
            // D-084 : le premier palier de chaque voie ne dépend d'aucune autre voie : elles sont parallèles.
            var p = Rich();
            var tier1Egide = C.Upgrades.Voie("egide")[0];
            Assert.That(Workshop.PickTalent(p, C, tier1Egide.Tier, tier1Egide.Options[0].Id), Is.EqualTo(PurchaseResult.Ok));
        }

        [Test]
        public void Sans_assez_de_points_le_talent_n_est_pas_achete()
        {
            var p = Rich(100000, talentPoints: 0);
            var tier1 = C.Upgrades.Voie("lumiere")[0];
            Assert.That(Workshop.PickTalent(p, C, tier1.Tier, tier1.Options[0].Id), Is.EqualTo(PurchaseResult.NotEnoughTalentPoints));
            Assert.That(p.Loadout.Talents, Is.Empty);
        }

        [Test]
        public void Le_palier_s_ouvre_pile_a_son_cout_en_points()
        {
            var tier1 = C.Upgrades.Voie("lumiere")[0];
            var pauvre = Rich(1000, talentPoints: tier1.Cost - 1);
            var juste = Rich(1000, talentPoints: tier1.Cost);
            if (tier1.Cost > 0) Assert.That(Workshop.PickTalent(pauvre, C, tier1.Tier, tier1.Options[0].Id), Is.EqualTo(PurchaseResult.NotEnoughTalentPoints));
            Assert.That(Workshop.PickTalent(juste, C, tier1.Tier, tier1.Options[0].Id), Is.EqualTo(PurchaseResult.Ok));
        }

        [Test]
        public void Palier_ou_option_inconnus_sont_refuses()
        {
            var p = Rich();
            var tier1Lumiere = C.Upgrades.Voie("lumiere")[0];
            var tier1Egide = C.Upgrades.Voie("egide")[0];
            Assert.That(Workshop.PickTalent(p, C, 9999, tier1Lumiere.Options[0].Id), Is.EqualTo(PurchaseResult.UnknownItem));
            Assert.That(Workshop.PickTalent(p, C, tier1Lumiere.Tier, "fantome"), Is.EqualTo(PurchaseResult.UnknownItem));
            Assert.That(Workshop.PickTalent(p, C, tier1Lumiere.Tier, tier1Egide.Options[0].Id), Is.EqualTo(PurchaseResult.UnknownItem), "une option d'un autre palier");
        }

        [Test]
        public void Les_paliers_s_achetent_dans_l_ordre_global()
        {
            var p = Rich(100000, talentPoints: C.Upgrades.TalentTiers.Sum(t => t.Cost));
            foreach (var tier in C.Upgrades.TalentTiers)
                Assert.That(Workshop.PickTalent(p, C, tier.Tier, tier.Options[0].Id), Is.EqualTo(PurchaseResult.Ok), "palier " + tier.Tier);
            Assert.That(HealerLeveling.TalentPointsAvailable(p, C), Is.EqualTo(0));
        }

        [Test]
        public void La_reinitialisation_vide_les_talents_et_rend_tous_les_points()
        {
            var p = Rich(1000, talentPoints: 10);
            var voie = C.Upgrades.Voie("lumiere");
            Workshop.PickTalent(p, C, voie[0].Tier, voie[0].Options[0].Id);
            Workshop.PickTalent(p, C, voie[1].Tier, voie[1].Options[1].Id);
            Assert.That(HealerLeveling.TalentPointsAvailable(p, C), Is.LessThan(10));
            Workshop.RespecTalents(p);
            Assert.That(p.Loadout.Talents, Is.Empty);
            Assert.That(HealerLeveling.TalentPointsAvailable(p, C), Is.EqualTo(10), "tous les points reviennent");
        }

        [Test]
        public void Toute_depense_d_or_de_l_atelier_passe_par_le_portefeuille_avec_une_raison()
        {
            // D-084 : les talents ne dépensent plus d'or (points de talent, recalculés depuis le niveau et non
            // journalisés individuellement — la réinitialisation gratuite en dépend). L'équipement et les
            // reliques restent des achats définitifs, journalisés.
            var p = Rich(2000);
            Workshop.BuyEquipment(p, C, "healer_armor");
            Workshop.BuyRelic(p, C, C.Upgrades.Relics[0].Id);
            var atelier = p.Wallet.Ledger.Where(e => e.Reason.StartsWith("atelier:")).ToList();
            Assert.That(atelier, Has.Count.EqualTo(2));
            Assert.That(atelier.All(e => e.Amount < 0), Is.True);
        }

        // ---- Reliques (E02-T07, D-084) ----

        [Test]
        public void Acheter_une_relique_depense_l_or_et_l_ajoute_aux_possedees()
        {
            var p = Rich(1000);
            var relic = C.Upgrades.Relics[0];
            Assert.That(Workshop.BuyRelic(p, C, relic.Id), Is.EqualTo(PurchaseResult.Ok));
            Assert.That(p.OwnedRelics, Does.Contain(relic.Id));
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(1000 - relic.Cost));
        }

        [Test]
        public void Racheter_une_relique_deja_possedee_ne_coute_rien()
        {
            var p = Rich(1000);
            var relic = C.Upgrades.Relics[0];
            Workshop.BuyRelic(p, C, relic.Id);
            int gold = p.Wallet.Balance(Wallet.Gold);
            Assert.That(Workshop.BuyRelic(p, C, relic.Id), Is.EqualTo(PurchaseResult.Ok));
            Assert.That(p.Wallet.Balance(Wallet.Gold), Is.EqualTo(gold));
        }

        [Test]
        public void On_ne_peut_pas_equiper_une_relique_non_possedee()
        {
            var p = Rich(1000);
            Assert.That(Workshop.EquipRelic(p, C, C.Upgrades.Relics[0].Id), Is.EqualTo(PurchaseResult.RelicNotOwned));
        }

        [Test]
        public void Au_dela_de_la_limite_d_emplacements_l_equipement_de_relique_est_refuse()
        {
            var p = Rich(100000);
            foreach (var r in C.Upgrades.Relics) Workshop.BuyRelic(p, C, r.Id);
            for (int i = 0; i < UpgradeCatalog.MaxEquippedRelics; i++)
                Assert.That(Workshop.EquipRelic(p, C, C.Upgrades.Relics[i].Id), Is.EqualTo(PurchaseResult.Ok));
            Assert.That(Workshop.EquipRelic(p, C, C.Upgrades.Relics[UpgradeCatalog.MaxEquippedRelics].Id), Is.EqualTo(PurchaseResult.RelicSlotsFull));
        }

        [Test]
        public void Deequiper_une_relique_libere_l_emplacement()
        {
            var p = Rich(100000);
            var relic = C.Upgrades.Relics[0];
            Workshop.BuyRelic(p, C, relic.Id);
            Workshop.EquipRelic(p, C, relic.Id);
            Workshop.UnequipRelic(p, relic.Id);
            Assert.That(p.Loadout.EquippedRelics, Does.Not.Contain(relic.Id));
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
        public void Tout_acheter_demande_un_nombre_de_parcours_raisonnable_ni_trop_court_ni_un_grind()
        {
            // D-084 : les talents ne coûtent plus d'or (points de talent via le niveau, E02-T06) ; seuls
            // équipement et reliques restent chiffrés en or ici.
            int total = C.Upgrades.Equipment.Sum(t => t.Costs.Sum()) + C.Upgrades.Relics.Sum(r => r.Cost);
            int firstClear = C.Levels.Sum(l => l.RewardGold + 3 * l.StarBonusGold);
            int perLap = C.Levels.Sum(l => l.RepeatGold);
            double laps = (double)(total - firstClear) / perLap;
            Assert.That(laps, Is.InRange(5, 60), $"il faut {laps:0.#} parcours complets pour tout acheter (équipement + reliques)");
        }

        [Test]
        public void Les_niveaux_d_equipement_plus_chers_donnent_le_meme_gain_qu_avant_le_prix_seul_augmente()
        {
            foreach (var t in C.Upgrades.Equipment)
                for (int i = 1; i < t.Costs.Count; i++) Assert.That(t.Costs[i], Is.GreaterThan(t.Costs[i - 1]), t.Id);
        }
    }

    /// <summary>Sauvegarde de l'équipement, des talents et des reliques.</summary>
    public class LoadoutPersistenceTests
    {
        private static readonly GameContent C = Fixtures.FullContent();

        private static PlayerProfile Played()
        {
            var p = PlayerProfile.NewGame(C);
            foreach (var l in C.Levels) p.Levels[l.Id] = new LevelRecord { Completed = true, BestStars = 3, Clears = 1 };
            p.Wallet.Grant(Wallet.Gold, 5000, "test");
            p.Wallet.Grant(Wallet.TalentPoints, 10, "test");
            for (int i = 0; i < 3; i++) Workshop.BuyEquipment(p, C, "tank_armor");
            Workshop.BuyEquipment(p, C, "healer_weapon");
            var tier1 = C.Upgrades.Voie("lumiere")[0];
            var tier1Egide = C.Upgrades.Voie("egide")[0];
            Workshop.PickTalent(p, C, tier1.Tier, tier1.Options[1].Id); // l'option "risquée" (2ᵉ), pour un test qui vérifie qu'elle augmente le soin
            Workshop.PickTalent(p, C, tier1Egide.Tier, tier1Egide.Options[0].Id);
            Workshop.BuyRelic(p, C, C.Upgrades.Relics[0].Id);
            Workshop.EquipRelic(p, C, C.Upgrades.Relics[0].Id);
            return p;
        }

        [Test]
        public void L_equipement_les_talents_et_les_reliques_survivent_a_une_sauvegarde()
        {
            var p = Played();
            Assert.That(ProfileStore.TryLoad(ProfileStore.ToJson(p), C, out var loaded, out var error), Is.True, error);
            Assert.That(loaded.Loadout.Equipment, Is.EquivalentTo(p.Loadout.Equipment));
            Assert.That(loaded.Loadout.Talents, Is.EquivalentTo(p.Loadout.Talents));
            Assert.That(loaded.OwnedRelics, Is.EquivalentTo(p.OwnedRelics));
            Assert.That(loaded.Loadout.EquippedRelics, Is.EquivalentTo(p.Loadout.EquippedRelics));
            Assert.That(loaded.Wallet.Balance(Wallet.Gold), Is.EqualTo(p.Wallet.Balance(Wallet.Gold)));
            Assert.That(loaded.Wallet.Balance(Wallet.TalentPoints), Is.EqualTo(p.Wallet.Balance(Wallet.TalentPoints)));
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
            // D-084 : palier 2 de la voie Lumière exige le palier 1 (même voie), absent ici.
            var tier2 = C.Upgrades.Voie("lumiere")[1];
            var option = tier2.Options[0].Id;
            ProfileStore.TryLoad("{\"version\": 1, \"wallet\": {\"talentPoints\": 20}, \"talents\": {\"" + tier2.Tier + "\": \"" + option + "\"}}", C, out var p, out _);
            Assert.That(p.Loadout.Talents, Is.Empty);
        }

        [Test]
        public void Un_talent_sans_assez_de_points_est_retire()
        {
            var tier1 = C.Upgrades.Voie("lumiere")[0];
            ProfileStore.TryLoad("{\"version\": 1, \"wallet\": {\"talentPoints\": 0}, \"talents\": {\"" + tier1.Tier + "\": \"" + tier1.Options[0].Id + "\"}}", C, out var p, out _);
            Assert.That(p.Loadout.Talents, Is.Empty, "aucun point : le palier 1 n'a pas pu être acheté");
        }

        [Test]
        public void Une_option_inconnue_ou_d_un_autre_palier_est_retiree()
        {
            var tier1Egide = C.Upgrades.Voie("egide")[0];
            var tier1Vitalite = C.Upgrades.Voie("vitalite")[0];
            ProfileStore.TryLoad("{\"version\": 1, \"wallet\": {\"talentPoints\": 20}, \"talents\": {\"" + tier1Egide.Tier + "\": \"" + tier1Egide.Options[0].Id + "\", \"" + tier1Vitalite.Tier + "\": \"n_importe_quoi\"}}", C, out var p, out _);
            Assert.That(p.Loadout.Talents, Does.ContainKey(tier1Egide.Tier));
            Assert.That(p.Loadout.Talents.Values, Does.Not.Contain("n_importe_quoi"));
        }

        [Test]
        public void Un_niveau_de_talent_illisible_ne_plante_pas_le_chargement()
        {
            var tier1 = C.Upgrades.Voie("lumiere")[0];
            Assert.That(ProfileStore.TryLoad("{\"version\": 1, \"talents\": {\"abc\": \"" + tier1.Options[0].Id + "\", \"" + tier1.Tier + "\": 42}}", C, out var p, out _), Is.True);
            Assert.That(p.Loadout.Talents, Is.Empty);
        }

        [Test]
        public void Le_combat_utilise_l_equipement_du_profil_charge()
        {
            var p = Played();
            ProfileStore.TryLoad(ProfileStore.ToJson(p), C, out var loaded, out _);
            var enc = C.CreateEncounter("boss1", 1, loaded.OwnedCharacters, loaded.Loadout);
            Assert.That(enc.Allies.Single(a => a.Id == "tank").MaxHp, Is.GreaterThan(900));
            Assert.That(enc.Skills.Single(s => s.Id == "heal_single").HealAmount, Is.GreaterThan(170), "le talent de la voie Lumière augmente le Soin");
        }
    }
}
