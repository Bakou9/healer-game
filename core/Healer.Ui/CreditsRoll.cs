using System.Collections.Generic;
using Healer.Combat;

namespace Healer.Ui
{
    public enum CreditLineKind { Title, Heading, Name, Detail, Thanks, Space }

    public readonly struct CreditLine
    {
        public CreditLineKind Kind { get; }
        public string Text { get; }
        public CreditLine(CreditLineKind kind, string text = "") { Kind = kind; Text = text; }

        /// <summary>Hauteur de la ligne sur la grille logique (le client fait défiler le tout).</summary>
        public double Height => Kind switch
        {
            CreditLineKind.Title => 96,
            CreditLineKind.Heading => 58,
            CreditLineKind.Name => 36,
            CreditLineKind.Detail => 26,
            CreditLineKind.Thanks => 44,
            _ => 30,
        };
    }

    /// <summary>Le générique : remercie chaque auteur de chaque ressource (D-060). Pur et testé.</summary>
    public static class CreditsRoll
    {
        public static IReadOnlyList<CreditLine> Build(CreditsData data)
        {
            var lines = new List<CreditLine>
            {
                new CreditLine(CreditLineKind.Title, data.Title),
                new CreditLine(CreditLineKind.Space),
                new CreditLine(CreditLineKind.Thanks, "Merci à tous les auteurs dont le travail rend ce jeu possible."),
                new CreditLine(CreditLineKind.Space),
                new CreditLine(CreditLineKind.Heading, "Ressources"),
            };
            if (data.Assets.Count == 0)
                lines.Add(new CreditLine(CreditLineKind.Detail, "Aucune ressource externe pour l'instant : modèles, sons et musique sont générés par le code."));
            foreach (var a in data.Assets) AddEntry(lines, a);
            lines.Add(new CreditLine(CreditLineKind.Space));
            lines.Add(new CreditLine(CreditLineKind.Heading, "Outils"));
            foreach (var t in data.Tools) AddEntry(lines, t);
            lines.Add(new CreditLine(CreditLineKind.Space));
            lines.Add(new CreditLine(CreditLineKind.Thanks, "Merci d'avoir joué !"));
            return lines;
        }

        private static void AddEntry(List<CreditLine> lines, CreditEntry e)
        {
            lines.Add(new CreditLine(CreditLineKind.Space));
            lines.Add(new CreditLine(CreditLineKind.Name, "« " + e.Name + " » — " + e.Author));
            lines.Add(new CreditLine(CreditLineKind.Detail, e.UsedFor + " · licence " + e.License + (e.Modified ? " · modifié" : "")));
            lines.Add(new CreditLine(CreditLineKind.Detail, e.Url));
        }

        public static double TotalHeight(IReadOnlyList<CreditLine> lines)
        {
            double h = 0;
            foreach (var l in lines) h += l.Height;
            return h;
        }
    }
}
