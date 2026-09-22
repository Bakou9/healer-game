using System;
using System.Collections.Generic;
using System.Linq;
using Healer.Ui;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Maintenir un sort pour l'enchaîner (D-050).</summary>
    public class HoldRepeatTests
    {
        private static readonly CastResolution Cast = new CastResolution(CastKind.Cast, "tank");
        private static readonly CastResolution NeedTarget = new CastResolution(CastKind.NeedTarget);
        private static readonly CastResolution Unavailable = new CastResolution(CastKind.Unavailable);

        [Test]
        public void Sans_appui_rien_n_est_repete()
        {
            var h = new HoldRepeat();
            Assert.That(h.HeldSkillId, Is.Null);
            Assert.That(h.ShouldCast(ScreenState.Playing, Cast), Is.False);
        }

        [Test]
        public void Un_sort_maintenu_et_lancable_est_repete_en_combat()
        {
            var h = new HoldRepeat();
            h.Press("heal_single");
            Assert.That(h.ShouldCast(ScreenState.Playing, Cast), Is.True);
        }

        [Test]
        public void Le_sort_maintenu_n_est_pas_lance_quand_il_est_indisponible()
        {
            var h = new HoldRepeat();
            h.Press("heal_single");
            Assert.That(h.ShouldCast(ScreenState.Playing, Unavailable), Is.False, "recharge ou mana");
        }

        [Test]
        public void Sans_cible_on_ne_repete_pas_le_message_choisissez_un_allie()
        {
            var h = new HoldRepeat();
            h.Press("heal_single");
            Assert.That(h.ShouldCast(ScreenState.Playing, NeedTarget), Is.False);
        }

        [TestCase(ScreenState.Start)]
        [TestCase(ScreenState.Paused)]
        [TestCase(ScreenState.Ended)]
        public void Rien_n_est_lance_hors_du_combat_actif(ScreenState state)
        {
            var h = new HoldRepeat();
            h.Press("heal_single");
            Assert.That(h.ShouldCast(state, Cast), Is.False);
        }

        [Test]
        public void Relacher_arrete_la_repetition()
        {
            var h = new HoldRepeat();
            h.Press("heal_single");
            h.Release("heal_single");
            Assert.That(h.HeldSkillId, Is.Null);
            Assert.That(h.ShouldCast(ScreenState.Playing, Cast), Is.False);
        }

        [Test]
        public void Le_dernier_appui_remplace_le_precedent()
        {
            var h = new HoldRepeat();
            h.Press("heal_single");
            h.Press("shield");
            Assert.That(h.HeldSkillId, Is.EqualTo("shield"));
        }

        [Test]
        public void Relacher_l_ancienne_touche_ne_coupe_pas_la_nouvelle()
        {
            var h = new HoldRepeat();
            h.Press("heal_single");
            h.Press("shield");
            h.Release("heal_single");
            Assert.That(h.HeldSkillId, Is.EqualTo("shield"));
            h.Release("shield");
            Assert.That(h.HeldSkillId, Is.Null);
        }

        [Test]
        public void Relacher_un_sort_jamais_appuye_ne_fait_rien()
        {
            var h = new HoldRepeat();
            Assert.DoesNotThrow(() => h.Release("purge"));
            Assert.That(h.HeldSkillId, Is.Null);
        }

        [Test]
        public void Tout_relacher_est_toujours_permis()
        {
            var h = new HoldRepeat();
            h.Press("purge");
            h.ReleaseAll();
            Assert.That(h.HeldSkillId, Is.Null);
            Assert.DoesNotThrow(() => h.ReleaseAll());
        }

        [Test]
        public void Appuyer_deux_fois_de_suite_sur_le_meme_sort_est_idempotent()
        {
            var h = new HoldRepeat();
            h.Press("heal_single");
            h.Press("heal_single");
            Assert.That(h.HeldSkillId, Is.EqualTo("heal_single"));
            h.Release("heal_single");
            Assert.That(h.HeldSkillId, Is.Null);
        }
    }

    /// <summary>La répétition, simulée avec un vrai combat, se comporte comme la règle l'annonce.</summary>
    public class HoldRepeatSimulationTests
    {
        private static readonly GameContent C = Fixtures.FullContent();

        /// <summary>Joue en maintenant un sort de la seconde 0 jusqu'à la fin, une décision par image de 100 ms.</summary>
        private static (Battle battle, List<double> castTimes) Hold(string skillId, string? target, string boss = "boss1", uint seed = 1, double untilMs = 30000)
        {
            var battle = new Battle(C.CreateEncounter(boss, seed));
            var selection = new TargetSelection();
            if (target != null) selection.Tap(target, true);
            var hold = new HoldRepeat();
            hold.Press(skillId);
            var skill = C.Skills.Single(s => s.Id == skillId);
            var casts = new List<double>();
            battle.Subscribe(e => { if (e.Type == "skillUsed") casts.Add(e.TimeMs); });
            for (double t = 0; t < untilMs && battle.GetResult() == BattleResults.Ongoing; t += 100)
            {
                selection.Sync(battle.GetAllies().Where(a => a.Alive).Select(a => a.Id));
                var resolution = selection.Resolve(skill.Target == "all", battle.CanUseSkillNow("healer", skillId));
                if (hold.ShouldCast(ScreenState.Playing, resolution))
                    battle.IssueCommand(new Command { TimeMs = battle.GetClock(), SkillId = skillId, TargetId = resolution.TargetId });
                battle.Step(100);
            }
            return (battle, casts);
        }

        [Test]
        public void Maintenir_le_Soin_sur_une_cible_l_incante_bout_a_bout_sans_jamais_depasser_le_temps_d_incantation()
        {
            var (_, casts) = Hold("heal_single", "tank", untilMs: 10000);
            double castMs = C.Skills.Single(s => s.Id == "heal_single").CastMs;
            Assert.That(castMs, Is.GreaterThan(0), "le Soin de base s'incante");
            Assert.That(casts.Count, Is.GreaterThanOrEqualTo(6));
            for (int i = 1; i < casts.Count; i++) Assert.That(casts[i] - casts[i - 1], Is.GreaterThanOrEqualTo(castMs - 1e-6), "une incantation à la fois");
            Assert.That(casts.Count, Is.LessThanOrEqualTo(10000 / castMs + 1));
        }

        [Test]
        public void Le_premier_lancer_demarre_tout_de_suite_et_aboutit_apres_l_incantation()
        {
            var (_, casts) = Hold("heal_single", "tank", untilMs: 3000);
            double castMs = C.Skills.Single(s => s.Id == "heal_single").CastMs;
            Assert.That(casts[0], Is.InRange(castMs, castMs + 200));
        }

        [Test]
        public void Sans_cible_un_sort_cible_maintenu_ne_lance_rien()
        {
            var (_, casts) = Hold("heal_single", null, untilMs: 10000);
            Assert.That(casts, Is.Empty);
        }

        [Test]
        public void Un_sort_de_zone_maintenu_se_lance_sans_cible()
        {
            var (_, casts) = Hold("heal_aoe", null, untilMs: 12000);
            Assert.That(casts.Count, Is.GreaterThanOrEqualTo(2));
            for (int i = 1; i < casts.Count; i++) Assert.That(casts[i] - casts[i - 1], Is.GreaterThanOrEqualTo(5000 - 1e-6));
        }

        [Test]
        public void La_repetition_s_arrete_quand_le_mana_manque_et_reprend_avec_la_regeneration()
        {
            var (battle, casts) = Hold("heal_aoe", null, untilMs: 40000);
            // 45 mana par soin de zone, 100 de mana, +6/s : le mana ne suffit pas à tout enchaîner sur 40 s.
            Assert.That(casts.Count, Is.LessThan(40000 / 5000));
            Assert.That(casts.Count, Is.GreaterThan(2));
            Assert.That(battle.GetAllies().Single(a => a.Id == "healer").Mana, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Si_la_cible_tombe_la_repetition_du_soin_cible_s_arrete()
        {
            var battle = new Battle(C.CreateEncounter("boss1", 1));
            var selection = new TargetSelection();
            selection.Tap("dps2", true);
            var hold = new HoldRepeat();
            hold.Press("heal_single");
            int casts = 0;
            battle.Subscribe(e => { if (e.Type == "skillUsed") casts++; });
            bool died = false;
            battle.Subscribe(e => { if (e.Type == "unitDied" && e.UnitId == "dps2") died = true; });
            int castsAtDeath = -1;
            for (double t = 0; t < 120000 && battle.GetResult() == BattleResults.Ongoing; t += 100)
            {
                selection.Sync(battle.GetAllies().Where(a => a.Alive).Select(a => a.Id));
                var resolution = selection.Resolve(false, battle.CanUseSkillNow("healer", "heal_single"));
                if (hold.ShouldCast(ScreenState.Playing, resolution))
                    battle.IssueCommand(new Command { TimeMs = battle.GetClock(), SkillId = "heal_single", TargetId = resolution.TargetId });
                battle.Step(100);
                if (died && castsAtDeath < 0) castsAtDeath = casts;
            }
            if (died) Assert.That(casts - castsAtDeath, Is.LessThanOrEqualTo(1), "aucun soin de plus après la mort de la cible");
            Assert.That(selection.Selected, died ? Is.Null : Is.EqualTo("dps2"));
        }

        [TestCase("boss1")]
        [TestCase("boss2")]
        [TestCase("boss3")]
        public void Maintenir_un_seul_sort_ne_suffit_pas_a_gagner(string boss)
        {
            // La commodité de maintenir ne doit pas remplacer les décisions (bouclier au bon moment, purge, zone).
            int wins = 0;
            for (uint seed = 1; seed <= 30; seed++)
            {
                var (battle, _) = Hold("heal_single", "tank", boss, seed, untilMs: 150000);
                if (battle.GetResult() == BattleResults.Victory) wins++;
            }
            Assert.That(wins, Is.LessThanOrEqualTo(1), boss);
        }
    }

    /// <summary>Dégâts infligés au boss par chaque membre de l'équipe (bilan de combat).</summary>
    public class DamageByAllyTests
    {
        private static CombatStats Played(uint seed, string boss = "boss1")
        {
            var c = Fixtures.FullContent();
            var battle = new Battle(c.CreateEncounter(boss, seed));
            var stats = new CombatStats();
            stats.Attach(battle);
            ReferenceHealerBot.Run(battle, 150000);
            return stats;
        }

        [Test]
        public void Le_total_par_membre_est_le_total_des_degats_au_boss()
        {
            var s = Played(7);
            Assert.That(s.DamageByAlly.Values.Sum(), Is.EqualTo(s.DamageToBoss).Within(1e-6));
        }

        [Test]
        public void Le_soigneur_inflige_bien_moins_que_les_trois_autres_qui_infligent_tous()
        {
            // D-082 : le soigneur attaque désormais lui aussi entre ses incantations, mais son rôle reste le soin :
            // sa contribution aux dégâts doit rester nettement la plus faible de l'équipe, pas comparable aux trois autres.
            var s = Played(7);
            foreach (var id in new[] { "tank", "dps1", "dps2", "healer" }) Assert.That(s.DamageByAlly[id], Is.GreaterThan(0), id);
            double weakestDps = new[] { s.DamageByAlly["tank"], s.DamageByAlly["dps1"], s.DamageByAlly["dps2"] }.Min();
            Assert.That(s.DamageByAlly["healer"], Is.LessThan(weakestDps / 2), "le soigneur ne doit pas rivaliser avec les dps");
        }

        [Test]
        public void Le_mage_inflige_plus_que_l_archere_qui_inflige_plus_que_le_garde()
        {
            var s = Played(7);
            Assert.That(s.DamageByAlly["dps2"], Is.GreaterThan(s.DamageByAlly["dps1"]));
            Assert.That(s.DamageByAlly["dps1"], Is.GreaterThan(s.DamageByAlly["tank"]));
        }

        [Test]
        public void Les_parts_sont_entre_zero_et_un_et_totalisent_un()
        {
            var s = Played(7);
            double sum = 0;
            foreach (var id in new[] { "tank", "dps1", "dps2", "healer" })
            {
                double share = s.DamageShare(id);
                Assert.That(share, Is.InRange(0, 1), id);
                sum += share;
            }
            Assert.That(sum, Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void Sans_aucun_degat_toutes_les_parts_valent_zero()
        {
            var s = new CombatStats();
            Assert.That(s.DamageShare("tank"), Is.EqualTo(0));
            Assert.That(s.DamageByAlly, Is.Empty);
        }

        [Test]
        public void Un_allie_inconnu_a_une_part_nulle() => Assert.That(Played(7).DamageShare("fantome"), Is.EqualTo(0));

        [Test]
        public void Les_evenements_cumulent_les_degats_de_chaque_source()
        {
            var s = new CombatStats();
            s.Apply(new BattleEvent { Type = "bossDamaged", SourceId = "tank", Amount = 10 });
            s.Apply(new BattleEvent { Type = "bossDamaged", SourceId = "dps1", Amount = 30 });
            s.Apply(new BattleEvent { Type = "bossDamaged", SourceId = "tank", Amount = 20 });
            Assert.That(s.DamageByAlly["tank"], Is.EqualTo(30));
            Assert.That(s.DamageByAlly["dps1"], Is.EqualTo(30));
            Assert.That(s.DamageShare("tank"), Is.EqualTo(0.5).Within(1e-9));
        }

        [Test]
        public void Un_allie_mort_tot_inflige_moins_que_s_il_avait_survecu()
        {
            var c = Fixtures.FullContent();
            double Damage(bool healer)
            {
                var b = new Battle(c.CreateEncounter("boss1", 3));
                var s = new CombatStats(); s.Attach(b);
                if (healer) ReferenceHealerBot.Run(b, 150000); else b.Run(150000);
                return s.DamageByAlly.TryGetValue("dps2", out var d) ? d : 0;
            }
            Assert.That(Damage(false), Is.LessThan(Damage(true)));
        }

        [Test]
        public void Les_degats_par_membre_sont_identiques_pour_une_meme_graine()
        {
            var a = Played(11); var b = Played(11);
            Assert.That(a.DamageByAlly.OrderBy(k => k.Key).Select(k => (k.Key, k.Value)), Is.EqualTo(b.DamageByAlly.OrderBy(k => k.Key).Select(k => (k.Key, k.Value))));
        }
    }

    public class EndScreenLayoutTests
    {
        [Test]
        public void Les_deux_panneaux_du_bilan_tiennent_dans_l_ecran_sans_se_chevaucher()
        {
            Assert.That(Layout.EndStatsPanel.InsideScreen(), Is.True);
            Assert.That(Layout.EndDamagePanel.InsideScreen(), Is.True);
            Assert.That(Layout.EndStatsPanel.Overlaps(Layout.EndDamagePanel), Is.False);
        }

        [Test]
        public void Les_panneaux_sont_au_dessus_des_boutons_de_fin_de_combat()
        {
            foreach (var b in Layout.EndButtonRects(3))
            {
                Assert.That(Layout.EndStatsPanel.Overlaps(b), Is.False);
                Assert.That(Layout.EndDamagePanel.Overlaps(b), Is.False);
            }
        }

        [Test]
        public void Les_panneaux_sont_alignes_et_centres_sur_l_ecran()
        {
            Assert.That(Layout.EndStatsPanel.Y, Is.EqualTo(Layout.EndDamagePanel.Y));
            Assert.That(Layout.EndStatsPanel.H, Is.EqualTo(Layout.EndDamagePanel.H));
            double left = Layout.EndStatsPanel.X, right = Layout.GameW - Layout.EndDamagePanel.Right;
            Assert.That(left, Is.EqualTo(right).Within(0.001));
        }

        [Test]
        public void Le_panneau_de_degats_peut_afficher_quatre_membres_lisiblement()
        {
            // Titre + quatre lignes de 40 px.
            Assert.That(Layout.EndDamagePanel.H, Is.GreaterThanOrEqualTo(36 + 4 * 40));
        }
    }
}
