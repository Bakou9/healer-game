export type Role = "tank" | "dps" | "healer";

export interface CharacterDef {
  id: string;
  name: string;
  role: Role;
  maxHp: number;
  atk: number;
  def: number;
  /** Seul le soigneur en a besoin dans ce prototype. */
  maxMana?: number;
  manaRegenPerSec?: number;
}

export interface SkillDef {
  id: string;
  name: string;
  description?: string;
  manaCost: number;
  cooldownMs: number;
  target: "single" | "all";
  healAmount?: number;
  shieldAmount?: number;
  cleanse?: boolean;
}

/** Effet appliqué à une unité pendant un certain temps (piloté par les données). */
export interface EffectDef {
  id: string;
  name: string;
  kind: "damageOverTime";
  damagePerTick: number;
  tickMs: number;
  durationMs: number;
}

/** Vue d'un effet actif sur une unité, pour l'UI. */
export interface ActiveEffectState {
  id: string;
  name: string;
  msRemaining: number;
}

export type BossActionType = "attack" | "bigAttack" | "poison";

export interface BossActionDef {
  type: BossActionType;
  /** Durée avant l'impact pendant laquelle l'attaque est visible/prévisible. */
  telegraphMs: number;
  /** Multiplicateur des dégâts (ATK du boss × multiplier). 0 = aucun dégât, seulement l'effet. */
  multiplier?: number;
  hitsAll?: boolean;
  /** Effet appliqué aux cibles touchées (id dans effects.json). */
  effectId?: string;
}

/** Changement de comportement du boss quand ses PV passent sous un seuil. */
export interface BossPhaseDef {
  name: string;
  /** Seuil de PV (ratio de 0 à 1) qui déclenche la phase. */
  atHpRatio: number;
  tickMs: number;
  pattern: BossActionDef[];
}

export interface BossDef {
  id: string;
  name: string;
  maxHp: number;
  atk: number;
  def: number;
  /** Intervalle entre deux actions du boss. */
  tickMs: number;
  pattern: BossActionDef[];
  /** Phases suivantes, triées par seuil décroissant. */
  phases?: BossPhaseDef[];
}

export interface EncounterDef {
  id: string;
  boss: BossDef;
  allies: CharacterDef[];
  effects: EffectDef[];
  seed: number;
}

export interface Command {
  /** Horodatage en millisecondes depuis le début du combat. */
  timeMs: number;
  skillId: string;
  targetId?: string;
}

export interface UnitState {
  id: string;
  name: string;
  role: Role;
  maxHp: number;
  hp: number;
  maxMana: number;
  mana: number;
  shield: number;
  alive: boolean;
  effects: ActiveEffectState[];
}

export interface BossPhaseState {
  /** 0 = phase initiale, 1 = première phase suivante, etc. */
  index: number;
  name: string;
}

export interface Telegraph {
  type: BossActionType;
  msRemaining: number;
}

export type BattleResult = "ongoing" | "victory" | "defeat";
