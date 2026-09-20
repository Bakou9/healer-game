using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Règles de combat : événements, effets sur la durée (poison, purge), phases de boss.</summary>
    public class RulesTests
    {
        private static List<BattleEvent> Collect(Battle battle)
        {
            var events = new List<BattleEvent>();
            battle.Subscribe(e => events.Add(e));
            return events;
        }

        private static EncounterDef WithBoss(EncounterDef enc, List<BossActionDef> pattern, double tickMs = 1000, bool removePhases = true)
        {
            var boss = new BossDef
            {
                Id = enc.Boss.Id, Name = enc.Boss.Name, MaxHp = enc.Boss.MaxHp, Atk = enc.Boss.Atk, Def = enc.Boss.Def,
                TickMs = tickMs, Pattern = pattern, Phases = removePhases ? null : enc.Boss.Phases,
            };
            enc.Boss = boss;
            return enc;
        }

        private static readonly BossActionDef Poison = new BossActionDef { Type = "poison", TelegraphMs = 0, Multiplier = 0, EffectId = "poison" };
        private static readonly BossActionDef Idle = new BossActionDef { Type = "attack", TelegraphMs = 0, Multiplier = 0 };

        private static EncounterDef SinglePoison(uint seed = 1)
        {
            var pattern = new List<BossActionDef> { Poison };
            pattern.AddRange(Enumerable.Repeat(Idle, 200));
            return WithBoss(Fixtures.Encounter(seed), pattern);
        }

        private static List<Command> KeepAlive() =>
            Enumerable.Range(0, 40).Select(i => new Command { TimeMs = i * 1200, SkillId = "heal_aoe" }).ToList();

        // ---- Événements -------------------------------------------------------

        [Test]
        public void Un_seul_battleEnded_en_dernier_coherent_avec_le_resultat()
        {
            var battle = new Battle(Fixtures.Encounter(7));
            var events = Collect(battle);
            ReferenceHealerBot.Run(battle, 120000);
            var ended = events.Where(e => e.Type == "battleEnded").ToList();
            Assert.That(ended, Has.Count.EqualTo(1));
            Assert.That(events.Last(), Is.SameAs(ended[0]));
            Assert.That(ended[0].Result, Is.EqualTo(battle.GetResult()));
        }

        [Test]
        public void Les_evenements_sont_chronologiques()
        {
            var battle = new Battle(Fixtures.Encounter(42));
            var events = Collect(battle);
            ReferenceHealerBot.Run(battle, 120000);
            var times = events.Select(e => e.TimeMs).ToList();
            Assert.That(times, Is.Ordered);
        }

        [Test]
        public void Un_observateur_ne_voit_jamais_de_PV_negatifs()
        {
            var battle = new Battle(Fixtures.Encounter(1)); // sans soigneur : des alliés meurent
            double minHp = double.MaxValue;
            battle.Subscribe(_ => { foreach (var u in battle.GetAllies()) minHp = System.Math.Min(minHp, u.Hp); });
            battle.Run(120000);
            Assert.That(minHp, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Se_desabonner_arrete_la_reception()
        {
            var battle = new Battle(Fixtures.Encounter(1));
            int received = 0;
            var unsubscribe = battle.Subscribe(_ => received++);
            battle.Run(5000);
            int before = received;
            unsubscribe();
            battle.Run(20000);
            Assert.That(before, Is.GreaterThan(0));
            Assert.That(received, Is.EqualTo(before));
        }

        [Test]
        public void Un_soin_n_annonce_que_les_PV_reellement_rendus()
        {
            var battle = new Battle(Fixtures.Encounter(1), new[] { new Command { TimeMs = 0, SkillId = "heal_single", TargetId = "tank" } });
            var healed = new List<double>();
            battle.Subscribe(e => { if (e.Type == "healed") healed.Add(e.Amount); });
            battle.Step(100);
            Assert.That(healed, Is.EqualTo(new[] { 0.0 })); // le tank est déjà à pleine vie
        }

        // ---- Effets sur la durée ----------------------------------------------

        [Test]
        public void Chaque_tick_inflige_exactement_les_degats_des_donnees()
        {
            var enc = SinglePoison();
            var battle = new Battle(enc, KeepAlive());
            var events = Collect(battle);
            battle.Run(12000);
            var ticks = events.Where(e => e.Type == "effectTick").ToList();
            Assert.That(ticks, Is.Not.Empty);
            foreach (var t in ticks) Assert.That(t.Amount + t.Absorbed, Is.EqualTo(enc.Effects[0].DamagePerTick));
        }

        [Test]
        public void Le_poison_expire_apres_sa_duree_avec_le_nombre_de_ticks_attendu()
        {
            var enc = SinglePoison();
            var fx = enc.Effects[0];
            var battle = new Battle(enc, KeepAlive());
            var events = Collect(battle);
            battle.Run(12000);
            Assert.That(events.Count(e => e.Type == "effectApplied"), Is.EqualTo(1));
            var ended = events.Where(e => e.Type == "effectEnded").ToList();
            Assert.That(ended, Has.Count.EqualTo(1));
            Assert.That(ended[0].Reason, Is.EqualTo("expired"));
            Assert.That(ended[0].TimeMs, Is.EqualTo(1000 + fx.DurationMs));
            Assert.That(events.Count(e => e.Type == "effectTick"), Is.EqualTo((int)(fx.DurationMs / fx.TickMs)));
        }

        [Test]
        public void Une_nouvelle_application_rafraichit_le_poison_sans_l_empiler()
        {
            var battle = new Battle(WithBoss(Fixtures.Encounter(1), new List<BossActionDef> { Poison }));
            int maxStacks = 0;
            battle.Subscribe(_ => { foreach (var u in battle.GetAllies()) maxStacks = System.Math.Max(maxStacks, u.Effects.Count); });
            battle.Run(20000);
            Assert.That(maxStacks, Is.EqualTo(1));
        }

        [Test]
        public void La_Purge_retire_le_poison_de_la_cible_et_arrete_ses_degats()
        {
            var enc = SinglePoison();
            var probe = new Battle(SinglePoison(), null);
            var applied = Collect(probe);
            probe.Run(1000);
            string victim = applied.First(e => e.Type == "effectApplied").UnitId;

            var commands = KeepAlive();
            commands.Add(new Command { TimeMs = 2500, SkillId = "purge", TargetId = victim });
            var battle = new Battle(enc, commands);
            var events = Collect(battle);
            battle.Run(12000);
            Assert.That(events.Count(e => e.Type == "effectEnded" && e.Reason == "cleansed"), Is.EqualTo(1));
            Assert.That(events.Where(e => e.Type == "effectTick").All(e => e.TimeMs < 2500));
            Assert.That(battle.GetAllies().Single(u => u.Id == victim).Effects, Is.Empty);
        }

        // ---- Phases de boss ---------------------------------------------------

        [Test]
        public void Le_boss_change_de_phase_une_seule_fois_au_bon_seuil()
        {
            var battle = new Battle(Fixtures.Encounter(7));
            var events = Collect(battle);
            ReferenceHealerBot.Run(battle, 150000);
            var changes = events.Where(e => e.Type == "bossPhaseChanged").ToList();
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Phase, Is.EqualTo(1));
            Assert.That(changes[0].Name, Is.EqualTo("Fureur"));
        }

        [Test]
        public void Aucune_phase_avant_le_seuil()
        {
            var battle = new Battle(Fixtures.Encounter(7));
            var events = Collect(battle);
            battle.Run(10000);
            Assert.That(battle.GetBossHp() / battle.GetBossMaxHp(), Is.GreaterThan(0.5));
            Assert.That(events.Any(e => e.Type == "bossPhaseChanged"), Is.False);
            Assert.That(battle.GetBossPhase().Index, Is.EqualTo(0));
            Assert.That(battle.GetBossPhase().Name, Is.EqualTo(""));
        }

        [Test]
        public void Le_poison_n_apparait_qu_a_partir_de_la_phase_2()
        {
            var battle = new Battle(Fixtures.Encounter(7));
            var events = Collect(battle);
            ReferenceHealerBot.Run(battle, 150000);
            double phaseTime = events.First(e => e.Type == "bossPhaseChanged").TimeMs;
            var poisonActions = events.Where(e => e.Type == "bossAction" && e.Action == "poison").ToList();
            Assert.That(poisonActions, Is.Not.Empty);
            Assert.That(poisonActions.All(e => e.TimeMs > phaseTime), Is.True);
        }

        // ---- Règles de base ---------------------------------------------------

        [Test]
        public void L_equipe_est_vaincue_si_le_soigneur_ne_soigne_jamais()
        {
            Assert.That(new Battle(Fixtures.Encounter(1)).Run(60000), Is.EqualTo(BattleResults.Defeat));
        }

        [Test]
        public void Le_meme_seed_et_les_memes_commandes_donnent_le_meme_resultat()
        {
            string Play()
            {
                var b = new Battle(Fixtures.Encounter(42));
                ReferenceHealerBot.Run(b, 60000);
                return string.Join(",", b.GetAllies().Select(u => $"{u.Id}:{System.Math.Round(u.Hp)}")) + $"|boss:{b.GetBossHp()}";
            }
            Assert.That(Play(), Is.EqualTo(Play()));
        }

        [Test]
        public void Le_bouclier_absorbe_les_degats_avant_les_PV()
        {
            var battle = new Battle(Fixtures.Encounter(5), new[] { new Command { TimeMs = 0, SkillId = "shield", TargetId = "tank" } });
            battle.Step(100);
            double before = battle.GetAllies().Single(u => u.Id == "tank").Hp;
            battle.Run(2300);
            double after = battle.GetAllies().Single(u => u.Id == "tank").Hp;
            Assert.That(after, Is.GreaterThanOrEqualTo(before - 50));
        }

        [Test]
        public void Le_boss_telegraphie_sa_grosse_attaque_avant_de_frapper()
        {
            var battle = new Battle(Fixtures.Encounter(9));
            bool sawTelegraph = false;
            for (int t = 0; t < 8000; t += 100)
            {
                battle.Step(100);
                if (battle.GetTelegraph()?.Type == "bigAttack") sawTelegraph = true;
            }
            Assert.That(sawTelegraph, Is.True);
        }

        [Test]
        public void Un_soin_echoue_sans_assez_de_mana_meme_si_la_recharge_est_prete()
        {
            var spam = Enumerable.Range(0, 8).Select(i => new Command { TimeMs = i * 1200, SkillId = "heal_single", TargetId = "tank" }).ToList();
            var battle = new Battle(Fixtures.Encounter(3), spam);
            battle.Run(9600);
            var healer = battle.GetAllies().Single(u => u.Role == "healer");
            Assert.That(healer.Mana, Is.LessThan(18));
            Assert.That(battle.CanUseSkillNow("healer", "heal_single"), Is.False);
        }
    }
}
