using System.Linq;
using Healer.Combat;
using Healer.Combat.Progress;
using Healer.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using Rect = UnityEngine.Rect;

namespace Healer.Client
{
    /// <summary>
    /// Écrans hors combat : menu principal et choix du niveau (IMGUI, comme le HUD de combat). Aucune règle de
    /// jeu : lecture du profil et de la progression, et gestes vers GameFlow. Chaque zone cliquable passe par
    /// InputGate, donc un écran ne capte jamais les clics d'un autre.
    /// </summary>
    public sealed class AppHud : MonoBehaviour
    {
        private GameFlow _flow = null!;

        public void Init(GameFlow flow) => _flow = flow;

        private static Rect R(Healer.Ui.Rect r) => new Rect((float)r.X, (float)r.Y, (float)r.W, (float)r.H);

        private bool Hit(Rect r, UiAction action) =>
            InputGate.Allows(_flow.Screen, ScreenState.Playing, action) && GUI.Button(r, GUIContent.none, GUIStyle.none);

        private void OnGUI()
        {
            if (_flow == null || _flow.Screen == AppScreen.Battle) return;
            ScreenMap.Refresh();
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(ScreenMap.OffsetX, ScreenMap.OffsetY, 0), Quaternion.identity, new Vector3(ScreenMap.Scale, ScreenMap.Scale, 1));
            UiKit.Fill(new Rect(-2000, -2000, 5000, 5000), new Color(0.03f, 0.04f, 0.09f, 0.72f), 0);
            if (_flow.Screen == AppScreen.MainMenu) DrawMenu();
            else DrawLevels();
            DrawFooter();
            GUI.matrix = previous;
        }

        // ---- Menu principal ----------------------------------------------------------------------

        private void DrawMenu()
        {
            float w = (float)Layout.GameW;
            UiKit.Label(new Rect(0, 100, w, 90), "Healer Game", 72, Color.white, TextAnchor.MiddleCenter, true);
            UiKit.Label(new Rect(0, 192, w, 32), "Gardez votre équipe en vie.", Layout.Font.Title, UiKit.Muted, TextAnchor.MiddleCenter);

            var play = R(Layout.MenuPlay);
            UiKit.Button(play, "Jouer", 30, Palette.Hex("245C43"), Palette.Hex("5FB98D"));
            if (Hit(play, UiAction.MenuPlay)) _flow.OpenLevels();

            var sound = R(Layout.MenuSound);
            UiKit.Button(sound, _flow.Profile.Settings.Muted ? "Son : coupé" : "Son : activé", Layout.Font.Title, Palette.Hex("22273B"), Palette.Hex("6F7698"));
            if (Hit(sound, UiAction.MenuToggleSound)) _flow.ToggleMute();

            if (CanQuit)
            {
                var quit = R(Layout.MenuQuit);
                UiKit.Button(quit, "Quitter", Layout.Font.Title, Palette.Hex("22273B"), Palette.Hex("6F7698"));
                if (Hit(quit, UiAction.MenuQuit)) _flow.Quit();
            }
            if (Keyboard.current != null)
                UiKit.Label(new Rect(0, 570, w, 24), "Entrée : jouer · M : son", Layout.Font.Small, UiKit.Muted, TextAnchor.MiddleCenter);
        }

        /// <summary>Un bouton « Quitter » n'a de sens que sur PC (les téléphones ferment l'application autrement).</summary>
        private static bool CanQuit => Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer;

        // ---- Choix du niveau ---------------------------------------------------------------------

