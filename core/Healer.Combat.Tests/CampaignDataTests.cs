using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Intégrité de la campagne : niveaux, boss, effets (core/content).</summary>
    public class CampaignDataTests
    {
        private static GameContent C => Fixtures.FullContent();

        [Test]
        public void Il_y_a_au_moins_trois_boss_et_trois_niveaux()
        {
            Assert.That(C.Bosses.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(C.Levels.Count, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void Les_ids_de_boss_et_de_niveaux_sont_uniques()
        {
            Assert.That(C.Bosses.Select(b => b.Id).Distinct().Count(), Is.EqualTo(C.Bosses.Count));
            Assert.That(C.Levels.Select(l => l.Id).Distinct().Count(), Is.EqualTo(C.Levels.Count));
        }

        [Test]
        public void Chaque_niveau_pointe_vers_un_boss_qui_existe()
        {
            foreach (var l in C.Levels) Assert.That(C.Bosses.Select(b => b.Id), Does.Contain(l.BossId), l.Id);
        }

        [Test]
        public void Chaque_boss_a_un_niveau_aucun_boss_orphelin()
        {
            foreach (var b in C.Bosses) Assert.That(C.Levels.Select(l => l.BossId), Does.Contain(b.Id), b.Id);
        }

        [Test]
        public void Les_boss_sont_tous_differents_par_leur_nom_et_leur_pattern()
        {
            Assert.That(C.Bosses.Select(b => b.Name).Distinct().Count(), Is.EqualTo(C.Bosses.Count));
            // Signature = pattern de base + tous les patterns de phases (deux boss ne se jouent pas pareil).
            string Signature(BossDef b) => string.Join(";", new[] { b.Pattern }.Concat((b.Phases ?? new List<BossPhaseDef>()).Select(p => p.Pattern)).Select(pat => string.Join(",", pat.Select(a => a.Type + a.EffectId + a.Multiplier))));
            Assert.That(C.Bosses.Select(Signature).Distinct().Count(), Is.EqualTo(C.Bosses.Count));
        }

        [Test]
        public void Le_premier_niveau_est_libre_et_les_suivants_exigent_le_precedent()
        {
            Assert.That(C.Levels[0].Requires, Is.Null);
            for (int i = 1; i < C.Levels.Count; i++) Assert.That(C.Levels[i].Requires, Is.EqualTo(C.Levels[i - 1].Id), C.Levels[i].Id);
        }

        [Test]
        public void Les_recompenses_sont_des_entiers_positifs_et_croissent_avec_la_difficulte()
        {
            foreach (var l in C.Levels)
            {
                Assert.That(l.RewardGold, Is.GreaterThan(0), l.Id);
                Assert.That(l.RepeatGold, Is.GreaterThan(0), l.Id);
                Assert.That(l.StarBonusGold, Is.GreaterThan(0), l.Id);
                Assert.That(l.RepeatGold, Is.LessThan(l.RewardGold), "rejouer paie moins que la première victoire: " + l.Id);
            }
            for (int i = 1; i < C.Levels.Count; i++) Assert.That(C.Levels[i].RewardGold, Is.GreaterThan(C.Levels[i - 1].RewardGold));
        }

        [Test]
        public void Le_seuil_de_troisieme_etoile_est_strictement_positif()
        {
            foreach (var l in C.Levels) Assert.That(l.ThreeStarMaxDamageTaken, Is.GreaterThan(0), l.Id);
        }

        [Test]
        public void Chaque_boss_a_des_statistiques_valides_et_des_telegraphes_plus_courts_que_son_rythme()
        {
            foreach (var b in C.Bosses)
            {
                Assert.That(b.MaxHp, Is.GreaterThan(0), b.Id);
                Assert.That(b.Atk, Is.GreaterThan(0), b.Id);
                Assert.That(b.Pattern, Is.Not.Empty, b.Id);
                foreach (var a in b.Pattern) Assert.That(a.TelegraphMs, Is.LessThan(b.TickMs), b.Id + " " + a.Type);
                foreach (var ph in b.Phases ?? new List<BossPhaseDef>())
                {
                    Assert.That(ph.Pattern, Is.Not.Empty, b.Id + " " + ph.Name);
                    foreach (var a in ph.Pattern) Assert.That(a.TelegraphMs, Is.LessThan(ph.TickMs), b.Id + " " + ph.Name + " " + a.Type);
                }
            }
        }

        [Test]
        public void Les_phases_d_un_boss_sont_par_pv_decroissants_et_dans_l_intervalle_ouvert()
        {
            foreach (var b in C.Bosses)
            {
                var ratios = (b.Phases ?? new List<BossPhaseDef>()).Select(p => p.AtHpRatio).ToList();
                foreach (var r in ratios) Assert.That(r, Is.GreaterThan(0).And.LessThan(1), b.Id);
                Assert.That(ratios, Is.Ordered.Descending, b.Id);
            }
        }

        [Test]
        public void Chaque_action_de_poison_reference_un_effet_qui_existe()
        {
            var effectIds = C.Effects.Select(e => e.Id).ToHashSet();
            foreach (var b in C.Bosses)
            {
                var actions = b.Pattern.Concat((b.Phases ?? new List<BossPhaseDef>()).SelectMany(p => p.Pattern));
                foreach (var a in actions.Where(a => a.Type == "poison"))
                    Assert.That(effectIds, Does.Contain(a.EffectId), b.Id);
            }
        }

        [Test]
        public void Les_effets_ont_des_valeurs_valides_et_des_ids_uniques()
        {
            Assert.That(C.Effects.Select(e => e.Id).Distinct().Count(), Is.EqualTo(C.Effects.Count));
            foreach (var e in C.Effects)
            {
                Assert.That(e.DamagePerTick, Is.GreaterThan(0), e.Id);
                Assert.That(e.TickMs, Is.GreaterThan(0), e.Id);
                Assert.That(e.DurationMs, Is.GreaterThanOrEqualTo(e.TickMs), e.Id);
            }
        }

        [Test]
        public void Le_pas_de_simulation_est_plus_petit_que_les_intervalles_de_tous_les_boss()
        {
            foreach (var b in C.Bosses)
            {
                Assert.That(FixedStepper.DefaultStepMs, Is.LessThan(b.TickMs), b.Id);
                foreach (var ph in b.Phases ?? new List<BossPhaseDef>()) Assert.That(FixedStepper.DefaultStepMs, Is.LessThan(ph.TickMs), b.Id + " " + ph.Name);
            }
        }

        [Test]
        public void Toute_quantite_de_jeu_est_un_entier_rond_seuls_les_multiplicateurs_sont_decimaux()
        {
            foreach (var file in Directory.GetFiles(Fixtures.ContentDir, "*.json"))
                Walk(JToken.Parse(File.ReadAllText(file)), System.IO.Path.GetFileName(file), "");
        }

        private static void Walk(JToken token, string file, string key)
        {
            switch (token)
            {
                case JObject o:
                    foreach (var p in o.Properties()) Walk(p.Value, file, p.Name);
                    break;
                case JArray a:
                    foreach (var item in a) Walk(item, file, key);
                    break;
                case JValue v when v.Type == JTokenType.Float:
                    Assert.That(key == "multiplier" || key.EndsWith("Ratio"), Is.True, $"{file} : « {key} » = {v} devrait être un entier");
                    break;
            }
        }

        [Test]
        public void Les_ids_codes_en_dur_dans_le_client_existent()
        {
            Assert.That(C.Levels.Select(l => l.Id), Does.Contain("l1"));
            Assert.That(C.Bosses.Select(b => b.Id), Does.Contain("boss1"));
        }
    }
}
