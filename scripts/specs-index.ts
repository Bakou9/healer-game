// Régénère les tableaux de tickets : docs/specs/README.md et README de chaque epic.
// Usage : npm run specs:index
import { readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { SPECS_DIR, applyEpicTable, applyIndex, loadEpics } from "../src/testing/specs";

const epics = loadEpics();

for (const epic of epics) {
  const updated = applyEpicTable(epic.readme, epic);
  if (updated !== epic.readme) writeFileSync(epic.readmePath, updated, "utf-8");
}

const indexPath = join(SPECS_DIR, "README.md");
const index = readFileSync(indexPath, "utf-8");
const updatedIndex = applyIndex(index, epics);
if (updatedIndex !== index) writeFileSync(indexPath, updatedIndex, "utf-8");

const total = epics.reduce((n, e) => n + e.tickets.length, 0);
console.log(`Index des spécifications régénéré : ${epics.length} epics, ${total} tickets.`);
