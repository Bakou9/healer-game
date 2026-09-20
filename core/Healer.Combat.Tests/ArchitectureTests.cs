using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>
    /// Garde-fous d'architecture (règles de CLAUDE.md et docs/ARCHITECTURE_UNITY.md, ticket E14-T09) :
    /// le cœur reste pur et déterministe. Ces tests ne s'affaiblissent pas.
    /// </summary>
    public class ArchitectureTests
    {
        private static readonly string CombatDir = Path.Combine(Fixtures.CoreDir, "Healer.Combat");
        private static readonly string[] PureLibraries = { "Healer.Combat", "Healer.Ui" };

        private static IEnumerable<(string name, string code)> CoreSources()
        {
            foreach (var file in PureLibraries.SelectMany(lib => Directory.GetFiles(Path.Combine(Fixtures.CoreDir, lib), "*.cs", SearchOption.AllDirectories)))
            {
                var sep = Path.DirectorySeparatorChar;
                if (file.Contains($"{sep}obj{sep}") || file.Contains($"{sep}bin{sep}")) continue;
                var raw = File.ReadAllText(file);
                // On retire les commentaires : ils ont le droit de citer ces termes.
                var code = Regex.Replace(raw, @"/\*[\s\S]*?\*/", "");
                code = Regex.Replace(code, @"//.*$", "", RegexOptions.Multiline);
                yield return (Path.GetFileName(file), code);
            }
        }

        private static readonly (string pattern, string why)[] Forbidden =
        {
            (@"\bUnityEngine\b", "le cœur ne doit jamais dépendre d'Unity"),
            (@"\bSystem\.Random\b|\bnew\s+Random\s*\(", "aléa interdit hors du Rng seedé (déterminisme)"),
            (@"\bDateTime\b|\bDateTimeOffset\b|\bStopwatch\b|Environment\.TickCount", "l'horloge réelle casse le déterminisme : utiliser l'horloge de Battle"),
            (@"\bFile\.|\bDirectory\.|\bStreamReader\b|\bStreamWriter\b|\bFileStream\b", "pas d'accès disque dans le cœur (il doit pouvoir tourner sur un serveur)"),
            (@"\bHttpClient\b|\bWebClient\b|\bSocket\b", "pas d'accès réseau dans le cœur"),
            (@"\bConsole\.", "pas d'écriture console dans le cœur"),
        };

        [Test]
        public void Le_cœur_est_bien_trouve()
        {
            Assert.That(CoreSources().Select(s => s.name), Does.Contain("Battle.cs").And.Contain("Format.cs"));
        }

        [TestCaseSource(nameof(ForbiddenCases))]
        public void Le_cœur_reste_pur(string pattern, string why)
        {
            var offenders = CoreSources().Where(s => Regex.IsMatch(s.code, pattern)).Select(s => s.name).ToList();
            Assert.That(offenders, Is.Empty, why);
        }

        public static IEnumerable<TestCaseData> ForbiddenCases() =>
            Forbidden.Select(f => new TestCaseData(f.pattern, f.why).SetName("Le_cœur_reste_pur : " + f.why));

        [TestCase("Healer.Combat")]
        [TestCase("Healer.Ui")]
        public void Chaque_bibliotheque_est_un_paquet_Unity_sans_references_moteur(string lib)
        {
            var asmdef = File.ReadAllText(Path.Combine(Fixtures.CoreDir, lib, lib + ".asmdef"));
            Assert.That(asmdef, Does.Contain("\"noEngineReferences\": true"), "le paquet ne doit référencer aucun module moteur");
            Assert.That(File.Exists(Path.Combine(Fixtures.CoreDir, lib, "package.json")), Is.True);
        }

        [Test]
        public void Les_dependances_vont_vers_le_bas_le_cœur_ne_depend_jamais_de_l_interface()
        {
            // Sens autorisé : Healer.Ui peut utiliser Healer.Combat ; l'inverse est interdit (docs/ARCHITECTURE_UNITY.md).
            var combatCode = CoreSources().Where(s => File.Exists(Path.Combine(CombatDir, s.name))).ToList();
            Assert.That(combatCode, Is.Not.Empty);
            Assert.That(combatCode.Where(s => Regex.IsMatch(s.code, @"\bHealer\.Ui\b")).Select(s => s.name), Is.Empty);
            var csproj = File.ReadAllText(Path.Combine(CombatDir, "Healer.Combat.csproj"));
            Assert.That(csproj, Does.Not.Contain("Healer.Ui"));
            var asmdef = File.ReadAllText(Path.Combine(CombatDir, "Healer.Combat.asmdef"));
            Assert.That(asmdef, Does.Not.Contain("Healer.Ui"));
        }

        [Test]
        public void Aucun_fichier_genere_par_dotnet_ne_traine_a_cote_des_sources()
        {
            foreach (var lib in PureLibraries)
            {
                Assert.That(Directory.Exists(Path.Combine(Fixtures.CoreDir, lib, "obj")), Is.False, lib + "/obj : Unity compilerait ces fichiers");
                Assert.That(Directory.Exists(Path.Combine(Fixtures.CoreDir, lib, "bin")), Is.False, lib + "/bin");
            }
        }

        [Test]
        public void Le_projet_du_cœur_ne_reference_pas_Unity()
        {
            var csproj = File.ReadAllText(Path.Combine(CombatDir, "Healer.Combat.csproj"));
            Assert.That(csproj, Does.Not.Contain("UnityEngine"));
            Assert.That(csproj, Does.Contain("netstandard2.1"), "compatibilité Unity 6");
        }
    }
}
