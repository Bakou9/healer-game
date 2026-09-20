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
        private List<string>? _owned;
        private readonly HoldRepeat _hold = new HoldRepeat();
        private bool _heldByPointer;

        /// <summary>Horloge du combat au dernier lancer : tant qu'elle n'a pas avancé, la commande n'est pas encore traitée et on n'en réémet pas.</summary>
        private double _lastCastClock = -1;
        private Healer.Combat.Progress.Loadout? _loadout;

        public bool Paused { get; private set; }

        /// <summary>Faux tant que le joueur n'a pas touché « Jouer » : la simulation ne tourne pas.</summary>
        public bool Started { get; private set; }

        public void StartFight()
        {
            Started = true;
            Debug.Log("[Healer] état : combat démarré");
        }

        /// <summary>État de l'écran pour les entrées (voir Healer.Ui.InputGate) : souris, toucher et clavier s'y plient.</summary>
        public ScreenState State => InputGate.StateOf(Started, Paused, _battle != null && _battle.GetResult() != BattleResults.Ongoing);

        /// <summary>Met le jeu en pause quand la fenêtre perd le focus (on ne perd pas un combat en changeant de fenêtre).</summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (Autoplay || E2eInput.Active || !Started || _battle == null) return;
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
        public GameContent Content => _content;

        /// <summary>Niveau en cours (boss, récompenses, seuils d'étoiles).</summary>
        public LevelDef? Level { get; private set; }
        public TargetSelection Selection => _selection;
        /// <summary>Sorts du combat EN COURS, bonus d'équipement et de talents compris (le catalogue de base est dans GameContent.Skills).</summary>
        public IReadOnlyList<SkillDef> Skills => _encounter?.Skills ?? _content.Skills;
        private EncounterDef? _encounter;
        /// <summary>Personnages du combat EN COURS, bonus d'équipement et de talents compris.</summary>
        public IReadOnlyList<CharacterDef> Characters => _encounter?.Allies ?? (IReadOnlyList<CharacterDef>)System.Array.Empty<CharacterDef>();
        public bool HintActive => _battle != null && _battle.GetClock() < _hintUntilMs;
        public string LastAction { get; private set; } = "";

        /// <summary>Statistiques du combat en cours (calculées par le cœur à partir des événements).</summary>
        public CombatStats Stats { get; private set; } = new CombatStats();

        /// <summary>Vrai dès que le joueur a lancé un sort : arrête le guidage du premier geste.</summary>
        public bool HasCast { get; private set; }

        public event Action<BattleEvent>? EventEmitted;
        public event Action? Restarted;

        /// <summary>Prépare un combat pour un niveau avec l'équipe possédée par le joueur ; il ne démarre qu'au « Jouer ».</summary>
        public void StartLevel(GameContent content, LevelDef level, IEnumerable<string>? owned, Healer.Combat.Progress.Loadout? loadout, uint seed)
        {
            _content = content;
            Level = level;
            _owned = owned?.ToList();
            _loadout = loadout?.Clone();
            Started = false;
            _pausedByFocus = false;
            _hold.ReleaseAll();
            _lastCastClock = -1;
            StartBattle(seed);
            Restarted?.Invoke();
        }

        /// <summary>Arrête le guidage du premier geste (joueur qui a déjà gagné).</summary>
        public void SkipGuidance() => HasCast = true;

        private void StartBattle(uint seed)
        {
            _unsubscribe?.Invoke();
            _encounter = _content.CreateEncounter(Level!.BossId, seed, _owned, _loadout);
            _battle = new Battle(_encounter);
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
            Debug.Log("[Healer] état : nouveau combat");
            StartBattle((uint)(Environment.TickCount & 0x7fffffff));
            Started = true;
            Restarted?.Invoke();
        }

        public void TogglePause()
        {
            Paused = !Paused;
            _pausedByFocus = false;
            Debug.Log(Paused ? "[Healer] état : pause" : "[Healer] état : reprise");
        }

        /// <summary>Vrai tant que le doigt ou le bouton gauche de la souris est appuyé.</summary>
        private static bool PointerDown() =>
            (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
            || (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.isPressed);

        /// <summary>Appui sur un sort : le lance tout de suite ; s'il est maintenu, il s'enchaîne (HoldRepeat, D-050).</summary>
        public void PressSkill(SkillDef skill, bool byPointer)
        {
            TapSkill(skill);
            _hold.Press(skill.Id);
            _heldByPointer = byPointer;
        }

        public void ReleaseSkill(SkillDef? skill = null)
        {
            if (skill == null) _hold.ReleaseAll();
            else _hold.Release(skill.Id);
        }

        private void RepeatHeldSkill()
        {
            if (_hold.HeldSkillId == null || _battle == null) return;
            if (_heldByPointer && !PointerDown()) { _hold.ReleaseAll(); return; }
            var skill = _content.Skills.FirstOrDefault(s => s.Id == _hold.HeldSkillId);
            if (skill == null) { _hold.ReleaseAll(); return; }
            if (_battle.GetClock() <= _lastCastClock) return; // la commande précédente n'a pas encore été traitée
            var resolution = _selection.Resolve(skill.Target == "all", _battle.CanUseSkillNow(HealerId, skill.Id));
            if (_hold.ShouldCast(State, resolution))
            {
                Debug.Log($"[Healer] geste : sort {skill.Id} → Cast (maintenu, cible {resolution.TargetId ?? "-"})");
                Cast(skill, resolution);
            }
        }

        private void Update()
        {
            RepeatHeldSkill();
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
                    Cast(skill, resolution);
                    break;
            }
        }

        private void Cast(SkillDef skill, CastResolution resolution)
        {
            HasCast = true;
            _lastCastClock = _battle.GetClock();
            _battle.IssueCommand(new Command { TimeMs = _battle.GetClock(), SkillId = skill.Id, TargetId = resolution.TargetId });
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
                case "battleEnded":
                    Debug.Log("[Healer] état : combat terminé (" + e.Result + ")");
                    break;
                case "bossAction":
                    LastAction = $"{time}  Le boss : {(e.Action == "bigAttack" ? "attaque de zone" : e.Action == "focusAttack" ? "attaque ciblée" : e.Action == "poison" ? "poison" : "attaque")}";
                    break;
                case "focusMarked":
                    Debug.Log($"[Healer] état : attaque ciblée annoncée sur {AllyName(e.UnitId)}");
                    break;
                case "unitDied":
                    LastAction = $"{time}  {AllyName(e.UnitId)} est K.O.";
                    break;
                case "bossEnraged":
                    LastAction = $"{time}  Le boss s'enrage : +{Format.Number(e.Amount)} % de dégâts";
                    Debug.Log($"[Healer] état : boss enragé palier {e.Phase} (+{Format.Number(e.Amount)} %)");
                    break;
                case "bossPhaseChanged":
                    LastAction = $"{time}  Le boss passe en phase « {e.Name} »";
                    break;
            }
            EventEmitted?.Invoke(e);
        }
    }
}
