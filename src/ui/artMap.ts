/**
 * Correspondance entre le contenu de jeu (personnages, sorts) et les icônes
 * dessinées (docs/UX.md : identité visuelle, rôle jamais porté par la couleur
 * seule). Données pures, testées : ajouter un sort ou un personnage sans icône
 * fait échouer `artMap.test.ts` plutôt que d'afficher un trou à l'écran.
 */
export type PortraitIcon = "shield" | "bow" | "staff" | "cross";
export type SkillIcon = "heal" | "wave" | "barrier" | "purify";

/** Icône propre à un personnage (par id) ; à défaut, celle de son rôle. */
export const PORTRAIT_BY_ID: Record<string, PortraitIcon> = {
  tank: "shield",
  dps1: "bow",
  dps2: "staff",
  healer: "cross",
};

export const PORTRAIT_BY_ROLE: Record<string, PortraitIcon> = {
  tank: "shield",
  dps: "bow",
  healer: "cross",
};

export const SKILL_ICON: Record<string, SkillIcon> = {
  heal_single: "heal",
  heal_aoe: "wave",
  shield: "barrier",
  purge: "purify",
};

export function portraitFor(id: string, role: string): PortraitIcon | undefined {
  return PORTRAIT_BY_ID[id] ?? PORTRAIT_BY_ROLE[role];
}
