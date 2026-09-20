using Healer.Ui;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Correspondance entre la grille logique de l'interface (Healer.Ui.Layout) et l'écran réel, avec bandes
    /// noires si le rapport d'aspect diffère. Le calcul est dans Healer.Ui.ScreenFit (pur, testé) ; ici on ne
    /// fait que lire la taille de l'écran. Sert aussi à aligner la scène 3D sur les cartes de l'interface.
    /// </summary>
    public static class ScreenMap
    {
        private static ScreenFit _fit = new ScreenFit(Layout.GameW, Layout.GameH);
        public static float Scale => (float)_fit.Scale;
        public static float OffsetX => (float)_fit.OffsetX;
        public static float OffsetY => (float)_fit.OffsetY;

        public static void Refresh() => _fit = new ScreenFit(Screen.width, Screen.height);

        /// <summary>Point logique vers pixels d'écran (y compté depuis le haut).</summary>
        public static Vector2 LogicalToScreen(float x, float y) { var (sx, sy) = _fit.ToScreen(x, y); return new Vector2((float)sx, (float)sy); }

        /// <summary>Pixels d'écran (y depuis le haut) vers point logique.</summary>
        public static Vector2 ScreenToLogical(float x, float y) { var (lx, ly) = _fit.ToLogical(x, y); return new Vector2((float)lx, (float)ly); }

        /// <summary>Point logique (y compté depuis le haut) vers coordonnées de viewport Unity (0..1, y depuis le bas).</summary>
        public static Vector2 LogicalToViewport(float x, float y)
        {
            var (sx, sy) = _fit.ToScreen(x, y);
            return new Vector2((float)sx / Screen.width, 1f - (float)sy / Screen.height);
        }
    }
}
