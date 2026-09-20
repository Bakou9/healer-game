using System;
using System.Globalization;

namespace Healer.Ui
{
    /// <summary>
    /// Affichage des valeurs à hauteur humaine (règle du projet, CLAUDE.md et docs/EQUILIBRAGE.md §3) :
    /// tout nombre montré au joueur est TRONQUÉ (jamais arrondi vers le haut : on n'affiche pas 1 PV de
    /// plus que la réalité), sans décimales inutiles, et abrégé quand il est long. Aucun nombre brut
    /// (7.199999, 1234567) ne doit atteindre l'écran : passer par ces fonctions, jamais par ToString("F1")
    /// ou Mathf.Round. Port fidèle de src/ui/format.ts de la version Phaser.
    /// </summary>
    public static class Format
    {
        /// <summary>Tronque vers zéro à `decimals` décimales, en absorbant l'erreur des flottants (4,35 × 100 = 434,999…).</summary>
        public static double Truncate(double value, int decimals = 0)
        {
            double factor = Math.Pow(10, decimals);
            double scaled = value * factor;
            double epsilon = Math.Abs(scaled) * 1e-12 + 1e-9;
            return Math.Truncate(scaled + Math.Sign(scaled) * epsilon) / factor;
        }

        private static string WithComma(double n)
        {
            string s = n == Math.Floor(n) ? ((long)n).ToString(CultureInfo.InvariantCulture) : n.ToString("R", CultureInfo.InvariantCulture);
            return s.Replace('.', ',');
        }

        /// <summary>Nombre à `decimals` décimales tronquées : (6,39 ; 1) → "6,3", (6 ; 1) → "6".</summary>
        public static string Decimal(double value, int decimals = 1) => WithComma(Truncate(value, decimals));

        /// <summary>7000 → "7000", 12399 → "12,3k", 2 500 000 → "2,5M". Tronqué, jamais arrondi.</summary>
        public static string Number(double value)
        {
            double abs = Math.Abs(value);
            if (abs >= 1_000_000) return WithComma(Truncate(value / 1_000_000, 1)) + "M";
            if (abs >= 10_000) return WithComma(Truncate(value / 1_000, 1)) + "k";
            return WithComma(Truncate(value));
        }

        /// <summary>Durée en secondes, une décimale tronquée : 4960 → "4,9s", 12000 → "12s".</summary>
        public static string Seconds(double ms) => WithComma(Truncate(Math.Max(0, ms) / 1000, 1)) + "s";

        /// <summary>"612/900" (valeur actuelle / maximum), tronqués.</summary>
        public static string Ratio(double current, double max) => Number(current) + "/" + Number(max);
    }
}
