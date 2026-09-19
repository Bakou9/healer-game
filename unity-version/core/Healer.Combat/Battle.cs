using System;
using System.Collections.Generic;
using System.Linq;

namespace Healer.Combat
{
    /// <summary>
    /// Simulation de combat pure (sans rendu, sans Unity). Le temps avance par pas fixes via
    /// <see cref="Step"/>, ce qui la rend testable et rejouable à l'identique. Le client ne fait que
    /// lire l'état exposé et envoyer des commandes. Port fidèle de la version TypeScript : mêmes
    /// règles, même ordre d'émission des événements (vérifié par les références golden).
    /// </summary>
    public sealed class Battle
    {
        /// <summary>Intervalle d'attaque automatique des alliés non-soigneurs.</summary>
        public const double AllyAttackIntervalMs = 1600;

        private sealed class RunningEffect
        {
            public EffectDef Def = new EffectDef();
            public double ExpiresAt;
            public double NextTickAt;
        }

        private sealed class Unit
        {
            public string Id = "";
            public string Name = "";
            public string Role = "";
            public double MaxHp;
            public double Hp;
            public double MaxMana;
            public double Mana;
            public double Shield;
            public bool Alive;
            public double Atk;
            public double Def;
            public double ManaRegenPerSec;
            public double NextAttackAt;
            public Dictionary<string, double> Cooldowns = new Dictionary<string, double>();
            public List<RunningEffect> Effects = new List<RunningEffect>();
        }

        private sealed class BossRuntime
        {
            public BossDef Def = new BossDef();
            public double Hp;
            public List<BossActionDef> Pattern = new List<BossActionDef>();
            public double TickMs;
            public int PhaseIndex;
            public int PatternIndex;
            public double NextTickAt;
        }

        private double _clock;
        private readonly Rng _rng;
        private readonly List<Unit> _allies;
        private readonly Dictionary<string, EffectDef> _effectDefs;
        private readonly Dictionary<string, SkillDef> _skills;
        private readonly BossRuntime _boss;
        private readonly List<Command> _commands;
        private int _commandIndex;
        private string _result = BattleResults.Ongoing;
        private readonly List<Action<BattleEvent>> _listeners = new List<Action<BattleEvent>>();

        public Battle(EncounterDef encounter, IEnumerable<Command>? commands = null)
        {
            _rng = new Rng(encounter.Seed);
            // Tri STABLE (comme en JavaScript) : OrderBy conserve l'ordre des commandes simultanées.
            _commands = (commands ?? Enumerable.Empty<Command>()).OrderBy(c => c.TimeMs).ToList();
            _allies = encounter.Allies.Select(ToUnit).ToList();
            _effectDefs = encounter.Effects.ToDictionary(e => e.Id);
            _skills = encounter.Skills.ToDictionary(s => s.Id);
            _boss = new BossRuntime
            {
                Def = encounter.Boss,
                Hp = encounter.Boss.MaxHp,
                Pattern = encounter.Boss.Pattern,
                TickMs = encounter.Boss.TickMs,
                PhaseIndex = 0,
                PatternIndex = 0,
                NextTickAt = encounter.Boss.TickMs,
            };
        }

        private static Unit ToUnit(CharacterDef c) => new Unit
        {
            Id = c.Id,
            Name = c.Name,
            Role = c.Role,
            MaxHp = c.MaxHp,
            Hp = c.MaxHp,
            MaxMana = c.MaxMana ?? 0,
            Mana = c.MaxMana ?? 0,
            Shield = 0,
            Alive = true,
            Atk = c.Atk,
            Def = c.Def,
            ManaRegenPerSec = c.ManaRegenPerSec ?? 0,
            NextAttackAt = AllyAttackIntervalMs,
        };

        // ---- Événements (Observer) ------------------------------------------------

        /// <summary>S'abonne aux événements du combat. Renvoie l'action de désabonnement.</summary>
        public Action Subscribe(Action<BattleEvent> listener)
        {
            _listeners.Add(listener);
            return () => _listeners.Remove(listener);
        }

        private void Emit(BattleEvent e)
        {
            // Copie : un observateur peut se désabonner pendant la notification.
            foreach (var listener in _listeners.ToArray()) listener(e);
        }

