using System;
using System.Collections.Generic;
using System.Linq;
using Healer.Combat.Progress;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Arènes isolées : un boss de test, deux alliés quasi immortels, pour mesurer UNE mécanique à la fois.</summary>
    internal static class Arena
    {
        public static EncounterDef Make(Action<BossDef>? boss = null, Action<CharacterDef, CharacterDef>? allies = null,
            Action<List<SkillDef>>? skills = null, List<EffectDef>? effects = null, uint seed = 1)
        {
            var content = Fixtures.FullContent();
            var b = new BossDef
            {
                Id = "dummy", Name = "Mannequin", MaxHp = 1e12, Atk = 100, Def = 10, TickMs = 1000,
                Pattern = new List<BossActionDef> { new BossActionDef { Type = "attack", TelegraphMs = 0 } },
            };
            boss?.Invoke(b);
            var tank = new CharacterDef { Id = "tank", Name = "Garde", Role = "tank", MaxHp = 1e9, Atk = 50, Def = 10 };
            var healer = new CharacterDef { Id = "healer", Name = "Vous", Role = "healer", MaxHp = 1e9, Atk = 0, Def = 10, MaxMana = 100, ManaRegenPerSec = 6 };
            allies?.Invoke(tank, healer);
            var sk = content.Skills;
            skills?.Invoke(sk);
            return new EncounterDef { Id = "arena", Boss = b, Allies = new List<CharacterDef> { tank, healer }, Effects = effects ?? content.Effects, Skills = sk, Seed = seed };
        }

        public static List<BattleEvent> Play(EncounterDef enc, double ms, IEnumerable<Command>? commands = null, double step = 50)
        {
            var battle = new Battle(enc, commands);
            var events = new List<BattleEvent>();
            battle.Subscribe(events.Add);
            while (battle.GetClock() < ms && battle.GetResult() == BattleResults.Ongoing) battle.Step(step);
            return events;
        }

        /// <summary>Applique la même modification aux deux alliés (le boss vise au hasard : un test ne doit pas dépendre de qui est visé).</summary>
        public static Action<CharacterDef, CharacterDef> Both(Action<CharacterDef> change) => (t, h) => { change(t); change(h); };

        /// <summary>Le boss frappe TOUS les alliés à chaque tour : chacun est touché, sans hasard de cible.</summary>
        public static void AoePattern(BossDef b) => b.Pattern = new List<BossActionDef> { new BossActionDef { Type = "bigAttack", HitsAll = true, TelegraphMs = 0 } };

        public static IEnumerable<BattleEvent> Hits(IEnumerable<BattleEvent> e, string unit = "tank") => e.Where(x => x.Type == "unitDamaged" && x.UnitId == unit);
        public static IEnumerable<BattleEvent> AllyAttacks(IEnumerable<BattleEvent> e) => e.Where(x => x.Type == "bossDamaged");
    }

    public class CritTests
    {
        [Test]
        public void Sans_chance_de_critique_aucun_coup_n_est_critique_et_les_degats_sont_constants()
        {
            var e = Arena.Play(Arena.Make(), 20000);
            Assert.That(e.Any(x => x.Crit), Is.False);
            Assert.That(Arena.AllyAttacks(e).Select(x => x.Amount).Distinct().Single(), Is.EqualTo(40)); // 50 - 10
        }

        [Test]
        public void Une_chance_de_cent_pour_cent_rend_chaque_attaque_critique_avec_le_multiplicateur()
        {
            var e = Arena.Play(Arena.Make(allies: (t, h) => { t.CritPct = 100; t.CritMultPct = 200; }), 20000);
            var attacks = Arena.AllyAttacks(e).ToList();
            Assert.That(attacks, Is.Not.Empty);
            Assert.That(attacks.All(x => x.Crit), Is.True);
            Assert.That(attacks.Select(x => x.Amount).Distinct().Single(), Is.EqualTo(90)); // 50 x 2 - 10
        }

        [Test]
        public void Le_multiplicateur_par_defaut_est_de_cent_cinquante_pour_cent()
        {
            var e = Arena.Play(Arena.Make(allies: (t, h) => t.CritPct = 100), 10000);
            Assert.That(Arena.AllyAttacks(e).First().Amount, Is.EqualTo(Math.Floor(50 * 1.5 + 0.5) - 10)); // 75 - 10
        }

        [Test]
        public void Une_chance_de_cinquante_pour_cent_donne_environ_la_moitie_de_critiques()
        {
            var e = Arena.Play(Arena.Make(allies: (t, h) => t.CritPct = 50), 640000);
            var a = Arena.AllyAttacks(e).ToList();
            Assert.That(a.Count, Is.GreaterThan(300));
            double rate = a.Count(x => x.Crit) / (double)a.Count;
            Assert.That(rate, Is.InRange(0.42, 0.58));
        }

        [Test]
        public void Une_chance_superieure_a_cent_est_bornee_a_cent()
        {
            var e = Arena.Play(Arena.Make(allies: (t, h) => t.CritPct = 500), 10000);
            Assert.That(Arena.AllyAttacks(e).All(x => x.Crit), Is.True);
        }

        [Test]
        public void Les_critiques_sont_deterministes_pour_une_graine()
        {
            List<string> Trace() => Arena.Play(Arena.Make(allies: (t, h) => t.CritPct = 40, seed: 9), 60000).Select(x => x.Format()).ToList();
            Assert.That(Trace(), Is.EqualTo(Trace()));
        }

        [Test]
        public void Une_chance_nulle_ne_consomme_aucun_tirage_les_cibles_du_boss_ne_changent_pas()
        {
            // Sinon toute mécanique « désactivée » décalerait tous les combats existants.
            List<string> Targets(Action<CharacterDef, CharacterDef>? a) =>
                Arena.Hits(Arena.Play(Arena.Make(allies: a, seed: 4), 50000), "tank").Select(x => x.TimeMs.ToString()).ToList();
            Assert.That(Targets((t, h) => t.CritPct = 0), Is.EqualTo(Targets(null)));
        }

        [Test]
        public void Le_boss_peut_faire_des_coups_critiques()
        {
            var e = Arena.Play(Arena.Make(b => { b.CritPct = 100; b.CritMultPct = 200; }), 8000);
            var hits = e.Where(x => x.Type == "unitDamaged").ToList();
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits.All(x => x.Crit), Is.True);
            Assert.That(hits.All(x => x.Amount == 190), Is.True); // 100 x 2 - 10
        }

        [Test]
        public void Un_soin_critique_rend_plus_de_PV_et_est_signale()
        {
            var enc = Arena.Make(b => { b.Atk = 1000; Arena.AoePattern(b); }, (t, h) => { h.CritPct = 100; h.CritMultPct = 200; });
            var e = Arena.Play(enc, 1900, new[] { new Command { TimeMs = 1500, SkillId = "heal_single", TargetId = "tank" } });
            var heal = e.Single(x => x.Type == "healed");
            Assert.That(heal.Crit, Is.True);
            Assert.That(heal.Amount, Is.EqualTo(340)); // 170 x 2
        }

        [Test]
        public void Un_soin_de_zone_critique_fait_un_seul_tirage_pour_tous_les_soignes()
        {
            var enc = Arena.Make(b => { b.Atk = 1000; Arena.AoePattern(b); }, (t, h) => h.CritPct = 50, seed: 3);
            var cmds = Enumerable.Range(0, 30).Select(i => new Command { TimeMs = 1500 + i * 5100, SkillId = "heal_aoe" }).ToList();
            var e = Arena.Play(enc, 170000, cmds);
            foreach (var group in e.Where(x => x.Type == "healed").GroupBy(x => x.TimeMs))
                Assert.That(group.Select(x => x.Crit).Distinct().Count(), Is.EqualTo(1), "à " + group.Key);
        }

        [Test]
        public void Un_sort_qui_ne_soigne_pas_ne_consomme_pas_de_tirage_de_critique()
        {
            List<string> Trace(double crit) => Arena.Play(Arena.Make(allies: (t, h) => h.CritPct = crit, seed: 5), 40000,
                new[] { new Command { TimeMs = 1500, SkillId = "shield", TargetId = "tank" } }).Select(x => x.Format()).ToList();
            Assert.That(Trace(50), Is.EqualTo(Trace(0)));
        }

        [Test]
        public void Le_format_du_journal_signale_un_critique_et_reste_muet_sinon()
        {
            Assert.That(new BattleEvent { Type = "unitDamaged", TimeMs = 5, UnitId = "tank", Amount = 30, Absorbed = 2, Crit = true }.Format(), Does.EndWith("CRIT"));
            Assert.That(new BattleEvent { Type = "unitDamaged", TimeMs = 5, UnitId = "tank", Amount = 30, Absorbed = 2 }.Format(), Does.Not.Contain("CRIT"));
        }
    }

    public class ResistanceTests
    {
        private static BossDef Fire(BossDef b) { b.DamageType = "fire"; b.Atk = 100; return b; }

        private static double FirstHit(EncounterDef e) => Arena.Play(e, 1500).First(x => x.Type == "unitDamaged").Amount;

        [Test]
        public void Une_resistance_reduit_les_degats_du_type_correspondant()
        {
            double baseline = FirstHit(Arena.Make(b => Fire(b)));
            double resisted = FirstHit(Arena.Make(b => Fire(b), Arena.Both(c => c.Resist = new Dictionary<string, int> { ["fire"] = 50 })));
            Assert.That(baseline, Is.EqualTo(90));
            Assert.That(resisted, Is.EqualTo(40)); // 100 x 0,5 - 10
        }

        [Test]
        public void Une_vulnerabilite_negative_augmente_les_degats()
        {
            double v = FirstHit(Arena.Make(b => Fire(b), Arena.Both(c => c.Resist = new Dictionary<string, int> { ["fire"] = -50 })));
            Assert.That(v, Is.EqualTo(140)); // 100 x 1,5 - 10
        }

        [Test]
        public void Une_resistance_a_un_autre_type_ne_change_rien()
        {
            double v = FirstHit(Arena.Make(b => Fire(b), Arena.Both(c => c.Resist = new Dictionary<string, int> { ["poison"] = 90 })));
            Assert.That(v, Is.EqualTo(90));
        }

        [Test]
        public void La_resistance_est_plafonnee_a_quatre_vingt_dix_pour_cent()
        {
            double v = FirstHit(Arena.Make(b => Fire(b), Arena.Both(c => c.Resist = new Dictionary<string, int> { ["fire"] = 100 })));
            Assert.That(v, Is.EqualTo(Math.Max(1, 100 * 0.1 - 10))); // 10 - 10 -> minimum 1
            Assert.That(v, Is.EqualTo(1));
        }

        [Test]
        public void La_vulnerabilite_est_plafonnee_a_moins_cent_pour_cent()
        {
            double v = FirstHit(Arena.Make(b => Fire(b), Arena.Both(c => c.Resist = new Dictionary<string, int> { ["fire"] = -900 })));
            Assert.That(v, Is.EqualTo(190)); // 100 x 2 - 10
        }

        [Test]
        public void Un_coup_ne_fait_jamais_moins_d_un_degat()
        {
            double v = FirstHit(Arena.Make(b => { Fire(b); b.Atk = 5; }, Arena.Both(c => c.Resist = new Dictionary<string, int> { ["fire"] = 90 })));
            Assert.That(v, Is.EqualTo(1));
        }

        [Test]
        public void Le_type_d_une_action_prime_sur_celui_du_boss()
        {
            var enc = Arena.Make(b => { b.DamageType = "physical"; b.Pattern[0].DamageType = "fire"; }, Arena.Both(c => c.Resist = new Dictionary<string, int> { ["fire"] = 50 }));
            Assert.That(FirstHit(enc), Is.EqualTo(40));
        }

        [Test]
        public void Les_resistances_s_appliquent_aux_effets_sur_la_duree()
        {
            var effects = new List<EffectDef> { new EffectDef { Id = "brasier", Name = "Brasier", Kind = "damageOverTime", DamagePerTick = 40, TickMs = 1000, DurationMs = 5000, DamageType = "fire" } };
            List<double> Ticks(Dictionary<string, int>? resist)
            {
                var enc = Arena.Make(b => b.Pattern = new List<BossActionDef> { new BossActionDef { Type = "poison", Multiplier = 0, EffectId = "brasier", TelegraphMs = 0 } },
                    Arena.Both(c => c.Resist = resist), effects: effects);
                return Arena.Play(enc, 6000).Where(x => x.Type == "effectTick").Select(x => x.Amount).ToList();
            }
            Assert.That(Ticks(null).Distinct().Single(), Is.EqualTo(40));
            Assert.That(Ticks(new Dictionary<string, int> { ["fire"] = 50 }).Distinct().Single(), Is.EqualTo(20));
            Assert.That(Ticks(new Dictionary<string, int> { ["poison"] = 50 }).Distinct().Single(), Is.EqualTo(40));
        }

        [Test]
        public void Un_effet_sans_type_ignore_les_resistances()
        {
            var effects = new List<EffectDef> { new EffectDef { Id = "x", Name = "X", Kind = "damageOverTime", DamagePerTick = 40, TickMs = 1000, DurationMs = 5000 } };
            var enc = Arena.Make(b => b.Pattern = new List<BossActionDef> { new BossActionDef { Type = "poison", Multiplier = 0, EffectId = "x", TelegraphMs = 0 } },
                Arena.Both(c => c.Resist = new Dictionary<string, int> { ["poison"] = 90, ["fire"] = 90 }), effects: effects);
            Assert.That(Arena.Play(enc, 6000).Where(x => x.Type == "effectTick").Select(x => x.Amount).Distinct().Single(), Is.EqualTo(40));
        }

        [Test]
        public void Le_boss_a_ses_propres_resistances_selon_le_type_de_l_attaquant()
        {
            double Dmg(string type, int resist)
            {
                var enc = Arena.Make(b => b.Resist = new Dictionary<string, int> { [type] = resist }, (t, h) => t.DamageType = type);
                return Arena.AllyAttacks(Arena.Play(enc, 2000)).First().Amount;
            }
            Assert.That(Dmg("magic", 0), Is.EqualTo(40));
            Assert.That(Dmg("magic", 50), Is.EqualTo(Math.Floor(50 * 0.5 + 0.5) - 10)); // 15
            Assert.That(Dmg("magic", -50), Is.EqualTo(Math.Floor(50 * 1.5 + 0.5) - 10)); // 65
        }

        [Test]
        public void La_resistance_du_boss_a_un_type_ne_touche_pas_les_autres_types()
        {
            var enc = Arena.Make(b => b.Resist = new Dictionary<string, int> { ["magic"] = 90 }, (t, h) => t.DamageType = "physical");
            Assert.That(Arena.AllyAttacks(Arena.Play(enc, 2000)).First().Amount, Is.EqualTo(40));
        }

        [Test]
        public void Le_type_de_degats_est_indique_dans_le_journal_seulement_s_il_n_est_pas_physique()
        {
            Assert.That(new BattleEvent { Type = "unitDamaged", TimeMs = 1, UnitId = "tank", Amount = 5, DamageType = "fire" }.Format(), Does.Contain("[fire]"));
            Assert.That(new BattleEvent { Type = "unitDamaged", TimeMs = 1, UnitId = "tank", Amount = 5, DamageType = "physical" }.Format(), Does.Not.Contain("["));
            Assert.That(new BattleEvent { Type = "unitDamaged", TimeMs = 1, UnitId = "tank", Amount = 5 }.Format(), Does.Not.Contain("["));
        }
    }

    public class ArmorTests
    {
        private static double FirstHit(EncounterDef e) => Arena.Play(e, 1500).First(x => x.Type == "unitDamaged").Amount;

        [Test]
        public void L_armure_en_pourcentage_reduit_les_degats_physiques_avant_la_defense_fixe()
        {
            double v = FirstHit(Arena.Make(null, Arena.Both(c => c.ArmorPct = 25)));
            Assert.That(v, Is.EqualTo(65)); // 100 x 0,75 = 75, puis - 10
        }

        [Test]
        public void L_ordre_est_pourcentage_puis_soustraction_fixe()
        {
            double v = FirstHit(Arena.Make(null, Arena.Both(c => c.ArmorPct = 50)));
            Assert.That(v, Is.EqualTo(40)); // 50 - 10, et non 100 - 10 x 0,5
        }

        [Test]
        public void L_armure_ne_protege_pas_des_degats_non_physiques()
        {
            double v = FirstHit(Arena.Make(b => b.DamageType = "fire", Arena.Both(c => c.ArmorPct = 50)));
            Assert.That(v, Is.EqualTo(90));
        }

        [Test]
        public void L_armure_est_plafonnee_a_quatre_vingts_pour_cent()
        {
            double v = FirstHit(Arena.Make(null, Arena.Both(c => c.ArmorPct = 200)));
            Assert.That(v, Is.EqualTo(10)); // 100 x 0,2 - 10
        }

        [Test]
        public void Sans_armure_le_resultat_est_celui_d_avant()
        {
            Assert.That(FirstHit(Arena.Make(null, Arena.Both(c => c.ArmorPct = 0))), Is.EqualTo(90));
        }

        [Test]
        public void L_armure_se_combine_avec_une_resistance_physique()
        {
            double v = FirstHit(Arena.Make(null, Arena.Both(c => { c.ArmorPct = 50; c.Resist = new Dictionary<string, int> { ["physical"] = 50 }; })));
            Assert.That(v, Is.EqualTo(Math.Floor(100 * 0.5 * 0.5 + 0.5) - 10)); // 25 - 10
        }

        [Test]
        public void L_armure_ne_reduit_pas_les_degats_des_effets_sur_la_duree()
        {
            var effects = new List<EffectDef> { new EffectDef { Id = "p", Name = "P", Kind = "damageOverTime", DamagePerTick = 40, TickMs = 1000, DurationMs = 5000, DamageType = "poison" } };
            var enc = Arena.Make(b => b.Pattern = new List<BossActionDef> { new BossActionDef { Type = "poison", Multiplier = 0, EffectId = "p", TelegraphMs = 0 } }, Arena.Both(c => c.ArmorPct = 80), effects: effects);
            Assert.That(Arena.Play(enc, 6000).Where(x => x.Type == "effectTick").Select(x => x.Amount).Distinct().Single(), Is.EqualTo(40));
        }

        [Test]
        public void Toujours_au_moins_un_degat_meme_avec_une_grosse_armure()
        {
            var enc = Arena.Make(b => b.Atk = 12, Arena.Both(c => { c.ArmorPct = 80; c.Def = 50; }));
            Assert.That(FirstHit(enc), Is.EqualTo(1));
        }
    }

    public class DodgeTests
    {
        [Test]
        public void Sans_esquive_aucun_evenement_d_esquive()
        {
            Assert.That(Arena.Play(Arena.Make(), 30000).Any(x => x.Type == "unitDodged"), Is.False);
        }

        [Test]
        public void Une_esquive_de_soixante_pour_cent_evite_environ_soixante_pour_cent_des_coups()
        {
            var e = Arena.Play(Arena.Make(null, (t, h) => t.DodgePct = 60), 400000);
            int dodged = e.Count(x => x.Type == "unitDodged" && x.UnitId == "tank");
            int hit = e.Count(x => x.Type == "unitDamaged" && x.UnitId == "tank");
            double rate = dodged / (double)(dodged + hit);
            Assert.That(dodged + hit, Is.GreaterThan(100));
            Assert.That(rate, Is.InRange(0.5, 0.7));
        }

        [Test]
        public void L_esquive_est_plafonnee_a_soixante_pour_cent()
        {
            var e = Arena.Play(Arena.Make(null, (t, h) => t.DodgePct = 100), 200000);
            Assert.That(e.Count(x => x.Type == "unitDamaged" && x.UnitId == "tank"), Is.GreaterThan(0), "même avec 100 % affiché, tout n'est pas évité");
        }

        [Test]
        public void Un_coup_esquive_n_applique_pas_son_effet()
        {
            var enc = Arena.Make(b => b.Pattern = new List<BossActionDef> { new BossActionDef { Type = "poison", Multiplier = 0, EffectId = "poison", TelegraphMs = 0 } }, (t, h) => t.DodgePct = 60);
            var e = Arena.Play(enc, 300000);
            int dodged = e.Count(x => x.Type == "unitDodged" && x.UnitId == "tank");
            int applied = e.Count(x => x.Type == "effectApplied" && x.UnitId == "tank");
            int actions = e.Count(x => x.Type == "bossAction");
            Assert.That(dodged, Is.GreaterThan(0));
            // Chaque action touche UN des deux alliés : les effets appliqués au Garde + esquivés ne dépassent pas les actions.
            Assert.That(applied + dodged, Is.LessThanOrEqualTo(actions));
        }

        [Test]
        public void Une_attaque_de_zone_est_esquivee_ou_non_pour_chaque_cible_separement()
        {
            var enc = Arena.Make(b => b.Pattern = new List<BossActionDef> { new BossActionDef { Type = "bigAttack", HitsAll = true, TelegraphMs = 0 } }, (t, h) => { t.DodgePct = 60; });
            var e = Arena.Play(enc, 200000);
            var groups = e.Where(x => x.Type is "unitDamaged" or "unitDodged").GroupBy(x => x.TimeMs).ToList();
            Assert.That(groups.Any(g => g.Any(x => x.Type == "unitDodged") && g.Any(x => x.Type == "unitDamaged")), Is.True, "au moins une zone touche l'un et manque l'autre");
        }

        [Test]
        public void L_esquive_ne_protege_pas_des_effets_deja_appliques()
        {
            var enc = Arena.Make(b => b.Pattern = new List<BossActionDef> { new BossActionDef { Type = "poison", Multiplier = 0, EffectId = "poison", TelegraphMs = 0 } }, (t, h) => t.DodgePct = 60);
            var e = Arena.Play(enc, 200000);
            Assert.That(e.Any(x => x.Type == "effectTick" && x.UnitId == "tank"), Is.True);
        }

        [Test]
        public void Le_soigneur_peut_aussi_esquiver()
        {
            var e = Arena.Play(Arena.Make(null, (t, h) => h.DodgePct = 60), 200000);
            Assert.That(e.Any(x => x.Type == "unitDodged" && x.UnitId == "healer"), Is.True);
        }

        [Test]
        public void Les_esquives_sont_deterministes()
        {
            List<string> Trace() => Arena.Play(Arena.Make(null, (t, h) => t.DodgePct = 40, seed: 12), 60000).Select(x => x.Format()).ToList();
            Assert.That(Trace(), Is.EqualTo(Trace()));
        }

        [Test]
        public void Une_esquive_nulle_ne_consomme_aucun_tirage()
        {
            List<string> Trace(double d) => Arena.Play(Arena.Make(null, (t, h) => t.DodgePct = d, seed: 6), 40000).Select(x => x.Format()).ToList();
            Assert.That(Trace(0), Is.EqualTo(Arena.Play(Arena.Make(seed: 6), 40000).Select(x => x.Format()).ToList()));
        }
    }

    public class ThreatTests
    {
        private static int TankShare(Action<BossDef>? boss, Action<CharacterDef, CharacterDef>? allies, uint seed)
        {
            var e = Arena.Play(Arena.Make(boss, allies, seed: seed), 12000);
            return e.Count(x => x.Type == "unitDamaged" && x.UnitId == "tank");
        }

        [Test]
        public void Une_attaque_d_allie_genere_de_la_menace_proportionnelle_au_modificateur()
        {
            foreach (double mod in new[] { 100.0, 300.0 })
            {
                var battle = new Battle(Arena.Make(null, (t, h) => t.ThreatMod = mod));
                double dealt = 0;
                battle.Subscribe(e => { if (e.Type == "bossDamaged") dealt += e.Amount; });
                battle.Run(40000);
                Assert.That(battle.GetAllies().Single(a => a.Id == "tank").Threat, Is.EqualTo(dealt * mod / 100.0).Within(1e-6), "modificateur " + mod);
            }
        }

        [Test]
        public void Soigner_genere_de_la_menace_pour_le_soigneur()
        {
            var battle = new Battle(Arena.Make(b => { b.Atk = 1000; Arena.AoePattern(b); }), new[] { new Command { TimeMs = 1500, SkillId = "heal_single", TargetId = "tank" } });
            double healed = 0;
            battle.Subscribe(e => { if (e.Type == "healed") healed += e.Amount; });
            battle.Run(5000);
            Assert.That(healed, Is.GreaterThan(0));
            Assert.That(battle.GetAllies().Single(a => a.Id == "healer").Threat, Is.EqualTo(healed * Battle.HealThreatFactor).Within(1e-6));
        }

        [Test]
        public void Un_soin_a_vide_ne_genere_aucune_menace()
        {
            var battle = new Battle(Arena.Make(), new[] { new Command { TimeMs = 0, SkillId = "heal_single", TargetId = "tank" } });
            battle.Run(500);
            Assert.That(battle.GetAllies().Single(a => a.Id == "healer").Threat, Is.EqualTo(0));
        }

        [Test]
        public void Un_modificateur_nul_ne_genere_aucune_menace()
        {
            var battle = new Battle(Arena.Make(null, (t, h) => t.ThreatMod = 0));
            battle.Run(40000);
            Assert.That(battle.GetAllies().Single(a => a.Id == "tank").Threat, Is.EqualTo(0));
        }

        [Test]
        public void Le_ciblage_au_hasard_ignore_la_menace()
        {
            // Par défaut (« random ») les deux alliés sont visés à peu près à égalité, même si l'un a énormément de menace.
            int tank = 0, total = 0;
            for (uint seed = 1; seed <= 40; seed++)
            {
                var e = Arena.Play(Arena.Make(null, (t, h) => t.ThreatMod = 5000, seed: seed), 30000);
                tank += e.Count(x => x.Type == "unitDamaged" && x.UnitId == "tank");
                total += e.Count(x => x.Type == "unitDamaged");
            }
            Assert.That(tank / (double)total, Is.InRange(0.42, 0.58));
        }

        [Test]
        public void Le_ciblage_par_menace_attire_les_coups_sur_le_tank()
        {
            int tank = 0, total = 0;
            for (uint seed = 1; seed <= 40; seed++)
            {
                var e = Arena.Play(Arena.Make(b => b.Targeting = "threat", (t, h) => t.ThreatMod = 1000, seed: seed), 30000);
                tank += e.Count(x => x.Type == "unitDamaged" && x.UnitId == "tank");
                total += e.Count(x => x.Type == "unitDamaged");
            }
            Assert.That(tank / (double)total, Is.GreaterThan(0.75));
        }

        [Test]
        public void Sans_menace_acquise_le_ciblage_par_menace_reste_repartit_egalement()
        {
            // Au premier coup (1 s) personne n'a encore attaqué : poids égaux.
            int tank = 0, total = 0;
            for (uint seed = 1; seed <= 300; seed++)
            {
                var e = Arena.Play(Arena.Make(b => b.Targeting = "threat", seed: seed), 1000);
                var first = e.FirstOrDefault(x => x.Type == "unitDamaged");
                if (first == null) continue;
                total++; if (first.UnitId == "tank") tank++;
            }
            Assert.That(tank / (double)total, Is.InRange(0.4, 0.6));
        }

        [Test]
        public void Une_unite_sans_menace_peut_encore_etre_visee()
        {
            var e = Arena.Play(Arena.Make(b => b.Targeting = "threat", (t, h) => t.ThreatMod = 1000, seed: 2), 400000);
            Assert.That(e.Any(x => x.Type == "unitDamaged" && x.UnitId == "healer"), Is.True);
        }

        [Test]
        public void La_menace_la_plus_haute_est_exposee_et_un_allie_mort_n_est_plus_vise()
        {
            var battle = new Battle(Arena.Make(null, (t, h) => t.ThreatMod = 500));
            battle.Run(20000);
            Assert.That(battle.GetTopThreatId(), Is.EqualTo("tank"));
        }

        [Test]
        public void Le_ciblage_par_menace_est_deterministe()
        {
            List<string> Trace() => Arena.Play(Arena.Make(b => b.Targeting = "threat", (t, h) => t.ThreatMod = 300, seed: 21), 60000).Select(x => x.Format()).ToList();
            Assert.That(Trace(), Is.EqualTo(Trace()));
        }

        [Test]
        public void Le_ciblage_par_menace_ne_vise_jamais_un_allie_mort()
        {
            var enc = Arena.Make(b => { b.Targeting = "threat"; b.Atk = 5000; }, (t, h) => { t.MaxHp = 100; t.ThreatMod = 1000; }, seed: 3);
            var e = Arena.Play(enc, 30000);
            double? diedAt = e.FirstOrDefault(x => x.Type == "unitDied" && x.UnitId == "tank")?.TimeMs;
            Assert.That(diedAt, Is.Not.Null);
            Assert.That(e.Any(x => x.Type == "unitDamaged" && x.UnitId == "tank" && x.TimeMs > diedAt), Is.False);
        }
    }

    public class CastTimeTests
    {
        private static EncounterDef WithCast(double castMs, double cooldownMs = 0, Action<BossDef>? boss = null, Action<CharacterDef, CharacterDef>? allies = null) =>
            Arena.Make(boss, allies, skills: s => { var h = s.Single(x => x.Id == "heal_single"); h.CastMs = castMs; h.CooldownMs = cooldownMs; });

        private static Command Cast(double t, string skill = "heal_single", string? target = "tank") => new Command { TimeMs = t, SkillId = skill, TargetId = target };

        [Test]
        public void Le_soin_a_incantation_commence_puis_se_lance_au_bout_du_temps()
        {
            var e = Arena.Play(WithCast(1000, boss: b => { b.Atk = 1000; Arena.AoePattern(b); }), 4000, new[] { Cast(1500) });
            var start = e.Single(x => x.Type == "castStarted");
            var used = e.Single(x => x.Type == "skillUsed");
            Assert.That(start.TimeMs, Is.EqualTo(1500));
            Assert.That(start.Amount, Is.EqualTo(1000));
            Assert.That(used.TimeMs, Is.EqualTo(2500));
            Assert.That(e.Single(x => x.Type == "healed").TimeMs, Is.EqualTo(2500));
        }

        [Test]
        public void Rien_n_est_soigne_avant_la_fin_de_l_incantation()
        {
            var e = Arena.Play(WithCast(1000, boss: b => { b.Atk = 1000; Arena.AoePattern(b); }), 2400, new[] { Cast(1500) });
            Assert.That(e.Any(x => x.Type == "healed"), Is.False);
            Assert.That(e.Any(x => x.Type == "castStarted"), Is.True);
        }

        [Test]
        public void Le_mana_est_depense_a_l_achevement_pas_au_debut()
        {
            var battle = new Battle(WithCast(1000, boss: b => { b.Atk = 1000; Arena.AoePattern(b); }), new[] { Cast(0) });
            battle.Step(50);
            battle.Step(400);
            double midCast = battle.GetAllies().Single(a => a.Id == "healer").Mana;
            Assert.That(midCast, Is.GreaterThanOrEqualTo(100 - 1e-6), "aucun mana dépensé tant que le sort n'est pas lancé");
            battle.Run(1500, 50);
            Assert.That(battle.GetAllies().Single(a => a.Id == "healer").Mana, Is.LessThan(100));
        }

        [Test]
        public void La_recharge_court_a_partir_de_l_achevement()
        {
            // Incantation 1000 ms, recharge 800 ms : le sort est de nouveau lançable à 1800 ms.
            var battle = new Battle(WithCast(1000, 800), new[] { Cast(0) });
            battle.Run(1700, 50);
            Assert.That(battle.CanUseSkillNow("healer", "heal_single"), Is.False);
            battle.Run(1900, 50);
            Assert.That(battle.CanUseSkillNow("healer", "heal_single"), Is.True);
        }

        [Test]
        public void Une_seule_incantation_a_la_fois()
        {
            var e = Arena.Play(WithCast(1000, boss: b => { b.Atk = 1000; Arena.AoePattern(b); }), 3000, new[] { Cast(0), Cast(300), Cast(600) });
            Assert.That(e.Count(x => x.Type == "castStarted"), Is.EqualTo(1));
        }

        [Test]
        public void Un_sort_instantane_reste_possible_pendant_l_incantation_sans_l_annuler()
        {
            var e = Arena.Play(WithCast(1000, boss: b => { b.Atk = 1000; Arena.AoePattern(b); }), 3000, new[] { Cast(0), Cast(400, "shield") });
            Assert.That(e.Any(x => x.Type == "shielded" && x.TimeMs == 400), Is.True);
            Assert.That(e.Any(x => x.Type == "skillUsed" && x.SkillId == "heal_single" && x.TimeMs == 1000), Is.True);
        }

        [Test]
        public void Pendant_l_incantation_les_sorts_a_incantation_sont_indisponibles_les_instantanes_non()
        {
            var battle = new Battle(WithCast(1000), new[] { Cast(0) });
            battle.Step(50); battle.Step(50);
            Assert.That(battle.CanUseSkillNow("healer", "heal_single"), Is.False);
            Assert.That(battle.CanUseSkillNow("healer", "shield"), Is.True);
            Assert.That(battle.CanUseSkillNow("healer", "heal_aoe"), Is.True);
        }

        [Test]
        public void L_etat_de_l_incantation_est_lisible_avec_une_progression_de_zero_a_un()
        {
            var battle = new Battle(WithCast(1000), new[] { Cast(0) });
            Assert.That(battle.GetCast(), Is.Null);
            battle.Step(50);
            var mid = battle.GetCast();
            Assert.That(mid, Is.Not.Null);
            Assert.That(mid!.SkillId, Is.EqualTo("heal_single"));
            Assert.That(mid.TargetId, Is.EqualTo("tank"));
            Assert.That(mid.TotalMs, Is.EqualTo(1000));
            double previous = mid.Progress;
            for (int i = 0; i < 15; i++)
            {
                battle.Step(50);
                var c = battle.GetCast();
                if (c == null) break;
                Assert.That(c.Progress, Is.InRange(previous, 1.0));
                previous = c.Progress;
            }
            battle.Step(300);
            Assert.That(battle.GetCast(), Is.Null, "terminée");
        }

        [Test]
        public void Une_cible_qui_meurt_pendant_l_incantation_fait_echouer_le_sort_sans_cout()
        {
            var enc = WithCast(1500, boss: b => { b.Atk = 5000; Arena.AoePattern(b); }, allies: (t, h) => t.MaxHp = 100);
            var e = Arena.Play(enc, 4000, new[] { Cast(500) });
            var fail = e.SingleOrDefault(x => x.Type == "castFailed");
            Assert.That(fail, Is.Not.Null);
            Assert.That(fail!.Reason, Is.EqualTo("target"));
            Assert.That(e.Any(x => x.Type == "skillUsed"), Is.False);
        }

        [Test]
        public void Le_soigneur_qui_meurt_pendant_l_incantation_ne_lance_rien()
        {
            var enc = WithCast(3000, boss: b => { b.Atk = 5000; Arena.AoePattern(b); }, allies: (t, h) => { t.MaxHp = 1e9; h.MaxHp = 100; });
            var e = Arena.Play(enc, 9000, new[] { Cast(500) });
            Assert.That(e.Any(x => x.Type == "unitDied" && x.UnitId == "healer"), Is.True);
            Assert.That(e.Any(x => x.Type == "skillUsed"), Is.False);
        }

        [Test]
        public void Sans_assez_de_mana_l_incantation_ne_demarre_pas()
        {
            var enc = Arena.Make(null, (t, h) => h.MaxMana = 10, s => { var x = s.Single(y => y.Id == "heal_single"); x.CastMs = 1000; x.CooldownMs = 0; });
            var e = Arena.Play(enc, 3000, new[] { Cast(0) });
            Assert.That(e.Any(x => x.Type == "castStarted"), Is.False);
        }

        [Test]
        public void Un_sort_instantane_qui_vide_le_mana_pendant_l_incantation_la_fait_echouer()
        {
            var enc = Arena.Make(null, (t, h) => { h.MaxMana = 40; h.ManaRegenPerSec = 0; }, s => { var x = s.Single(y => y.Id == "heal_single"); x.CastMs = 1000; x.CooldownMs = 0; });
            var e = Arena.Play(enc, 3000, new[] { Cast(0), Cast(200, "shield") }); // 18 + 28 > 40
            var fail = e.Single(x => x.Type == "castFailed");
            Assert.That(fail.Reason, Is.EqualTo("mana"));
        }

        [Test]
        public void Les_incantations_s_enchainent_bout_a_bout()
        {
            var cmds = Enumerable.Range(0, 8).Select(i => Cast(i * 1000.0)).ToList();
            var e = Arena.Play(WithCast(1000, boss: b => { b.Atk = 1000; Arena.AoePattern(b); }), 9000, cmds);
            var used = e.Where(x => x.Type == "skillUsed").Select(x => x.TimeMs).ToList();
            Assert.That(used.Count, Is.GreaterThanOrEqualTo(6));
            for (int i = 1; i < used.Count; i++) Assert.That(used[i] - used[i - 1], Is.EqualTo(1000));
        }

        [Test]
        public void L_incantation_s_acheve_a_l_heure_exacte_meme_avec_de_gros_pas_de_simulation()
        {
            var e = Arena.Play(WithCast(1000, boss: b => { b.Atk = 1000; Arena.AoePattern(b); }), 5000, new[] { Cast(1500) }, step: 400);
            Assert.That(e.Single(x => x.Type == "skillUsed").TimeMs, Is.EqualTo(2500));
        }

        [Test]
        public void Un_soin_de_zone_a_incantation_soigne_tous_les_vivants_a_l_achevement()
        {
            var enc = Arena.Make(b => { b.Atk = 1000; Arena.AoePattern(b); }, skills: s => { var x = s.Single(y => y.Id == "heal_aoe"); x.CastMs = 1200; });
            var e = Arena.Play(enc, 5000, new[] { new Command { TimeMs = 1500, SkillId = "heal_aoe" } });
            Assert.That(e.Where(x => x.Type == "healed").Select(x => x.UnitId).Distinct().Count(), Is.EqualTo(2));
            Assert.That(e.Where(x => x.Type == "healed").All(x => x.TimeMs == 2700), Is.True);
        }

        [Test]
        public void Un_soin_a_incantation_critique_et_genere_de_la_menace_a_l_achevement()
        {
            var enc = WithCast(1000, boss: b => { b.Atk = 1000; Arena.AoePattern(b); }, allies: (t, h) => { h.CritPct = 100; h.CritMultPct = 200; });
            var battle = new Battle(enc, new[] { Cast(1500) });
            var events = new List<BattleEvent>(); battle.Subscribe(events.Add);
            battle.Run(4000, 50);
            Assert.That(events.Single(x => x.Type == "healed").Crit, Is.True);
            Assert.That(battle.GetAllies().Single(a => a.Id == "healer").Threat, Is.GreaterThan(0));
        }

        [Test]
        public void Un_sort_sans_temps_d_incantation_se_lance_instantanement_comme_avant()
        {
            var e = Arena.Play(Arena.Make(b => b.Atk = 1000), 3000, new[] { Cast(1500) });
            Assert.That(e.Any(x => x.Type == "castStarted"), Is.False);
            Assert.That(e.Single(x => x.Type == "skillUsed").TimeMs, Is.EqualTo(1500));
        }

        [Test]
        public void L_incantation_est_deterministe()
        {
            List<string> Trace() => Arena.Play(WithCast(1000, boss: b => b.Atk = 300, allies: (t, h) => h.CritPct = 30), 60000,
                Enumerable.Range(0, 40).Select(i => Cast(i * 1000.0)).ToList()).Select(x => x.Format()).ToList();
            Assert.That(Trace(), Is.EqualTo(Trace()));
        }

        [Test]
        public void Le_journal_decrit_le_debut_et_l_echec_d_une_incantation()
        {
            Assert.That(new BattleEvent { Type = "castStarted", TimeMs = 10, CasterId = "healer", SkillId = "heal_single", Amount = 1000 }.Format(), Is.EqualTo("10ms castStarted healer heal_single 1000ms"));
            Assert.That(new BattleEvent { Type = "castFailed", TimeMs = 20, CasterId = "healer", SkillId = "heal_single", Reason = "mana" }.Format(), Is.EqualTo("20ms castFailed healer heal_single mana"));
        }
    }

    public class MechanicsStatsAndCloneTests
    {
        [Test]
        public void Les_statistiques_comptent_critiques_esquives_et_incantations_echouees()
        {
            var s = new CombatStats();
            s.Apply(new BattleEvent { Type = "unitDamaged", Crit = true, Amount = 5 });
            s.Apply(new BattleEvent { Type = "bossDamaged", Crit = true, SourceId = "tank", Amount = 5 });
            s.Apply(new BattleEvent { Type = "healed", Crit = true, Amount = 5 });
            s.Apply(new BattleEvent { Type = "unitDodged", UnitId = "tank" });
            s.Apply(new BattleEvent { Type = "unitDodged", UnitId = "tank" });
            s.Apply(new BattleEvent { Type = "castFailed", Reason = "target" });
            Assert.That((s.Crits, s.Dodges, s.FailedCasts), Is.EqualTo((3, 2, 1)));
        }

        [Test]
        public void Les_nouvelles_statistiques_survivent_a_la_copie_des_personnages_et_des_sorts()
        {
            var c = Fixtures.FullContent();
            c.Characters[0].ArmorPct = 30; c.Characters[0].DodgePct = 12; c.Characters[0].CritPct = 7; c.Characters[0].CritMultPct = 180;
            c.Characters[0].ThreatMod = 400; c.Characters[0].DamageType = "magic"; c.Characters[0].Resist = new Dictionary<string, int> { ["fire"] = 25 };
            c.Skills[0].CastMs = 900;
            var (ch, sk) = LoadoutApplier.Apply(c.Upgrades, null, c.Characters, c.Skills);
            Assert.That((ch[0].ArmorPct, ch[0].DodgePct, ch[0].CritPct, ch[0].CritMultPct, ch[0].ThreatMod, ch[0].DamageType), Is.EqualTo((30.0, 12.0, 7.0, 180.0, 400.0, "magic")));
            Assert.That(ch[0].Resist!["fire"], Is.EqualTo(25));
            Assert.That(sk[0].CastMs, Is.EqualTo(900));
            ch[0].Resist!["fire"] = 99;
            Assert.That(c.Characters[0].Resist!["fire"], Is.EqualTo(25), "la copie est indépendante");
        }

        [Test]
        public void Les_unites_exposent_leurs_statistiques_defensives_et_offensives()
        {
            var battle = new Battle(Arena.Make(null, (t, h) => { t.ArmorPct = 30; t.DodgePct = 12; t.CritPct = 7; }));
            var tank = battle.GetAllies().Single(a => a.Id == "tank");
            Assert.That((tank.ArmorPct, tank.DodgePct, tank.CritPct), Is.EqualTo((30.0, 12.0, 7.0)));
        }

        [Test]
        public void Les_valeurs_hors_borne_sont_ramenees_dans_leurs_limites_a_la_creation()
        {
            var battle = new Battle(Arena.Make(null, (t, h) => { t.ArmorPct = 500; t.DodgePct = 500; t.CritPct = 500; }));
            var tank = battle.GetAllies().Single(a => a.Id == "tank");
            Assert.That((tank.ArmorPct, tank.DodgePct, tank.CritPct), Is.EqualTo((80.0, 60.0, 100.0)));
        }
    }
}
