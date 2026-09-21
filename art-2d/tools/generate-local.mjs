/**
 * Génère les illustrations avec un modèle d'images LOCAL (D-077) : ComfyUI + FLUX.1-schnell, sur la carte graphique
 * de la machine. Gratuit, illimité, sans compte ni clé. Même rôle que generate.mjs (qui, lui, passe par une API payante).
 *
 * Lancer :  node art-2d/tools/generate-local.mjs [ids...] [--tout] [--essai] [--forcer] [--graine N]
 *   (sans id)   n'engendre que les unités qui manquent dans art-2d/inbox/
 *   --essai     vérifie que ComfyUI et le modèle sont là, sans rien générer
 *   --forcer    regénère même si l'image existe déjà
 *   --graine    point de départ de l'aléa (une graine donnée redonne les mêmes images)
 *
 * Le script démarre ComfyUI tout seul s'il ne tourne pas déjà, en tâche DÉTACHÉE (jamais suivie par la session,
 * décision D-021) : son journal va dans %TEMP%\comfyui.log.
 *
 * Les prompts viennent de art-2d/prompts.txt, comme pour generate.mjs.
 */
import { execFileSync, spawn } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const ICI = path.dirname(fileURLToPath(import.meta.url));
const RACINE = path.resolve(ICI, "..", "..");
const INBOX = path.join(RACINE, "art-2d", "inbox");
const PROMPTS = path.join(RACINE, "art-2d", "prompts.txt");

const COMFY = "C:\\IA\\ComfyUI_windows_portable";
const MODELE = "flux1-schnell-fp8.safetensors";
const HOTE = "http://127.0.0.1:8188";
const LARGEUR = 1024, HAUTEUR = 1536;
const ETAPES = 4;                     // FLUX.1-schnell est fait pour 4 étapes
const ATTENTE_MAX_MS = 15 * 60 * 1000;

const args = process.argv.slice(2);
const essai = args.includes("--essai");
const forcer = args.includes("--forcer");
const tout = args.includes("--tout");
const iGraine = args.indexOf("--graine");
const graineBase = iGraine >= 0 ? Number(args[iGraine + 1]) : 1;
const demandes = args.filter((a, i) => !a.startsWith("--") && (iGraine < 0 || i !== iGraine + 1));

// ---- prompts -------------------------------------------------------------------------------------
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

fs.mkdirSync(INBOX, { recursive: true });
const cible = u => path.join(INBOX, `${u.id}_idle.png`);
let aFaire = unites.filter(u => (demandes.length ? demandes.includes(u.id) : true));
if (!forcer) aFaire = aFaire.filter(u => !fs.existsSync(cible(u)));

// ---- vérifications -------------------------------------------------------------------------------
// On appelle le Python embarqué directement : le .bat fourni finit par « pause » et resterait bloqué en tâche détachée.
const exePython = path.join(COMFY, "python_embeded", "python.exe");
const dossierModeles = path.join(COMFY, "ComfyUI", "models", "checkpoints");
const cheminModele = path.join(dossierModeles, MODELE);
if (!fs.existsSync(COMFY)) { console.error(`ComfyUI introuvable dans ${COMFY}.`); process.exit(1); }
if (!fs.existsSync(cheminModele)) { console.error(`Modèle absent : ${cheminModele}`); process.exit(1); }
console.log(`ComfyUI : ${COMFY}\nModèle  : ${MODELE} (${(fs.statSync(cheminModele).size / 2 ** 30).toFixed(1)} Go)`);
console.log(`${aFaire.length} image(s) à produire : ${aFaire.map(u => u.id).join(", ") || "aucune"}`);
if (essai) { console.log("--essai : rien n'a été généré."); process.exit(0); }
if (aFaire.length === 0) process.exit(0);

// ---- démarrage de ComfyUI (détaché) --------------------------------------------------------------
const dors = ms => new Promise(r => setTimeout(r, ms));

async function vivant() {
  try {
    const r = await fetch(`${HOTE}/system_stats`, { signal: AbortSignal.timeout(2000) });
    return r.ok;
  } catch { return false; }
}

