using System;
using System.Collections.Generic;
using System.Linq;
using Healer.Combat.Presentation;
using Healer.Combat.Progress;
using Healer.Ui;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Quels événements du combat font quel son.</summary>
    public class AudioCuesTests
    {
        private static CueRequest? Cue(string type, Action<BattleEvent>? setup = null)
        {
            var e = new BattleEvent { Type = type };
            setup?.Invoke(e);
            return AudioCues.ForEvent(e);
        }

        [Test]
        public void Un_soin_qui_rend_des_PV_fait_le_son_de_soin_un_soin_a_vide_reste_muet()
        {
            Assert.That(Cue("healed", e => e.Amount = 100)!.Value.Cue, Is.EqualTo(SoundCue.Heal));
            Assert.That(Cue("healed", e => e.Amount = 0), Is.Null);
        }

        [Test]
        public void Un_coup_qui_touche_fait_un_bruit_un_coup_entierement_absorbe_non()
        {
            Assert.That(Cue("unitDamaged", e => e.Amount = 30)!.Value.Cue, Is.EqualTo(SoundCue.Hit));
            Assert.That(Cue("unitDamaged", e => { e.Amount = 0; e.Absorbed = 30; }), Is.Null);
        }

        [Test]
        public void Seule_une_purge_fait_le_son_de_purge_pas_l_expiration_ni_la_mort()
        {
            Assert.That(Cue("effectEnded", e => e.Reason = "cleansed")!.Value.Cue, Is.EqualTo(SoundCue.Purge));
            Assert.That(Cue("effectEnded", e => e.Reason = "expired"), Is.Null);
            Assert.That(Cue("effectEnded", e => e.Reason = "died"), Is.Null);
        }

        [Test]
        public void L_attaque_de_zone_fait_le_grondement_l_attaque_simple_un_tic()
        {
            Assert.That(Cue("bossAction", e => e.Action = "bigAttack")!.Value.Cue, Is.EqualTo(SoundCue.Boom));
            Assert.That(Cue("bossAction", e => e.Action = "attack")!.Value.Cue, Is.EqualTo(SoundCue.BossTick));
            Assert.That(Cue("bossAction", e => e.Action = "poison")!.Value.Cue, Is.EqualTo(SoundCue.BossTick));
        }

        [Test]
        public void La_fin_du_combat_fait_le_bon_theme()
        {
            Assert.That(Cue("battleEnded", e => e.Result = BattleResults.Victory)!.Value.Cue, Is.EqualTo(SoundCue.Victory));
            Assert.That(Cue("battleEnded", e => e.Result = BattleResults.Defeat)!.Value.Cue, Is.EqualTo(SoundCue.Defeat));
        }

        [TestCase("skillUsed", SoundCue.Cast)]
        [TestCase("shielded", SoundCue.Shield)]
        [TestCase("effectApplied", SoundCue.Poison)]
        [TestCase("unitDied", SoundCue.Death)]
        [TestCase("bossPhaseChanged", SoundCue.Roar)]
        [TestCase("bossEnraged", SoundCue.Enrage)]
        public void Chaque_evenement_important_a_son_son(string type, SoundCue expected) =>
            Assert.That(Cue(type)!.Value.Cue, Is.EqualTo(expected));

        [TestCase("bossDamaged")]
        [TestCase("effectTick")]
        [TestCase("inconnu")]
        [TestCase("")]
        public void Les_evenements_sans_son_ne_font_rien(string type) => Assert.That(Cue(type), Is.Null);

        [Test]
        public void Tous_les_volumes_sont_dans_l_intervalle_utile()
        {
            foreach (var type in new[] { "skillUsed", "healed", "shielded", "effectEnded", "effectApplied", "unitDamaged", "unitDied", "bossAction", "bossPhaseChanged", "bossEnraged", "battleEnded" })
            {
                var c = Cue(type, e => { e.Amount = 10; e.Reason = "cleansed"; e.Action = "bigAttack"; e.Result = BattleResults.Victory; });
                Assert.That(c, Is.Not.Null, type);
                Assert.That(c!.Value.Volume, Is.GreaterThan(0).And.LessThanOrEqualTo(1), type);
            }
        }

        [Test]
        public void Le_grondement_de_zone_est_plus_fort_que_le_tic_d_attaque()
        {
            Assert.That(Cue("bossAction", e => e.Action = "bigAttack")!.Value.Volume, Is.GreaterThan(Cue("bossAction", e => e.Action = "attack")!.Value.Volume));
        }

        [Test]
        public void Un_vrai_combat_produit_les_sons_attendus_et_le_dernier_boss_le_rugissement_d_enrage()
        {
            var c = Fixtures.FullContent();
            var seen = new HashSet<SoundCue>();
            var battle = new Battle(c.CreateEncounter("boss3", 7));
            battle.Subscribe(e => { var r = AudioCues.ForEvent(e); if (r != null) seen.Add(r.Value.Cue); });
            ReferenceHealerBot.Run(battle, 150000);
            foreach (var cue in new[] { SoundCue.Cast, SoundCue.Heal, SoundCue.Shield, SoundCue.Purge, SoundCue.Hit, SoundCue.BossTick, SoundCue.Boom, SoundCue.Poison, SoundCue.Enrage, SoundCue.Roar, SoundCue.Victory })
                Assert.That(seen, Does.Contain(cue), cue.ToString());
        }

        [Test]
        public void Le_meme_evenement_donne_toujours_le_meme_son()
        {
            var e = new BattleEvent { Type = "healed", Amount = 50 };
            Assert.That(AudioCues.ForEvent(e)!.Value.Cue, Is.EqualTo(AudioCues.ForEvent(e)!.Value.Cue));
        }
    }

    public class CueLimiterTests
    {
        [Test]
        public void Le_meme_son_deux_fois_trop_vite_n_est_joue_qu_une_fois()
        {
            var l = new CueLimiter();
            Assert.That(l.TryPlay(SoundCue.Heal, 1.000), Is.True);
            Assert.That(l.TryPlay(SoundCue.Heal, 1.020), Is.False);
        }

        [Test]
        public void Apres_l_intervalle_le_son_peut_etre_rejoue()
        {
            var l = new CueLimiter();
            l.TryPlay(SoundCue.Heal, 1.0);
            Assert.That(l.TryPlay(SoundCue.Heal, 1.0 + CueLimiter.MinIntervalSec(SoundCue.Heal)), Is.True);
        }

        [Test]
        public void Deux_sons_differents_ne_se_gênent_pas()
        {
            var l = new CueLimiter();
            Assert.That(l.TryPlay(SoundCue.Heal, 2.0), Is.True);
            Assert.That(l.TryPlay(SoundCue.Shield, 2.0), Is.True);
            Assert.That(l.TryPlay(SoundCue.Hit, 2.0), Is.True);
        }

        [Test]
        public void Un_soin_de_zone_qui_touche_quatre_allies_ne_fait_qu_un_son()
        {
            var l = new CueLimiter();
            int played = 0;
            for (int i = 0; i < 4; i++) if (l.TryPlay(SoundCue.Heal, 5.0)) played++;
            Assert.That(played, Is.EqualTo(1));
        }

        [Test]
        public void Les_themes_de_fin_et_les_rugissements_ont_un_long_delai()
        {
            foreach (var cue in new[] { SoundCue.Victory, SoundCue.Defeat, SoundCue.Roar, SoundCue.Enrage })
                Assert.That(CueLimiter.MinIntervalSec(cue), Is.GreaterThanOrEqualTo(1.0), cue.ToString());
        }

        [Test]
        public void Le_tic_du_boss_est_moins_frequent_que_les_coups()
        {
            Assert.That(CueLimiter.MinIntervalSec(SoundCue.BossTick), Is.GreaterThan(CueLimiter.MinIntervalSec(SoundCue.Hit)));
        }

        [Test]
        public void Un_temps_qui_recule_ne_bloque_pas_le_son()
        {
            var l = new CueLimiter();
            l.TryPlay(SoundCue.Heal, 100);
            Assert.That(l.TryPlay(SoundCue.Heal, 1), Is.True, "redémarrage d'un combat : l'horloge repart de zéro");
        }

        [Test]
        public void Reinitialiser_libere_tous_les_sons()
        {
            var l = new CueLimiter();
            l.TryPlay(SoundCue.Heal, 1.0);
            l.Reset();
            Assert.That(l.TryPlay(SoundCue.Heal, 1.0), Is.True);
        }

        [Test]
        public void Dans_un_vrai_combat_le_limiteur_reduit_les_sons_sans_les_supprimer()
        {
            var c = Fixtures.FullContent();
            var battle = new Battle(c.CreateEncounter("boss1", 7));
            var limiter = new CueLimiter();
            int raw = 0, played = 0;
            battle.Subscribe(e =>
            {
                var r = AudioCues.ForEvent(e);
                if (r == null) return;
                raw++;
                if (limiter.TryPlay(r.Value.Cue, e.TimeMs / 1000.0)) played++;
            });
            ReferenceHealerBot.Run(battle, 150000);
            Assert.That(played, Is.GreaterThan(0));
            Assert.That(played, Is.LessThan(raw), "les soins de zone et les coups groupés sont fusionnés");
        }

        [TestCase(SoundCue.Heal)]
        [TestCase(SoundCue.Boom)]
        [TestCase(SoundCue.Click)]
        public void Chaque_son_a_un_intervalle_strictement_positif(SoundCue cue) =>
            Assert.That(CueLimiter.MinIntervalSec(cue), Is.GreaterThan(0));
    }

    public class MixTests
    {
        [Test]
        public void Coupe_le_son_donne_un_gain_nul_quel_que_soit_le_volume() => Assert.That(Mix.Gain(100, true), Is.EqualTo(0));

        [TestCase(0, 0.0)]
        [TestCase(50, 0.5)]
        [TestCase(100, 1.0)]
        public void Le_gain_est_le_volume_en_pourcentage(int volume, double gain) => Assert.That(Mix.Gain(volume, false), Is.EqualTo(gain).Within(1e-9));

        [TestCase(-20)]
        [TestCase(250)]
        public void Un_volume_hors_borne_est_borne(int volume) => Assert.That(Mix.Gain(volume, false), Is.InRange(0.0, 1.0));

        [TestCase(50, 1, 60)]
        [TestCase(50, -1, 40)]
        [TestCase(100, 1, 100)]
        [TestCase(0, -1, 0)]
        [TestCase(0, 1, 10)]
        public void Un_reglage_change_d_un_pas_et_reste_entre_0_et_100(int current, int direction, int expected) =>
            Assert.That(Mix.Adjust(current, direction), Is.EqualTo(expected));

        [Test]
        public void Un_volume_qui_n_est_pas_un_multiple_de_10_est_ramene_au_pas_le_plus_proche()
        {
            Assert.That(Mix.Adjust(53, 1), Is.EqualTo(60));
            Assert.That(Mix.Adjust(57, -1), Is.EqualTo(50));
        }

        [Test]
        public void Une_direction_nulle_ne_change_que_l_alignement()
        {
            Assert.That(Mix.Adjust(50, 0), Is.EqualTo(50));
        }

        [Test]
        public void Toutes_les_valeurs_atteignables_sont_des_multiples_de_10()
        {
            int v = 0;
            for (int i = 0; i < 30; i++) { v = Mix.Adjust(v, 1); Assert.That(v % 10, Is.EqualTo(0)); Assert.That(v, Is.InRange(0, 100)); }
            Assert.That(v, Is.EqualTo(100));
            for (int i = 0; i < 30; i++) v = Mix.Adjust(v, -1);
            Assert.That(v, Is.EqualTo(0));
        }
    }

    /// <summary>Musique adaptative : intensité et couches.</summary>
    public class MusicDirectorTests
    {
        private static MusicInput Calm() => new MusicInput { InCombat = true, LowestAllyRatio = 1 };

        [Test]
        public void Hors_combat_l_intensite_est_nulle()
        {
            var s = Calm(); s.InCombat = false; s.LowestAllyRatio = 0;
            Assert.That(MusicDirector.Intensity(s), Is.EqualTo(0));
        }

        [Test]
        public void En_combat_tranquille_l_intensite_est_faible_mais_pas_nulle()
        {
            Assert.That(MusicDirector.Intensity(Calm()), Is.InRange(0.15, 0.35));
        }

        [Test]
        public void Plus_un_allie_est_bas_plus_la_musique_monte()
        {
            double prev = -1;
            foreach (double ratio in new[] { 1.0, 0.8, 0.6, 0.4, 0.2, 0.05 })
            {
                var s = Calm(); s.LowestAllyRatio = ratio;
                double i = MusicDirector.Intensity(s);
                Assert.That(i, Is.GreaterThan(prev), "ratio " + ratio);
                prev = i;
            }
        }

        [Test]
        public void Une_attaque_annoncee_une_phase_avancee_et_l_enrage_ajoutent_de_l_intensite()
        {
            double baseI = MusicDirector.Intensity(Calm());
            var a = Calm(); a.AttackTelegraphed = true;
            var b = Calm(); b.BossPhase = 2;
            var c = Calm(); c.EnrageLevel = 3;
            Assert.That(MusicDirector.Intensity(a), Is.GreaterThan(baseI));
            Assert.That(MusicDirector.Intensity(b), Is.GreaterThan(baseI));
            Assert.That(MusicDirector.Intensity(c), Is.GreaterThan(baseI));
        }

        [Test]
        public void L_intensite_ne_depasse_jamais_un_meme_dans_le_pire_cas()
        {
            var s = new MusicInput { InCombat = true, LowestAllyRatio = 0, AttackTelegraphed = true, BossPhase = 99, EnrageLevel = 99 };
            Assert.That(MusicDirector.Intensity(s), Is.EqualTo(1));
        }

        [TestCase(-5.0)]
        [TestCase(3.0)]
        public void Un_ratio_de_PV_absurde_ne_sort_pas_de_l_intervalle(double ratio)
        {
            var s = Calm(); s.LowestAllyRatio = ratio;
            Assert.That(MusicDirector.Intensity(s), Is.InRange(0.0, 1.0));
        }

        [Test]
        public void Dans_les_menus_seule_la_nappe_joue()
        {
            var v = MusicDirector.LayerVolumes(0, false);
            Assert.That(v[0], Is.GreaterThan(0));
            Assert.That(v.Skip(1), Is.All.EqualTo(0));
        }

        [Test]
        public void Les_couches_arrivent_dans_l_ordre_pulsation_melodie_alarme()
        {
            double Threshold(int layer) { for (double i = 0; i <= 1.0001; i += 0.01) if (MusicDirector.LayerVolumes(i, true)[layer] > 0.01) return i; return 2; }
            Assert.That(Threshold(1), Is.LessThan(Threshold(2)));
            Assert.That(Threshold(2), Is.LessThan(Threshold(3)));
        }

        [Test]
        public void Au_calme_seule_la_nappe_joue_au_danger_maximal_toutes_les_couches_jouent()
        {
            var calm = MusicDirector.LayerVolumes(0.2, true);
            Assert.That(calm[0], Is.GreaterThan(0));
            Assert.That(calm.Skip(1), Is.All.EqualTo(0));
            Assert.That(MusicDirector.LayerVolumes(1, true), Is.All.GreaterThan(0));
        }

        [Test]
        public void Le_volume_de_chaque_couche_ne_diminue_jamais_quand_le_danger_augmente()
        {
            for (int layer = 0; layer < MusicDirector.Layers; layer++)
            {
                double prev = -1;
                for (double i = 0; i <= 1.0001; i += 0.02)
                {
                    double v = MusicDirector.LayerVolumes(i, true)[layer];
                    Assert.That(v, Is.GreaterThanOrEqualTo(prev - 1e-9), $"couche {layer} à {i:0.00}");
                    Assert.That(v, Is.InRange(0.0, 1.0));
                    prev = v;
                }
            }
        }

        [Test]
        public void Le_nombre_de_couches_est_celui_annonce()
        {
            Assert.That(MusicDirector.LayerVolumes(0.5, true), Has.Length.EqualTo(MusicDirector.Layers));
            Assert.That(MusicDirector.LayerVolumes(0.5, false), Has.Length.EqualTo(MusicDirector.Layers));
        }

        [Test]
        public void Le_lissage_converge_vers_la_cible_sans_depassement()
        {
            double v = 0;
            for (int i = 0; i < 600; i++)
            {
                double next = MusicDirector.Smooth(v, 0.8, 1 / 60.0);
                Assert.That(next, Is.GreaterThanOrEqualTo(v));
                Assert.That(next, Is.LessThanOrEqualTo(0.8 + 1e-9));
                v = next;
            }
            Assert.That(v, Is.EqualTo(0.8).Within(0.01));
        }

        [Test]
        public void Le_danger_se_ressent_vite_le_calme_revient_lentement()
        {
            double up = 0, down = 1;
            for (int i = 0; i < 60; i++) { up = MusicDirector.Smooth(up, 1, 1 / 60.0); down = MusicDirector.Smooth(down, 0, 1 / 60.0); }
            Assert.That(up, Is.GreaterThan(1 - down), "en une seconde, la montée avance plus que la descente");
        }

        [Test]
        public void Sans_ecoulement_du_temps_la_valeur_ne_bouge_pas()
        {
            Assert.That(MusicDirector.Smooth(0.3, 1, 0), Is.EqualTo(0.3));
        }

        [Test]
        public void Un_tres_long_delai_atteint_la_cible_sans_depasser()
        {
            Assert.That(MusicDirector.Smooth(0, 0.7, 1000), Is.EqualTo(0.7).Within(1e-6));
        }

        [Test]
        public void Dans_un_vrai_combat_l_intensite_reste_bornee_et_monte_avec_le_danger()
        {
            var c = Fixtures.FullContent();
            var battle = new Battle(c.CreateEncounter("boss3", 7));
            double min = 1, max = 0;
            for (double t = 0; t < 150000 && battle.GetResult() == BattleResults.Ongoing; t += 100)
            {
                if (t % 500 == 0) ReferenceHealerBot.Decide(battle, t, true);
                battle.Step(100);
                var allies = battle.GetAllies().Where(a => a.Alive).ToList();
                var tele = battle.GetTelegraph();
                double i = MusicDirector.Intensity(new MusicInput
                {
                    InCombat = true,
                    LowestAllyRatio = allies.Count == 0 ? 0 : allies.Min(a => a.Hp / a.MaxHp),
                    AttackTelegraphed = tele != null && tele.Type == "bigAttack",
                    BossPhase = battle.GetBossPhase().Index,
                    EnrageLevel = battle.GetEnrageLevel(),
                });
                Assert.That(i, Is.InRange(0.0, 1.0));
                min = Math.Min(min, i); max = Math.Max(max, i);
            }
            Assert.That(max - min, Is.GreaterThan(0.2), "la musique doit réellement bouger pendant un combat");
        }
    }

    /// <summary>Attitude des unités pilotée par les événements.</summary>
    public class UnitAnimatorTests
    {
        private static BattleEvent Attack(double t, string source) => new BattleEvent { Type = "bossDamaged", TimeMs = t, SourceId = source, Amount = 30 };
        private static BattleEvent Damaged(double t, string unit, double amount = 30) => new BattleEvent { Type = "unitDamaged", TimeMs = t, UnitId = unit, Amount = amount };

        [Test]
        public void Au_repos_toutes_les_valeurs_sont_nulles()
        {
            var p = new UnitAnimator("tank").Sample(1000);
            Assert.That((p.Lunge, p.Recoil, p.Cast, p.Glow, p.Fall), Is.EqualTo((0.0, 0.0, 0.0, 0.0, 0.0)));
        }

        [Test]
        public void Une_attaque_lance_un_elan_qui_monte_puis_retombe()
        {
            var a = new UnitAnimator("dps1");
            a.OnEvent(Attack(1000, "dps1"));
            Assert.That(a.Sample(1000).Lunge, Is.EqualTo(0).Within(1e-9));
            double mid = a.Sample(1000 + UnitAnimator.LungeMs / 2).Lunge;
            Assert.That(mid, Is.EqualTo(1).Within(1e-9));
            Assert.That(a.Sample(1000 + UnitAnimator.LungeMs).Lunge, Is.EqualTo(0));
            Assert.That(a.Sample(1000 + UnitAnimator.LungeMs + 500).Lunge, Is.EqualTo(0));
        }

        [Test]
        public void Seul_l_allie_qui_frappe_avance()
        {
            var a = new UnitAnimator("tank");
            a.OnEvent(Attack(1000, "dps2"));
            Assert.That(a.Sample(1000 + UnitAnimator.LungeMs / 2).Lunge, Is.EqualTo(0));
        }

        [Test]
        public void Un_coup_recu_donne_un_recul_immediat_qui_s_eteint()
        {
            var a = new UnitAnimator("tank");
            a.OnEvent(Damaged(2000, "tank"));
            Assert.That(a.Sample(2000).Recoil, Is.EqualTo(1).Within(1e-9));
            Assert.That(a.Sample(2000 + UnitAnimator.RecoilMs / 2).Recoil, Is.InRange(0.01, 0.99));
            Assert.That(a.Sample(2000 + UnitAnimator.RecoilMs).Recoil, Is.EqualTo(0));
        }

        [Test]
        public void Un_coup_entierement_absorbe_ne_fait_pas_reculer()
        {
            var a = new UnitAnimator("tank");
            a.OnEvent(Damaged(2000, "tank", 0));
            Assert.That(a.Sample(2000).Recoil, Is.EqualTo(0));
        }

        [Test]
        public void Le_coup_recu_par_un_autre_allie_ne_me_fait_pas_reculer()
        {
            var a = new UnitAnimator("tank");
            a.OnEvent(Damaged(2000, "dps1"));
            Assert.That(a.Sample(2000).Recoil, Is.EqualTo(0));
        }

        [Test]
        public void Le_soigneur_leve_le_baton_quand_il_lance_un_sort()
        {
            var a = new UnitAnimator("healer");
            a.OnEvent(new BattleEvent { Type = "skillUsed", TimeMs = 500, CasterId = "healer" });
            Assert.That(a.Sample(500 + UnitAnimator.CastMs / 2).Cast, Is.EqualTo(1).Within(1e-9));
            Assert.That(a.Sample(500 + UnitAnimator.CastMs + 1).Cast, Is.EqualTo(0));
        }

        [Test]
        public void Un_allie_soigne_ou_protege_brille_puis_s_eteint()
        {
            foreach (var type in new[] { "healed", "shielded" })
            {
                var a = new UnitAnimator("tank");
                a.OnEvent(new BattleEvent { Type = type, TimeMs = 100, UnitId = "tank", Amount = 50 });
                Assert.That(a.Sample(100).Glow, Is.EqualTo(1).Within(1e-9), type);
                Assert.That(a.Sample(100 + UnitAnimator.GlowMs).Glow, Is.EqualTo(0), type);
            }
        }

        [Test]
        public void Une_unite_qui_meurt_tombe_progressivement_et_reste_a_terre()
        {
            var a = new UnitAnimator("dps2");
            a.OnEvent(new BattleEvent { Type = "unitDied", TimeMs = 3000, UnitId = "dps2" });
            Assert.That(a.Sample(2999).Fall, Is.EqualTo(0));
            Assert.That(a.Sample(3000).Fall, Is.EqualTo(0).Within(1e-9));
            double half = a.Sample(3000 + UnitAnimator.FallMs / 2).Fall;
            Assert.That(half, Is.InRange(0.1, 0.9));
            Assert.That(a.Sample(3000 + UnitAnimator.FallMs).Fall, Is.EqualTo(1).Within(1e-9));
            Assert.That(a.Sample(90000).Fall, Is.EqualTo(1));
        }

        [Test]
        public void La_chute_est_continue_sans_recul_de_l_attitude()
        {
            var a = new UnitAnimator("dps2");
            a.OnEvent(new BattleEvent { Type = "unitDied", TimeMs = 0, UnitId = "dps2" });
            double prev = 0;
            for (double t = 0; t <= UnitAnimator.FallMs; t += 10)
            {
                double f = a.Sample(t).Fall;
                Assert.That(f, Is.GreaterThanOrEqualTo(prev - 1e-9));
                prev = f;
            }
        }

        [Test]
        public void Le_boss_avance_quand_il_attaque_et_recule_quand_il_est_touche()
        {
            var b = new UnitAnimator("boss", isBoss: true);
            b.OnEvent(new BattleEvent { Type = "bossAction", TimeMs = 5000, Action = "attack" });
            b.OnEvent(Attack(6000, "dps1"));
            Assert.That(b.Sample(5000 + UnitAnimator.LungeMs / 2).Lunge, Is.EqualTo(1).Within(1e-9));
            Assert.That(b.Sample(6000).Recoil, Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void Le_boss_ne_reagit_pas_aux_evenements_des_allies()
        {
            var b = new UnitAnimator("boss", isBoss: true);
            b.OnEvent(new BattleEvent { Type = "skillUsed", TimeMs = 100, CasterId = "boss" });
            b.OnEvent(new BattleEvent { Type = "healed", TimeMs = 100, UnitId = "boss", Amount = 5 });
            b.OnEvent(new BattleEvent { Type = "unitDied", TimeMs = 100, UnitId = "boss" });
            var p = b.Sample(200);
            Assert.That((p.Cast, p.Glow, p.Fall), Is.EqualTo((0.0, 0.0, 0.0)));
        }

        [Test]
        public void Reinitialiser_remet_l_unite_debout()
        {
            var a = new UnitAnimator("dps1");
            a.OnEvent(new BattleEvent { Type = "unitDied", TimeMs = 10, UnitId = "dps1" });
            a.Reset();
            Assert.That(a.Sample(99999).Fall, Is.EqualTo(0));
        }

        [Test]
        public void Avant_l_evenement_rien_ne_se_passe_meme_si_le_temps_recule()
        {
            var a = new UnitAnimator("tank");
            a.OnEvent(Damaged(5000, "tank"));
            Assert.That(a.Sample(4000).Recoil, Is.EqualTo(0));
        }

        [Test]
        public void Toutes_les_valeurs_restent_entre_zero_et_un_sur_un_vrai_combat_et_sont_deterministes()
        {
            List<double> Trace()
            {
                var c = Fixtures.FullContent();
                var battle = new Battle(c.CreateEncounter("boss2", 5));
                var animators = battle.GetAllies().Select(u => new UnitAnimator(u.Id)).Concat(new[] { new UnitAnimator("boss", true) }).ToList();
                battle.Subscribe(e => { foreach (var a in animators) a.OnEvent(e); });
                var samples = new List<double>();
                for (double t = 0; t < 60000 && battle.GetResult() == BattleResults.Ongoing; t += 100)
                {
                    if (t % 500 == 0) ReferenceHealerBot.Decide(battle, t, true);
                    battle.Step(100);
                    foreach (var a in animators)
                    {
                        var p = a.Sample(battle.GetClock());
                        foreach (var v in new[] { p.Lunge, p.Recoil, p.Cast, p.Glow, p.Fall })
                        {
                            Assert.That(v, Is.InRange(0.0, 1.0));
                            samples.Add(v);
                        }
                    }
                }
                return samples;
            }
            var first = Trace();
            Assert.That(first, Is.Not.Empty);
            Assert.That(first.Any(v => v > 0), Is.True, "il se passe quelque chose à l'écran");
            Assert.That(Trace(), Is.EqualTo(first));
        }
    }

    /// <summary>Réglages : volumes et confort, sauvegardés avec le profil.</summary>
    public class SettingsTests
    {
        private static readonly GameContent C = Fixtures.FullContent();

        [Test]
        public void Les_valeurs_par_defaut_sont_raisonnables()
        {
            var s = new Settings();
            Assert.That(s.MusicVolume, Is.EqualTo(Settings.DefaultMusic));
            Assert.That(s.SfxVolume, Is.EqualTo(Settings.DefaultSfx));
            Assert.That(s.ScreenShake, Is.True);
            Assert.That(s.Muted, Is.False);
            Assert.That(Settings.DefaultMusic, Is.LessThan(Settings.DefaultSfx), "la musique est plus discrète que les effets");
        }

        [Test]
        public void Les_reglages_survivent_a_une_sauvegarde()
        {
            var p = PlayerProfile.NewGame(C);
            p.Settings.MusicVolume = 30; p.Settings.SfxVolume = 100; p.Settings.ScreenShake = false; p.Settings.Muted = true;
            Assert.That(ProfileStore.TryLoad(ProfileStore.ToJson(p), C, out var loaded, out var error), Is.True, error);
            Assert.That((loaded.Settings.MusicVolume, loaded.Settings.SfxVolume, loaded.Settings.ScreenShake, loaded.Settings.Muted), Is.EqualTo((30, 100, false, true)));
        }

        [Test]
        public void Une_ancienne_sauvegarde_sans_ces_reglages_prend_les_valeurs_par_defaut()
        {
            ProfileStore.TryLoad("{\"version\": 1, \"settings\": {\"muted\": true}}", C, out var p, out _);
            Assert.That(p.Settings.Muted, Is.True);
            Assert.That(p.Settings.MusicVolume, Is.EqualTo(Settings.DefaultMusic));
            Assert.That(p.Settings.SfxVolume, Is.EqualTo(Settings.DefaultSfx));
            Assert.That(p.Settings.ScreenShake, Is.True);
        }

        [TestCase(-40, 0)]
        [TestCase(250, 100)]
        [TestCase(70, 70)]
        public void Un_volume_lu_hors_borne_est_ramene_dans_0_100(int stored, int expected)
        {
            ProfileStore.TryLoad("{\"version\": 1, \"settings\": {\"musicVolume\": " + stored + ", \"sfxVolume\": " + stored + "}}", C, out var p, out _);
            Assert.That(p.Settings.MusicVolume, Is.EqualTo(expected));
            Assert.That(p.Settings.SfxVolume, Is.EqualTo(expected));
        }

        [Test]
        public void Le_texte_reste_stable_apres_rechargement()
        {
            var p = PlayerProfile.NewGame(C);
            p.Settings.MusicVolume = 20;
            var json = ProfileStore.ToJson(p);
            ProfileStore.TryLoad(json, C, out var loaded, out _);
            Assert.That(ProfileStore.ToJson(loaded), Is.EqualTo(json));
        }

        [Test]
        public void Couper_le_son_garde_les_volumes_choisis()
        {
            var p = PlayerProfile.NewGame(C);
            p.Settings.MusicVolume = 40;
            p.Settings.Muted = true;
            Assert.That(Mix.Gain(p.Settings.MusicVolume, p.Settings.Muted), Is.EqualTo(0));
            p.Settings.Muted = false;
            Assert.That(Mix.Gain(p.Settings.MusicVolume, p.Settings.Muted), Is.EqualTo(0.4).Within(1e-9));
        }
    }

    public class SettingsScreenTests
    {
        private static void AssertValid(Healer.Ui.Rect r, string what)
        {
            Assert.That(r.InsideScreen(), Is.True, what);
            Assert.That(r.W, Is.GreaterThanOrEqualTo(Layout.MinTouch), what);
            Assert.That(r.H, Is.GreaterThanOrEqualTo(Layout.MinTouch), what);
        }

        [Test]
        public void Les_reglages_s_ouvrent_depuis_le_menu_et_on_revient_au_menu()
        {
            var n = new Navigator();
            Assert.That(n.OpenSettings(), Is.True);
            Assert.That(n.Screen, Is.EqualTo(AppScreen.Settings));
            Assert.That(n.BackToMenu(), Is.True);
            Assert.That(n.Screen, Is.EqualTo(AppScreen.MainMenu));
        }

        [Test]
        public void Les_reglages_ne_s_ouvrent_que_depuis_le_menu_et_ne_lancent_pas_de_niveau()
        {
            var n = new Navigator();
            n.OpenLevelSelect();
            Assert.That(n.OpenSettings(), Is.False);
            n.BackToMenu();
            n.OpenSettings();
            Assert.That(n.StartLevel("l1", true), Is.False);
            Assert.That(n.OpenWorkshop(), Is.False);
            Assert.That(n.OpenLevelSelect(), Is.False);
        }

        [Test]
        public void L_ecran_de_reglages_ne_laisse_passer_que_ses_boutons()
        {
            var allowed = new[] { UiAction.AdjustSetting, UiAction.BackToMenu };
            foreach (UiAction a in Enum.GetValues(typeof(UiAction)))
                Assert.That(InputGate.Allows(AppScreen.Settings, ScreenState.Playing, a), Is.EqualTo(allowed.Contains(a)), a.ToString());
        }

        [Test]
        public void Le_geste_de_reglage_n_est_permis_nulle_part_ailleurs()
        {
            foreach (var screen in new[] { AppScreen.MainMenu, AppScreen.LevelSelect, AppScreen.Workshop })
                Assert.That(InputGate.Allows(screen, ScreenState.Playing, UiAction.AdjustSetting), Is.False, screen.ToString());
            foreach (ScreenState s in Enum.GetValues(typeof(ScreenState)))
                Assert.That(InputGate.Allows(AppScreen.Battle, s, UiAction.AdjustSetting), Is.False, s.ToString());
        }

        [Test]
        public void Le_menu_principal_propose_les_reglages()
        {
            Assert.That(InputGate.Allows(AppScreen.MainMenu, ScreenState.Playing, UiAction.MenuSettings), Is.True);
            Assert.That(InputGate.Allows(AppScreen.Settings, ScreenState.Playing, UiAction.MenuSettings), Is.False);
        }

        [Test]
        public void Les_cinq_boutons_du_menu_sont_valides_centres_et_separes()
        {
            var buttons = new[] { Layout.MenuPlay, Layout.MenuWorkshop, Layout.MenuSettings, Layout.MenuSound, Layout.MenuQuit };
            foreach (var b in buttons)
            {
                AssertValid(b, "bouton du menu");
                Assert.That(b.X + b.W / 2, Is.EqualTo(Layout.GameW / 2).Within(0.001));
            }
            for (int i = 0; i < buttons.Length; i++)
                for (int j = i + 1; j < buttons.Length; j++) Assert.That(buttons[i].Overlaps(buttons[j]), Is.False, $"{i}/{j}");
        }

        [Test]
        public void Les_rangees_de_reglages_tiennent_dans_l_ecran_sans_chevauchement()
        {
            for (int i = 0; i < Layout.SettingsRows; i++)
            {
                Assert.That(Layout.SettingsRow(i).InsideScreen(), Is.True);
                if (i > 0) Assert.That(Layout.SettingsRow(i - 1).Overlaps(Layout.SettingsRow(i)), Is.False);
                Assert.That(Layout.SettingsRow(i).Overlaps(Layout.BackButton), Is.False);
            }
        }

        [Test]
        public void Chaque_rangee_a_deux_boutons_tactiles_de_part_et_d_autre_de_la_barre()
        {
            for (int i = 0; i < Layout.SettingsRows; i++)
            {
                var minus = Layout.SettingsMinus(i); var plus = Layout.SettingsPlus(i); var bar = Layout.SettingsBar(i); var row = Layout.SettingsRow(i);
                AssertValid(minus, "moins"); AssertValid(plus, "plus");
                Assert.That(minus.Overlaps(bar), Is.False); Assert.That(plus.Overlaps(bar), Is.False); Assert.That(minus.Overlaps(plus), Is.False);
                foreach (var r in new[] { minus, plus, bar })
                {
                    Assert.That(r.X, Is.GreaterThanOrEqualTo(row.X)); Assert.That(r.Right, Is.LessThanOrEqualTo(row.Right + 0.001));
                    Assert.That(r.Y, Is.GreaterThanOrEqualTo(row.Y)); Assert.That(r.Bottom, Is.LessThanOrEqualTo(row.Bottom + 0.001));
                }
                Assert.That(minus.Right, Is.LessThan(bar.X)); Assert.That(bar.Right, Is.LessThan(plus.X));
            }
        }

        [Test]
        public void Le_libelle_a_gauche_a_la_place_de_s_ecrire()
        {
            var row = Layout.SettingsRow(0);
            Assert.That(Layout.SettingsMinus(0).X - row.X, Is.GreaterThanOrEqualTo(200));
        }
    }
}

namespace Healer.Combat.Tests
{
    using Healer.Combat.Presentation;

    /// <summary>Animation et sons des nouvelles mécaniques (incantation, critique, esquive).</summary>
    public class MechanicsPresentationTests
    {
        [Test]
        public void Pendant_une_incantation_le_soigneur_garde_le_baton_leve_pendant_tout_le_sort()
        {
            var a = new UnitAnimator("healer");
            a.OnEvent(new BattleEvent { Type = "castStarted", TimeMs = 1000, CasterId = "healer", SkillId = "heal_single", Amount = 1000 });
            Assert.That(a.Sample(1500).Cast, Is.EqualTo(1).Within(1e-9), "mi-incantation : geste au maximum");
            Assert.That(a.Sample(1900).Cast, Is.GreaterThan(0), "encore levé juste avant la fin");
        }

        [Test]
        public void A_l_achevement_du_sort_le_geste_se_termine()
        {
            var a = new UnitAnimator("healer");
            a.OnEvent(new BattleEvent { Type = "castStarted", TimeMs = 1000, CasterId = "healer", SkillId = "heal_single", Amount = 1000 });
            a.OnEvent(new BattleEvent { Type = "skillUsed", TimeMs = 2000, CasterId = "healer", SkillId = "heal_single" });
            Assert.That(a.Sample(2100).Cast, Is.EqualTo(0));
        }

        [Test]
        public void Une_incantation_qui_echoue_baisse_le_baton()
        {
            var a = new UnitAnimator("healer");
            a.OnEvent(new BattleEvent { Type = "castStarted", TimeMs = 1000, CasterId = "healer", SkillId = "heal_single", Amount = 1000 });
            a.OnEvent(new BattleEvent { Type = "castFailed", TimeMs = 1400, CasterId = "healer", SkillId = "heal_single", Reason = "target" });
            Assert.That(a.Sample(1500).Cast, Is.EqualTo(0));
        }

        [Test]
        public void Un_sort_instantane_garde_un_geste_court()
        {
            var a = new UnitAnimator("healer");
            a.OnEvent(new BattleEvent { Type = "skillUsed", TimeMs = 500, CasterId = "healer", SkillId = "shield" });
            Assert.That(a.Sample(500 + UnitAnimator.CastMs / 2).Cast, Is.EqualTo(1).Within(1e-9));
            Assert.That(a.Sample(500 + UnitAnimator.CastMs + 1).Cast, Is.EqualTo(0));
        }

        [Test]
        public void Une_esquive_a_son_son_et_un_critique_aussi()
        {
            Assert.That(AudioCues.ForEvent(new BattleEvent { Type = "unitDodged", UnitId = "tank" })!.Value.Cue, Is.EqualTo(SoundCue.Dodge));
            Assert.That(AudioCues.ForEvent(new BattleEvent { Type = "unitDamaged", Amount = 30, Crit = true })!.Value.Cue, Is.EqualTo(SoundCue.Crit));
            Assert.That(AudioCues.ForEvent(new BattleEvent { Type = "unitDamaged", Amount = 30 })!.Value.Cue, Is.EqualTo(SoundCue.Hit));
            Assert.That(AudioCues.ForEvent(new BattleEvent { Type = "healed", Amount = 30, Crit = true })!.Value.Cue, Is.EqualTo(SoundCue.Crit));
        }

        [Test]
        public void Un_critique_sonne_plus_fort_qu_un_coup_normal()
        {
            var normal = AudioCues.ForEvent(new BattleEvent { Type = "unitDamaged", Amount = 30 })!.Value.Volume;
            var crit = AudioCues.ForEvent(new BattleEvent { Type = "unitDamaged", Amount = 30, Crit = true })!.Value.Volume;
            Assert.That(crit, Is.GreaterThan(normal));
        }
    }
}
