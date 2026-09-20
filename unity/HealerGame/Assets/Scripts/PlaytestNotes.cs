using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Healer.Combat;
using Healer.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Rect = UnityEngine.Rect;

namespace Healer.Client
{
    /// <summary>
    /// Notes de playtest (touche F8) : capture d'écran, pause du combat, saisie d'une remarque, puis ajout d'une
    /// entrée datée à playtest/notes.md (dossier des données du jeu) avec l'état du jeu au moment de la note. Le
    /// développeur relit ce fichier avec le testeur. Pendant la saisie, les raccourcis du jeu sont suspendus.
    /// </summary>
    public sealed class PlaytestNotes : MonoBehaviour
    {
        /// <summary>Vrai pendant la saisie : BattleKeys et le raccourci son (M) ne réagissent pas aux lettres tapées.</summary>
        public static bool Open { get; private set; }

        private UiRoot _root = null!;
        private GameFlow _flow = null!;
        private string _dir = "";
        private VisualElement? _panel;
        private Label? _field;
        private readonly StringBuilder _text = new StringBuilder();
        private const int MaxLength = 400;
        private Label? _toast;
        private string _shot = "";
        private bool _pausedByNote;
        private float _toastUntil;

        public string NotesPath => Path.Combine(_dir, "notes.md");

