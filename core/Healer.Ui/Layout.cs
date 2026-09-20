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
    /// Jetons de design et mise en page PAYSAGE de l'écran de combat (décision D-042 : PC d'abord, mobile
    /// toujours compatible ; docs/UX.md, tickets E04-T02 et E04-T03), en pixels LOGIQUES sur une toile de
    /// 1280×720 (16:9). Le client Unity adapte cette grille à l'écran ; les règles UX (cibles ≥ 48 px, texte
    /// ≥ 14 px, marges de sécurité) sont testées ici sans Unity. La version Phaser gardait la grille portrait 480×854.
    /// </summary>
    public static class Layout
    {
        public const double GameW = 1280;
        public const double GameH = 720;

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
        public const double SafeBottom = 16;
        public const double SafeSide = 12;
        public const double Gap = 8;

        /// <summary>Zones de l'écran : colonne gauche (alliés), scène centrale, colonne droite (sorts) (docs/UX.md §4, D-046).</summary>
        public static class Zones
        {
            /// <summary>Largeur de chaque colonne latérale (un pouce par colonne, D-046).</summary>
            public const double SideW = 264;

            /// <summary>Colonne de gauche : cartes d'alliés (barres de vie), pouce gauche.</summary>
            public const double LeftX = SafeSide;

            /// <summary>Colonne de droite : sorts et mana, pouce droit (symétrique de la gauche).</summary>
            public const double RightX = GameW - SafeSide - SideW;

            /// <summary>Scène centrale (boss et alliés en 3D), entre les deux colonnes.</summary>
            public const double CenterX = LeftX + SideW + SafeSide;
            public const double CenterW = RightX - SafeSide - CenterX;

            public static readonly Rect TopBar = new Rect(0, SafeTop, GameW, 52);
            public static readonly Rect Boss = new Rect(CenterX, 64, CenterW, 420);
            public static readonly Rect Team = new Rect(LeftX, 72, SideW, 632);
            public static readonly Rect Strip = new Rect(RightX, 72, SideW, 40);
            public static readonly Rect Skills = new Rect(RightX, 120, SideW, 584);
            public static readonly Rect Band = new Rect(CenterX, 660, CenterW, 44);

            public static IReadOnlyList<(string name, Rect rect)> InOrder => new[]
            {
                ("topBar", TopBar), ("boss", Boss), ("band", Band), ("team", Team), ("strip", Strip), ("skills", Skills),
            };
        }

        /// <summary>Bouton de pause : cible tactile ≥ 48 px, en haut à droite.</summary>
        public static readonly Rect PauseButton = new Rect(GameW - SafeSide - MinTouch, SafeTop, MinTouch, MinTouch);

        /// <summary>Boutons des écrans modaux (démarrage, bilan). Volontairement au-dessus de la mise en page : voir InputGate.</summary>
        public static readonly Rect StartButton = new Rect(GameW / 2 - 120, 470, 240, 60);
        public static readonly Rect RestartButton = new Rect(GameW / 2 - 120, 450, 240, 56);
        // ---- Autres écrans (menu principal, choix du niveau, fin de combat, pause) ----

        /// <summary>Boutons du menu principal, empilés au centre : Jouer, Son, Quitter.</summary>
        public static readonly Rect MenuPlay = new Rect(GameW / 2 - 160, 290, 320, 64);
        public static readonly Rect MenuWorkshop = new Rect(GameW / 2 - 160, 370, 320, 56);
        public static readonly Rect MenuSound = new Rect(GameW / 2 - 160, 442, 320, 56);
        public static readonly Rect MenuQuit = new Rect(GameW / 2 - 160, 514, 320, 56);

        /// <summary>Bouton « retour » en haut à gauche des écrans secondaires.</summary>
        public static readonly Rect BackButton = new Rect(SafeSide, SafeTop, 150, MinTouch);

        public const double LevelCardW = 300;
        public const double LevelCardH = 380;

        /// <summary>Cartes de niveau, centrées sur une rangée.</summary>
        public static Rect[] LevelCardRects(int count)
        {
            // Taille pleine tant que ça tient ; sinon les cartes rétrécissent pour que toutes restent visibles.
            double w = Math.Min(LevelCardW, (GameW - 2 * SafeSide - (count - 1) * 28) / count);
            double total = count * w + (count - 1) * 28;
            double x0 = (GameW - total) / 2;
            var result = new Rect[count];
            for (int i = 0; i < count; i++) result[i] = new Rect(x0 + i * (w + 28), 150, w, LevelCardH);
            return result;
        }

        // ---- Atelier : équipement à gauche (une carte par piste), talents à droite (trois paliers de deux options) ----

        /// <summary>Panneau d'équipement (deux colonnes de cartes).</summary>
        public static readonly Rect WorkshopEquipment = new Rect(SafeSide, 84, 760, 588);

        /// <summary>Panneau de talents (un palier par ligne, deux options côte à côte).</summary>
        public static readonly Rect WorkshopTalents = new Rect(SafeSide + 760 + SafeSide, 84, GameW - 2 * SafeSide - 760 - SafeSide, 588);

        /// <summary>Cartes de pistes d'équipement : deux colonnes, remplies ligne par ligne.</summary>
        public static Rect[] WorkshopEquipmentCards(int count)
        {
            int rows = Math.Max(1, (count + 1) / 2);
            double w = (WorkshopEquipment.W - Gap) / 2;
            double h = (WorkshopEquipment.H - Gap * (rows - 1)) / rows;
            var result = new Rect[count];
            for (int i = 0; i < count; i++)
                result[i] = new Rect(WorkshopEquipment.X + (i % 2) * (w + Gap), WorkshopEquipment.Y + (i / 2) * (h + Gap), w, h);
            return result;
        }

        /// <summary>Bouton « Acheter » d'une carte d'équipement, en haut à droite (cible tactile ≥ 48 px).</summary>
        public static Rect WorkshopBuyButton(Rect card) => new Rect(card.Right - 128, card.Y + 12, 116, MinTouch);

        private const double TalentHeaderH = 34;

        /// <summary>Zone d'un palier de talent (titre + deux options).</summary>
        public static Rect WorkshopTalentTier(int index, int tierCount)
        {
            double h = (WorkshopTalents.H - Gap * (tierCount - 1)) / tierCount;
            return new Rect(WorkshopTalents.X, WorkshopTalents.Y + index * (h + Gap), WorkshopTalents.W, h);
        }

        /// <summary>Option (0 ou 1) d'un palier : deux cartes côte à côte sous le titre du palier.</summary>
        public static Rect WorkshopTalentOption(int index, int tierCount, int option)
        {
            var tier = WorkshopTalentTier(index, tierCount);
            double w = (tier.W - Gap) / 2;
            return new Rect(tier.X + option * (w + Gap), tier.Y + TalentHeaderH, w, tier.H - TalentHeaderH);
        }

        /// <summary>Bilan de combat : statistiques à gauche, dégâts infligés par membre à droite.</summary>
        public static readonly Rect EndStatsPanel = new Rect(100, 170, 500, 236);
        public static readonly Rect EndDamagePanel = new Rect(620, 170, 560, 236);

        public const double EndButtonW = 220;

        /// <summary>Boutons de fin de combat (Recommencer, Niveau suivant, Carte), centrés sur une rangée.</summary>
        public static Rect[] EndButtonRects(int count)
        {
            double total = count * EndButtonW + (count - 1) * 16;
            double x0 = (GameW - total) / 2;
            var result = new Rect[count];
            for (int i = 0; i < count; i++) result[i] = new Rect(x0 + i * (EndButtonW + 16), 590, EndButtonW, 56);
            return result;
        }

        /// <summary>Menu de pause : Reprendre, Quitter le niveau.</summary>
        public static readonly Rect PauseResume = new Rect(GameW / 2 - 140, 330, 280, 56);
        public static readonly Rect PauseLeave = new Rect(GameW / 2 - 140, 402, 280, 56);

        /// <summary>Barre de PV du boss : s'arrête avant le bouton de pause pour ne pas le chevaucher.</summary>
        public static readonly Rect BossHpBar = new Rect(Zones.CenterX + SafeSide, SafeTop + 34, Zones.CenterW - 2 * SafeSide, 14);

        /// <summary>Répartit `count` éléments égaux sur la largeur d'une zone, avec Gap entre eux.</summary>
        public static Rect[] SplitRow(Rect zone, int count)
        {
            double width = (zone.W - Gap * (count - 1)) / count;
            var result = new Rect[count];
            for (int i = 0; i < count; i++) result[i] = new Rect(zone.X + i * (width + Gap), zone.Y, width, zone.H);
            return result;
        }

        /// <summary>Répartit `count` éléments égaux sur la hauteur d'une zone, avec Gap entre eux (colonnes latérales).</summary>
        public static Rect[] SplitColumn(Rect zone, int count)
        {
            double height = (zone.H - Gap * (count - 1)) / count;
            var result = new Rect[count];
            for (int i = 0; i < count; i++) result[i] = new Rect(zone.X, zone.Y + i * (height + Gap), zone.W, height);
            return result;
        }

        public static Rect[] TeamCardRects(int count) => SplitColumn(Zones.Team, count);
        public static Rect[] SkillButtonRects(int count) => SplitColumn(Zones.Skills, count);

        /// <summary>Position logique (x) du n-ième allié dans la scène 3D : en ligne, centrée, devant le boss.</summary>
        public static double AllyStageX(int index, int count) => GameW / 2 + (index - (count - 1) / 2.0) * 165;

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
