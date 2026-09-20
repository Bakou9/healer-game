using System;
using System.Collections.Generic;
using System.Linq;

namespace Healer.Combat.Tests
{
    /// <summary>
    /// Scénarios de combat rejoués par les tests de conformité. Mêmes scénarios, mêmes graines et
    /// mêmes commandes que la version TypeScript (src/testing/scenarios.ts) : ils doivent reproduire
    /// exactement les fichiers de core/golden.
    /// </summary>
    public sealed class Scenario
    {
        public string Name { get; }
        private readonly Action<Battle> _drive;
        private readonly Func<List<Command>> _commands;
        private readonly uint _seed;

        public Scenario(string name, Action<Battle> drive, uint seed, Func<List<Command>>? commands = null)
        {
            Name = name;
            _drive = drive;
            _seed = seed;
            _commands = commands ?? (() => new List<Command>());
        }

        public Battle Build() => new Battle(Fixtures.Encounter(_seed), _commands());

        /// <summary>Déroulé complet : un événement par ligne, puis un résumé de l'état final.</summary>
        public List<string> Record()
        {
            var lines = new List<string>();
            var battle = Build();
            battle.Subscribe(e => lines.Add(e.Format()));
            _drive(battle);
            var allies = string.Join(" ", battle.GetAllies().Select(u =>
                $"{u.Id}:{Round(u.Hp)}/{Round(u.Shield)}/{Round(u.Mana)}"));
            lines.Add($"FINAL {battle.GetResult()} t={BattleEvent.Num(battle.GetClock())} boss={BattleEvent.Num(battle.GetBossHp())} {allies}");
            return lines;
        }

        /// <summary>Math.round de JavaScript : demi vers le haut (et non l'arrondi bancaire de C#).</summary>
        private static string Round(double v) => BattleEvent.Num(Math.Floor(v + 0.5));

        private const double MaxMs = 120000;

        private static List<Command> SpamHeal()
        {
            var list = new List<Command>();
            for (double t = 0; t < 9600; t += 1200) list.Add(new Command { TimeMs = t, SkillId = "heal_single", TargetId = "tank" });
            return list;
        }

        public static readonly IReadOnlyList<Scenario> All = new List<Scenario>
        {
            new Scenario("bot-seed7", b => ReferenceHealerBot.Run(b, MaxMs), 7),
            new Scenario("bot-seed42", b => ReferenceHealerBot.Run(b, MaxMs), 42),
            new Scenario("bot-lent-seed7", b => ReferenceHealerBot.Run(b, MaxMs, new ReferenceHealerOptions { DecisionEveryMs = 1500 }), 7),
            new Scenario("bot-sans-purge-seed7", b => ReferenceHealerBot.Run(b, MaxMs, new ReferenceHealerOptions { Purge = false }), 7),
            new Scenario("sans-soigneur-seed1", b => b.Run(MaxMs), 1),
            new Scenario("spam-soin-seed3", b => b.Run(MaxMs), 3, SpamHeal),
            new Scenario("bouclier-initial-seed5", b => b.Run(MaxMs), 5,
                () => new List<Command> { new Command { TimeMs = 0, SkillId = "shield", TargetId = "tank" } }),
        };
    }
}
