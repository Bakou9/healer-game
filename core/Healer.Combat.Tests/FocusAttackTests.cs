using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>
    /// Attaque ciblée du boss (« focusAttack », D-056) : une victime choisie parmi les alliés fragiles, annoncée dès le début du
    /// télégraphe, frappée fort. Un Soin de zone ne suffit pas : il faut protéger la victime (Bouclier, Soin ciblé).
    /// </summary>
    public class FocusAttackTests
    {
        private const double TickMs = 4000, TelegraphMs = 1500;

        private static EncounterDef Make(uint seed = 1, double hp = 1e9)
        {
            var enc = Arena.Make(boss: b =>
            {
                b.Atk = 100; b.TickMs = TickMs;
                b.Pattern = new List<BossActionDef> { new BossActionDef { Type = "focusAttack", TelegraphMs = TelegraphMs, Multiplier = 3 } };
            }, seed: seed);
            enc.Allies.Insert(1, new CharacterDef { Id = "dps", Name = "Archère", Role = "dps", MaxHp = hp, Atk = 0, Def = 10 });
            foreach (var a in enc.Allies) { a.MaxHp = hp; }
            return enc;
        }

        private static void StepTo(Battle b, double ms) { while (b.GetClock() < ms && b.GetResult() == BattleResults.Ongoing) b.Step(50); }

        [Test]
        public void La_victime_est_annoncee_des_le_debut_du_telegraphe_et_pas_avant()
        {
            var b = new Battle(Make());
            var events = new List<BattleEvent>();
            b.Subscribe(events.Add);
            StepTo(b, TickMs - TelegraphMs - 100);
            Assert.That(b.GetTelegraph(), Is.Null, "pas encore annoncée");
            Assert.That(events.Any(e => e.Type == "focusMarked"), Is.False);
            StepTo(b, TickMs - TelegraphMs + 100);
            var t = b.GetTelegraph();
            Assert.That(t, Is.Not.Null);
            Assert.That(t!.Type, Is.EqualTo("focusAttack"));
            Assert.That(t.TargetId, Is.Not.Null.And.Not.EqualTo("tank"));
            Assert.That(events.Count(e => e.Type == "focusMarked"), Is.EqualTo(1));
            Assert.That(events.First(e => e.Type == "focusMarked").UnitId, Is.EqualTo(t.TargetId));
        }

        [Test]
        public void La_cible_annoncee_ne_change_pas_pendant_le_telegraphe()
        {
            var b = new Battle(Make(seed: 5));
            StepTo(b, TickMs - TelegraphMs + 50);
            string first = b.GetTelegraph()!.TargetId!;
            for (int i = 0; i < 20; i++) { b.Step(50); if (b.GetClock() < TickMs) Assert.That(b.GetTelegraph()!.TargetId, Is.EqualTo(first)); }
        }

        [Test]
        public void Le_coup_touche_la_victime_annoncee_et_personne_d_autre()
        {
            var b = new Battle(Make(seed: 3));
            var events = new List<BattleEvent>();
            b.Subscribe(events.Add);
            StepTo(b, TickMs - 100);
            string victim = b.GetTelegraph()!.TargetId!;
            StepTo(b, TickMs + 100);
            var hits = events.Where(e => e.Type == "unitDamaged").ToList();
            Assert.That(hits.Select(h => h.UnitId).Distinct().Single(), Is.EqualTo(victim));
            Assert.That(hits.Single().Amount, Is.EqualTo(290), "100 x 3 - défense 10");
        }

        [Test]
        public void Le_tank_n_est_jamais_vise_tant_qu_un_allie_fragile_est_en_vie()
        {
            var marked = new List<string>();
            for (uint seed = 1; seed <= 20; seed++)
            {
                var b = new Battle(Make(seed));
                b.Subscribe(e => { if (e.Type == "focusMarked") marked.Add(e.UnitId); });
                StepTo(b, TickMs * 5);
            }
            Assert.That(marked.Count, Is.GreaterThanOrEqualTo(80));
            Assert.That(marked, Does.Not.Contain("tank"));
            Assert.That(marked.Distinct().Count(), Is.GreaterThan(1), "la victime varie");
        }

        [Test]
        public void Un_bouclier_pose_sur_la_victime_absorbe_le_coup()
        {
            var b = new Battle(Make(seed: 2));
            var events = new List<BattleEvent>();
            b.Subscribe(events.Add);
            StepTo(b, TickMs - TelegraphMs + 100);
            string victim = b.GetTelegraph()!.TargetId!;
            b.IssueCommand(new Command { TimeMs = b.GetClock(), SkillId = "shield", TargetId = victim });
            StepTo(b, TickMs + 100);
            var hit = events.Single(e => e.Type == "unitDamaged" && e.UnitId == victim);
            Assert.That(hit.Absorbed, Is.EqualTo(260), "le bouclier de 260 encaisse une partie des 290 dégâts");
            Assert.That(hit.Amount, Is.EqualTo(30), "il ne reste que 290 - 260 de dégâts sur les PV");
        }

        [Test]
        public void Un_soin_de_zone_ne_protege_pas_la_victime_du_coup()
        {
            // Le coup de 290 dépasse les PV d'un allié fragile : seul un allié déjà protégé ou soigné à temps y survit.
            var enc = Make(hp: 250);
            var b = new Battle(enc);
            StepTo(b, TickMs - TelegraphMs + 100);
            string victim = b.GetTelegraph()!.TargetId!;
            b.IssueCommand(new Command { TimeMs = b.GetClock(), SkillId = "heal_aoe" });
            StepTo(b, TickMs + 100);
            Assert.That(b.GetAllies().First(a => a.Id == victim).Alive, Is.False, "un soin de zone n'empêche pas la mort d'un allié à 250 PV");
        }

        [Test]
        public void Les_attaques_ciblees_sont_deterministes_pour_une_meme_graine()
        {
            List<string> Run(uint seed)
            {
                var ids = new List<string>();
                var b = new Battle(Make(seed));
                b.Subscribe(e => { if (e.Type == "focusMarked") ids.Add(e.UnitId); });
                StepTo(b, TickMs * 6);
                return ids;
            }
            Assert.That(Run(9), Is.EqualTo(Run(9)));
        }

        [Test]
        public void Le_marquage_apparait_dans_la_trace_des_evenements()
        {
            var b = new Battle(Make());
            var lines = new List<string>();
            b.Subscribe(e => lines.Add(e.Format()));
            StepTo(b, TickMs + 100);
            Assert.That(lines.Any(l => l.Contains(" focusMarked ")), Is.True);
            Assert.That(lines.Any(l => l.Contains("bossAction focusAttack")), Is.True);
        }
    }
}
