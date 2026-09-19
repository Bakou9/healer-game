import type { BattleResult, BossActionType } from "./types";

/**
 * Événements émis par la simulation (patron Observer).
 * La simulation ne connaît pas ses observateurs : l'UI (sons, chiffres
 * flottants), les tests de non-régression et un futur replay serveur s'y
 * abonnent sans que `Battle` ait à changer.
 */
export type BattleEvent =
  | { type: "skillUsed"; timeMs: number; casterId: string; skillId: string; targetIds: string[] }
  | { type: "healed"; timeMs: number; unitId: string; amount: number }
  | { type: "shielded"; timeMs: number; unitId: string; amount: number }
  | { type: "unitDamaged"; timeMs: number; unitId: string; amount: number; absorbed: number }
  | { type: "bossDamaged"; timeMs: number; sourceId: string; amount: number }
  | { type: "unitDied"; timeMs: number; unitId: string }
  | { type: "bossAction"; timeMs: number; action: BossActionType; hitsAll: boolean }
  | { type: "battleEnded"; timeMs: number; result: BattleResult };

export type BattleListener = (event: BattleEvent) => void;

/** Représentation texte stable d'un événement (utilisée par les tests de non-régression). */
export function formatEvent(e: BattleEvent): string {
  const head = `${e.timeMs}ms ${e.type}`;
  switch (e.type) {
    case "skillUsed":
      return `${head} ${e.casterId} ${e.skillId} -> [${e.targetIds.join(",")}]`;
    case "healed":
    case "shielded":
      return `${head} ${e.unitId} ${e.amount}`;
    case "unitDamaged":
      return `${head} ${e.unitId} ${e.amount} (absorbé ${e.absorbed})`;
    case "bossDamaged":
      return `${head} par ${e.sourceId} ${e.amount}`;
    case "unitDied":
      return `${head} ${e.unitId}`;
    case "bossAction":
      return `${head} ${e.action}${e.hitsAll ? " zone" : ""}`;
    case "battleEnded":
      return `${head} ${e.result}`;
  }
}
