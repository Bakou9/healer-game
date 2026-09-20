using System.Linq;
using Healer.Combat;
using Healer.Ui;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Healer.Client
{
    /// <summary>
    /// Raccourcis clavier (PC). Ne calcule rien : traduit une touche en geste du contrôleur, exactement comme
    /// un toucher (TapAlly / TapSkill). Les touches sont physiques (même position sur QWERTY et AZERTY) ; le
    /// HUD affiche le libellé réel de la disposition de l'utilisateur.
    ///   1-4 : cibler l'allié n° 1-4      Q W E R (A Z E R en AZERTY) : lancer le sort n° 1-4
    ///   Tab : allié suivant              Espace / Entrée : Jouer, puis Rejouer en fin de combat
    ///   Échap / P : pause                M : son
    /// </summary>
    public sealed class BattleKeys : MonoBehaviour
    {
        private static readonly Key[] AllyKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
        private static readonly Key[] NumpadKeys = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4 };
        private static readonly Key[] SkillKeys = { Key.Q, Key.W, Key.E, Key.R };

        private BattleController _ctl = null!;

        public void Init(BattleController controller) => _ctl = controller;

        /// <summary>Libellé de la touche du n-ième allié / sort, ou null s'il n'y a pas de clavier (mobile).</summary>
        public static string? AllyLabel(int index) => Keyboard.current != null && index >= 0 && index < AllyKeys.Length ? (index + 1).ToString() : null;
        public static string? SkillLabel(int index) => Label(SkillKeys, index);

        private static string? Label(Key[] keys, int index)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || index < 0 || index >= keys.Length) return null;
            return keyboard[keys[index]].displayName.ToUpperInvariant();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || _ctl == null || _ctl.Battle == null) return;

            bool confirm = kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
            var state = _ctl.State;

            // Toutes les touches passent par la même règle que la souris et le toucher (InputGate).
            if (confirm) Run(InputGate.ConfirmAction(state));
            if ((kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame) && InputGate.Allows(state, UiAction.TogglePause)) Run(UiAction.TogglePause);

            state = _ctl.State;
            var allies = _ctl.Battle.GetAllies();
            if (InputGate.Allows(state, UiAction.TapAlly))
            {
                for (int i = 0; i < AllyKeys.Length && i < allies.Count; i++)
                    if (kb[AllyKeys[i]].wasPressedThisFrame || kb[NumpadKeys[i]].wasPressedThisFrame) _ctl.TapAlly(allies[i].Id);
                if (kb.tabKey.wasPressedThisFrame) CycleTarget(allies.ToList(), kb.shiftKey.isPressed ? -1 : 1);
            }

            if (InputGate.Allows(state, UiAction.TapSkill))
            {
                var skills = _ctl.Skills;
                for (int i = 0; i < SkillKeys.Length && i < skills.Count; i++)
                    if (kb[SkillKeys[i]].wasPressedThisFrame) _ctl.TapSkill(skills[i]);
            }
        }

        private void Run(UiAction action)
        {
            switch (action)
            {
                case UiAction.StartFight: _ctl.StartFight(); break;
                case UiAction.TogglePause: _ctl.TogglePause(); break;
                case UiAction.Restart: _ctl.Restart(); break;
            }
        }

        private void CycleTarget(System.Collections.Generic.List<UnitState> allies, int step)
        {
            int count = allies.Count;
            if (count == 0) return;
            int current = allies.FindIndex(a => a.Id == _ctl.Selection.Selected);
            for (int n = 1; n <= count; n++)
            {
                var candidate = allies[((current < 0 ? (step > 0 ? -1 : 0) : current) + step * n % count + count) % count];
                if (!candidate.Alive) continue;
                _ctl.TapAlly(candidate.Id);
                return;
            }
        }
    }
}
