using Healer.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

namespace Healer.Client
{
    /// <summary>
    /// Racine de l'interface UI Toolkit. Le PanelSettings est créé par le code (rien à configurer dans l'Éditeur) :
    /// la grille logique 1280×720 de Healer.Ui.Layout est mise à l'échelle comme Healer.Ui.ScreenFit (toute la grille
    /// visible, bandes si le rapport diffère). Calques, du fond vers le dessus :
    ///   Vignette (plein écran) · Hud (grille) · Backdrop (voile plein écran) · Modal (grille : menus, pause, fin) ·
    ///   Danger (plein écran) · Note (grille : note de playtest).
    /// </summary>
    public sealed class UiRoot : MonoBehaviour
    {
        public VisualElement Vignette { get; private set; } = null!;
        public VisualElement Hud { get; private set; } = null!;
        public VisualElement Backdrop { get; private set; } = null!;
        public VisualElement Modal { get; private set; } = null!;
        public VisualElement Danger { get; private set; } = null!;
        public VisualElement Note { get; private set; } = null!;

        private AppScreens _app = null!;
        private BattleScreen _battle = null!;

        public void Init(GameFlow flow)
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int((int)Layout.GameW, (int)Layout.GameH);
            settings.screenMatchMode = PanelScreenMatchMode.Expand; // toute la grille visible, comme ScreenFit
            settings.themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            settings.sortingOrder = 10;

            // UI Toolkit reçoit souris et toucher par un EventSystem branché sur l'Input System (aussi pour les entrées injectées des tests).
            var events = new GameObject("EventSystem");
            events.transform.SetParent(transform, false);
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = settings;
            var root = doc.rootVisualElement;
            root.style.flexGrow = 1;
            root.pickingMode = PickingMode.Ignore;
            Ui.ApplyFont(root);

            Vignette = Ui.Fullscreen(root);
            Hud = Stage(root);
            Backdrop = Ui.Fullscreen(root);
            Modal = Stage(root);
            Danger = Ui.Fullscreen(root);
            Note = Stage(root);

            _app = new AppScreens(this, flow);
            _battle = new BattleScreen(this, flow);
        }

        /// <summary>Calque plein écran qui centre une scène de 1280×720 (les enfants se placent en coordonnées logiques).</summary>
        private static VisualElement Stage(VisualElement root)
        {
            var layer = Ui.Fullscreen(root);
            layer.style.alignItems = Align.Center;
            layer.style.justifyContent = Justify.Center;
            var stage = new VisualElement { pickingMode = PickingMode.Ignore };
            stage.style.width = (float)Layout.GameW;
            stage.style.height = (float)Layout.GameH;
            stage.style.flexShrink = 0;
            layer.Add(stage);
            return stage;
        }

        /// <summary>Voile derrière les menus et les fenêtres modales ; capte les clics quand il est visible.</summary>
        public void SetBackdrop(float alpha, float r = 0.03f, float g = 0.04f, float b = 0.09f)
        {
            Backdrop.style.backgroundColor = new Color(r, g, b, alpha);
            Backdrop.pickingMode = alpha > 0f ? PickingMode.Position : PickingMode.Ignore;
        }

        private void Update()
        {
            _app.Refresh();
            _battle.Refresh();
            SetBackdrop(_app.Active ? AppScreens.BackdropAlpha : _battle.BackdropAlpha);
        }
    }
}
