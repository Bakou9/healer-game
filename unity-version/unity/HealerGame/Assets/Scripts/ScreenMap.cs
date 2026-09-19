using Healer.Ui;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Correspondance entre la grille logique de l'interface (480×854, Healer.Ui.Layout) et l'écran réel,
    /// avec bandes noires si le rapport d'aspect diffère. Sert à aligner la scène 3D sur les cartes de l'interface.
    /// </summary>
    public static class ScreenMap
    {
        public static float Scale { get; private set; } = 1f;
        public static float OffsetX { get; private set; }
        public static float OffsetY { get; private set; }

        public static void Refresh()
        {
            Scale = Mathf.Min(Screen.width / (float)Layout.GameW, Screen.height / (float)Layout.GameH);
            OffsetX = (Screen.width - (float)Layout.GameW * Scale) * 0.5f;
            OffsetY = (Screen.height - (float)Layout.GameH * Scale) * 0.5f;
        }

        /// <summary>Point logique (y compté depuis le haut) vers coordonnées de viewport Unity (0..1, y depuis le bas).</summary>
        public static Vector2 LogicalToViewport(float x, float y) =>
            new Vector2((OffsetX + x * Scale) / Screen.width, 1f - (OffsetY + y * Scale) / Screen.height);
    }
}
