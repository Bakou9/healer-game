using System.Linq;
using Healer.Combat;
using Healer.Ui;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Healer.Client
{
    /// <summary>
    /// Raccourcis clavier (PC). Ne calcule rien : traduit une touche en geste, exactement comme un clic ou un
    /// toucher, et passe par la même règle (InputGate). Les touches sont physiques (même position sur QWERTY et
    /// AZERTY) ; le HUD affiche le libellé réel de la disposition de l'utilisateur.
    ///   Menu : Entrée / Espace = Jouer.   Choix du niveau : 1-9 = niveau, Échap = retour.
    ///   Combat : Q W E R T (A Z E R T en AZERTY : la rangée de lettres) cibler · 1-6 ou pavé numérique = sorts (D-055) · Tab allié suivant · Espace / Entrée jouer, pause,
    ///   enchaîner ou rejouer · Échap / P pause (ou retour à la carte avant et après le combat) · M son.
    /// </summary>
    public sealed class BattleKeys : MonoBehaviour
    {
        // Alliés sur la rangée de lettres (touches physiques : le libellé affiché suit la disposition, AZERTY ou autre) ;
        // sorts sur les chiffres, avec le pavé numérique en doublon.
        private static readonly Key[] AllyKeys = { Key.Q, Key.W, Key.E, Key.R, Key.T };
        private static readonly Key[] SkillKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };
        private static readonly Key[] NumpadKeys = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6 };
        private static readonly Key[] LevelKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };

        private GameFlow _flow = null!;

        public void Init(GameFlow flow) => _flow = flow;

        /// <summary>Libellé de la touche du n-ième allié / sort, ou null s'il n'y a pas de clavier (mobile).</summary>
        public static string? AllyLabel(int index)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || index < 0 || index >= AllyKeys.Length) return null;
            return keyboard[AllyKeys[index]].displayName.ToUpperInvariant();
        }

        public static string? SkillLabel(int index) => Keyboard.current != null && index >= 0 && index < SkillKeys.Length ? (index + 1).ToString() : null;

        public static string? LevelLabel(int index) => Keyboard.current != null && index >= 0 && index < LevelKeys.Length ? (index + 1).ToString() : null;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || _flow == null || _flow.Ctl.Battle == null || PlaytestNotes.Open) return; // saisie d'une note : pas de raccourcis
            bool confirm = kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
            bool back = kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame;

            switch (_flow.Screen)
            {
                case AppScreen.MainMenu:
                    if (confirm) _flow.OpenLevels();
                    return;
                case AppScreen.Workshop:
                case AppScreen.Settings:
                    if (back) _flow.BackToMenu();
                    return;
                case AppScreen.LevelSelect:
                    if (back) _flow.BackToMenu();
                    for (int i = 0; i < LevelKeys.Length && i < _flow.Content.Levels.Count; i++)
                        if (kb[LevelKeys[i]].wasPressedThisFrame) _flow.StartLevel(_flow.Content.Levels[i].Id);
                    return;
            }

            var ctl = _flow.Ctl;
            var state = ctl.State;
            bool victory = ctl.Battle.GetResult() == BattleResults.Victory;

            // Toutes les touches passent par la même règle que la souris et le toucher (InputGate).
            if (confirm) Run(InputGate.ConfirmAction(state, victory, _flow.NextLevel != null));
            if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)
            {
                if (InputGate.Allows(state, UiAction.TogglePause)) Run(UiAction.TogglePause);
                else if (kb.escapeKey.wasPressedThisFrame && InputGate.Allows(state, UiAction.BackToMap)) Run(UiAction.BackToMap);
            }

            state = ctl.State;
            var allies = ctl.Battle.GetAllies();
            if (InputGate.Allows(state, UiAction.TapAlly))
            {
                for (int i = 0; i < AllyKeys.Length && i < allies.Count; i++)
                    if (kb[AllyKeys[i]].wasPressedThisFrame) ctl.TapAlly(allies[i].Id);
                if (kb.tabKey.wasPressedThisFrame) CycleTarget(allies.ToList(), kb.shiftKey.isPressed ? -1 : 1);
            }

            var skills = ctl.Skills;
            for (int i = 0; i < SkillKeys.Length && i < skills.Count; i++)
            {
                bool released = kb[SkillKeys[i]].wasReleasedThisFrame || kb[NumpadKeys[i]].wasReleasedThisFrame;
                bool pressed = kb[SkillKeys[i]].wasPressedThisFrame || kb[NumpadKeys[i]].wasPressedThisFrame;
                if (released) ctl.ReleaseSkill(skills[i]);
                // Appui : lance tout de suite ; maintenue, la touche enchaîne le sort (HoldRepeat).
                else if (pressed && InputGate.Allows(state, UiAction.TapSkill)) ctl.PressSkill(skills[i], false);
            }
        }

        private void Run(UiAction action)
        {
            switch (action)
            {
                case UiAction.StartFight: _flow.Ctl.StartFight(); break;
                case UiAction.TogglePause: _flow.Ctl.TogglePause(); break;
                case UiAction.Restart: _flow.Restart(); break;
                case UiAction.NextLevel: _flow.NextLevelNow(); break;
                case UiAction.BackToMap: _flow.LeaveBattle(); break;
            }
        }

        private void CycleTarget(System.Collections.Generic.List<UnitState> allies, int step)
        {
            int count = allies.Count;
            if (count == 0) return;
            int current = allies.FindIndex(a => a.Id == _flow.Ctl.Selection.Selected);
            for (int n = 1; n <= count; n++)
            {
                var candidate = allies[((current < 0 ? (step > 0 ? -1 : 0) : current) + step * n % count + count) % count];
                if (!candidate.Alive) continue;
                _flow.Ctl.TapAlly(candidate.Id);
                return;
            }
        }
    }
}
