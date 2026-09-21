/**
 * Génère les illustrations des unités (D-076), pour que la chaîne 2D tourne sans intervention manuelle.
 *
 * Lancer :  node art-2d/tools/generate.mjs [ids...] [--tout] [--essai] [--forcer]
 *   (sans id)   n'engendre que les unités qui manquent dans art-2d/inbox/
 *   ids         limite à ces unités (ex. « tank dps1 »)
 *   --essai     n'appelle rien : montre ce qui serait demandé et ce que ça coûterait (à lancer en premier)
 *   --forcer    regénère même si l'image existe déjà
 *
 * La CLÉ D'API n'est jamais écrite dans le dépôt ni vue par l'assistant. Deux façons de la fournir :
 *   - un fichier « .env » à la racine (recommandé : à écrire une fois) contenant  OPENAI_API_KEY=sk-...
 *     Le script le charge tout seul, et REFUSE de s'exécuter si ce fichier n'est pas ignoré par git.
 *   - ou une variable d'environnement posée dans le terminal :
 *       PowerShell :  $env:OPENAI_API_KEY = "sk-..."
 *       Git Bash   :  export OPENAI_API_KEY="sk-..."
 *
 * Chaque appel est PAYANT (quelques centimes par image). Le script ne s'exécute donc jamais tout seul :
 * il faut une clé présente, et il affiche le détail avant d'appeler.
 *
 * Les prompts viennent de art-2d/prompts.txt (source unique, aussi reprise au générique du jeu).
 */
import { execFileSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const ICI = path.dirname(fileURLToPath(import.meta.url));
const RACINE = path.resolve(ICI, "..", "..");

/**
 * Charge un fichier « .env » local. Garde-fou : si ce fichier n'est pas ignoré par git, on s'arrête —
 * mieux vaut refuser de travailler que risquer de committer une clé d'API.
 */
function chargerEnv() {
  const fichier = path.join(RACINE, ".env");
  if (!fs.existsSync(fichier)) return;
  try {
    execFileSync("git", ["check-ignore", "-q", ".env"], { cwd: RACINE, stdio: "ignore" });
  } catch {
    console.error("DANGER : le fichier .env n'est pas ignoré par git — ajoutez « .env » au .gitignore avant de continuer.");
    process.exit(1);
  }
  for (const brute of fs.readFileSync(fichier, "utf8").split("\n")) {
    const ligne = brute.trim();
    if (!ligne || ligne.startsWith("#")) continue;
    const sep = ligne.indexOf("=");
    if (sep < 1) continue;
    const nom = ligne.slice(0, sep).trim();
    const valeur = ligne.slice(sep + 1).trim().replace(/^["']|["']$/g, "");
    if (valeur && !process.env[nom]) process.env[nom] = valeur;
  }
}

chargerEnv();
const INBOX = path.join(RACINE, "art-2d", "inbox");
const PROMPTS = path.join(RACINE, "art-2d", "prompts.txt");

const MODELE = "gpt-image-1";
const TAILLE = "1024x1536";          // portrait : le personnage est debout
const QUALITE = "high";

const args = process.argv.slice(2);
const essai = args.includes("--essai");
const forcer = args.includes("--forcer");
const tout = args.includes("--tout");
const demandes = args.filter(a => !a.startsWith("--"));

// ---- lecture des prompts -------------------------------------------------------------------------
let style = "";
const unites = [];
for (const brute of fs.readFileSync(PROMPTS, "utf8").split("\n")) {
  const ligne = brute.trim();
  if (!ligne || ligne.startsWith("#")) continue;
  if (ligne.startsWith("style ")) style = ligne.slice(6).trim();
  else if (ligne.startsWith("unite ")) {
    const reste = ligne.slice(6).trim();
    const sep = reste.indexOf(" ");
    unites.push({ id: reste.slice(0, sep), description: reste.slice(sep + 1).trim() });
  }
}
if (!style || unites.length === 0) {
  console.error("prompts.txt illisible : il faut une ligne « style » et au moins une ligne « unite ».");
  process.exit(1);
}

fs.mkdirSync(INBOX, { recursive: true });
const cible = u => path.join(INBOX, `${u.id}_idle.png`);
let aFaire = unites.filter(u => (demandes.length ? demandes.includes(u.id) : true));
if (!forcer) aFaire = aFaire.filter(u => !fs.existsSync(cible(u)));
if (!demandes.length && !tout && !forcer && aFaire.length === 0) {
  console.log("Rien à faire : toutes les illustrations sont déjà dans art-2d/inbox/ (utilisez --forcer pour refaire).");
  process.exit(0);
}

console.log(`Modèle ${MODELE}, ${TAILLE}, qualité ${QUALITE} — ${aFaire.length} image(s) :`);
for (const u of aFaire) console.log(`  ${u.id.padEnd(8)} ${u.description.slice(0, 90)}…`);

if (essai) {
  console.log("\n--essai : aucun appel n'a été fait. Relancez sans --essai pour générer (appels payants).");
  process.exit(0);
}

const cle = process.env.OPENAI_API_KEY;
if (!cle) {
  console.error("\nOPENAI_API_KEY absente : posez votre clé dans le terminal avant de relancer (voir l'en-tête de ce fichier).");
  process.exit(1);
}

// ---- génération ------------------------------------------------------------------------------------
let ok = 0;
for (const u of aFaire) {
  const prompt = `${style}\n\n${u.description}`;
  process.stdout.write(`  ${u.id} … `);
  try {
    const reponse = await fetch("https://api.openai.com/v1/images/generations", {
      method: "POST",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${cle}` },
      body: JSON.stringify({ model: MODELE, prompt, size: TAILLE, quality: QUALITE, n: 1 }),
    });
    if (!reponse.ok) {
      const texte = await reponse.text();
      console.log(`échec HTTP ${reponse.status} : ${texte.slice(0, 200)}`);
      continue;
    }
    const donnees = await reponse.json();
    const b64 = donnees?.data?.[0]?.b64_json;
    if (!b64) { console.log("réponse sans image"); continue; }
    fs.writeFileSync(cible(u), Buffer.from(b64, "base64"));
    console.log(`écrit ${path.relative(RACINE, cible(u))}`);
    ok++;
  } catch (e) {
    console.log(`erreur : ${e.message}`);
  }
}
console.log(`\n${ok}/${aFaire.length} image(s) générée(s).`);
if (ok > 0) console.log("Suite : blender --background --python art-2d/tools/process.py -- 512 --unity");
