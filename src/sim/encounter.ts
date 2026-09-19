import charactersData from "../data/characters.json";
import bossData from "../data/boss1.json";
import effectsData from "../data/effects.json";
import type { BossDef, CharacterDef, EffectDef, EncounterDef } from "./types";

const CHARACTERS = charactersData as CharacterDef[];
const BOSS = bossData as BossDef;
const EFFECTS = effectsData as EffectDef[];

/**
 * Construit une rencontre reproductible. `seed` contrôle tout l'aléatoire
 * (ciblage des attaques du boss) : même seed + mêmes commandes = même combat.
 */
export function createEncounter(seed: number): EncounterDef {
  return {
    id: "encounter-" + BOSS.id,
    boss: BOSS,
    allies: CHARACTERS,
    effects: EFFECTS,
    seed,
  };
}
