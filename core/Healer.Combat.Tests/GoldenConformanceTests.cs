using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>
    /// Conformité aux combats de référence de la version Phaser (core/golden/*.txt, un événement par
    /// ligne). Le portage C# est correct si et seulement s'il les reproduit exactement (ticket E14-T06).
    /// Toute divergence est une RÉGRESSION POTENTIELLE : à expliquer (cause, voulu ou accidentel) selon
    /// le protocole de CLAUDE.md. Ces fichiers ne sont jamais modifiés pour « faire passer » un test.
    /// </summary>
    public class GoldenConformanceTests
    {
        public static IEnumerable<string> ScenarioNames => Scenario.All.Select(s => s.Name);

        [TestCaseSource(nameof(ScenarioNames))]
        public void La_simulation_C_sharp_reproduit_la_reference(string name)
        {
            var scenario = Scenario.All.Single(s => s.Name == name);
            var actual = scenario.Record();
            var file = Path.Combine(Fixtures.GoldenDir, name + ".txt");
            Assert.That(File.Exists(file), $"Référence golden absente : {name}.txt");

            var expected = File.ReadAllText(file).Replace("\r\n", "\n").TrimEnd().Split('\n').ToList();
            var diff = FirstDivergence(expected, actual);
            if (diff != null)
            {
                Assert.Fail(string.Join("\n",
                    $"RÉGRESSION POTENTIELLE dans le scénario « {name} » :",
                    $"  - {diff.Value.count} ligne(s) différente(s), première à la ligne {diff.Value.line}",
                    $"  - attendu : {diff.Value.expected ?? "(fin du fichier)"}",
                    $"  - obtenu  : {diff.Value.actual ?? "(fin du combat)"}",
                    "Si ce changement est VOULU : expliquer la cause à l'utilisateur et obtenir son accord. Sinon, c'est une régression à corriger."));
            }
        }

        [Test]
        public void Chaque_scenario_de_reference_a_son_fichier_golden_et_inversement()
        {
            var files = Directory.GetFiles(Fixtures.GoldenDir, "*.txt").Select(Path.GetFileNameWithoutExtension).OrderBy(x => x).ToList();
            var scenarios = Scenario.All.Select(s => s.Name).OrderBy(x => x).ToList();
            Assert.That(scenarios, Is.EqualTo(files), "Chaque fichier golden doit avoir son scénario, et inversement.");
        }

        private static (int line, string? expected, string? actual, int count)? FirstDivergence(List<string> expected, List<string> actual)
        {
            int max = Math.Max(expected.Count, actual.Count);
            int first = -1, count = 0;
            for (int i = 0; i < max; i++)
            {
                string? e = i < expected.Count ? expected[i] : null;
                string? a = i < actual.Count ? actual[i] : null;
                if (e != a)
                {
                    if (first == -1) first = i;
                    count++;
                }
            }
            if (first == -1) return null;
            return (first + 1, first < expected.Count ? expected[first] : null, first < actual.Count ? actual[first] : null, count);
        }
    }
}
