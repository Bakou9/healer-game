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

    /// <summary>Écran de l'application : menu principal, choix du niveau, combat.</summary>
    public enum AppScreen
    {
        MainMenu,
        LevelSelect,
        /// <summary>Atelier : équipement et talents achetés avec l'or.</summary>
        Workshop,
        /// <summary>Réglages : volumes et confort.</summary>
        Settings,
        /// <summary>Générique : les auteurs des ressources.</summary>
        Credits,
        /// <summary>Galerie de modèles (mode développeur).</summary>
        Gallery,
        /// <summary>Fiches de personnages (D-084) : les mêmes statistiques que le menu de pause, hors combat.</summary>
        Roster,
        Battle,
    }

    /// <summary>Gestes que l'interface peut déclencher (clic, toucher ou raccourci clavier).</summary>
    public enum UiAction
    {
        // Combat
        StartFight,
        TapAlly,
        TapSkill,
        TogglePause,
        Restart,
        NextLevel,
        BackToMap,
        // Menu principal
        MenuPlay,
        MenuWorkshop,
        MenuSettings,
        MenuToggleSound,
        MenuQuit,
        MenuCredits,
        MenuGallery,
        MenuRoster,
        /// <summary>Tous les boutons de la galerie de modèles (sélection, paliers, poses, rotation, zoom).</summary>
        GalleryControl,
        // Choix du niveau
        PickLevel,
        BackToMenu,
        // Atelier
        BuyEquipment,
        PickTalent,
        RespecTalents,
        BuyRelic,
        PickRelic,
        SelectVoie,
        // Réglages
        AdjustSetting,
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

        /// <summary>Gestes permis pendant un combat, selon l'état de l'écran de combat.</summary>
        public static bool Allows(ScreenState state, UiAction action)
        {
            switch (state)
            {
                case ScreenState.Start: return action == UiAction.StartFight || action == UiAction.BackToMap;
                case ScreenState.Ended: return action == UiAction.Restart || action == UiAction.NextLevel || action == UiAction.BackToMap;
                case ScreenState.Paused: return action == UiAction.TogglePause || action == UiAction.TapAlly || action == UiAction.BackToMap;
                default: return action == UiAction.TapAlly || action == UiAction.TapSkill || action == UiAction.TogglePause;
            }
        }

        /// <summary>Gestes permis selon l'écran de l'application : un écran ne laisse jamais passer les gestes d'un autre.</summary>
        public static bool Allows(AppScreen screen, ScreenState battleState, UiAction action)
        {
            switch (screen)
            {
                case AppScreen.MainMenu:
                    return action == UiAction.MenuPlay || action == UiAction.MenuWorkshop || action == UiAction.MenuSettings || action == UiAction.MenuToggleSound || action == UiAction.MenuQuit || action == UiAction.MenuCredits || action == UiAction.MenuGallery || action == UiAction.MenuRoster;
                case AppScreen.Settings:
                    return action == UiAction.AdjustSetting || action == UiAction.BackToMenu;
                case AppScreen.Credits:
                    return action == UiAction.BackToMenu;
                case AppScreen.Gallery:
                    return action == UiAction.GalleryControl || action == UiAction.BackToMenu;
                case AppScreen.Roster:
                    return action == UiAction.BackToMenu;
                case AppScreen.Workshop:
                    return action == UiAction.BuyEquipment || action == UiAction.PickTalent || action == UiAction.RespecTalents || action == UiAction.BuyRelic || action == UiAction.PickRelic || action == UiAction.SelectVoie || action == UiAction.BackToMenu;
                case AppScreen.LevelSelect:
                    return action == UiAction.PickLevel || action == UiAction.BackToMenu;
                default:
                    return Allows(battleState, action);
            }
        }

        /// <summary>
        /// Touche de confirmation en fin de combat : niveau suivant après une victoire s'il existe, sinon rejouer.
        /// </summary>
        public static UiAction ConfirmAction(ScreenState state, bool victory, bool hasNextLevel) =>
            state == ScreenState.Ended && victory && hasNextLevel ? UiAction.NextLevel : ConfirmAction(state);

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
