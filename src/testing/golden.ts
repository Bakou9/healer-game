import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

/**
 * Tests "golden master" : on compare le déroulé complet d'un combat (une ligne
 * par événement) à une référence versionnée dans `src/testing/golden/`.
 * Toute divergence est une RÉGRESSION POTENTIELLE : elle peut être voulue
 * (règle modifiée exprès) ou accidentelle (effet de bord). Le message d'erreur
 * indique le premier point de divergence pour aider à trancher.
 */
const GOLDEN_DIR = join(dirname(fileURLToPath(import.meta.url)), "golden");

export const UPDATE_HINT =
  "Si ce changement est VOULU : expliquer la cause à l'utilisateur, obtenir son accord, " +
  "puis lancer `npm run test:update-golden`. Sinon, c'est une régression à corriger.";

export interface GoldenDiff {
  firstDivergenceLine: number;
  expected: string | undefined;
  actual: string | undefined;
  differingLines: number;
}

/** Calcule le premier point de divergence entre deux listes de lignes (exporté pour être testé). */
export function diffLines(expected: string[], actual: string[]): GoldenDiff | null {
  const max = Math.max(expected.length, actual.length);
  let first = -1;
  let count = 0;
  for (let i = 0; i < max; i++) {
    if (expected[i] !== actual[i]) {
      if (first === -1) first = i;
      count += 1;
    }
  }
  if (first === -1) return null;
  return {
    firstDivergenceLine: first + 1,
    expected: expected[first],
    actual: actual[first],
    differingLines: count,
  };
}

export function expectMatchesGolden(name: string, actual: string[]): void {
  const file = join(GOLDEN_DIR, `${name}.txt`);
  const text = actual.join("\n") + "\n";

  if (process.env.UPDATE_GOLDEN === "1") {
    mkdirSync(GOLDEN_DIR, { recursive: true });
    writeFileSync(file, text, "utf-8");
    return;
  }

  if (!existsSync(file)) {
    throw new Error(
      `Référence golden absente : ${name}.txt. Générer avec \`npm run test:update-golden\` ` +
        `(et vérifier son contenu avant de la valider).`,
    );
  }

  const expected = readFileSync(file, "utf-8").replace(/\r\n/g, "\n").trimEnd().split("\n");
  const diff = diffLines(expected, actual);
  if (diff) {
    throw new Error(
      [
        `RÉGRESSION POTENTIELLE dans le scénario « ${name} » :`,
        `  - ${diff.differingLines} ligne(s) différente(s), première à la ligne ${diff.firstDivergenceLine}`,
        `  - attendu : ${diff.expected ?? "(fin du fichier)"}`,
        `  - obtenu  : ${diff.actual ?? "(fin du combat)"}`,
        UPDATE_HINT,
      ].join("\n"),
    );
  }
}
