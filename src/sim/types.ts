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

export type BossActionType = "attack" | "bigAttack";

export interface BossActionDef {
  type: BossActionType;
  /** Durée avant l'impact pendant laquelle l'attaque est visible/prévisible. */
  telegraphMs: number;
  multiplier?: number;
  hitsAll?: boolean;
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
}

export interface EncounterDef {
  id: string;
  boss: BossDef;
  allies: CharacterDef[];
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
}

export interface Telegraph {
  type: BossActionType;
  msRemaining: number;
}

export type BattleResult = "ongoing" | "victory" | "defeat";
