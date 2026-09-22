using System.Collections.Generic;
using System.Linq;

namespace Healer.Combat.Progress
{
    /// <summary>Ce que le joueur obtient à la fin d'un combat.</summary>
    public sealed class RewardResult
    {
        public bool Victory { get; set; }
        public int Stars { get; set; }
        /// <summary>Étoiles gagnées pour la première fois (au-delà du meilleur résultat précédent).</summary>
        public int NewStars { get; set; }
        public bool FirstClear { get; set; }
        public bool NewBestTime { get; set; }
        public int GoldGained { get; set; }
        public List<string> UnlockedLevelIds { get; } = new List<string>();
        /// <summary>XP du Soigneur gagnée (E02-T06, D-084).</summary>
        public int XpGained { get; set; }
        /// <summary>Niveaux du Soigneur nouvellement atteints (vide = aucun), pour fêter la montée de niveau.</summary>
        public List<int> LevelsGained { get; } = new List<int>();
    }

    /// <summary>
    /// Règles de progression : étoiles, récompenses, déblocages. Pur et déterministe : mêmes statistiques de
    /// combat, même résultat. Toute récompense passe par le portefeuille (Wallet), jamais directement.
    /// </summary>
    public static class Progression
    {
        public const int MaxStars = 3;

        /// <summary>
        /// 0 étoile = défaite ; 1 = victoire ; 2 = victoire sans allié K.O. ; 3 = en plus, au plus le seuil de
        /// dégâts encaissés du niveau (les boucliers bien placés et les purges font baisser ce total).
        /// </summary>
        public static int Stars(LevelDef level, CombatStats stats)
        {
            if (stats.Result != BattleResults.Victory) return 0;
            if (stats.Deaths > 0) return 1;
            return stats.DamageTaken <= level.ThreeStarMaxDamageTaken ? 3 : 2;
        }

        /// <summary>Applique la fin d'un combat au profil (étoiles, meilleur temps, or, déblocages) et le raconte.</summary>
        public static RewardResult Complete(PlayerProfile profile, GameContent content, LevelDef level, CombatStats stats)
        {
            var result = new RewardResult();
            if (stats.Result != BattleResults.Victory) return result; // une défaite ne change rien, ne coûte rien
            result.Victory = true;
            result.Stars = Stars(level, stats);

            var before = profile.RecordOf(level.Id);
            var record = profile.Levels.TryGetValue(level.Id, out var existing) ? existing : (profile.Levels[level.Id] = new LevelRecord());
            var unlockedBefore = content.Levels.Where(profile.IsUnlocked).Select(l => l.Id).ToHashSet();

            result.FirstClear = !before.Completed;
            result.NewStars = System.Math.Max(0, result.Stars - before.BestStars);
            record.Completed = true;
            record.Clears++;
            record.BestStars = System.Math.Max(record.BestStars, result.Stars);
            if (record.BestTimeMs <= 0 || stats.DurationMs < record.BestTimeMs)
            {
                result.NewBestTime = record.BestTimeMs > 0;
                record.BestTimeMs = stats.DurationMs;
            }

            int gold = (result.FirstClear ? level.RewardGold : level.RepeatGold) + result.NewStars * level.StarBonusGold;
            if (gold > 0)
            {
                profile.Wallet.Grant(Wallet.Gold, gold, $"niveau:{level.Id}:{(result.FirstClear ? "première victoire" : "victoire")}");
                result.GoldGained = gold;
            }

            int xp = result.FirstClear ? level.RewardXp : level.RepeatXp;
            if (xp > 0)
            {
                result.XpGained = xp;
                result.LevelsGained.AddRange(HealerLeveling.GrantXp(profile, content, xp, $"niveau:{level.Id}:{(result.FirstClear ? "première victoire" : "victoire")}"));
            }

            foreach (var l in content.Levels)
                if (profile.IsUnlocked(l) && !unlockedBefore.Contains(l.Id)) result.UnlockedLevelIds.Add(l.Id);
            return result;
        }

        /// <summary>Niveau à proposer après celui-ci : le suivant de la liste s'il est débloqué.</summary>
        public static LevelDef? NextLevel(PlayerProfile profile, GameContent content, string levelId)
        {
            int i = content.Levels.FindIndex(l => l.Id == levelId);
            if (i < 0 || i + 1 >= content.Levels.Count) return null;
            var next = content.Levels[i + 1];
            return profile.IsUnlocked(next) ? next : null;
        }
    }
}
