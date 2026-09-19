import { defineConfig } from "vite";

export default defineConfig({
  // Capacitor sert le contenu depuis une racine relative sur Android.
  base: "./",
  server: {
    host: true,
    port: 5173,
    // Échoue plutôt que de changer de port en silence : l'onglet ouvert dans
    // le navigateur reste ainsi toujours celui qui se recharge.
    strictPort: true,
  },
  build: {
    outDir: "dist",
    sourcemap: true,
  },
});
