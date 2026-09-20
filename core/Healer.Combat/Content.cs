using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Healer.Combat
{
    /// <summary>
    /// Contenu de jeu (personnages, sorts, effets, boss) chargé depuis les JSON partagés de core/content.
    /// Le cœur ne lit JAMAIS de fichier lui-même (pas d'accès disque : il doit pouvoir tourner sur un
    /// serveur) : l'appelant (tests, client Unity) fournit le texte JSON.
    /// </summary>
    public sealed class GameContent
    {
        public List<CharacterDef> Characters { get; }
        public List<SkillDef> Skills { get; }
        public List<EffectDef> Effects { get; }
        public List<BossDef> Bosses { get; }
        public List<LevelDef> Levels { get; }

        /// <summary>Premier boss (compatibilité : les combats de référence et les tests historiques).</summary>
        public BossDef Boss => Bosses[0];

        public GameContent(List<CharacterDef> characters, List<SkillDef> skills, List<EffectDef> effects, BossDef boss)
            : this(characters, skills, effects, new List<BossDef> { boss }, new List<LevelDef>()) { }

        public GameContent(List<CharacterDef> characters, List<SkillDef> skills, List<EffectDef> effects, List<BossDef> bosses, List<LevelDef> levels)
        {
            Characters = characters;
            Skills = skills;
            Effects = effects;
            Bosses = bosses;
            Levels = levels;
        }

        public BossDef BossById(string id) =>
            Bosses.Find(b => b.Id == id) ?? throw new InvalidOperationException("Boss inconnu : " + id);

        public LevelDef LevelById(string id) =>
            Levels.Find(l => l.Id == id) ?? throw new InvalidOperationException("Niveau inconnu : " + id);

        public static GameContent FromJson(string charactersJson, string skillsJson, string effectsJson, string bossJson)
        {
            return new GameContent(
                Parse<List<CharacterDef>>(charactersJson, "characters.json"),
                Parse<List<SkillDef>>(skillsJson, "skills.json"),
                Parse<List<EffectDef>>(effectsJson, "effects.json"),
                Parse<BossDef>(bossJson, "boss1.json"));
        }

        /// <summary>Contenu complet : plusieurs boss (un JSON chacun, dans l'ordre) et les niveaux de la campagne.</summary>
        public static GameContent FromJson(string charactersJson, string skillsJson, string effectsJson, IEnumerable<string> bossJsons, string levelsJson)
        {
            var bosses = new List<BossDef>();
            int n = 0;
            foreach (var json in bossJsons) bosses.Add(Parse<BossDef>(json, "boss" + (++n) + ".json"));
            return new GameContent(
                Parse<List<CharacterDef>>(charactersJson, "characters.json"),
                Parse<List<SkillDef>>(skillsJson, "skills.json"),
                Parse<List<EffectDef>>(effectsJson, "effects.json"),
                bosses,
                Parse<List<LevelDef>>(levelsJson, "levels.json"));
        }

        private static T Parse<T>(string json, string label)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json) ?? throw new InvalidOperationException("JSON vide");
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Contenu invalide ({label}) : {e.Message}", e);
            }
        }

        /// <summary>
        /// Construit une rencontre reproductible. La graine contrôle tout l'aléatoire (ciblage des attaques
        /// du boss) : même graine + mêmes commandes = même combat.
        /// </summary>
        public EncounterDef CreateEncounter(uint seed) => CreateEncounter(Boss.Id, seed);

        public EncounterDef CreateEncounter(string bossId, uint seed) => CreateEncounter(bossId, seed, null);

        /// <summary>Rencontre avec l'équipe réellement possédée par le joueur (null = tous les personnages). Le soigneur en fait toujours partie.</summary>
        public EncounterDef CreateEncounter(string bossId, uint seed, IEnumerable<string>? ownedCharacterIds)
        {
            var owned = ownedCharacterIds == null ? null : new HashSet<string>(ownedCharacterIds);
            var team = owned == null ? Characters : Characters.FindAll(c => c.Role == "healer" || owned.Contains(c.Id));
            return Build(bossId, seed, team);
        }

        private EncounterDef Build(string bossId, uint seed, List<CharacterDef> team) => new EncounterDef
        {
            Id = "encounter-" + bossId,
            Boss = BossById(bossId),
            Allies = team,
            Effects = Effects,
            Skills = Skills,
            Seed = seed,
        };
    }
}
