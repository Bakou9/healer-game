import { existsSync, readdirSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

/**
 * Chargement des spécifications (docs/specs). Chaque epic est un dossier ;
 * chaque ticket est un fichier Markdown dont l'en-tête (entre deux lignes
 * `---`) contient des lignes `clé: valeur`. Ce module sert au test de
 * cohérence des specs et à la génération de l'index (`npm run specs:index`).
 */
export const SPECS_DIR = join(dirname(fileURLToPath(import.meta.url)), "..", "..", "docs", "specs");
export const EPICS_DIR = join(SPECS_DIR, "epics");

export const STATUSES = ["À faire", "En cours", "Terminé", "Abandonné"] as const;
export const PRIORITIES = ["P0", "P1", "P2", "P3"] as const;
export const TYPES = ["Feature", "Tech", "Test", "Design", "Bug"] as const;
export const SIZES = ["S", "M", "L", "XL"] as const;

export const TABLE_START = "<!-- TICKETS:START -->";
export const TABLE_END = "<!-- TICKETS:END -->";

export interface Ticket {
  id: string;
  epic: string;
  title: string;
  type: string;
  priority: string;
  phase: number;
  status: string;
  size: string;
  deps: string[];
  file: string;
  folder: string;
  body: string;
}

export interface Epic {
  id: string;
  title: string;
  folder: string;
  readmePath: string;
  readme: string;
  tickets: Ticket[];
}

export function parseHeader(text: string): Record<string, string> {
  const match = text.match(/^---\r?\n([\s\S]*?)\r?\n---/);
  const header: Record<string, string> = {};
  if (!match) return header;
  for (const line of match[1].split(/\r?\n/)) {
    const idx = line.indexOf(":");
    if (idx > 0) header[line.slice(0, idx).trim()] = line.slice(idx + 1).trim();
  }
  return header;
}

export function loadEpics(): Epic[] {
  if (!existsSync(EPICS_DIR)) return [];
  return readdirSync(EPICS_DIR, { withFileTypes: true })
    .filter((d) => d.isDirectory())
    .map((d) => d.name)
    .sort()
    .map((folder) => {
      const readmePath = join(EPICS_DIR, folder, "README.md");
      const readme = existsSync(readmePath) ? readFileSync(readmePath, "utf-8") : "";
      const head = parseHeader(readme);
      const tickets = readdirSync(join(EPICS_DIR, folder))
        .filter((f) => /^E\d+-T\d+.*\.md$/.test(f))
        .sort()
        .map((file): Ticket => {
          const body = readFileSync(join(EPICS_DIR, folder, file), "utf-8");
          const h = parseHeader(body);
          const deps = (h["dépendances"] ?? "").split(",").map((s) => s.trim()).filter((s) => s && s !== "aucune");
          return {
            id: h.id ?? "",
            epic: h.epic ?? "",
            title: h.titre ?? "",
            type: h.type ?? "",
            priority: h["priorité"] ?? "",
            phase: Number(h.phase),
            status: h.statut ?? "",
            size: h.taille ?? "",
            deps,
            file,
            folder,
            body,
          };
        });
      return { id: head.id ?? "", title: head.titre ?? "", folder, readmePath, readme, tickets };
    });
}

const link = (t: Ticket) => `[${t.id}](epics/${t.folder}/${t.file})`;

/** Tableau des tickets d'un epic (inséré entre les marqueurs du README de l'epic). */
export function renderEpicTable(epic: Epic): string {
  const rows = epic.tickets.map(
    (t) =>
      `| [${t.id}](${t.file}) | ${t.title} | ${t.type} | ${t.priority} | ${t.phase} | ${t.status} | ${t.deps.join(", ") || "—"} |`,
  );
  return [
    "| Ticket | Titre | Type | Priorité | Phase | Statut | Dépend de |",
    "|---|---|---|---|---|---|---|",
    ...rows,
  ].join("\n");
}

/** Remplace le contenu entre les marqueurs du README d'un epic. */
export function applyEpicTable(readme: string, epic: Epic): string {
  const start = readme.indexOf(TABLE_START);
  const end = readme.indexOf(TABLE_END);
  if (start === -1 || end === -1) return readme;
  return `${readme.slice(0, start + TABLE_START.length)}\n${renderEpicTable(epic)}\n${readme.slice(end)}`;
}

function progress(tickets: Ticket[]): string {
  const done = tickets.filter((t) => t.status === "Terminé").length;
  return `${done}/${tickets.length}`;
}

/** Index global des specs (docs/specs/README.md, partie générée). */
export function renderIndexTables(epics: Epic[]): string {
  const summary = epics.map(
    (e) => `| [${e.id}](epics/${e.folder}/README.md) | ${e.title} | ${e.tickets.length} | ${progress(e.tickets)} |`,
  );
  const all = epics.flatMap((e) => e.tickets);
  const detail = all.map(
    (t) => `| ${link(t)} | ${t.title} | ${t.priority} | ${t.phase} | ${t.status} |`,
  );
  return [
    "### Epics",
    "",
    "| Epic | Titre | Tickets | Terminés |",
    "|---|---|---|---|",
    ...summary,
    "",
    `Total : ${all.length} tickets, ${progress(all)} terminés.`,
    "",
    "### Tous les tickets",
    "",
    "| Ticket | Titre | Priorité | Phase | Statut |",
    "|---|---|---|---|---|",
    ...detail,
  ].join("\n");
}

export function applyIndex(readme: string, epics: Epic[]): string {
  const start = readme.indexOf(TABLE_START);
  const end = readme.indexOf(TABLE_END);
  if (start === -1 || end === -1) return readme;
  return `${readme.slice(0, start + TABLE_START.length)}\n${renderIndexTables(epics)}\n${readme.slice(end)}`;
}
