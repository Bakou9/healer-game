namespace Healer.Ui
{
    /// <summary>État de l'écran de jeu du point de vue des entrées.</summary>
    public enum ScreenState
    {
        /// <summary>Écran de démarrage : le combat n'a pas commencé.</summary>
        Start,
        Playing,
        Paused,
        /// <summary>Victoire ou défaite : écran de bilan.</summary>
        Ended,
    }

    /// <summary>Gestes que l'interface peut déclencher (clic, toucher ou raccourci clavier).</summary>
    public enum UiAction
    {
        StartFight,
        TapAlly,
        TapSkill,
        TogglePause,
        Restart,
    }

    /// <summary>
    /// Quels gestes sont permis dans quel état de l'écran. Règle centrale contre les clics « avalés » : un
    /// écran modal (démarrage, bilan) recouvre des cartes et des sorts qui, dessinés avant lui, captaient le
    /// clic à sa place (bug constaté : le bouton « Jouer » ne réagissait pas). Souris, toucher et clavier passent
    /// TOUS par cette règle, pour que les trois se comportent pareil.
    /// </summary>
    public static class InputGate
    {
        public static ScreenState StateOf(bool started, bool paused, bool ended)
        {
            if (!started) return ScreenState.Start;
            if (ended) return ScreenState.Ended;
            return paused ? ScreenState.Paused : ScreenState.Playing;
        }

        public static bool Allows(ScreenState state, UiAction action)
        {
            switch (state)
            {
                case ScreenState.Start: return action == UiAction.StartFight;
                case ScreenState.Ended: return action == UiAction.Restart;
                case ScreenState.Paused: return action == UiAction.TogglePause || action == UiAction.TapAlly;
                default: return action == UiAction.TapAlly || action == UiAction.TapSkill || action == UiAction.TogglePause;
            }
        }

        /// <summary>Ce que fait la touche de confirmation (Espace / Entrée) selon l'état : jouer, pause / reprise, rejouer.</summary>
        public static UiAction ConfirmAction(ScreenState state)
        {
            switch (state)
            {
                case ScreenState.Start: return UiAction.StartFight;
                case ScreenState.Ended: return UiAction.Restart;
                default: return UiAction.TogglePause;
            }
        }
    }

    /// <summary>
    /// Ajustement de la grille logique (Layout) à l'écran réel : échelle uniforme, bandes noires centrées si le
    /// rapport d'aspect diffère. Pur et testé ; le client Unity n'y ajoute que la lecture de Screen.width/height.
    /// </summary>
    public readonly struct ScreenFit
    {
        public double Scale { get; }
        public double OffsetX { get; }
        public double OffsetY { get; }

        public ScreenFit(double screenW, double screenH)
        {
            if (screenW <= 0 || screenH <= 0) { Scale = 1; OffsetX = 0; OffsetY = 0; return; }
            Scale = System.Math.Min(screenW / Layout.GameW, screenH / Layout.GameH);
            OffsetX = (screenW - Layout.GameW * Scale) * 0.5;
            OffsetY = (screenH - Layout.GameH * Scale) * 0.5;
        }

        /// <summary>Point logique (y depuis le haut) → écran en pixels (y depuis le haut).</summary>
        public (double x, double y) ToScreen(double lx, double ly) => (OffsetX + lx * Scale, OffsetY + ly * Scale);

        /// <summary>Point écran (pixels, y depuis le haut) → point logique : sert à retrouver l'élément touché.</summary>
        public (double x, double y) ToLogical(double sx, double sy) => ((sx - OffsetX) / Scale, (sy - OffsetY) / Scale);
    }
}
