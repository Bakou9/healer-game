// Régénère les références « golden » (src/testing/golden/*.txt).
// À n'utiliser qu'après avoir expliqué et fait valider un changement de comportement voulu.
import { spawnSync } from "node:child_process";

const result = spawnSync("npx", ["vitest", "run", "src/testing/regression.test.ts"], {
  stdio: "inherit",
  shell: true,
  env: { ...process.env, UPDATE_GOLDEN: "1" },
});
console.log("\nRéférences golden régénérées. Relisez le diff avant de les valider.");
process.exit(result.status ?? 1);
