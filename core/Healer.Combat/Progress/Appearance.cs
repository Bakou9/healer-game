using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Healer.Combat.Progress
{
    /// <summary>Palier d'apparence : à partir de ce niveau d'équipement, la pièce change de version.</summary>
    public sealed class AppearanceTierDef
    {
        public int MinLevel { get; set; }
        public string Name { get; set; } = "";
    }

    /// <summary>Une version d'une pièce : quelle pièce 3D (identifiant pour le client) et quelle palette (rôle → couleur hexadécimale).</summary>
    public sealed class AppearanceEntry
    {
        public string Part { get; set; } = "";
        public Dictionary<string, string> Palette { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Catalogue d'apparence (core/content/appearance.json, D-061) : pour chaque personnage et chaque emplacement d'équipement
    /// (arme, armure), une pièce et une palette par palier. Le niveau d'équipement acheté à l'Atelier choisit le palier :
    /// l'apparence suit donc ce que le joueur équipe. Données pures : le client ne fait que fabriquer la pièce demandée.
    /// </summary>
    public sealed class AppearanceCatalog
    {
        /// <summary>Emplacements d'équipement ; chacun correspond à une piste de l'Atelier « {personnage}_{emplacement} ».</summary>
        public static readonly IReadOnlyList<string> Slots = new[] { "weapon", "armor" };

        public List<AppearanceTierDef> Tiers { get; set; } = new List<AppearanceTierDef>(); // vide par défaut : Json.NET AJOUTE aux listes initialisées
        public Dictionary<string, Dictionary<string, List<AppearanceEntry>>> Characters { get; set; } = new Dictionary<string, Dictionary<string, List<AppearanceEntry>>>();

        public static AppearanceCatalog FromJson(string json) => JsonConvert.DeserializeObject<AppearanceCatalog>(json) ?? new AppearanceCatalog();

        /// <summary>Indice du palier atteint par un niveau d'équipement (0 = le plus simple).</summary>
        public int TierOf(int level)
        {
            int tier = 0;
            for (int i = 0; i < Tiers.Count; i++) if (level >= Tiers[i].MinLevel) tier = i;
            return tier;
        }

        public string TierName(int level) => Tiers.Count == 0 ? "" : Tiers[TierOf(level)].Name;

        /// <summary>Apparence d'un personnage avec l'équipement donné ; les emplacements sans données sont absents.</summary>
        public AppearanceSet Resolve(string characterId, Loadout? loadout)
        {
            var set = new AppearanceSet(characterId);
            if (!Characters.TryGetValue(characterId, out var slots)) return set;
            foreach (var slot in Slots)
            {
                if (!slots.TryGetValue(slot, out var versions) || versions.Count == 0) continue;
                int level = loadout?.LevelOf(characterId + "_" + slot) ?? 0;
                int tier = TierOf(level);
                var entry = versions[System.Math.Min(tier, versions.Count - 1)];
                set.Parts[slot] = new AppearancePart(entry.Part, tier, TierName(level), entry.Palette);
            }
            return set;
        }
    }

    /// <summary>Une pièce équipée : sa forme (Part), son palier (0 à n) et ses couleurs.</summary>
    public sealed class AppearancePart
    {
        public string Part { get; }
        public int Tier { get; }
        public string TierName { get; }
        public IReadOnlyDictionary<string, string> Palette { get; }
        public AppearancePart(string part, int tier, string tierName, IReadOnlyDictionary<string, string> palette)
        {
            Part = part; Tier = tier; TierName = tierName; Palette = palette;
        }
    }

    /// <summary>Apparence complète d'un personnage : une pièce par emplacement.</summary>
    public sealed class AppearanceSet
    {
        public string CharacterId { get; }
        public Dictionary<string, AppearancePart> Parts { get; } = new Dictionary<string, AppearancePart>();
        public AppearanceSet(string characterId) { CharacterId = characterId; }

        public int TierOf(string slot) => Parts.TryGetValue(slot, out var p) ? p.Tier : 0;

        /// <summary>Signature stable (personnage, pièces et paliers) : change si et seulement si l'apparence change.</summary>
        public string Signature() => CharacterId + ":" + string.Join(",", Parts.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value.Part + "@" + k.Value.Tier));
    }
}