        // ---- Lecture d'état (pour l'UI et les tests) ----------------------------

        public double GetClock() => _clock;
        public string GetResult() => _result;
        public double GetBossHp() => Math.Max(0, _boss.Hp);
        public double GetBossMaxHp() => _boss.Def.MaxHp;
        public string GetBossName() => _boss.Def.Name;

        public List<UnitState> GetAllies() => _allies.Select(u => new UnitState
        {
            Id = u.Id,
            Name = u.Name,
            Role = u.Role,
            MaxHp = u.MaxHp,
            Hp = u.Hp,
            MaxMana = u.MaxMana,
            Mana = u.Mana,
            Shield = u.Shield,
            Alive = u.Alive,
            Effects = u.Effects.Select(e => new ActiveEffectState
            {
                Id = e.Def.Id,
                Name = e.Def.Name,
                MsRemaining = Math.Max(0, e.ExpiresAt - _clock),
            }).ToList(),
        }).ToList();

        public BossPhaseState GetBossPhase()
        {
            int index = _boss.PhaseIndex;
            string name = "";
            if (index != 0 && _boss.Def.Phases != null && index - 1 < _boss.Def.Phases.Count)
                name = _boss.Def.Phases[index - 1].Name;
            return new BossPhaseState { Index = index, Name = name };
        }

        public double GetCooldownRemaining(string unitId, string skillId)
        {
            var unit = FindUnit(unitId);
            if (unit == null) return 0;
            double readyAt = unit.Cooldowns.TryGetValue(skillId, out var r) ? r : 0;
            return Math.Max(0, readyAt - _clock);
        }

        /// <summary>Renvoie l'attaque à venir du boss si elle est actuellement télégraphée.</summary>
        public Telegraph? GetTelegraph()
        {
            var action = _boss.Pattern[_boss.PatternIndex % _boss.Pattern.Count];
            if (action.TelegraphMs == 0) return null;
            double msUntilTick = _boss.NextTickAt - _clock;
            if (msUntilTick <= action.TelegraphMs)
                return new Telegraph { Type = action.Type, MsRemaining = Math.Max(0, msUntilTick), TotalMs = action.TelegraphMs };
            return null;
        }

        // ---- Commandes du joueur --------------------------------------------

        /// <summary>Ajoute une commande à chaud (utilisé par l'écran de jeu, le bot et les tests).</summary>
        public void IssueCommand(Command command)
        {
            // Insertion triée pour rester cohérent avec le traitement par lot de Step().
            int index = _commands.FindIndex(c => c.TimeMs > command.TimeMs);
            if (index == -1) _commands.Add(command);
            else _commands.Insert(index, command);
            if (index != -1 && index < _commandIndex) _commandIndex += 1;
        }

        public bool CanUseSkillNow(string unitId, string skillId)
        {
            var unit = FindUnit(unitId);
            if (unit == null || !_skills.TryGetValue(skillId, out var skill)) return false;
            return CanUse(unit, skill, _clock);
        }

        // ---- Boucle de simulation --------------------------------------------

        private Unit? FindUnit(string id) => _allies.FirstOrDefault(u => u.Id == id);

        private List<Unit> AliveAllies() => _allies.Where(u => u.Alive).ToList();

        private static bool CanUse(Unit unit, SkillDef skill, double now)
        {
            if (unit.Mana < skill.ManaCost) return false;
            double readyAt = unit.Cooldowns.TryGetValue(skill.Id, out var r) ? r : 0;
            return now >= readyAt;
        }

        private void ApplyHeal(Unit unit, double amount, double now)
        {
            double before = unit.Hp;
            unit.Hp = Math.Min(unit.MaxHp, unit.Hp + amount);
            Emit(new BattleEvent { Type = "healed", TimeMs = now, UnitId = unit.Id, Amount = unit.Hp - before });
        }

