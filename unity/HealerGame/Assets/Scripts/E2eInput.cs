using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Healer.Client
{
    /// <summary>
    /// Entrées injectées pour les tests de bout en bout (option -healer-e2e FICHIER). Le script de test ajoute une
    /// commande par ligne au fichier ; le jeu la joue sur une souris et un clavier VIRTUELS de l'Input System. Le
    /// geste suit ensuite le même chemin qu'un vrai (Input System, UI Toolkit, BattleKeys, InputGate, Layout) : seule la
    /// livraison par Windows est sautée. La fenêtre n'a donc pas besoin de focus, et la vraie souris et le vrai
    /// clavier de l'utilisateur restent libres (les vrais périphériques sont désactivés dans ce mode).
    /// Commandes (coordonnées logiques de Healer.Ui.Layout) :
    ///   click X Y · down X Y · up · key NOM · keydown NOM · keyup NOM   (NOM = UnityEngine.InputSystem.Key : Space, Escape, Digit1, Q…)
    ///   text TEXTE   saisie de texte (champs de saisie ; les espaces sont conservés)
    /// Chaque commande jouée écrit « [Healer] e2e N ok … » dans le journal : le script attend cet accusé.
    /// </summary>
    public sealed class E2eInput : MonoBehaviour
    {
        /// <summary>Vrai en mode test : le jeu ne se met pas en pause quand la fenêtre perd le focus.</summary>
        public static bool Active { get; private set; }

        private string _path = "";
        private int _done;
        private bool _busy;
        private Mouse _mouse = null!;
        private Keyboard _keyboard = null!;
        private Vector2 _screen;
        private readonly List<Key> _held = new List<Key>();

        public void Init(string path)
        {
            _path = path;
            Active = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var d in InputSystem.devices) InputSystem.DisableDevice(d);
            _mouse = InputSystem.AddDevice<Mouse>("E2E Mouse");
            _keyboard = InputSystem.AddDevice<Keyboard>("E2E Keyboard");
            Debug.Log("[Healer] e2e : entrées injectées depuis " + path);
        }

        private void Update()
        {
            if (_busy || !File.Exists(_path)) return;
            string[] lines;
            try
            {
                using var fs = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(fs);
                lines = reader.ReadToEnd().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }
            catch (IOException) { return; }
            if (lines.Length <= _done) return;
            _busy = true;
            StartCoroutine(Run(lines[_done].Trim(), _done + 1));
        }

        private IEnumerator Run(string line, int number)
        {
            var t = line.Split(' ');
            switch (t[0])
            {
                case "click":
                    Aim(t);
                    Pointer(false); yield return null;
                    Pointer(true); yield return new WaitForSecondsRealtime(0.08f);
                    Pointer(false);
                    break;
                case "down": Aim(t); Pointer(false); yield return null; Pointer(true); break;
                case "up": Pointer(false); break;
                case "key":
                    SetKey(t[1], true); yield return new WaitForSecondsRealtime(0.08f); SetKey(t[1], false);
                    break;
                case "text":
                    foreach (char ch in line.Substring(5)) { _keyboard.MakeCurrent(); InputSystem.QueueTextEvent(_keyboard, ch); yield return null; }
                    break;
                case "keydown": SetKey(t[1], true); break;
                case "keyup": SetKey(t[1], false); break;
                default: Debug.LogWarning("[Healer] e2e : commande inconnue : " + line); break;
            }
            Debug.Log($"[Healer] e2e {number} ok {line}");
            _done = number;
            _busy = false;
        }

        private void Aim(string[] t)
        {
            float x = float.Parse(t[1], CultureInfo.InvariantCulture), y = float.Parse(t[2], CultureInfo.InvariantCulture);
            ScreenMap.Refresh();
            var s = ScreenMap.LogicalToScreen(x, y);
            _screen = new Vector2(s.x, Screen.height - s.y);
        }

        private void Pointer(bool down)
        {
            _mouse.MakeCurrent();
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = _screen }.WithButton(MouseButton.Left, down));
        }

        private void SetKey(string name, bool down)
        {
            if (!Enum.TryParse(name, out Key key)) { Debug.LogWarning("[Healer] e2e : touche inconnue : " + name); return; }
            if (down && !_held.Contains(key)) _held.Add(key);
            if (!down) _held.Remove(key);
            _keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(_held.ToArray()));
        }
    }
}
