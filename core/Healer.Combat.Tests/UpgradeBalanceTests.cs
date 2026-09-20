using System;
using System.Collections.Generic;
using System.Linq;
using Healer.Combat.Progress;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>
    /// Batterie d'équilibrage entre choix de spécialisation (docs/EQUILIBRAGE.md §8 et §10, D-049) : chaque talent,
    /// chaque paire de talents d'un palier, les huit combinaisons, la montée en puissance de l'équipement et le
    /// plafond de puissance. Joueur de référence : le bot « attentif » (500 ms), 100 combats par mesure.
    /// Règle : on ne relâche jamais une borne pour faire passer une valeur ; on règle la valeur (outil : ZBalayageTalents).
    /// </summary>
    public class UpgradeBalanceTests
    {
        private const int Seeds = 100;
        private static readonly string[] Bosses = { "boss1", "boss2", "boss3" };
        private static readonly GameContent C = Fixtures.FullContent();
        private static UpgradeCatalog Cat => C.Upgrades;

        private sealed class M { public double Win, Death, Pv, MinMs, MeanMs; }

        private static readonly Dictionary<string, M> Cache = new Dictionary<string, M>();

        private static string Key(string boss, Loadout l, string kind) =>
            boss + "|" + kind + "|" + string.Join(",", l.Equipment.OrderBy(k => k.Key).Select(k => k.Key + k.Value)) + "|" + string.Join(",", l.Talents.OrderBy(k => k.Key).Select(k => k.Key + k.Value));

        private static M Measure(string boss, Loadout l, string kind = "attentif")
        {
            var key = Key(boss, l, kind);
            if (Cache.TryGetValue(key, out var cached)) return cached;
            int wins = 0, deaths = 0; double low = 0, min = double.MaxValue, total = 0;
            for (uint seed = 1; seed <= Seeds; seed++)
            {
                var battle = new Battle(C.CreateEncounter(boss, seed, null, l), kind == "spam"
                    ? Enumerable.Range(0, 100).Select(i => new Command { TimeMs = i * 1200, SkillId = "heal_single", TargetId = "tank" }).ToList()
                    : null);
                double lowest = 1;
                battle.Subscribe(_ => { foreach (var u in battle.GetAllies()) lowest = Math.Min(lowest, u.Hp / u.MaxHp); });
                if (kind == "attentif") ReferenceHealerBot.Run(battle, 150000);
                else battle.Run(150000);
                if (battle.GetResult() == BattleResults.Victory) wins++;
                if (battle.GetAllies().Any(u => !u.Alive)) deaths++;
                low += lowest; min = Math.Min(min, battle.GetClock()); total += battle.GetClock();
            }
            return Cache[key] = new M { Win = (double)wins / Seeds, Death = (double)deaths / Seeds, Pv = low / Seeds, MinMs = min, MeanMs = total / Seeds };
        }

        private static Loadout Talent(int tier, string option) { var l = new Loadout(); l.Talents[tier] = option; return l; }
        private static Loadout Maxed(params int[] options) => Loadout.Maxed(Cat, options.Length == 0 ? null : options);
        private static Loadout EquipmentOnly(int level)
        {
            var l = new Loadout();
            foreach (var t in Cat.Equipment) l.Equipment[t.Id] = Math.Min(level, t.MaxLevel);
            return l;
        }

        public static IEnumerable<TestCaseData> EachTalent()
        {
            foreach (var tier in Fixtures.FullContent().Upgrades.TalentTiers)
                foreach (var o in tier.Options)
                    yield return new TestCaseData(tier.Tier, o.Id).SetName($"palier {tier.Tier} : {o.Id}");
        }

        public static IEnumerable<TestCaseData> EachTier() =>
            Fixtures.FullContent().Upgrades.TalentTiers.Select(t => new TestCaseData(t.Tier).SetName($"palier {t.Tier}"));

        // ---- Chaque talent, seul ----

        [TestCaseSource(nameof(EachTalent))]
        public void Un_talent_seul_garde_le_combat_gagnable_sur_chaque_boss(int tier, string option)
        {
            foreach (var boss in Bosses)
            {
                var m = Measure(boss, Talent(tier, option));
                Assert.That(m.Win, Is.GreaterThanOrEqualTo(0.95), boss);
                Assert.That(m.Death, Is.LessThanOrEqualTo(0.10), boss);
            }
        }

        [TestCaseSource(nameof(EachTalent))]
        public void Un_talent_vaut_son_prix_il_aide_nettement_sur_au_moins_un_boss(int tier, string option)
        {
            double best = Bosses.Max(b => Measure(b, Talent(tier, option)).Pv - Measure(b, new Loadout()).Pv);
            Assert.That(best, Is.GreaterThanOrEqualTo(0.02), "un talent qui ne change rien est un piège");
        }

        [TestCaseSource(nameof(EachTalent))]
        public void Un_talent_ne_rend_pas_le_jeu_trivial_a_lui_seul(int tier, string option)
        {
            foreach (var b in Bosses) Assert.That(Measure(b, Talent(tier, option)).Pv, Is.LessThanOrEqualTo(0.70), b);
        }

        // ---- Chaque paire (choix exclusif d'un palier) ----

        [TestCaseSource(nameof(EachTier))]
        public void Les_deux_options_d_un_palier_sont_proches_sur_chaque_boss(int tier)
        {
            var (a, b) = (Cat.Tier(tier)!.Options[0], Cat.Tier(tier)!.Options[1]);
            foreach (var boss in Bosses)
                Assert.That(Math.Abs(Measure(boss, Talent(tier, a.Id)).Pv - Measure(boss, Talent(tier, b.Id)).Pv), Is.LessThanOrEqualTo(0.15), boss);
        }

        [TestCaseSource(nameof(EachTier))]
        public void Sur_toute_la_campagne_les_deux_options_d_un_palier_se_valent(int tier)
        {
            var (a, b) = (Cat.Tier(tier)!.Options[0], Cat.Tier(tier)!.Options[1]);
            double mean = Bosses.Average(boss => Measure(boss, Talent(tier, a.Id)).Pv - Measure(boss, Talent(tier, b.Id)).Pv);
            Assert.That(Math.Abs(mean), Is.LessThanOrEqualTo(0.05));
        }

        [TestCaseSource(nameof(EachTier))]
        public void Aucune_option_ne_domine_l_autre_chacune_est_meilleure_sur_un_boss(int tier)
        {
            var (a, b) = (Cat.Tier(tier)!.Options[0], Cat.Tier(tier)!.Options[1]);
            var diffs = Bosses.Select(boss => Measure(boss, Talent(tier, a.Id)).Pv - Measure(boss, Talent(tier, b.Id)).Pv).ToList();
            Assert.That(diffs.All(d => d > 0.02), Is.False, $"« {a.Id} » est meilleure partout : le choix n'existe pas");
            Assert.That(diffs.All(d => d < -0.02), Is.False, $"« {b.Id} » est meilleure partout : le choix n'existe pas");
        }

        // ---- Les huit combinaisons de talents ----

        public static IEnumerable<TestCaseData> AllCombos()
        {
            for (int mask = 0; mask < 8; mask++)
                yield return new TestCaseData(mask).SetName($"combinaison {(mask & 1) + 1}-{((mask >> 1) & 1) + 1}-{((mask >> 2) & 1) + 1}");
        }

        private static Loadout Combo(int mask)
        {
            var l = new Loadout();
            for (int i = 0; i < Cat.TalentTiers.Count; i++) l.Talents[i + 1] = Cat.TalentTiers[i].Options[(mask >> i) & 1].Id;
            return l;
        }

        [TestCaseSource(nameof(AllCombos))]
        public void Chaque_combinaison_de_talents_est_viable_sur_chaque_boss(int mask)
        {
            foreach (var boss in Bosses)
            {
                var m = Measure(boss, Combo(mask));
                Assert.That(m.Win, Is.GreaterThanOrEqualTo(0.95), boss);
                Assert.That(m.Death, Is.LessThanOrEqualTo(0.10), boss);
            }
        }

        [Test]
        public void Aucune_combinaison_n_est_un_piege_ni_un_choix_evident_ecart_de_PV_limite_par_boss()
        {
            foreach (var boss in Bosses)
            {
                var pvs = Enumerable.Range(0, 8).Select(m => Measure(boss, Combo(m)).Pv).ToList();
                Assert.That(pvs.Max() - pvs.Min(), Is.LessThanOrEqualTo(0.20), boss + " : la meilleure et la pire combinaison sont trop éloignées");
            }
        }

        [Test]
        public void La_meilleure_combinaison_n_est_pas_la_meme_sur_tous_les_boss()
        {
            var bestPerBoss = Bosses.Select(boss => Enumerable.Range(0, 8).OrderByDescending(m => Measure(boss, Combo(m)).Pv).First()).ToList();
            Assert.That(bestPerBoss.Distinct().Count(), Is.GreaterThan(1), "une seule combinaison gagne partout : les talents ne sont pas de vrais choix");
        }

        // ---- Équipement : montée en puissance ----

        [Test]
        public void L_equipement_rend_le_combat_plus_facile_de_facon_continue_niveau_0_2_5()
        {
            foreach (var boss in Bosses)
            {
                double p0 = Measure(boss, new Loadout()).Pv, p2 = Measure(boss, EquipmentOnly(2)).Pv, p5 = Measure(boss, EquipmentOnly(5)).Pv;
                Assert.That(p2, Is.GreaterThan(p0 + 0.02), boss + " : niveau 2 vs 0");
                Assert.That(p5, Is.GreaterThan(p2 + 0.02), boss + " : niveau 5 vs 2");
            }
        }

        [Test]
        public void L_equipement_au_maximum_reste_gagnable_et_garde_de_la_tension()
        {
            foreach (var boss in Bosses)
            {
                var m = Measure(boss, EquipmentOnly(5));
                Assert.That(m.Win, Is.GreaterThanOrEqualTo(0.99), boss);
                Assert.That(m.Pv, Is.LessThanOrEqualTo(0.70), boss + " : l'équipement seul ne doit pas rendre l'équipe intouchable");
            }
        }

        [Test]
        public void Un_equipement_raccourcit_les_combats_sans_les_rendre_expedies()
        {
            foreach (var boss in Bosses)
            {
                var m = Measure(boss, EquipmentOnly(5));
                Assert.That(m.MeanMs, Is.LessThan(Measure(boss, new Loadout()).MeanMs));
                Assert.That(m.MinMs, Is.GreaterThanOrEqualTo(45000), boss);
            }
        }

        public static IEnumerable<TestCaseData> EachTrack() =>
            Fixtures.FullContent().Upgrades.Equipment.Select(t => new TestCaseData(t.Id).SetName(t.Id));

        [TestCaseSource(nameof(EachTrack))]
        public void Aucune_piste_d_equipement_n_est_inutile(string trackId)
        {
            var track = Cat.Track(trackId)!;
            var l = new Loadout(); l.Equipment[trackId] = track.MaxLevel;
            double bestPv = Bosses.Max(b => Measure(b, l).Pv - Measure(b, new Loadout()).Pv);
            double bestTime = Bosses.Max(b => (Measure(b, new Loadout()).MeanMs - Measure(b, l).MeanMs) / 1000);
            Assert.That(bestPv >= 0.01 || bestTime >= 2, Is.True, $"{trackId} : +{bestPv:0.00} de PV minimum, {bestTime:0.#} s gagnées : sans effet mesurable");
        }

        [TestCaseSource(nameof(EachTrack))]
        public void Aucune_piste_d_equipement_ne_rend_le_jeu_trivial_a_elle_seule(string trackId)
        {
            var track = Cat.Track(trackId)!;
            var l = new Loadout(); l.Equipment[trackId] = track.MaxLevel;
            foreach (var b in Bosses) Assert.That(Measure(b, l).Pv, Is.LessThanOrEqualTo(0.65), b);
        }

        // ---- Plafond de puissance ----

        [Test]
        public void Tout_au_maximum_reste_gagnable_mais_pas_invincible()
        {
            foreach (var boss in Bosses)
            {
                var m = Measure(boss, Maxed());
                Assert.That(m.Win, Is.GreaterThanOrEqualTo(0.99), boss);
                Assert.That(m.Pv, Is.LessThanOrEqualTo(0.85), boss + " : même au maximum, l'équipe doit pouvoir souffrir");
                Assert.That(m.MinMs, Is.GreaterThanOrEqualTo(45000), boss);
            }
        }

        [Test]
        public void Meme_au_maximum_le_soigneur_reste_indispensable()
        {
            foreach (var boss in Bosses)
            {
                Assert.That(Measure(boss, Maxed(), "sans soigneur").Win, Is.EqualTo(0), boss + " : sans soigneur");
                Assert.That(Measure(boss, Maxed(), "spam").Win, Is.LessThanOrEqualTo(0.02), boss + " : en spammant un seul sort");
            }
        }

        [Test]
        public void Tout_au_maximum_est_nettement_plus_facile_que_le_jeu_de_base_la_progression_se_ressent()
        {
            foreach (var boss in Bosses)
                Assert.That(Measure(boss, Maxed()).Pv - Measure(boss, new Loadout()).Pv, Is.GreaterThanOrEqualTo(0.20), boss);
        }

        [Test]
        public void La_progression_est_progressive_a_mi_parcours_le_jeu_est_entre_le_debut_et_la_fin()
        {
            var mid = EquipmentOnly(2); mid.Talents[1] = Cat.TalentTiers[0].Options[0].Id;
            foreach (var boss in Bosses)
            {
                double start = Measure(boss, new Loadout()).Pv, middle = Measure(boss, mid).Pv, end = Measure(boss, Maxed()).Pv;
                Assert.That(middle, Is.GreaterThan(start).And.LessThan(end), boss);
            }
        }

        [Test]
        public void Le_jeu_de_base_sans_amelioration_garde_ses_mesures_de_reference()
        {
            // Filet : ajouter des améliorations ne doit jamais changer le combat de base.
            Assert.That(Measure("boss1", new Loadout()).Pv, Is.EqualTo(0.28).Within(0.03));
            Assert.That(Measure("boss1", new Loadout()).Win, Is.EqualTo(1.0));
        }
    }
}