        /// <summary>effectId renseigné = dégâts d'un effet sur la durée ; sinon, un coup direct.</summary>
        private void ApplyDamage(Unit unit, double amount, double now, string? effectId = null)
        {
            double remaining = amount;
            double absorbed = 0;
            if (unit.Shield > 0)
            {
                absorbed = Math.Min(unit.Shield, remaining);
                unit.Shield -= absorbed;
                remaining -= absorbed;
            }
            unit.Hp -= remaining;
            bool died = unit.Hp <= 0;
            if (died)
            {
                unit.Hp = 0;
                unit.Alive = false;
            }
            // L'état est final avant d'émettre : un observateur ne voit jamais de PV négatifs.
            if (effectId != null)
                Emit(new BattleEvent { Type = "effectTick", TimeMs = now, UnitId = unit.Id, EffectId = effectId, Amount = remaining, Absorbed = absorbed });
            else
                Emit(new BattleEvent { Type = "unitDamaged", TimeMs = now, UnitId = unit.Id, Amount = remaining, Absorbed = absorbed });
            if (died)
            {
                Emit(new BattleEvent { Type = "unitDied", TimeMs = now, UnitId = unit.Id });
                ClearEffects(unit, now, "died");
            }
        }

        private void ApplyEffect(Unit unit, string effectId, double now)
        {
            if (!_effectDefs.TryGetValue(effectId, out var def) || !unit.Alive) return;
            // Ré-application = la durée est rafraîchie (pas d'empilement).
            unit.Effects = unit.Effects.Where(e => e.Def.Id != effectId).ToList();
            unit.Effects.Add(new RunningEffect { Def = def, ExpiresAt = now + def.DurationMs, NextTickAt = now + def.TickMs });
            Emit(new BattleEvent { Type = "effectApplied", TimeMs = now, UnitId = unit.Id, EffectId = effectId });
        }

        private void ClearEffects(Unit unit, double now, string reason)
        {
            foreach (var fx in unit.Effects)
                Emit(new BattleEvent { Type = "effectEnded", TimeMs = now, UnitId = unit.Id, EffectId = fx.Def.Id, Reason = reason });
            unit.Effects = new List<RunningEffect>();
        }

        private void RunEffects(double now)
        {
            foreach (var unit in _allies)
            {
                foreach (var fx in unit.Effects.ToList())
                {
                    if (!unit.Alive) break;
                    if (now >= fx.NextTickAt)
                    {
                        ApplyDamage(unit, fx.Def.DamagePerTick, now, fx.Def.Id);
                        fx.NextTickAt += fx.Def.TickMs;
                    }
                    if (unit.Alive && now >= fx.ExpiresAt)
                    {
                        unit.Effects = unit.Effects.Where(e => e != fx).ToList();
                        Emit(new BattleEvent { Type = "effectEnded", TimeMs = now, UnitId = unit.Id, EffectId = fx.Def.Id, Reason = "expired" });
                    }
                }
            }
        }

        private void UseSkill(Unit healer, SkillDef skill, string? targetId, double now)
        {
            if (!CanUse(healer, skill, now)) return;
            healer.Mana -= skill.ManaCost;
            healer.Cooldowns[skill.Id] = now + skill.CooldownMs;

            List<Unit> targets;
            if (skill.Target == "all")
            {
                targets = AliveAllies();
            }
            else
            {
                var single = FindUnit(targetId ?? "") ?? healer;
                targets = new List<Unit> { single }.Where(u => u.Alive).ToList();
            }

            Emit(new BattleEvent
            {
                Type = "skillUsed",
                TimeMs = now,
                CasterId = healer.Id,
                SkillId = skill.Id,
                TargetIds = targets.Select(t => t.Id).ToList(),
            });
            foreach (var t in targets)
            {
                if (skill.HealAmount is > 0) ApplyHeal(t, skill.HealAmount.Value, now);
                if (skill.ShieldAmount is > 0)
                {
                    t.Shield += skill.ShieldAmount.Value;
                    Emit(new BattleEvent { Type = "shielded", TimeMs = now, UnitId = t.Id, Amount = skill.ShieldAmount.Value });
                }
                if (skill.Cleanse == true) ClearEffects(t, now, "cleansed");
            }
        }

