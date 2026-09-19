import { describe, it, expect } from "vitest";
import { formatNumber, formatRatio, formatSeconds, truncate } from "./format";

describe("truncate", () => {
  it("tronque vers zéro au lieu d'arrondir", () => {
    expect(truncate(7.99)).toBe(7);
    expect(truncate(-7.99)).toBe(-7);
    expect(truncate(4.96, 1)).toBe(4.9);
  });

  it("absorbe les erreurs de flottants (4,35 × 100 vaut 434,999… en binaire)", () => {
    expect(truncate(4.35, 2)).toBe(4.35);
    expect(truncate(0.3, 1)).toBe(0.3);
    expect(truncate(1.1, 1)).toBe(1.1);
  });
});

describe("formatNumber", () => {
  it("affiche un entier tronqué en dessous de 10 000", () => {
    expect(formatNumber(0)).toBe("0");
    expect(formatNumber(7.9)).toBe("7");
    expect(formatNumber(7000)).toBe("7000");
    expect(formatNumber(9999.99)).toBe("9999");
  });

  it("abrège avec k et M au-delà, en tronquant (jamais en arrondissant vers le haut)", () => {
    expect(formatNumber(10000)).toBe("10k");
    expect(formatNumber(12399)).toBe("12,3k");
    expect(formatNumber(999999)).toBe("999,9k");
    expect(formatNumber(2500000)).toBe("2,5M");
  });

  it("ne produit jamais de notation scientifique ni de longues décimales", () => {
    for (const n of [0.1 + 0.2, 1 / 3, 7.199999999, 123456789, 1e9]) {
      expect(formatNumber(n)).toMatch(/^-?\d+(,\d)?[kM]?$/);
    }
  });
});

describe("formatSeconds", () => {
  it("tronque à une décimale", () => {
    expect(formatSeconds(4960)).toBe("4,9s");
    expect(formatSeconds(12000)).toBe("12s");
    expect(formatSeconds(0)).toBe("0s");
  });

  it("ne descend jamais sous zéro", () => {
    expect(formatSeconds(-500)).toBe("0s");
  });
});

describe("formatRatio", () => {
  it("affiche courant/maximum tronqués", () => {
    expect(formatRatio(612.7, 900)).toBe("612/900");
  });
});