        public void Init(UiRoot root, GameFlow flow, string dir)
        {
            _root = root;
            _flow = flow;
            _dir = dir;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f8Key.wasPressedThisFrame)
            {
                if (Open) Close(false);
                else StartCoroutine(OpenRoutine());
            }
            if (Open && _panel != null && kb != null)
            {
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) { Save(); return; }
                if (kb.escapeKey.wasPressedThisFrame) { Close(false); return; }
            }
            if (Open && _field != null)
                _field.text = _text.Length == 0 ? "" : _text.ToString() + (Mathf.FloorToInt(Time.realtimeSinceStartup * 2f) % 2 == 0 ? "|" : " ");
            if (_toast != null && Time.realtimeSinceStartup > _toastUntil) { _toast.RemoveFromHierarchy(); _toast = null; }
        }

        private IEnumerator OpenRoutine()
        {
            Open = true;
            Directory.CreateDirectory(_dir);
            _shot = Path.Combine(_dir, $"note_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            ScreenCapture.CaptureScreenshot(_shot);
            yield return new WaitForEndOfFrame();
            yield return null;
            var ctl = _flow.Ctl;
            if (_flow.Screen == AppScreen.Battle && ctl.Started && !ctl.Paused && ctl.Battle.GetResult() == BattleResults.Ongoing)
            {
                ctl.TogglePause();
                _pausedByNote = true;
            }
            Build();
        }

        private void Build()
        {
            var note = _root.Note;
            _panel = Ui.Box(note, new Rect(0, 0, (float)Layout.GameW, (float)Layout.GameH), new Color(0, 0, 0, 0.72f), 0);
            _panel.pickingMode = PickingMode.Position;
            var box = Ui.Box(_panel, new Rect(240, 200, 800, 300), new Color(Ui.Panel.r, Ui.Panel.g, Ui.Panel.b, 0.98f), 14, Ui.Selected, 2);
            Ui.Text(box, new Rect(24, 14, 752, 34), "Noter un retour de playtest", 26, Color.white, TextAnchor.MiddleLeft, true);
            Ui.Text(box, new Rect(24, 52, 752, 44), "Ce qui vous a plu, gêné ou surpris. La capture d'écran et l'état du jeu sont joints automatiquement.", Layout.Font.Small, Ui.Muted, TextAnchor.UpperLeft, false, true);

            var fieldBox = Ui.Box(box, new Rect(24, 108, 752, 56), Palette.Hex("0E1019"), 8, Ui.ButtonStroke, 2);
            _field = Ui.Text(fieldBox, new Rect(12, 0, 728, 56), "", Layout.Font.Title, Color.white, TextAnchor.MiddleLeft);
            _text.Clear();
            foreach (var k in InputSystem.devices.OfType<Keyboard>()) k.onTextInput += OnChar;

            Ui.Text(box, new Rect(24, 176, 752, 24), "Entrée : enregistrer  ·  Échap ou F8 : annuler", Layout.Font.Small, Ui.Muted, TextAnchor.MiddleLeft);
            Ui.Button(box, new Rect(24, 226, 240, 56), "Enregistrer", Layout.Font.Title, Ui.PrimaryFill, Ui.PrimaryStroke, Save);
            Ui.Button(box, new Rect(280, 226, 200, 56), "Annuler", Layout.Font.Title, Ui.ButtonFill, Ui.ButtonStroke, () => Close(false));
        }

        /// <summary>Un caractère tapé (le clavier fournit déjà accents et majuscules) ; Retour arrière efface le dernier.</summary>
        private void OnChar(char c)
        {
            if (!Open || _panel == null) return;
            if (c == '\b') { if (_text.Length > 0) _text.Length--; return; }
            if (char.IsControl(c) || _text.Length >= MaxLength) return;
            _text.Append(c);
        }

        private void Save()
        {
            string text = _text.ToString().Trim();
            var sb = new StringBuilder();
            sb.AppendLine($"## {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine(string.IsNullOrEmpty(text) ? "_(note vide : capture seule)_" : text);
            sb.AppendLine();
            sb.AppendLine("- Capture : `" + Path.GetFileName(_shot) + "`");
            foreach (var line in Context()) sb.AppendLine("- " + line);
            sb.AppendLine();
            File.AppendAllText(NotesPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[Healer] note : " + (string.IsNullOrEmpty(text) ? "(vide)" : text) + " → " + NotesPath);
            Close(true);
        }

        /// <summary>État du jeu au moment de la note : de quoi comprendre la remarque sans rejouer la partie.</summary>
        private System.Collections.Generic.IEnumerable<string> Context()
        {
            var ctl = _flow.Ctl;
            yield return "Écran : " + _flow.Screen;
            var s = _flow.Profile.Settings;
            yield return $"Réglages : musique {s.MusicVolume} %, effets {s.SfxVolume} %, son {(s.Muted ? "coupé" : "activé")}, secousse {(s.ScreenShake ? "oui" : "non")}";
            yield return $"Progression : {_flow.Profile.TotalStars} étoiles, {Format.Number(_flow.Profile.Wallet.Balance(Healer.Combat.Progress.Wallet.Gold))} or";
            if (_flow.Screen != AppScreen.Battle || ctl.Battle == null) yield break;
            var b = ctl.Battle;
            yield return "Niveau : " + (_flow.CurrentLevel?.Name ?? "?") + " (" + b.GetBossName() + ")";
            yield return $"Combat : {Format.Seconds(b.GetClock())}, résultat {b.GetResult()}, {(ctl.Started ? "démarré" : "pas démarré")}";
            yield return $"Boss : {Format.Ratio(b.GetBossHp(), b.GetBossMaxHp())} PV, enrage palier {b.GetEnrageLevel()}";
            yield return "Équipe : " + string.Join(", ", b.GetAllies().Select(a => $"{a.Name} {(a.Alive ? Format.Ratio(a.Hp, a.MaxHp) : "K.O.")}"));
            var healer = b.GetAllies().FirstOrDefault(a => a.Id == BattleController.HealerId);
            if (healer != null) yield return $"Mana : {Format.Ratio(healer.Mana, healer.MaxMana)}";
            yield return "Cible : " + (ctl.Selection.Selected ?? "aucune") + "  ·  Dernière action du boss : " + (string.IsNullOrEmpty(ctl.LastAction) ? "-" : ctl.LastAction);
            yield return $"Statistiques : {ctl.Stats.Casts} sorts, {ctl.Stats.Deaths} K.O., soins {Format.Number(ctl.Stats.HealingDone)}, dégâts encaissés {Format.Number(ctl.Stats.DamageTaken)}";
        }

        private void Close(bool saved)
        {
            foreach (var k in InputSystem.devices.OfType<Keyboard>()) k.onTextInput -= OnChar;
            _panel?.RemoveFromHierarchy();
            _panel = null;
            _field = null;
            Open = false;
            var ctl = _flow.Ctl;
            if (_pausedByNote && ctl.Paused) ctl.TogglePause();
            _pausedByNote = false;
            if (saved)
            {
                _toast = Ui.Text(_root.Note, new Rect(0, 660, (float)Layout.GameW, 30), "Note enregistrée dans " + NotesPath, Layout.Font.Body, Palette.Heal, TextAnchor.MiddleCenter, true);
                _toastUntil = Time.realtimeSinceStartup + 4f;
            }
        }
    }
}
