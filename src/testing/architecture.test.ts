import { describe, it, expect } from "vitest";
import { readdirSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

/**
 * Garde-fous d'architecture (règles du CLAUDE.md) : ces tests échouent si la
 * simulation devient dépendante du rendu ou non déterministe.
 */
const SIM_DIR = join(dirname(fileURLToPath(import.meta.url)), "..", "sim");

function simSourceFiles(): Array<{ name: string; code: string }> {
  return readdirSync(SIM_DIR)
    .filter((f) => f.endsWith(".ts") && !f.endsWith(".test.ts"))
    .map((name) => {
      const raw = readFileSync(join(SIM_DIR, name), "utf-8");
      // On retire les commentaires : ils ont le droit de citer ces termes.
      const code = raw.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");
      return { name, code };
    });
}

const FORBIDDEN: Array<{ pattern: RegExp; why: string }> = [
  { pattern: /from\s+["']phaser["']|require\(["']phaser["']\)/, why: "src/sim ne doit jamais importer Phaser" },
  { pattern: /Math\.random\s*\(/, why: "aléatoire interdit hors du Rng seedé (déterminisme)" },
  { pattern: /\bDate\.now\s*\(|\bperformance\.now\s*\(|new Date\s*\(/, why: "l'horloge réelle casse le déterminisme : utiliser l'horloge de Battle" },
  { pattern: /\bwindow\b|\bdocument\b|\blocalStorage\b/, why: "pas d'accès DOM dans la simulation (elle doit pouvoir tourner côté serveur)" },
  { pattern: /from\s+["']\.\.\/scenes/, why: "la simulation ne doit pas dépendre des scènes" },
];

describe("architecture : la simulation reste pure", () => {
  const files = simSourceFiles();

  it("trouve bien les fichiers de la simulation", () => {
    expect(files.map((f) => f.name)).toContain("Battle.ts");
  });

  for (const { pattern, why } of FORBIDDEN) {
    it(`src/sim : ${why}`, () => {
      const offenders = files.filter((f) => pattern.test(f.code)).map((f) => f.name);
      expect(offenders).toEqual([]);
    });
  }
});
