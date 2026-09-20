using System.Collections.Generic;
using System.Linq;

namespace Healer.Combat.Progress
{
    /// <summary>Meilleur résultat d'un niveau.</summary>
    public sealed class LevelRecord
    {
        public bool Completed { get; set; }
        public int BestStars { get; set; }
        /// <summary>Durée de la meilleure victoire (0 = aucune).</summary>
        public double BestTimeMs { get; set; }
        public int Clears { get; set; }
    }

    public sealed class Settings
    {
        public bool Muted { get; set; }
    }

    /// <summary>
    /// Tout ce que le jeu retient du joueur : progression, personnages possédés, portefeuille, réglages.
    /// L'INVENTAIRE (« ce que je possède ») est séparé de la définition des personnages : en premium le joueur
    /// possède tout ; un futur gacha n'aura qu'à remplir cet inventaire autrement (D-047).
    /// </summary>
    public sealed class PlayerProfile
    {
        public const int CurrentVersion = 1;

        public int Version { get; set; } = CurrentVersion;
        public Dictionary<string, LevelRecord> Levels { get; } = new Dictionary<string, LevelRecord>();
        public List<string> OwnedCharacters { get; } = new List<string>();
        public Wallet Wallet { get; } = new Wallet();
        public Settings Settings { get; } = new Settings();

        /// <summary>Nouvelle partie : en premium, tous les personnages de base sont possédés d'emblée.</summary>
        public static PlayerProfile NewGame(GameContent content)
        {
            var p = new PlayerProfile();
            p.OwnedCharacters.AddRange(content.Characters.Select(c => c.Id));
            return p;
        }

        public LevelRecord RecordOf(string levelId) => Levels.TryGetValue(levelId, out var r) ? r : new LevelRecord();

        public bool Owns(string characterId) => OwnedCharacters.Contains(characterId);

        /// <summary>Un niveau est jouable s'il n'a pas de prérequis, ou si son prérequis est terminé.</summary>
        public bool IsUnlocked(LevelDef level) => level.Requires == null || RecordOf(level.Requires).Completed;

        public int TotalStars => Levels.Values.Sum(l => l.BestStars);

        /// <summary>Remet d'aplomb un profil chargé : retire l'inconnu, garantit le soigneur, borne les valeurs.</summary>
        public void Repair(GameContent content)
        {
            var levelIds = content.Levels.Select(l => l.Id).ToHashSet();
            foreach (var id in Levels.Keys.Where(k => !levelIds.Contains(k)).ToList()) Levels.Remove(id);
            foreach (var r in Levels.Values)
            {
                r.BestStars = System.Math.Max(0, System.Math.Min(3, r.BestStars));
                r.Clears = System.Math.Max(0, r.Clears);
                r.BestTimeMs = System.Math.Max(0, r.BestTimeMs);
                if (r.BestStars > 0) r.Completed = true;
            }
            var characterIds = content.Characters.Select(c => c.Id).ToHashSet();
            OwnedCharacters.RemoveAll(id => !characterIds.Contains(id));
            var seen = new HashSet<string>();
            OwnedCharacters.RemoveAll(id => !seen.Add(id));
            foreach (var c in content.Characters.Where(c => c.Role == "healer"))
                if (!OwnedCharacters.Contains(c.Id)) OwnedCharacters.Add(c.Id);
            if (OwnedCharacters.Count == 1) OwnedCharacters.AddRange(content.Characters.Where(c => !OwnedCharacters.Contains(c.Id) && c.Role == "tank").Select(c => c.Id));
            // Un niveau ne peut pas être terminé sans que son prérequis l'ait été.
            foreach (var level in content.Levels)
                if (level.Requires != null && RecordOf(level.Id).Completed && !RecordOf(level.Requires).Completed)
                    Levels[level.Id].Completed = false;
        }
    }
}