if (!(await vivant())) {
  console.log("Démarrage de ComfyUI (tâche détachée, journal dans %TEMP%\\comfyui.log)…");
  const journal = path.join(os.tmpdir(), "comfyui.log");
  const sortie = fs.openSync(journal, "a");
  // detached + unref : ComfyUI survit à ce script, et la session ne le suit pas (décision D-021).
  spawn(exePython, ["-s", "ComfyUI/main.py", "--windows-standalone-build"],
        { cwd: COMFY, detached: true, stdio: ["ignore", sortie, sortie], windowsHide: true }).unref();
  const limite = Date.now() + 8 * 60 * 1000;   // le premier démarrage installe et vérifie beaucoup de choses
  while (Date.now() < limite && !(await vivant())) await dors(3000);
  if (!(await vivant())) { console.error("ComfyUI n'a pas démarré : voyez %TEMP%\\comfyui.log"); process.exit(1); }
}
console.log("ComfyUI répond.");

// ---- workflow FLUX.1-schnell (format « API » de ComfyUI) -------------------------------------------
function workflow(prompt, graine) {
  return {
    1: { class_type: "CheckpointLoaderSimple", inputs: { ckpt_name: MODELE } },
    2: { class_type: "CLIPTextEncode", inputs: { text: prompt, clip: ["1", 1] } },
    3: { class_type: "CLIPTextEncode", inputs: { text: "", clip: ["1", 1] } },
    4: { class_type: "EmptySD3LatentImage", inputs: { width: LARGEUR, height: HAUTEUR, batch_size: 1 } },
    5: {
      class_type: "KSampler",
      inputs: { seed: graine, steps: ETAPES, cfg: 1.0, sampler_name: "euler", scheduler: "simple", denoise: 1.0,
                model: ["1", 0], positive: ["2", 0], negative: ["3", 0], latent_image: ["4", 0] },
    },
    6: { class_type: "VAEDecode", inputs: { samples: ["5", 0], vae: ["1", 2] } },
    7: { class_type: "SaveImage", inputs: { filename_prefix: "art2d", images: ["6", 0] } },
  };
}

async function generer(u, graine) {
  const prompt = `${style}\n\n${u.description}`;
  const envoi = await fetch(`${HOTE}/prompt`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ prompt: workflow(prompt, graine), client_id: "healer-game" }),
  });
  if (!envoi.ok) throw new Error(`refus de ComfyUI : ${(await envoi.text()).slice(0, 300)}`);
  const { prompt_id } = await envoi.json();

  const limite = Date.now() + ATTENTE_MAX_MS;
  while (Date.now() < limite) {
    await dors(2000);
    const r = await fetch(`${HOTE}/history/${prompt_id}`);
    const h = await r.json();
    const entree = h?.[prompt_id];
    if (!entree) continue;
    if (entree.status?.status_str === "error") throw new Error("erreur d'exécution dans ComfyUI");
    const images = Object.values(entree.outputs ?? {}).flatMap(o => o.images ?? []);
    if (images.length === 0) continue;
    const im = images[0];
    const vue = await fetch(`${HOTE}/view?filename=${encodeURIComponent(im.filename)}&subfolder=${encodeURIComponent(im.subfolder ?? "")}&type=${im.type ?? "output"}`);
    fs.writeFileSync(cible(u), Buffer.from(await vue.arrayBuffer()));
    return true;
  }
  throw new Error("délai dépassé");
}

let ok = 0;
for (const [i, u] of aFaire.entries()) {
  process.stdout.write(`  ${u.id.padEnd(8)} … `);
  const debut = Date.now();
  try {
    await generer(u, graineBase + i);
    console.log(`écrit en ${((Date.now() - debut) / 1000).toFixed(0)} s`);
    ok++;
  } catch (e) {
    console.log(`échec : ${e.message}`);
  }
}
console.log(`\n${ok}/${aFaire.length} image(s) générée(s).`);
if (ok > 0) console.log("Suite : blender --background --python art-2d/tools/process.py -- 512 --unity");
