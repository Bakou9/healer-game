import { createRng, pickRandom, type Rng } from "./rng";
import type { BattleEvent, BattleListener } from "./events";
import skillsData from "../data/skills.json";
import type {
  BattleResult,
  BossDef,
  CharacterDef,
  Command,
  EncounterDef,
  SkillDef,
  Telegraph,
  UnitState,
} from "./types";

const SKILLS: Record<string, SkillDef> = Object.fromEntries(
  (skillsData as SkillDef[]).map((s) => [s.id, s]),
);

/** Intervalle d'attaque automatique des alliés non-soigneurs. */
export const ALLY_ATTACK_INTERVAL_MS = 1600;

interface InternalUnit extends UnitState {
  atk: number;
  def: number;
  manaRegenPerSec: number;
  nextAttackAt: number;
  cooldowns: Record<string, number>;
}

interface BossRuntime {
  def: BossDef;
  hp: number;
  patternIndex: number;
  nextTickAt: number;
}

/**
 * Simulation de combat pure (sans rendu). Le temps avance par pas fixes via
 * `step(dtMs)`, ce qui la rend testable et rejouable à l'identique.
 * Le rendu (Phaser) ne fait que lire l'état exposé par les getters.
 */
export class Battle {
  private clock = 0;
  private rng: Rng;
  private allies: InternalUnit[];
  private boss: BossRuntime;
  private commands: Command[];
  private commandIndex = 0;
  private result: BattleResult = "ongoing";
  private log: string[] = [];
  private listeners: BattleListener[] = [];

  constructor(encounter: EncounterDef, commands: Command[] = []) {
    this.rng = createRng(encounter.seed);
    this.commands = [...commands].sort((a, b) => a.timeMs - b.timeMs);
    this.allies = encounter.allies.map((c) => this.toUnit(c));
    this.boss = {
      def: encounter.boss,
      hp: encounter.boss.maxHp,
      patternIndex: 0,
      nextTickAt: encounter.boss.tickMs,
    };
  }

  private toUnit(c: CharacterDef): InternalUnit {
    return {
      id: c.id,
      name: c.name,
      role: c.role,
      maxHp: c.maxHp,
      hp: c.maxHp,
      maxMana: c.maxMana ?? 0,
      mana: c.maxMana ?? 0,
      shield: 0,
      alive: true,
      atk: c.atk,
      def: c.def,
      manaRegenPerSec: c.manaRegenPerSec ?? 0,
      nextAttackAt: ALLY_ATTACK_INTERVAL_MS,
      cooldowns: {},
    };
  }

  // ---- Événements (Observer) ------------------------------------------------

  /** S'abonne aux événements du combat. Renvoie la fonction de désabonnement. */
  subscribe(listener: BattleListener): () => void {
    this.listeners.push(listener);
    return () => {
      this.listeners = this.listeners.filter((l) => l !== listener);
    };
  }

  private emit(event: BattleEvent): void {
    for (const listener of this.listeners) listener(event);
  }

  // ---- Lecture d'état (pour l'UI et les tests) ----------------------------

  getClock(): number {
    return this.clock;
  }

  getResult(): BattleResult {
    return this.result;
  }

  getBossHp(): number {
    return Math.max(0, this.boss.hp);
  }

  getBossMaxHp(): number {
    return this.boss.def.maxHp;
  }

  getBossName(): string {
    return this.boss.def.name;
  }

  getAllies(): UnitState[] {
    return this.allies.map(
      ({ atk: _atk, def: _def, manaRegenPerSec: _mr, nextAttackAt: _na, cooldowns: _cd, ...rest }) => rest,
    );
  }

  getCooldownRemaining(unitId: string, skillId: string): number {
    const unit = this.findUnit(unitId);
    if (!unit) return 0;
    const readyAt = unit.cooldowns[skillId] ?? 0;
    return Math.max(0, readyAt - this.clock);
  }

  getLog(): readonly string[] {
    return this.log;
  }

  /** Renvoie l'attaque à venir du boss si elle est actuellement télégraphiée. */
  getTelegraph(): Telegraph | null {
    const action = this.boss.def.pattern[this.boss.patternIndex % this.boss.def.pattern.length];
    if (!action.telegraphMs) return null;
    const msUntilTick = this.boss.nextTickAt - this.clock;
    if (msUntilTick <= action.telegraphMs) {
      return { type: action.type, msRemaining: Math.max(0, msUntilTick) };
    }
    return null;
  }

  // ---- Commandes du joueur --------------------------------------------

  /** Permet d'ajouter une commande à chaud (utilisé par l'écran de jeu). */
  issueCommand(command: Command): void {
    // Insertion triée pour rester cohérent avec le traitement par lot de step().
    const index = this.commands.findIndex((c) => c.timeMs > command.timeMs);
    if (index === -1) this.commands.push(command);
    else this.commands.splice(index, 0, command);
    if (index !== -1 && index < this.commandIndex) this.commandIndex += 1;
  }

  canUseSkillNow(unitId: string, skillId: string): boolean {
    const unit = this.findUnit(unitId);
    const skill = SKILLS[skillId];
    if (!unit || !skill) return false;
    return this.canUse(unit, skill, this.clock);
  }

  // ---- Boucle de simulation --------------------------------------------

  private findUnit(id: string) {
    return this.allies.find((u) => u.id === id);
  }

  private aliveAllies() {
    return this.allies.filter((u) => u.alive);
  }

