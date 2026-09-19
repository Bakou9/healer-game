/**
 * PRNG déterministe (mulberry32). À seed égale et commandes égales, une
 * bataille rejoue toujours exactement de la même façon : indispensable pour
 * les tests, le débogage, et plus tard pour valider les tirages côté serveur.
 */
export type Rng = () => number;

export function createRng(seed: number): Rng {
  let a = seed >>> 0;
  return function rng(): number {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

export function pickRandom<T>(rng: Rng, items: readonly T[]): T {
  if (items.length === 0) {
    throw new Error("pickRandom: la liste est vide");
  }
  const index = Math.floor(rng() * items.length);
  return items[Math.min(index, items.length - 1)];
}