        private void RunAllyAttacks(double now)
        {
            foreach (var unit in _allies)
            {
                if (!unit.Alive || unit.Role == "healer") continue;
                if (now >= unit.NextAttackAt)
                {
                    double dmg = Math.Max(1, unit.Atk - _boss.Def.Def);
                    _boss.Hp -= dmg;
                    Emit(new BattleEvent { Type = "bossDamaged", TimeMs = now, SourceId = unit.Id, Amount = dmg });
                    unit.NextAttackAt = now + AllyAttackIntervalMs;
                }
            }
        }

        private void RunManaRegen(double dtMs)
        {
            foreach (var unit in _allies)
            {
                if (!unit.Alive || unit.MaxMana == 0) continue;
                unit.Mana = Math.Min(unit.MaxMana, unit.Mana + (unit.ManaRegenPerSec * dtMs) / 1000);
            }
        }

        /// <summary>Passe à la phase suivante quand les PV du boss franchissent son seuil (table de transitions dans les données).</summary>
        private void CheckBossPhase(double now)
        {
            var phases = _boss.Def.Phases;
            if (phases == null || _boss.PhaseIndex >= phases.Count || _boss.Hp <= 0) return;
            var next = phases[_boss.PhaseIndex];
            if (_boss.Hp / _boss.Def.MaxHp > next.AtHpRatio) return;
            _boss.PhaseIndex += 1;
            _boss.Pattern = next.Pattern;
            _boss.TickMs = next.TickMs;
            _boss.PatternIndex = 0;
            _boss.NextTickAt = now + next.TickMs;
            Emit(new BattleEvent { Type = "bossPhaseChanged", TimeMs = now, Phase = _boss.PhaseIndex, Name = next.Name });
        }

        private void RunBossTick(double now)
        {
            if (now < _boss.NextTickAt) return;
            var action = _boss.Pattern[_boss.PatternIndex % _boss.Pattern.Count];
            var alive = AliveAllies();
            List<Unit> targets;
            if (action.HitsAll == true) targets = alive;
            else targets = alive.Count > 0 ? new List<Unit> { _rng.PickRandom(alive) } : new List<Unit>();
            // Arrondi « demi vers le haut » comme Math.round en JavaScript (et non l'arrondi bancaire de C#).
            double dmg = Math.Floor(_boss.Def.Atk * (action.Multiplier ?? 1) + 0.5);
            Emit(new BattleEvent { Type = "bossAction", TimeMs = now, Action = action.Type, HitsAll = action.HitsAll == true });
            foreach (var t in targets)
            {
                if (dmg > 0) ApplyDamage(t, Math.Max(1, dmg - t.Def), now);
                if (action.EffectId != null) ApplyEffect(t, action.EffectId, now);
            }
            _boss.PatternIndex += 1;
            _boss.NextTickAt = now + _boss.TickMs;
        }

        private void CheckEnd()
        {
            if (_boss.Hp <= 0) _result = BattleResults.Victory;
            else if (AliveAllies().Count == 0) _result = BattleResults.Defeat;
            else return;
            Emit(new BattleEvent { Type = "battleEnded", TimeMs = _clock, Result = _result });
        }

        /// <summary>Avance la simulation de dtMs millisecondes.</summary>
        public void Step(double dtMs)
        {
            if (_result != BattleResults.Ongoing) return;
            double targetClock = _clock + dtMs;

            while (_commandIndex < _commands.Count && _commands[_commandIndex].TimeMs <= targetClock)
            {
                var cmd = _commands[_commandIndex];
                _commandIndex += 1;
                var healer = _allies.FirstOrDefault(u => u.Role == "healer" && u.Alive);
                if (healer != null && _skills.TryGetValue(cmd.SkillId, out var skill))
                    UseSkill(healer, skill, cmd.TargetId, cmd.TimeMs);
            }

            _clock = targetClock;
            RunManaRegen(dtMs);
            RunEffects(_clock);
            RunAllyAttacks(_clock);
            CheckBossPhase(_clock);
            RunBossTick(_clock);
            CheckEnd();
        }

        /// <summary>Exécute la simulation jusqu'à maxMs ou jusqu'à la fin du combat. Utile pour les tests.</summary>
        public string Run(double maxMs, double stepMs = 100)
        {
            while (_result == BattleResults.Ongoing && _clock < maxMs) Step(stepMs);
            return _result;
        }
    }
}