        private void DrawLevels()
        {
            float w = (float)Layout.GameW;
            var content = _flow.Content;
            var profile = _flow.Profile;
            UiKit.Label(new Rect(0, 40, w, 50), "Choisir un niveau", 40, Color.white, TextAnchor.MiddleCenter, true);

            var back = R(Layout.BackButton);
            UiKit.Button(back, "← Menu", Layout.Font.Body, Palette.Hex("22273B"), Palette.Hex("6F7698"));
            if (Hit(back, UiAction.BackToMenu)) _flow.BackToMenu();

            var rects = Layout.LevelCardRects(content.Levels.Count);
            for (int i = 0; i < content.Levels.Count; i++)
            {
                var level = content.Levels[i];
                var r = R(rects[i]);
                bool unlocked = profile.IsUnlocked(level);
                var record = profile.RecordOf(level.Id);
                var accent = ModelFactory.BossColors(level.BossId).calm;

                UiKit.Fill(r, new Color(UiKit.Panel.r, UiKit.Panel.g, UiKit.Panel.b, unlocked ? 0.94f : 0.7f), 10);
                UiKit.Outline(r, unlocked ? UiKit.Selected : UiKit.PanelStroke, unlocked ? 2 : 1.5f, 10);
                UiKit.Fill(new Rect(r.x + 10, r.y + 10, r.width - 20, 8), new Color(accent.r, accent.g, accent.b, unlocked ? 1f : 0.3f), 4);
                UiKit.Label(new Rect(r.x, r.y + 28, r.width, 22), "Niveau " + (i + 1), Layout.Font.Small, UiKit.Muted, TextAnchor.MiddleCenter);
                UiKit.Label(new Rect(r.x + 6, r.y + 52, r.width - 12, 30), level.Name, Layout.Font.Title, unlocked ? Color.white : UiKit.Muted, TextAnchor.MiddleCenter, true);
                UiKit.Label(new Rect(r.x, r.y + 84, r.width, 22), "Boss : " + content.BossById(level.BossId).Name, Layout.Font.Small, UiKit.Muted, TextAnchor.MiddleCenter);

                if (unlocked)
                {
                    UiKit.Stars(new Rect(r.x, r.y + 120, r.width, 60), record.BestStars, 44);
                    UiKit.Label(new Rect(r.x, r.y + 196, r.width, 22), record.Completed ? "Meilleur temps : " + Format.Seconds(record.BestTimeMs) : "Pas encore terminé", Layout.Font.Body, Color.white, TextAnchor.MiddleCenter);
                    UiKit.Label(new Rect(r.x, r.y + 220, r.width, 22), record.Clears > 0 ? "Victoires : " + record.Clears : " ", Layout.Font.Small, UiKit.Muted, TextAnchor.MiddleCenter);
                    UiKit.Label(new Rect(r.x, r.y + 250, r.width, 22),
                        record.Completed ? "Répétition : " + Format.Number(level.RepeatGold) + " or" : "Première victoire : " + Format.Number(level.RewardGold) + " or",
                        Layout.Font.Small, UiKit.Gold, TextAnchor.MiddleCenter);
                    var play = new Rect(r.x + 40, r.yMax - 66, r.width - 80, 48);
                    UiKit.Button(play, record.Completed ? "Rejouer" : "Jouer", Layout.Font.Title, Palette.Hex("245C43"), Palette.Hex("5FB98D"));
                }
                else
                {
                    var prev = level.Requires != null ? content.LevelById(level.Requires).Name : "";
                    UiKit.Label(new Rect(r.x, r.y + 150, r.width, 30), "Verrouillé", Layout.Font.Title, UiKit.Muted, TextAnchor.MiddleCenter, true);
                    UiKit.Label(new Rect(r.x + 10, r.y + 190, r.width - 20, 44), "Terminez d'abord\n« " + prev + " »", Layout.Font.Small, UiKit.Muted, TextAnchor.UpperCenter);
                }

                var cap = BattleKeys.LevelLabel(i);
                if (cap != null && unlocked) DrawKeyCap(r, cap);
                if (Hit(r, UiAction.PickLevel)) _flow.StartLevel(level.Id);
            }
        }

        private static void DrawKeyCap(Rect card, string label)
        {
            var cap = new Rect(card.x + 8, card.y + 26, 24, 24);
            UiKit.Fill(cap, new Color(0f, 0f, 0f, 0.6f), 5);
            UiKit.Outline(cap, new Color(1f, 1f, 1f, 0.5f), 1, 5);
            UiKit.Label(cap, label, Layout.Font.Small, Color.white, TextAnchor.MiddleCenter, true);
        }

        // ---- Pied de page : progression globale --------------------------------------------------

        private void DrawFooter()
        {
            var p = _flow.Profile;
            int max = _flow.Content.Levels.Count * Progression.MaxStars;
            float w = (float)Layout.GameW;
            UiKit.Label(new Rect(0, 664, w, 30), $"Étoiles : {p.TotalStars} / {max}      ·      Or : {Format.Number(p.Wallet.Balance(Wallet.Gold))}", Layout.Font.Body, UiKit.Gold, TextAnchor.MiddleCenter);
        }
    }
}
