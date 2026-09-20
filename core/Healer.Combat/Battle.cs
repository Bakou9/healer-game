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
            public double ArmorPct;
            public double DodgePct;
            public double CritPct;
            public double CritMult = 150;
            public double ThreatMod = 100;
            public double Threat;
            public string DamageType = "physical";
            public Dictionary<string, int>? Resist;
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
            /// <summary>Cible annoncée de la prochaine attaque « focusAttack » (choisie au début du télégraphe).</summary>
            public string? FocusTargetId;
        }

        private sealed class CastRuntime
        {
            public SkillDef Skill = new SkillDef();
            public string? TargetId;
            public double StartAt;
            public double EndAt;
        }

        /// <summary>Part des soins effectifs convertie en menace du soigneur (avant son modificateur).</summary>
        public const double HealThreatFactor = 0.2;

        private CastRuntime? _cast;
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
            ArmorPct = Math.Max(0, Math.Min(80, c.ArmorPct)),
            DodgePct = Math.Max(0, Math.Min(60, c.DodgePct)),
            CritPct = Math.Max(0, Math.Min(100, c.CritPct)),
            CritMult = c.CritMultPct,
            ThreatMod = Math.Max(0, c.ThreatMod),
            DamageType = string.IsNullOrEmpty(c.DamageType) ? "physical" : c.DamageType,
            Resist = c.Resist,
        };

        // ---- Aléa des mécaniques : on ne tire un nombre QUE si une chance est non nulle (les combats sans ces mécaniques
        // ---- consomment exactement les mêmes tirages qu'avant : leurs références golden ne bougent pas).

        private bool Roll(double pct) => pct > 0 && _rng.Next() * 100 < pct;

        private static double ResistOf(Dictionary<string, int>? map, string type)
        {
            if (map == null || string.IsNullOrEmpty(type) || !map.TryGetValue(type, out var v)) return 0;
            return Math.Max(-100, Math.Min(90, v));
        }

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

        /// <summary>Incantation en cours (null si le soigneur n'incante pas).</summary>
        public CastState? GetCast() => _cast == null ? null : new CastState
        {
            SkillId = _cast.Skill.Id,
            TargetId = _cast.TargetId,
            TotalMs = _cast.EndAt - _cast.StartAt,
            ElapsedMs = _clock - _cast.StartAt,
        };

        /// <summary>Allié vivant que le boss visera le plus probablement (menace la plus haute) ; null si personne.</summary>
        public string? GetTopThreatId() => AliveAllies().OrderByDescending(u => u.Threat).FirstOrDefault()?.Id;
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
            Threat = u.Threat,
            ArmorPct = u.ArmorPct,
            DodgePct = u.DodgePct,
            CritPct = u.CritPct,
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
                return new Telegraph { Type = action.Type, MsRemaining = Math.Max(0, msUntilTick), TotalMs = action.TelegraphMs, TargetId = action.Type == "focusAttack" ? _boss.FocusTargetId : null };
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
            if (skill.CastMs > 0 && _cast != null) return false; // une incantation à la fois
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

        private void ApplyHeal(Unit unit, double amount, double now, bool crit = false, Unit? source = null)
        {
            double before = unit.Hp;
            unit.Hp = Math.Min(unit.MaxHp, unit.Hp + amount);
            double healed = unit.Hp - before;
            Emit(new BattleEvent { Type = "healed", TimeMs = now, UnitId = unit.Id, Amount = healed, Overheal = amount - healed, Crit = crit });
            // Soigner attire l'attention du boss : une part des soins EFFECTIFS devient de la menace du soigneur.
            if (source != null && healed > 0) source.Threat += healed * HealThreatFactor * source.ThreatMod / 100.0;
        }

        /// <summary>effectId renseigné = dégâts d'un effet sur la durée ; sinon, un coup direct.</summary>
        private void ApplyDamage(Unit unit, double amount, double now, string? effectId = null, bool crit = false, string? damageType = null)
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
                Emit(new BattleEvent { Type = "effectTick", TimeMs = now, UnitId = unit.Id, EffectId = effectId, Amount = remaining, Absorbed = absorbed, DamageType = damageType ?? "" });
            else
                Emit(new BattleEvent { Type = "unitDamaged", TimeMs = now, UnitId = unit.Id, Amount = remaining, Absorbed = absorbed, Crit = crit, DamageType = damageType ?? "" });
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
                        double tick = fx.Def.DamagePerTick;
                        double resist = ResistOf(unit.Resist, fx.Def.DamageType);
                        if (resist != 0) tick = Math.Max(0, Math.Floor(tick * (100 - resist) / 100.0 + 0.5));
                        ApplyDamage(unit, tick, now, fx.Def.Id, false, fx.Def.DamageType);
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
            if (skill.CastMs > 0)
            {
                // Sort à incantation : on le lance au bout de CastMs (mana et recharge à l'achèvement). Une seule
                // incantation à la fois ; les sorts instantanés restent possibles pendant qu'on incante.
                if (_cast != null) return;
                _cast = new CastRuntime { Skill = skill, TargetId = targetId, StartAt = now, EndAt = now + skill.CastMs };
                Emit(new BattleEvent
                {
                    Type = "castStarted",
                    TimeMs = now,
                    CasterId = healer.Id,
                    SkillId = skill.Id,
                    TargetIds = ResolveTargets(healer, skill, targetId).Select(u => u.Id).ToList(),
                    Amount = skill.CastMs,
                });
                return;
            }
            Resolve(healer, skill, targetId, now);
        }

        private List<Unit> ResolveTargets(Unit healer, SkillDef skill, string? targetId)
        {
            if (skill.Target == "all") return AliveAllies();
            var single = FindUnit(targetId ?? "") ?? healer;
            return new List<Unit> { single }.Where(u => u.Alive).ToList();
        }

        /// <summary>Achève les incantations dont l'heure est venue (dans l'ordre du temps), ou les fait échouer.</summary>
        private void CompleteCasts(double upTo)
        {
            while (_cast != null && _cast.EndAt <= upTo)
            {
                var cast = _cast;
                _cast = null;
                var healer = _allies.FirstOrDefault(u => u.Role == "healer");
                string? failure = null;
                if (healer == null || !healer.Alive) failure = "died";
                else if (ResolveTargets(healer, cast.Skill, cast.TargetId).Count == 0) failure = "target";
                else if (!CanUse(healer, cast.Skill, cast.EndAt)) failure = "mana";
                if (failure != null)
                {
                    Emit(new BattleEvent { Type = "castFailed", TimeMs = cast.EndAt, CasterId = healer?.Id ?? "", SkillId = cast.Skill.Id, Reason = failure });
                    continue;
                }
                Resolve(healer!, cast.Skill, cast.TargetId, cast.EndAt);
            }
        }

        /// <summary>Applique le sort : mana, recharge, événement, effets sur les cibles (un seul tirage de critique par lancer).</summary>
        private void Resolve(Unit healer, SkillDef skill, string? targetId, double now)
        {
            healer.Mana -= skill.ManaCost;
            healer.Cooldowns[skill.Id] = now + skill.CooldownMs;
            var targets = ResolveTargets(healer, skill, targetId);
            bool crit = skill.HealAmount is > 0 && Roll(healer.CritPct);

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
                if (skill.HealAmount is > 0)
                {
                    double amount = skill.HealAmount.Value;
                    if (crit) amount = Math.Floor(amount * healer.CritMult / 100.0 + 0.5);
                    ApplyHeal(t, amount, now, crit, healer);
                }
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
                    bool crit = Roll(unit.CritPct);
                    double raw = unit.Atk;
                    if (crit) raw *= unit.CritMult / 100.0;
                    double resist = ResistOf(_boss.Def.Resist, unit.DamageType);
                    if (resist != 0) raw *= (100 - resist) / 100.0;
                    double dmg = Math.Max(1, Math.Floor(raw + 0.5) - _boss.Def.Def);
                    _boss.Hp -= dmg;
                    unit.Threat += dmg * unit.ThreatMod / 100.0;
                    Emit(new BattleEvent { Type = "bossDamaged", TimeMs = now, SourceId = unit.Id, Amount = dmg, Crit = crit, DamageType = unit.DamageType });
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
            _boss.FocusTargetId = null;
            _boss.NextTickAt = now + next.TickMs;
            Emit(new BattleEvent { Type = "bossPhaseChanged", TimeMs = now, Phase = _boss.PhaseIndex, Name = next.Name });
        }

        private int _enrageLevel;

        /// <summary>Palier d'enrage atteint à cet instant (0 = pas enragé ou pas d'enrage pour ce boss).</summary>
        private int EnrageLevelAt(double now)
        {
            var e = _boss.Def.Enrage;
            if (e == null || e.EveryMs <= 0 || now < e.AfterMs) return 0;
            return (int)Math.Floor((now - e.AfterMs) / e.EveryMs) + 1;
        }

        /// <summary>Multiplicateur de dégâts directs du boss dû à l'enrage (1 = aucun).</summary>
        public double GetEnrageMultiplier()
        {
            var e = _boss.Def.Enrage;
            return e == null ? 1 : 1 + EnrageLevelAt(_clock) * e.Pct / 100.0;
        }

        /// <summary>Palier d'enrage actuel (0 = calme) : l'interface l'affiche.</summary>
        public int GetEnrageLevel() => EnrageLevelAt(_clock);

        private void CheckEnrage(double now)
        {
            int level = EnrageLevelAt(now);
            if (level <= _enrageLevel) return;
            _enrageLevel = level;
            Emit(new BattleEvent { Type = "bossEnraged", TimeMs = now, Phase = level, Amount = level * _boss.Def.Enrage!.Pct });
        }

        /// <summary>Choisit la cible d'une attaque à cible unique : au hasard, ou en proportion de la menace (1 + menace) selon le boss.</summary>
        private Unit PickTarget(List<Unit> alive)
        {
            if (_boss.Def.Targeting != "threat") return _rng.PickRandom(alive);
            double total = alive.Sum(u => 1 + u.Threat);
            double r = _rng.Next() * total;
            foreach (var u in alive)
            {
                r -= 1 + u.Threat;
                if (r < 0) return u;
            }
            return alive[alive.Count - 1];
        }

        /// <summary>
        /// Attaque ciblée (« focusAttack ») : la victime est choisie au HASARD parmi les alliés fragiles (tout sauf le tank, sauf s'il
        /// ne reste que lui) DÈS le début du télégraphe, puis annoncée (événement focusMarked, Telegraph.TargetId) : le joueur a le
        /// temps de la protéger, un soin de zone ne suffit pas à la sauver. Si la victime meurt avant le coup, une autre est choisie.
        /// </summary>
        private void MarkFocusTarget(double now)
        {
            var action = _boss.Pattern[_boss.PatternIndex % _boss.Pattern.Count];
            if (action.Type != "focusAttack") { _boss.FocusTargetId = null; return; }
            if (_boss.NextTickAt - now > action.TelegraphMs) return; // pas encore annoncée
            var current = _boss.FocusTargetId == null ? null : FindUnit(_boss.FocusTargetId);
            if (current != null && current.Alive) return;
            var alive = AliveAllies();
            if (alive.Count == 0) return;
            var fragile = alive.Where(u => u.Role != "tank").ToList();
            var pick = _rng.PickRandom(fragile.Count > 0 ? fragile : alive);
            _boss.FocusTargetId = pick.Id;
            Emit(new BattleEvent { Type = "focusMarked", TimeMs = now, UnitId = pick.Id });
        }

        private void RunBossTick(double now)
        {
            if (now < _boss.NextTickAt) return;
            var action = _boss.Pattern[_boss.PatternIndex % _boss.Pattern.Count];
            var alive = AliveAllies();
            List<Unit> targets;
            if (action.HitsAll == true) targets = alive;
            else if (action.Type == "focusAttack" && alive.Count > 0)
            {
                var marked = _boss.FocusTargetId == null ? null : alive.FirstOrDefault(u => u.Id == _boss.FocusTargetId);
                targets = new List<Unit> { marked ?? PickTarget(alive) };
                _boss.FocusTargetId = null;
            }
            else targets = alive.Count > 0 ? new List<Unit> { PickTarget(alive) } : new List<Unit>();
            // Arrondi « demi vers le haut » comme Math.round en JavaScript (et non l'arrondi bancaire de C#).
            double baseDmg = _boss.Def.Atk * (action.Multiplier ?? 1) * (1 + EnrageLevelAt(now) * (_boss.Def.Enrage?.Pct ?? 0) / 100.0);
            bool hasDamage = Math.Floor(baseDmg + 0.5) > 0;
            string type = !string.IsNullOrEmpty(action.DamageType) ? action.DamageType! : (string.IsNullOrEmpty(_boss.Def.DamageType) ? "physical" : _boss.Def.DamageType);
            Emit(new BattleEvent { Type = "bossAction", TimeMs = now, Action = action.Type, HitsAll = action.HitsAll == true });
            foreach (var t in targets)
            {
                // Esquive : le coup est évité en entier, effet compris.
                if (Roll(t.DodgePct))
                {
                    Emit(new BattleEvent { Type = "unitDodged", TimeMs = now, UnitId = t.Id });
                    continue;
                }
                if (hasDamage)
                {
                    bool crit = Roll(_boss.Def.CritPct);
                    double raw = baseDmg;
                    if (crit) raw *= _boss.Def.CritMultPct / 100.0;
                    double resist = ResistOf(t.Resist, type);
                    if (resist != 0) raw *= (100 - resist) / 100.0;
                    if (type == "physical" && t.ArmorPct > 0) raw *= (100 - t.ArmorPct) / 100.0; // armure en % : dégâts physiques seulement
                    double dmg = Math.Floor(raw + 0.5);
                    ApplyDamage(t, Math.Max(1, dmg - t.Def), now, null, crit, type);
                }
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
                CompleteCasts(cmd.TimeMs);
                var healer = _allies.FirstOrDefault(u => u.Role == "healer" && u.Alive);
                if (healer != null && _skills.TryGetValue(cmd.SkillId, out var skill))
                    UseSkill(healer, skill, cmd.TargetId, cmd.TimeMs);
            }
            CompleteCasts(targetClock);

            _clock = targetClock;
            RunManaRegen(dtMs);
            RunEffects(_clock);
            RunAllyAttacks(_clock);
            CheckBossPhase(_clock);
            CheckEnrage(_clock);
            MarkFocusTarget(_clock);
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
