using System.Collections.Generic;
using System.Linq;

namespace Healer.Combat.Progress
{
    /// <summary>Un palier de niveau du Soigneur (core/content/leveling.json, E02-T06, D-084) : XP cumulée pour
    /// l'atteindre, et points de talent gagnés à ce moment-là.</summary>
    public class LevelUpDef
    {
        public int Level { get; set; }
        /// <summary>XP CUMULÉE (depuis 0) nécessaire pour atteindre ce niveau.</summary>
        public int XpRequired { get; set; }
        /// <summary>Points de talent (E02-T03) gagnés en atteignant ce niveau.</summary>
        public int TalentPoints { get; set; }
    }

    /// <summary>
    /// Niveaux et XP du Soigneur (E02-T06, D-084). Le niveau 1 est le départ (0 XP, 0 point) et n'est pas dans
    /// la courbe. Pur et déterministe : l'XP passe par le portefeuille (Wallet.Xp), comme l'or ; les points de
    /// talent gagnés passent aussi par le portefeuille (Wallet.TalentPoints), jamais accordés directement.
    /// </summary>
    public static class HealerLeveling
    {
        /// <summary>Niveau atteint avec cette XP cumulée (1 si en dessous du premier palier de la courbe).</summary>
        public static int LevelForXp(IReadOnlyList<LevelUpDef> curve, int xp)
        {
            int level = 1;
            foreach (var l in curve.OrderBy(l => l.Level))
                if (xp >= l.XpRequired) level = l.Level;
            return level;
        }

        /// <summary>XP cumulée nécessaire pour le PROCHAIN niveau (-1 si la courbe est épuisée : niveau maximum atteint).</summary>
        public static int XpForNextLevel(IReadOnlyList<LevelUpDef> curve, int currentLevel)
        {
            var next = curve.Where(l => l.Level == currentLevel + 1).Select(l => (int?)l.XpRequired).FirstOrDefault();
            return next ?? -1;
        }

        /// <summary>Niveau maximum défini par la courbe (1 si la courbe est vide).</summary>
        public static int MaxLevel(IReadOnlyList<LevelUpDef> curve) => curve.Count == 0 ? 1 : curve.Max(l => l.Level);

        public static int TotalTalentPoints(IReadOnlyList<LevelUpDef> curve, int atLevel) =>
            curve.Where(l => l.Level <= atLevel).Sum(l => l.TalentPoints);

        /// <summary>Niveau actuel du Soigneur (XP dans profile.Wallet).</summary>
        public static int LevelOf(PlayerProfile profile, GameContent content) =>
            LevelForXp(content.Leveling, profile.Wallet.Balance(Wallet.Xp));

        /// <summary>Solde BRUT de points de talent du portefeuille (avant déduction des paliers achetés) : la
        /// SEULE source de vérité (comme l'or), jamais re-dérivée de la courbe XP — sinon des points accordés
        /// autrement (mode développeur, ancienne courbe) seraient effacés au chargement (PlayerProfile.Repair).</summary>
        public static int WalletTalentPoints(PlayerProfile profile) => profile.Wallet.Balance(Wallet.TalentPoints);

        /// <summary>Points de talent dépensés (un par palier acheté, coût = TalentTierDef.Cost).</summary>
        public static int TalentPointsSpent(PlayerProfile profile, GameContent content) =>
            profile.Loadout.Talents.Keys.Sum(tier => content.Upgrades.Tier(tier)?.Cost ?? 0);

        /// <summary>Points de talent disponibles = gagnés par le niveau, moins dépensés (jamais négatif).</summary>
        public static int TalentPointsAvailable(PlayerProfile profile, GameContent content) =>
            System.Math.Max(0, profile.Wallet.Balance(Wallet.TalentPoints) - TalentPointsSpent(profile, content));

        /// <summary>
        /// Ajoute l'XP au portefeuille et accorde les points de talent des niveaux nouvellement franchis.
        /// Renvoie les niveaux gagnés (vide si aucun), pour que la présentation puisse fêter la montée de niveau.
        /// </summary>
        public static List<int> GrantXp(PlayerProfile profile, GameContent content, int amount, string reason)
        {
            int before = LevelOf(profile, content);
            profile.Wallet.Grant(Wallet.Xp, amount, reason);
            int after = LevelOf(profile, content);
            var gained = new List<int>();
            for (int lvl = before + 1; lvl <= after; lvl++) gained.Add(lvl);
            int points = content.Leveling.Where(l => l.Level > before && l.Level <= after).Sum(l => l.TalentPoints);
            if (points > 0) profile.Wallet.Grant(Wallet.TalentPoints, points, "niveau " + after);
            return gained;
        }
    }
}