  private canUse(unit: InternalUnit, skill: SkillDef, now: number): boolean {
    if (unit.mana < skill.manaCost) return false;
    const readyAt = unit.cooldowns[skill.id] ?? 0;
    return now >= readyAt;
  }

  private applyHeal(unit: InternalUnit, amount: number, now: number) {
    const before = unit.hp;
    unit.hp = Math.min(unit.maxHp, unit.hp + amount);
    this.emit({ type: "healed", timeMs: now, unitId: unit.id, amount: unit.hp - before });
  }

  private applyDamage(unit: InternalUnit, amount: number, now: number) {
    let remaining = amount;
    let absorbed = 0;
    if (unit.shield > 0) {
      absorbed = Math.min(unit.shield, remaining);
      unit.shield -= absorbed;
      remaining -= absorbed;
    }
    unit.hp -= remaining;
    this.emit({ type: "unitDamaged", timeMs: now, unitId: unit.id, amount: remaining, absorbed });
    if (unit.hp <= 0) {
      unit.hp = 0;
      unit.alive = false;
      this.emit({ type: "unitDied", timeMs: now, unitId: unit.id });
    }
  }

  private useSkill(healer: InternalUnit, skill: SkillDef, targetId: string | undefined, now: number) {
    if (!this.canUse(healer, skill, now)) return;
    healer.mana -= skill.manaCost;
    healer.cooldowns[skill.id] = now + skill.cooldownMs;

    const targets =
      skill.target === "all"
        ? this.aliveAllies()
        : [this.findUnit(targetId ?? "") ?? healer].filter((u) => u.alive);

    this.emit({
      type: "skillUsed",
      timeMs: now,
      casterId: healer.id,
      skillId: skill.id,
      targetIds: targets.map((t) => t.id),
    });
    for (const t of targets) {
      if (skill.healAmount) this.applyHeal(t, skill.healAmount, now);
      if (skill.shieldAmount) {
        t.shield += skill.shieldAmount;
        this.emit({ type: "shielded", timeMs: now, unitId: t.id, amount: skill.shieldAmount });
      }
    }
    this.log.push(`${now}ms: ${healer.name} utilise ${skill.name}`);
  }

  private runAllyAttacks(now: number) {
    for (const unit of this.allies) {
      if (!unit.alive || unit.role === "healer") continue;
      if (now >= unit.nextAttackAt) {
        const dmg = Math.max(1, unit.atk - this.boss.def.def);
        this.boss.hp -= dmg;
        this.emit({ type: "bossDamaged", timeMs: now, sourceId: unit.id, amount: dmg });
        unit.nextAttackAt = now + ALLY_ATTACK_INTERVAL_MS;
      }
    }
  }

  private runManaRegen(dtMs: number) {
    for (const unit of this.allies) {
      if (!unit.alive || unit.maxMana === 0) continue;
      unit.mana = Math.min(unit.maxMana, unit.mana + (unit.manaRegenPerSec * dtMs) / 1000);
    }
  }

  private runBossTick(now: number) {
    if (now < this.boss.nextTickAt) return;
    const action = this.boss.def.pattern[this.boss.patternIndex % this.boss.def.pattern.length];
    const alive = this.aliveAllies();
    const targets = action.hitsAll ? alive : alive.length ? [pickRandom(this.rng, alive)] : [];
    const dmg = Math.round(this.boss.def.atk * (action.multiplier ?? 1));
    this.emit({ type: "bossAction", timeMs: now, action: action.type, hitsAll: !!action.hitsAll });
    for (const t of targets) {
      const mitigated = Math.max(1, dmg - t.def);
      this.applyDamage(t, mitigated, now);
    }
    this.log.push(`${now}ms: le boss utilise ${action.type}${action.hitsAll ? " (zone)" : ""}`);
    this.boss.patternIndex += 1;
    this.boss.nextTickAt = now + this.boss.def.tickMs;
  }

  private checkEnd() {
    if (this.boss.hp <= 0) {
      this.result = "victory";
    } else if (this.aliveAllies().length === 0) {
      this.result = "defeat";
    } else {
      return;
    }
    this.emit({ type: "battleEnded", timeMs: this.clock, result: this.result });
  }

  /** Avance la simulation de `dtMs` millisecondes. */
  step(dtMs: number): void {
    if (this.result !== "ongoing") return;
    const targetClock = this.clock + dtMs;

    while (
      this.commandIndex < this.commands.length &&
      this.commands[this.commandIndex].timeMs <= targetClock
    ) {
      const cmd = this.commands[this.commandIndex];
      this.commandIndex += 1;
      const healer = this.allies.find((u) => u.role === "healer" && u.alive);
      const skill = SKILLS[cmd.skillId];
      if (healer && skill) {
        this.useSkill(healer, skill, cmd.targetId, cmd.timeMs);
      }
    }

    this.clock = targetClock;
    this.runManaRegen(dtMs);
    this.runAllyAttacks(this.clock);
    this.runBossTick(this.clock);
    this.checkEnd();
  }

  /** Exécute la simulation jusqu'à `maxMs` ou jusqu'à la fin du combat. Utile pour les tests. */
  run(maxMs: number, stepMs = 100): BattleResult {
    while (this.result === "ongoing" && this.clock < maxMs) {
      this.step(stepMs);
    }
    return this.result;
  }
}

export { SKILLS };
