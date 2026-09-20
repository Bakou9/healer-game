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
        public const int DefaultMusic = 60;
        public const int DefaultSfx = 80;

        /// <summary>Coupe tout le son (touche M, bouton du menu) sans perdre les volumes choisis.</summary>
        public bool Muted { get; set; }

        /// <summary>Volume de la musique, 0 à 100 (par pas de 10).</summary>
        public int MusicVolume { get; set; } = DefaultMusic;

        /// <summary>Volume des effets sonores, 0 à 100 (par pas de 10).</summary>
        public int SfxVolume { get; set; } = DefaultSfx;

        /// <summary>Secousse de caméra : la désactiver aide les joueurs sensibles au mouvement.</summary>
        public bool ScreenShake { get; set; } = true;
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

        /// <summary>Équipement acheté et talents choisis.</summary>
        public Loadout Loadout { get; } = new Loadout();

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
            // Améliorations : on retire l'inconnu, on borne les niveaux, on ne garde que des paliers de talent cohérents.
            foreach (var id in Loadout.Equipment.Keys.ToList())
            {
                var track = content.Upgrades.Track(id);
                if (track == null || !characterIds.Contains(track.CharacterId)) { Loadout.Equipment.Remove(id); continue; }
                int level = System.Math.Max(0, System.Math.Min(track.MaxLevel, Loadout.Equipment[id]));
                if (level == 0) Loadout.Equipment.Remove(id); else Loadout.Equipment[id] = level;
            }
            foreach (var tier in Loadout.Talents.Keys.ToList())
            {
                var def = content.Upgrades.Tier(tier);
                if (def == null || def.Options.All(o => o.Id != Loadout.Talents[tier])) Loadout.Talents.Remove(tier);
            }
            // Un palier n'existe que si le précédent existe et si les étoiles requises sont là.
            int expected = 1;
            foreach (var tier in Loadout.Talents.Keys.OrderBy(k => k).ToList())
            {
                var def = content.Upgrades.Tier(tier)!;
                if (tier != expected || TotalStars < def.RequiresStars) { Loadout.Talents.Remove(tier); continue; }
                expected++;
            }

            // Un niveau ne peut pas être terminé sans que son prérequis l'ait été.
            foreach (var level in content.Levels)
                if (level.Requires != null && RecordOf(level.Id).Completed && !RecordOf(level.Requires).Completed)
                    Levels[level.Id].Completed = false;
        }
    }
}
