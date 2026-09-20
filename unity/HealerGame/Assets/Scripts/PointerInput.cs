using Healer.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using Rect = UnityEngine.Rect;

namespace Healer.Client
{
    /// <summary>
    /// Pointeur unique du jeu : souris, toucher ou pointeur injecté par les tests (E2eInput) passent tous par
    /// l'Input System. Les écrans ne lisent plus les événements IMGUI : ils demandent « ce geste tombe-t-il dans
    /// cette zone logique ? » (grille de Healer.Ui.Layout). Un geste n'est consommé qu'une fois par image, par la
    /// première zone qui le réclame, comme le faisait GUI.Button.
    /// </summary>
    public static class PointerInput
    {
        private static int _frame = -1;
        private static bool _pressed, _released, _pressConsumed, _releaseConsumed;
        private static Vector2 _pos, _pressPos;

        /// <summary>Relit l'état du pointeur une seule fois par image (OnGUI est appelé plusieurs fois par image).</summary>
        private static void Refresh()
        {
            if (_frame == Time.frameCount) return;
            _frame = Time.frameCount;
            _pressed = _released = _pressConsumed = _releaseConsumed = false;
            var mouse = Mouse.current;
            var touch = Touchscreen.current;
            Vector2 screen = _screenPos;
            if (mouse != null)
            {
                screen = mouse.position.ReadValue();
                _pressed = mouse.leftButton.wasPressedThisFrame;
                _released = mouse.leftButton.wasReleasedThisFrame;
            }
            if (touch != null && (touch.primaryTouch.press.wasPressedThisFrame || touch.primaryTouch.press.wasReleasedThisFrame || touch.primaryTouch.press.isPressed))
            {
                screen = touch.primaryTouch.position.ReadValue();
                _pressed |= touch.primaryTouch.press.wasPressedThisFrame;
                _released |= touch.primaryTouch.press.wasReleasedThisFrame;
            }
            _screenPos = screen;
            ScreenMap.Refresh();
            var logical = ScreenMap.ScreenToLogical(screen.x, Screen.height - screen.y);
            if (_pressed) _pressPos = logical;
            _pos = logical;
        }

        private static Vector2 _screenPos;

        /// <summary>Position du pointeur sur la grille logique (y depuis le haut).</summary>
        public static Vector2 Position { get { Refresh(); return _pos; } }

        /// <summary>Le bouton ou le doigt vient d'être relâché (où que ce soit).</summary>
        public static bool ReleasedThisFrame { get { Refresh(); return _released; } }

        /// <summary>Un appui a commencé et fini dans la zone : un « clic ». Consomme le geste.</summary>
        public static bool Tapped(Rect zone)
        {
            Refresh();
            if (!_released || _releaseConsumed || !zone.Contains(_pos) || !zone.Contains(_pressPos)) return false;
            _releaseConsumed = true;
            return true;
        }

        /// <summary>Un appui commence dans la zone (pour maintenir un sort). Consomme le geste.</summary>
        public static bool Pressed(Rect zone)
        {
            Refresh();
            if (!_pressed || _pressConsumed || !zone.Contains(_pos)) return false;
            _pressConsumed = true;
            return true;
        }
    }
}
