using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Intégrité des données de jeu (core/content), reprise des tests de la version Phaser (E14-T07).</summary>
    public class DataIntegrityTests
    {
        private static readonly string[] HardCodedCharacterIds = { "healer", "tank" };
        private static readonly string[] HardCodedSkillIds = { "heal_single", "heal_aoe", "shield", "purge" };

        private static GameContent Content => Fixtures.Content();

        [Test]
        public void Les_ids_de_personnages_sont_uniques_et_ceux_references_dans_le_code_existent()
        {
            var ids = Content.Characters.Select(c => c.Id).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
            foreach (var id in HardCodedCharacterIds) Assert.That(ids, Does.Contain(id));
        }

        [Test]
        public void Il_y_a_exactement_un_soigneur_et_il_a_du_mana()
        {
            var healers = Content.Characters.Where(c => c.Role == "healer").ToList();
            Assert.That(healers, Has.Count.EqualTo(1));
            Assert.That(healers[0].MaxMana, Is.GreaterThan(0));
            Assert.That(healers[0].ManaRegenPerSec, Is.GreaterThan(0));
        }

        [Test]
        public void Les_personnages_ont_des_statistiques_valides()
        {
            foreach (var c in Content.Characters)
            {
                Assert.That(c.MaxHp, Is.GreaterThan(0), c.Id);
                Assert.That(c.Atk, Is.GreaterThanOrEqualTo(0), c.Id);
                Assert.That(c.Def, Is.GreaterThanOrEqualTo(0), c.Id);
            }
        }

        [Test]
        public void Les_ids_de_sorts_sont_uniques_et_ceux_references_dans_le_code_existent()
        {
            var ids = Content.Skills.Select(s => s.Id).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
            foreach (var id in HardCodedSkillIds) Assert.That(ids, Does.Contain(id));
        }

        [Test]
        public void Chaque_sort_a_un_cout_une_recharge_et_au_moins_un_effet()
        {
            foreach (var s in Content.Skills)
            {
                Assert.That(s.ManaCost, Is.GreaterThanOrEqualTo(0), s.Id);
                Assert.That(s.CooldownMs > 0 || s.CastMs > 0, Is.True, s.Id + " : ni recharge ni incantation");
                Assert.That(s.CooldownMs, Is.GreaterThanOrEqualTo(0), s.Id);
                Assert.That(s.CastMs, Is.GreaterThanOrEqualTo(0), s.Id);
                bool hasEffect = s.HealAmount > 0 || s.ShieldAmount > 0 || s.Cleanse == true;
                Assert.That(hasEffect, $"{s.Id} n'a aucun effet");
            }
        }

        [Test]
        public void Le_boss_a_des_statistiques_et_un_pattern_valides_avec_des_telegraphes_plus_courts_que_le_rythme()
        {
            var boss = Content.Boss;
            Assert.That(boss.MaxHp, Is.GreaterThan(0));
            Assert.That(boss.TickMs, Is.GreaterThan(0));
            Assert.That(boss.Pattern, Is.Not.Empty);
            foreach (var a in boss.Pattern) Assert.That(a.TelegraphMs, Is.LessThan(boss.TickMs), a.Type);
        }

        [Test]
        public void Le_pas_de_simulation_est_plus_petit_que_les_plus_petits_intervalles_des_donnees()
        {
            var c = Content;
            Assert.That(FixedStepper.DefaultStepMs, Is.LessThan(c.Boss.TickMs));
            Assert.That(FixedStepper.DefaultStepMs, Is.LessThan(Battle.AllyAttackIntervalMs));
            foreach (var e in c.Effects) Assert.That(FixedStepper.DefaultStepMs, Is.LessThan(e.TickMs), e.Id);
            foreach (var p in c.Boss.Phases ?? new List<BossPhaseDef>()) Assert.That(FixedStepper.DefaultStepMs, Is.LessThan(p.TickMs), p.Name);
        }

        [Test]
        public void Les_effets_sont_valides_et_les_actions_de_boss_referencent_des_effets_existants()
        {
            var c = Content;
            var ids = c.Effects.Select(e => e.Id).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
            foreach (var e in c.Effects)
            {
                Assert.That(e.DamagePerTick, Is.GreaterThan(0), e.Id);
                Assert.That(e.DurationMs, Is.GreaterThanOrEqualTo(e.TickMs), e.Id);
            }
            var actions = c.Boss.Pattern.Concat((c.Boss.Phases ?? new List<BossPhaseDef>()).SelectMany(p => p.Pattern));
            foreach (var a in actions.Where(a => a.EffectId != null)) Assert.That(ids, Does.Contain(a.EffectId), a.EffectId);
        }

        [Test]
        public void Les_phases_du_boss_sont_triees_par_seuil_decroissant_et_ont_un_rythme_valide()
        {
            var phases = Content.Boss.Phases ?? new List<BossPhaseDef>();
            var thresholds = phases.Select(p => p.AtHpRatio).ToList();
            Assert.That(thresholds, Is.EqualTo(thresholds.OrderByDescending(x => x).ToList()));
            foreach (var p in phases)
            {
                Assert.That(p.AtHpRatio, Is.GreaterThan(0).And.LessThan(1));
                Assert.That(p.Pattern, Is.Not.Empty);
                foreach (var a in p.Pattern) Assert.That(a.TelegraphMs, Is.LessThan(p.TickMs), $"{p.Name}/{a.Type}");
            }
        }

        [Test]
        public void Valeurs_a_hauteur_humaine_toute_quantite_de_jeu_est_un_entier_sauf_multiplicateurs_et_ratios()
        {
            var offenders = new List<string>();
            void Walk(JToken token, string path, string key)
            {
                switch (token.Type)
                {
                    case JTokenType.Float:
                        if (!Regex.IsMatch(key, "multiplier|ratio", RegexOptions.IgnoreCase)) offenders.Add($"{path} = {token}");
                        break;
                    case JTokenType.Array:
                        int i = 0;
                        foreach (var child in token) Walk(child, $"{path}[{i++}]", key);
                        break;
                    case JTokenType.Object:
                        foreach (var p in ((JObject)token).Properties()) Walk(p.Value, $"{path}.{p.Name}", p.Name);
                        break;
                }
            }
            foreach (var (name, json) in new[] { ("characters", Fixtures.CharactersJson), ("skills", Fixtures.SkillsJson), ("boss", Fixtures.BossJson), ("effects", Fixtures.EffectsJson) })
                Walk(JToken.Parse(json), name, "");
            Assert.That(offenders, Is.Empty);
        }

        [Test]
        public void Un_contenu_invalide_donne_une_erreur_claire()
        {
            var ex = Assert.Throws<System.InvalidOperationException>(() =>
                GameContent.FromJson("{ pas du json", Fixtures.SkillsJson, Fixtures.EffectsJson, Fixtures.BossJson));
            Assert.That(ex!.Message, Does.Contain("characters.json"));
        }
    }
}
