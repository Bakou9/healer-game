import { configDefaults, defineConfig } from "vitest/config";

// Les tests de la version Phaser (racine) ne doivent pas ramasser ceux du sous-projet Unity,
// qui a son propre outillage (unity-version/package.json) et sa propre intégration continue.
export default defineConfig({
  test: {
    exclude: [...configDefaults.exclude, "unity-version/**"],
  },
});
