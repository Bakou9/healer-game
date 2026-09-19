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
        public BossDef Boss { get; }

        public GameContent(List<CharacterDef> characters, List<SkillDef> skills, List<EffectDef> effects, BossDef boss)
        {
            Characters = characters;
            Skills = skills;
            Effects = effects;
            Boss = boss;
        }

        public static GameContent FromJson(string charactersJson, string skillsJson, string effectsJson, string bossJson)
        {
            return new GameContent(
                Parse<List<CharacterDef>>(charactersJson, "characters.json"),
                Parse<List<SkillDef>>(skillsJson, "skills.json"),
                Parse<List<EffectDef>>(effectsJson, "effects.json"),
                Parse<BossDef>(bossJson, "boss1.json"));
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
        public EncounterDef CreateEncounter(uint seed) => new EncounterDef
        {
            Id = "encounter-" + Boss.Id,
            Boss = Boss,
            Allies = Characters,
            Effects = Effects,
            Skills = Skills,
            Seed = seed,
        };
    }
}
