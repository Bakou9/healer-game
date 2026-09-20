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

    /// <summary>
    /// Modèle 3D importé qui remplace la pièce dessinée par le code (D-062). Path = chemin sous Assets/Resources sans extension.
    /// Un chemin « Imported/&lt;Dossier&gt;/… » désigne une ressource externe : son dossier doit être crédité dans credits.json.
    /// Un chemin « Parts/… » désigne une création de l'équipe (pas de crédit).
    /// </summary>
    public sealed class AppearanceModel
    {
        public string Path { get; set; } = "";
        /// <summary>Taille voulue du modèle : sa plus grande dimension en CENTIÈMES d'unité du pivot (220 = 2,2). Le client normalise le fichier importé : FBX, glTF et OBJ n'ont pas la même unité. Entiers, comme toute donnée du jeu.</summary>
        public int Size { get; set; } = 100;
        /// <summary>Décalage local (x, y, z) par rapport au pivot de l'emplacement, en centièmes d'unité.</summary>
        public int[] Offset { get; set; } = new int[] { 0, 0, 0 };
        /// <summary>Rotation locale en degrés entiers (x, y, z).</summary>
        public int[] Euler { get; set; } = new int[] { 0, 0, 0 };

        /// <summary>Dossier de la ressource externe (« Imported/KayKit/Knight » → « KayKit »), ou null si le modèle est une création de l'équipe.</summary>
        public string? ImportedFolder
        {
            get
            {
                var parts = Path.Split('/');
                return parts.Length >= 3 && parts[0] == "Imported" ? parts[1] : null;
            }
        }
    }

    /// <summary>Une version d'une pièce : quelle pièce 3D (identifiant pour le client), quelle palette (rôle → couleur hexadécimale) et, facultativement, un modèle importé.</summary>
    public sealed class AppearanceEntry
    {
        public string Part { get; set; } = "";
        public Dictionary<string, string> Palette { get; set; } = new Dictionary<string, string>();
        /// <summary>Si présent, ce modèle remplace la pièce dessinée par le code ; s'il est introuvable, le client garde la version dessinée.</summary>
        public AppearanceModel? Model { get; set; }
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
                set.Parts[slot] = new AppearancePart(entry.Part, tier, TierName(level), entry.Palette, entry.Model);
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
        public AppearanceModel? Model { get; }
        public AppearancePart(string part, int tier, string tierName, IReadOnlyDictionary<string, string> palette, AppearanceModel? model = null)
        {
            Part = part; Tier = tier; TierName = tierName; Palette = palette; Model = model;
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
        public string Signature() => CharacterId + ":" + string.Join(",", Parts.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value.Part + "@" + k.Value.Tier + (k.Value.Model != null ? "#" + k.Value.Model.Path : "")));
    }
}
