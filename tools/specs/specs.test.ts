import { describe, it, expect } from "vitest";
import { existsSync, readFileSync } from "node:fs";
import { join } from "node:path";
import {
  PRIORITIES,
  SIZES,
  SPECS_DIR,
  STATUSES,
  TYPES,
  applyEpicTable,
  applyIndex,
  loadEpics,
} from "./specs";

/**
 * Cohérence des spécifications (docs/specs) : une spec qui dérive est pire
 * que pas de spec. Si ce test échoue après une modification de ticket, lancer
 * `npm run specs:index` (tableaux) ou corriger l'en-tête/les sections signalés.
 */
const epics = loadEpics();
const tickets = epics.flatMap((e) => e.tickets);
const REQUIRED_SECTIONS = [
  "## Contexte",
  "## Critères d'acceptation",
  "## Tests automatiques exigés",
  "## Impact équilibrage",
];

describe("spécifications : structure", () => {
  it("contient les documents de référence et des epics", () => {
    for (const doc of ["VISION.md", "README.md"]) expect(existsSync(join(SPECS_DIR, doc)), doc).toBe(true);
    for (const doc of ["DECISIONS.md", "ARCHITECTURE.md", "UX.md", "EQUILIBRAGE.md", "REVUES.md"]) {
      expect(existsSync(join(SPECS_DIR, "..", doc)), doc).toBe(true);
    }
    expect(epics.length).toBeGreaterThanOrEqual(13);
  });

  it("chaque epic a un README avec un id cohérent avec son dossier et des marqueurs de tableau", () => {
    for (const e of epics) {
      expect(e.id, e.folder).toMatch(/^E\d\d$/);
      expect(e.folder.startsWith(e.id), e.folder).toBe(true);
      expect(e.title, e.folder).not.toBe("");
      expect(e.readme, e.folder).toContain("<!-- TICKETS:START -->");
      expect(e.tickets.length, e.folder).toBeGreaterThan(0);
    }
  });

  it("les ids de ticket sont uniques, bien formés et cohérents avec leur epic et leur fichier", () => {
    const seen = new Set<string>();
    for (const t of tickets) {
      expect(t.id, t.file).toMatch(/^E\d\d-T\d\d$/);
      expect(seen.has(t.id), `id en double : ${t.id}`).toBe(false);
      seen.add(t.id);
      expect(t.file.startsWith(t.id), t.file).toBe(true);
      expect(t.id.startsWith(t.epic), t.id).toBe(true);
      expect(t.folder.startsWith(t.epic), t.id).toBe(true);
    }
  });

  it("les en-têtes ont des valeurs valides", () => {
    for (const t of tickets) {
      expect(t.title, t.id).not.toBe("");
      expect(TYPES as readonly string[], t.id).toContain(t.type);
      expect(PRIORITIES as readonly string[], t.id).toContain(t.priority);
      expect(STATUSES as readonly string[], t.id).toContain(t.status);
      expect(SIZES as readonly string[], t.id).toContain(t.size);
      expect(t.phase, t.id).toBeGreaterThanOrEqual(1);
      expect(t.phase, t.id).toBeLessThanOrEqual(5);
    }
  });

  it("chaque ticket contient les sections requises et au moins 2 critères d'acceptation", () => {
    for (const t of tickets) {
      for (const section of REQUIRED_SECTIONS) expect(t.body, `${t.id} : section « ${section} » absente`).toContain(section);
      const criteria = t.body.match(/^- \[[ x]\] /gm) ?? [];
      expect(criteria.length, `${t.id} : critères d'acceptation`).toBeGreaterThanOrEqual(2);
    }
  });
});

