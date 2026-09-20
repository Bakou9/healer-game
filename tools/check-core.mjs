// Vérifie le cœur C# (core/) : tests unitaires, conformité aux références golden, équilibrage, données, architecture.
// Ne ment jamais : s'il y a des tests C# mais pas de SDK .NET, la vérification ÉCHOUE au lieu de « passer ».
import { spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(fileURLToPath(new URL(".", import.meta.url)), "..");
const testProject = join(root, "core", "Healer.Combat.Tests", "Healer.Combat.Tests.csproj");

if (!existsSync(testProject)) {
  console.log("[check-core] Aucun projet de tests C# pour l'instant : rien à vérifier.");
  process.exit(0);
}

// `dotnet` peut ne pas être dans le PATH d'un terminal ouvert avant l'installation du SDK.
const candidates = ["dotnet", join(process.env.ProgramFiles ?? "C:\\Program Files", "dotnet", "dotnet.exe")];
const dotnet = candidates.find((c) => spawnSync(c, ["--version"], { encoding: "utf-8" }).status === 0);
if (!dotnet) {
  console.error("[check-core] ÉCHEC : des tests C# existent mais le SDK .NET est introuvable. Installer .NET 8 (voir docs/UNITY_SETUP.md).");
  process.exit(1);
}

const run = spawnSync(dotnet, ["test", testProject, "--nologo"], { stdio: "inherit" });
process.exit(run.status ?? 1);
