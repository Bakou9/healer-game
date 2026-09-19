/**
 * Affichage des valeurs à hauteur humaine (règle du projet, voir CLAUDE.md et
 * docs/EQUILIBRAGE.md) : tout nombre montré au joueur est TRONQUÉ (jamais
 * arrondi vers le haut : on n'affiche pas 1 PV de plus que la réalité), sans
 * décimales inutiles, et abrégé quand il devient long. Aucun nombre brut
 * (`7.199999`, `1234567`) ne doit atteindre l'écran : passer par ces fonctions.
 */

/** Tronque vers zéro à `decimals` décimales, en absorbant l'erreur des flottants (4,35 × 100 = 434,999…). */
export function truncate(value: number, decimals = 0): number {
  const factor = 10 ** decimals;
  const scaled = value * factor;
  const epsilon = Math.abs(scaled) * 1e-12 + 1e-9;
  return Math.trunc(scaled + Math.sign(scaled) * epsilon) / factor;
}

function withComma(n: number): string {
  return String(n).replace(".", ",");
}

/** 7000 → "7000", 12399 → "12,3k", 2 500 000 → "2,5M". Tronqué, jamais arrondi. */
export function formatNumber(value: number): string {
  const abs = Math.abs(value);
  if (abs >= 1_000_000) return `${withComma(truncate(value / 1_000_000, 1))}M`;
  if (abs >= 10_000) return `${withComma(truncate(value / 1_000, 1))}k`;
  return String(truncate(value));
}

/** Durée en secondes, une décimale tronquée : 4960 → "4,9s", 12000 → "12s". */
export function formatSeconds(ms: number): string {
  return `${withComma(truncate(Math.max(0, ms) / 1000, 1))}s`;
}

/** "612/900" (valeur actuelle / maximum), tronqués. */
export function formatRatio(current: number, max: number): string {
  return `${formatNumber(current)}/${formatNumber(max)}`;
}
