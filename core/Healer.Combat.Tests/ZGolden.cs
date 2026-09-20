using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Healer.Combat.Tests
{
    /// <summary>Crée les fichiers golden MANQUANTS des nouveaux scénarios. N'écrase jamais un fichier existant.</summary>
    public class ZGolden
    {
        [Test, Explicit]
        public void Creer_les_golden_manquants()
        {
            foreach (var s in Scenario.All)
            {
                var file = Path.Combine(Fixtures.GoldenDir, s.Name + ".txt");
                if (File.Exists(file)) continue;
                File.WriteAllText(file, string.Join("\n", s.Record()) + "\n");
                TestContext.Progress.WriteLine("créé : " + file);
            }
        }
    }
}
