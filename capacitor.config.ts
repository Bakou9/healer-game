import type { CapacitorConfig } from "@capacitor/cli";

const config: CapacitorConfig = {
  appId: "com.healergame.app",
  appName: "Healer Game",
  webDir: "dist",
  android: {
    // Utile pendant le développement pour voir les logs JS dans Logcat.
    allowMixedContent: false,
  },
};

export default config;
