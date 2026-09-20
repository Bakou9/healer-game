using System;
using System.IO;

namespace Healer.Combat.Tests
{
    /// <summary>Accès aux fichiers partagés de core/ (contenu JSON et références golden).</summary>
    public static class Fixtures
    {
        public static readonly string CoreDir = FindCoreDir();
        public static string ContentDir => Path.Combine(CoreDir, "content");
        public static string GoldenDir => Path.Combine(CoreDir, "golden");

        private static string FindCoreDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "core");
                if (Directory.Exists(Path.Combine(candidate, "content"))) return candidate;
                if (Directory.Exists(Path.Combine(dir.FullName, "content")) && Directory.Exists(Path.Combine(dir.FullName, "golden")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("Dossier core/ (content et golden) introuvable depuis " + AppContext.BaseDirectory);
        }

        private static string Read(string name) => File.ReadAllText(Path.Combine(ContentDir, name));

        public static string CharactersJson => Read("characters.json");
        public static string SkillsJson => Read("skills.json");
        public static string EffectsJson => Read("effects.json");
        public static string BossJson => Read("boss1.json");

        public static GameContent Content() =>
            GameContent.FromJson(CharactersJson, SkillsJson, EffectsJson, BossJson);

        public static EncounterDef Encounter(uint seed) => Content().CreateEncounter(seed);
    }
}
