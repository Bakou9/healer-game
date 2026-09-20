namespace Healer.Ui
{
    /// <summary>
    /// Navigation entre les écrans (menu → choix du niveau → combat → choix du niveau…). Pure : ne sait rien
    /// de Unity ni de la sauvegarde ; l'appelant lui dit si un niveau est débloqué. Une transition interdite
    /// renvoie false et ne change rien.
    /// </summary>
    public sealed class Navigator
    {
        public AppScreen Screen { get; private set; } = AppScreen.MainMenu;

        /// <summary>Niveau en cours (écran de combat) ; null ailleurs.</summary>
        public string? LevelId { get; private set; }

        public bool OpenLevelSelect()
        {
            if (Screen != AppScreen.MainMenu) return false;
            Screen = AppScreen.LevelSelect;
            return true;
        }

        public bool OpenWorkshop()
        {
            if (Screen != AppScreen.MainMenu) return false;
            Screen = AppScreen.Workshop;
            return true;
        }

        public bool OpenSettings()
        {
            if (Screen != AppScreen.MainMenu) return false;
            Screen = AppScreen.Settings;
            return true;
        }

        public bool OpenCredits()
        {
            if (Screen != AppScreen.MainMenu) return false;
            Screen = AppScreen.Credits;
            return true;
        }

        public bool OpenGallery()
        {
            if (Screen != AppScreen.MainMenu) return false;
            Screen = AppScreen.Gallery;
            return true;
        }

        public bool BackToMenu()
        {
            if (Screen != AppScreen.LevelSelect && Screen != AppScreen.Workshop && Screen != AppScreen.Settings && Screen != AppScreen.Credits && Screen != AppScreen.Gallery) return false;
            Screen = AppScreen.MainMenu;
            return true;
        }

        /// <summary>Lance un niveau depuis le choix du niveau, ou enchaîne sur le suivant depuis un combat. Refusé si verrouillé.</summary>
        public bool StartLevel(string levelId, bool unlocked)
        {
            if (Screen == AppScreen.MainMenu || Screen == AppScreen.Workshop || Screen == AppScreen.Settings || Screen == AppScreen.Credits || Screen == AppScreen.Gallery || !unlocked || string.IsNullOrEmpty(levelId)) return false;
            Screen = AppScreen.Battle;
            LevelId = levelId;
            return true;
        }

        public bool LeaveBattle()
        {
            if (Screen != AppScreen.Battle) return false;
            Screen = AppScreen.LevelSelect;
            LevelId = null;
            return true;
        }

        /// <summary>Démarrage direct dans un combat (options de test et de capture).</summary>
        public void StartAt(string levelId)
        {
            Screen = AppScreen.Battle;
            LevelId = levelId;
        }
    }
}
