using System;
using System.Collections.Generic;

namespace Healer.Ui
{
    /// <summary>Rectangle en pixels logiques de l'écran de combat (origine en haut à gauche).</summary>
    public readonly struct Rect
    {
        public double X { get; }
        public double Y { get; }
        public double W { get; }
        public double H { get; }

        public Rect(double x, double y, double w, double h)
        {
            X = x; Y = y; W = w; H = h;
        }

        public double Right => X + W;
        public double Bottom => Y + H;

        public bool Overlaps(Rect o) => X < o.Right && o.X < Right && Y < o.Bottom && o.Y < Bottom;
        public bool InsideScreen() => X >= 0 && Y >= 0 && Right <= Layout.GameW && Bottom <= Layout.GameH;
    }

    /// <summary>Bande de couleur de la barre de PV (la couleur n'est jamais seule : la valeur est toujours affichée).</summary>
    public enum HpBand
    {
        High,
        Mid,
        Low,
    }

    /// <summary>
    /// Jetons de design et mise en page portrait de l'écran de combat (docs/UX.md, tickets E04-T02 et
    /// E04-T03), en pixels LOGIQUES sur une toile de 480×854. Le client Unity adapte cette grille à
    /// l'écran (Canvas Scaler) ; les règles UX (cibles ≥ 48 px, texte ≥ 14 px, marges de sécurité) sont
    /// testées ici sans Unity. Port fidèle de src/ui/layout.ts.
    /// </summary>
    public static class Layout
    {
        public const double GameW = 480;
        public const double GameH = 854;

        /// <summary>Cible tactile minimale : règle UX « ≥ 48 px ».</summary>
        public const double MinTouch = 48;

        /// <summary>Tailles de texte (px logiques). Aucune n'est inférieure à Small (règle UX « ≥ 14 px »).</summary>
        public static class Font
        {
            public const int Small = 14;
            public const int Body = 16;
            public const int Strong = 18;
            public const int Title = 20;
            public const int Banner = 28;
            public const int Cooldown = 24;

            public static readonly IReadOnlyDictionary<string, int> All = new Dictionary<string, int>
            {
                ["small"] = Small, ["body"] = Body, ["strong"] = Strong,
                ["title"] = Title, ["banner"] = Banner, ["cooldown"] = Cooldown,
            };
        }

        /// <summary>Marges de sécurité (encoches, barres système) ; à vérifier sur appareil réel (E11-T01).</summary>
        public const double SafeTop = 8;
        public const double SafeBottom = 40;
        public const double SafeSide = 12;
        public const double Gap = 8;

        /// <summary>Zones verticales de l'écran, de haut en bas (docs/UX.md §4).</summary>
        public static class Zones
        {
            public static readonly Rect TopBar = new Rect(0, SafeTop, GameW, 52);
            public static readonly Rect Boss = new Rect(0, 64, GameW, 186);
            public static readonly Rect Band = new Rect(SafeSide, 262, GameW - 2 * SafeSide, 66);
            public static readonly Rect Team = new Rect(SafeSide, 340, GameW - 2 * SafeSide, 200);
            public static readonly Rect Strip = new Rect(SafeSide, 552, GameW - 2 * SafeSide, 64);
            public static readonly Rect Skills = new Rect(SafeSide, 632, GameW - 2 * SafeSide, 164);

            public static IReadOnlyList<(string name, Rect rect)> InOrder => new[]
            {
                ("topBar", TopBar), ("boss", Boss), ("band", Band), ("team", Team), ("strip", Strip), ("skills", Skills),
            };
        }

        /// <summary>Bouton de pause : cible tactile ≥ 48 px, en haut à droite.</summary>
        public static readonly Rect PauseButton = new Rect(GameW - SafeSide - MinTouch, SafeTop, MinTouch, MinTouch);

        /// <summary>Barre de PV du boss : s'arrête avant le bouton de pause pour ne pas le chevaucher.</summary>
        public static readonly Rect BossHpBar = new Rect(SafeSide, SafeTop + 34, PauseButton.X - Gap - SafeSide, 14);

        /// <summary>Répartit `count` éléments égaux sur la largeur d'une zone, avec Gap entre eux.</summary>
        public static Rect[] SplitRow(Rect zone, int count)
        {
            double width = (zone.W - Gap * (count - 1)) / count;
            var result = new Rect[count];
            for (int i = 0; i < count; i++) result[i] = new Rect(zone.X + i * (width + Gap), zone.Y, width, zone.H);
            return result;
        }

        public static Rect[] TeamCardRects(int count) => SplitRow(Zones.Team, count);
        public static Rect[] SkillButtonRects(int count) => SplitRow(Zones.Skills, count);

        /// <summary>Bande de la barre de PV selon le ratio (seuils lisibles : > 60 %, > 30 %, sinon bas).</summary>
        public static HpBand HpBandFor(double ratio)
        {
            if (ratio > 0.6) return HpBand.High;
            if (ratio > 0.3) return HpBand.Mid;
            return HpBand.Low;
        }

        /// <summary>Borne le ratio de pixels de l'appareil : net sur écrans denses, sans surcoût excessif.</summary>
        public static int ClampRenderScale(double devicePixelRatio)
        {
            if (double.IsNaN(devicePixelRatio) || double.IsInfinity(devicePixelRatio) || devicePixelRatio < 1) return 1;
            return (int)Math.Min(Math.Ceiling(devicePixelRatio), 3);
        }
    }
}