describe("spécifications : cohérence entre tickets", () => {
  const byId = new Map(tickets.map((t) => [t.id, t]));

  it("toutes les dépendances existent et aucun ticket ne dépend de lui-même", () => {
    for (const t of tickets) {
      for (const dep of t.deps) {
        expect(byId.has(dep), `${t.id} dépend d'un ticket inconnu : ${dep}`).toBe(true);
        expect(dep, `${t.id} dépend de lui-même`).not.toBe(t.id);
      }
    }
  });

  it("il n'y a aucun cycle de dépendances", () => {
    const state = new Map<string, "visiting" | "done">();
    const visit = (id: string, path: string[]) => {
      if (state.get(id) === "done") return;
      expect(state.get(id) === "visiting", `cycle : ${[...path, id].join(" → ")}`).toBe(false);
      state.set(id, "visiting");
      for (const dep of byId.get(id)?.deps ?? []) visit(dep, [...path, id]);
      state.set(id, "done");
    };
    for (const t of tickets) visit(t.id, []);
  });

  it("un ticket terminé a tous ses critères cochés", () => {
    for (const t of tickets.filter((x) => x.status === "Terminé")) {
      const unchecked = t.body.match(/^- \[ \] .*/gm) ?? [];
      expect(unchecked, `${t.id} est « Terminé » avec des critères non cochés`).toEqual([]);
    }
  });

  it("un ticket terminé ne dépend que de tickets terminés", () => {
    for (const t of tickets.filter((x) => x.status === "Terminé")) {
      for (const dep of t.deps) {
        expect(byId.get(dep)?.status, `${t.id} terminé mais ${dep} ne l'est pas`).toBe("Terminé");
      }
    }
  });
});

describe("spécifications : index à jour", () => {
  it("les tableaux de chaque epic correspondent aux tickets (lancer `npm run specs:index` sinon)", () => {
    for (const e of epics) expect(applyEpicTable(e.readme, e), e.folder).toBe(e.readme);
  });

  it("l'index global correspond aux tickets (lancer `npm run specs:index` sinon)", () => {
    const readme = readFileSync(join(SPECS_DIR, "README.md"), "utf-8");
    expect(applyIndex(readme, epics)).toBe(readme);
  });
});

describe("journal des décisions", () => {
  const decisions = readFileSync(join(SPECS_DIR, "..", "DECISIONS.md"), "utf-8");
  const ids = [...decisions.matchAll(/^### (D-\d{3})/gm)].map((m) => m[1]);

  it("contient des décisions numérotées sans doublon", () => {
    expect(ids.length).toBeGreaterThan(0);
    expect(new Set(ids).size).toBe(ids.length);
  });

  it("chaque décision renvoie à des tickets existants", () => {
    const known = new Set(tickets.map((t) => t.id));
    const referenced = [...decisions.matchAll(/\bE\d\d-T\d\d\b/g)].map((m) => m[0]);
    for (const id of referenced) expect(known.has(id), `DECISIONS.md cite un ticket inconnu : ${id}`).toBe(true);
  });
});

describe("registre des revues (préambule systématique)", () => {
  const revues = readFileSync(join(SPECS_DIR, "..", "REVUES.md"), "utf-8");
  const rows = revues
    .split(/\r?\n/)
    .filter((line) => /^\| E\d\d-T\d\d \|/.test(line))
    .map((line) => line.split("|").map((cell) => cell.trim()).filter((_, i, all) => i > 0 && i < all.length - 1));

  it("chaque ticket terminé a une ligne de revue (specs remises en question, équilibrage, validation)", () => {
    const reviewed = new Set(rows.map((r) => r[0]));
    for (const t of tickets.filter((x) => x.status === "Terminé")) {
      expect(reviewed.has(t.id), `${t.id} est « Terminé » sans ligne dans docs/REVUES.md`).toBe(true);
    }
  });

  it("chaque ligne renseigne les trois colonnes avec une validation reconnue", () => {
    for (const row of rows) {
      expect(row.length, row[0]).toBe(4);
      for (const cell of row) expect(cell.length, `cellule vide pour ${row[0]}`).toBeGreaterThan(0);
      expect(["Non requise", "À valider", "Validé"], row[0]).toContain(row[3]);
    }
  });

  it("chaque ligne renvoie à un ticket existant, sans doublon", () => {
    const known = new Set(tickets.map((t) => t.id));
    const ids = rows.map((r) => r[0]);
    for (const id of ids) expect(known.has(id), id).toBe(true);
    expect(new Set(ids).size).toBe(ids.length);
  });
});
