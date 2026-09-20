using System;
using System.Collections.Generic;
using System.Linq;
using Healer.Combat;
using Healer.Ui;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Lien entre le cœur (Battle, sans Unity) et Unity. Aucune règle ici : le contrôleur fait avancer la
    /// simulation par pas fixes (FixedStepper alimenté par le temps de l'image), transmet les commandes du
    /// joueur, et relaie les événements de combat (patron Observer) à la scène 3D et à l'interface.
    /// </summary>
    public sealed class BattleController : MonoBehaviour
    {
        public const string HealerId = "healer";
        private const double HintDurationMs = 1500;
        private const double AutoplayDecisionMs = 500;

        private GameContent _content = null!;
        private Battle _battle = null!;
        private FixedStepper _stepper = new FixedStepper();
        private readonly TargetSelection _selection = new TargetSelection();
        private double _hintUntilMs;
        private double _sinceDecisionMs;
        private Action? _unsubscribe;

        public bool Paused { get; private set; }

        /// <summary>Faux tant que le joueur n'a pas touché « Jouer » : la simulation ne tourne pas.</summary>
        public bool Started { get; private set; }

        public void StartFight() => Started = true;

        /// <summary>Met le jeu en pause quand la fenêtre perd le focus (on ne perd pas un combat en changeant de fenêtre).</summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (Autoplay || !Started || _battle == null) return;
            if (!hasFocus)
            {
                // Ne réactive pas seul une pause voulue par le joueur : on ne reprendra que celle-ci.
                if (!Paused && _battle.GetResult() == BattleResults.Ongoing) { Paused = true; _pausedByFocus = true; }
            }
            else if (_pausedByFocus)
            {
                _pausedByFocus = false;
                Paused = false;
            }
        }

        private bool _pausedByFocus;

        /// <summary>Accélère le temps (captures automatiques uniquement) ; 1 en jeu normal.</summary>
        public float TimeScale { get; set; } = 1f;

        /// <summary>Le soin est joué par le bot de référence (captures et démonstration), jamais en jeu normal.</summary>
        public bool Autoplay { get; set; }

        public Battle Battle => _battle;
        public TargetSelection Selection => _selection;
        public IReadOnlyList<SkillDef> Skills => _content.Skills;
        public bool HintActive => _battle != null && _battle.GetClock() < _hintUntilMs;
        public string LastAction { get; private set; } = "";

        /// <summary>Statistiques du combat en cours (calculées par le cœur à partir des événements).</summary>
        public CombatStats Stats { get; private set; } = new CombatStats();

        /// <summary>Vrai dès que le joueur a lancé un sort : arrête le guidage du premier geste.</summary>
        public bool HasCast { get; private set; }

        public event Action<BattleEvent>? EventEmitted;
        public event Action? Restarted;

        public void Begin(GameContent content, uint seed)
        {
            _content = content;
            StartBattle(seed);
        }

        private void StartBattle(uint seed)
        {
            _unsubscribe?.Invoke();
            _battle = new Battle(_content.CreateEncounter(seed));
            Stats = new CombatStats();
            Stats.Attach(_battle);
            _stepper = new FixedStepper();
            _selection.Clear();
            _hintUntilMs = 0;
            _sinceDecisionMs = 0;
            LastAction = "";
            Paused = false;
            _unsubscribe = _battle.Subscribe(OnBattleEvent);
        }

        public void Restart()
        {
            StartBattle((uint)(Environment.TickCount & 0x7fffffff));
            Started = true;
            Restarted?.Invoke();
        }

        public void TogglePause() { Paused = !Paused; _pausedByFocus = false; }

        private void Update()
        {
            if (_battle == null || !Started || Paused || _battle.GetResult() != BattleResults.Ongoing) return;
            double dtMs = Time.deltaTime * 1000.0 * TimeScale;
            _stepper.Advance(dtMs, step =>
            {
                if (Autoplay)
                {
                    _sinceDecisionMs += step;
                    if (_sinceDecisionMs >= AutoplayDecisionMs)
                    {
                        _sinceDecisionMs = 0;
                        ReferenceHealerBot.Decide(_battle, _battle.GetClock(), true);
                    }
                }
                _battle.Step(step);
            });
            _selection.Sync(_battle.GetAllies().Where(a => a.Alive).Select(a => a.Id));
        }

        // ---- Gestes du joueur -------------------------------------------------------------------

        public void TapAlly(string unitId)
        {
            if (!Started) return;
            var ally = _battle.GetAllies().FirstOrDefault(a => a.Id == unitId);
            if (ally != null) _selection.Tap(unitId, ally.Alive);
            Debug.Log($"[Healer] geste : carte {unitId} → cible = {_selection.Selected ?? "aucune"}");
        }

        public void TapSkill(SkillDef skill)
        {
            if (!Started || Paused || _battle.GetResult() != BattleResults.Ongoing) return;
            var resolution = _selection.Resolve(skill.Target == "all", _battle.CanUseSkillNow(HealerId, skill.Id));
            Debug.Log($"[Healer] geste : sort {skill.Id} → {resolution.Kind} (cible {resolution.TargetId ?? "-"})");
            switch (resolution.Kind)
            {
                case CastKind.NeedTarget:
                    _hintUntilMs = _battle.GetClock() + HintDurationMs;
                    break;
                case CastKind.Cast:
                    HasCast = true;
                    _battle.IssueCommand(new Command { TimeMs = _battle.GetClock(), SkillId = skill.Id, TargetId = resolution.TargetId });
                    break;
            }
        }

        // ---- Événements ---------------------------------------------------------------------------

        private string AllyName(string id) => _battle.GetAllies().FirstOrDefault(a => a.Id == id)?.Name ?? id;

        private void OnBattleEvent(BattleEvent e)
        {
            string time = Format.Seconds(e.TimeMs);
            switch (e.Type)
            {
                case "skillUsed":
                    HasCast = true;
                    var skill = _content.Skills.FirstOrDefault(s => s.Id == e.SkillId);
                    string targets = e.TargetIds.Count > 1 ? "toute l'équipe" : AllyName(e.TargetIds.FirstOrDefault() ?? e.CasterId);
                    LastAction = $"{time}  {skill?.Name ?? e.SkillId} → {targets}";
                    break;
                case "bossAction":
                    LastAction = $"{time}  Le boss : {(e.Action == "bigAttack" ? "attaque de zone" : e.Action == "poison" ? "poison" : "attaque")}";
                    break;
                case "unitDied":
                    LastAction = $"{time}  {AllyName(e.UnitId)} est K.O.";
                    break;
                case "bossPhaseChanged":
                    LastAction = $"{time}  Le boss passe en phase « {e.Name} »";
                    break;
            }
            EventEmitted?.Invoke(e);
        }
    }
}
