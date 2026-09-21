/**
 * Produit d'AUTRES POSES du même personnage (D-079), pour animer autrement qu'en déformant une image figée.
 *
 * Méthode : « image vers image ». On repart de l'illustration déjà retenue et on la regénère avec un prompt de pose
 * différente, en ne laissant le modèle réécrire qu'une partie de l'image (le « bruit », réglé par --force). Bas, le
 * personnage reste identique mais bouge peu ; haut, il bouge beaucoup mais son visage et son costume dérivent.
 * C'est tout l'enjeu : trouver le réglage qui change la pose sans changer le personnage.
 *
 * Lancer :  node art-2d/tools/poses.mjs <unité> [poses...] [--force 0.55] [--variantes 2] [--graine N]
 *   poses   noms pris dans la table POSES ci-dessous (par défaut : toutes)
 *
 * Sortie :  art-2d/essais/<unité>_<pose>_f<force>_v<n>.png — à comparer avec node art-2d/tools/planche.mjs
 */
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const ICI = path.dirname(fileURLToPath(import.meta.url));
const RACINE = path.resolve(ICI, "..", "..");
const INBOX = path.join(RACINE, "art-2d", "inbox");
const ESSAIS = path.join(RACINE, "art-2d", "essais");
const PROMPTS = path.join(RACINE, "art-2d", "prompts.txt");
const HOTE = "http://127.0.0.1:8188";
const MODELE = "flux1-schnell-fp8.safetensors";

/** Ce qui est ajouté au prompt du personnage pour obtenir chaque pose. */
const POSES = {
  attack: "caught in the middle of a powerful attack, weapon swung forward, body lunging ahead, cape and cloth flying",
  cast: "casting a spell, both arms raised high, glowing magic swirling around the hands, cloth billowing upward",
  hit: "staggering backward after being struck, head thrown back, arms flung out, off balance",
  death: "collapsing to the ground, on one knee, head down, body slumped, weapon dropped",
};

const args = process.argv.slice(2);
const opt = (n, d) => { const i = args.indexOf("--" + n); return i >= 0 ? args[i + 1] : d; };
const libres = args.filter((a, i) => !a.startsWith("--") && !args[i - 1]?.startsWith("--"));
const unite = libres[0];
if (!unite) { console.error("Usage : node art-2d/tools/poses.mjs <unité> [poses...]"); process.exit(1); }
const demandees = libres.slice(1).filter(p => p in POSES);
const poses = demandees.length ? demandees : Object.keys(POSES);
const force = Number(opt("force", 0.55));
// --neuf : on REGÉNÈRE le personnage dans la pose voulue au lieu de retoucher l illustration.
// La pose change vraiment (l image vers image, elle, n y arrive pas), mais le personnage dérive : à juger à l oeil.
const neuf = args.includes("--neuf");
const variantes = Number(opt("variantes", 1));
const graine = Number(opt("graine", 1));
const ETAPES = 10;   // en « image vers image », il faut plus d'étapes qu'en création pure : seule une fraction est réellement parcourue

const source = path.join(INBOX, `${unite}_idle.png`);
if (!fs.existsSync(source)) { console.error(`Illustration de départ absente : ${source}`); process.exit(1); }
fs.mkdirSync(ESSAIS, { recursive: true });

let style = "", description = "";
for (const brute of fs.readFileSync(PROMPTS, "utf8").split("\n")) {
  const l = brute.trim();
  if (l.startsWith("style ")) style = l.slice(6).trim();
  else if (l.startsWith("unite ")) {
    const r = l.slice(6).trim();
    const sep = r.indexOf(" ");
    if (r.slice(0, sep) === unite) description = r.slice(sep + 1).trim();
  }
}
if (!description) { console.error(`Aucun prompt pour « ${unite} » dans prompts.txt`); process.exit(1); }

const dors = ms => new Promise(r => setTimeout(r, ms));
try {
  if (!(await fetch(`${HOTE}/system_stats`, { signal: AbortSignal.timeout(3000) })).ok) throw new Error();
} catch {
  console.error("ComfyUI ne répond pas : lancez d'abord node art-2d/tools/generate-local.mjs --essai, ou une génération.");
  process.exit(1);
}

// Envoi de l'image de départ dans ComfyUI.
const corps = new FormData();
corps.append("image", new Blob([fs.readFileSync(source)], { type: "image/png" }), `${unite}_src.png`);
corps.append("overwrite", "true");
const envoiImage = await fetch(`${HOTE}/upload/image`, { method: "POST", body: corps });
if (!envoiImage.ok) { console.error("Envoi de l'image refusé :", (await envoiImage.text()).slice(0, 200)); process.exit(1); }
const { name: nomSource } = await envoiImage.json();

function workflow(prompt, g) {
  if (neuf) return {
    1: { class_type: "CheckpointLoaderSimple", inputs: { ckpt_name: MODELE } },
    2: { class_type: "CLIPTextEncode", inputs: { text: prompt, clip: ["1", 1] } },
    3: { class_type: "CLIPTextEncode", inputs: { text: "", clip: ["1", 1] } },
    4: { class_type: "EmptySD3LatentImage", inputs: { width: 1024, height: 1536, batch_size: 1 } },
    5: { class_type: "KSampler", inputs: { seed: g, steps: 4, cfg: 1.0, sampler_name: "euler", scheduler: "simple", denoise: 1.0,
                                            model: ["1", 0], positive: ["2", 0], negative: ["3", 0], latent_image: ["4", 0] } },
    7: { class_type: "VAEDecode", inputs: { samples: ["5", 0], vae: ["1", 2] } },
    8: { class_type: "SaveImage", inputs: { filename_prefix: "pose", images: ["7", 0] } },
  };
  return {
    1: { class_type: "CheckpointLoaderSimple", inputs: { ckpt_name: MODELE } },
    2: { class_type: "CLIPTextEncode", inputs: { text: prompt, clip: ["1", 1] } },
    3: { class_type: "CLIPTextEncode", inputs: { text: "", clip: ["1", 1] } },
    4: { class_type: "LoadImage", inputs: { image: nomSource, upload: "image" } },
    5: { class_type: "VAEEncode", inputs: { pixels: ["4", 0], vae: ["1", 2] } },
    6: {
      class_type: "KSampler",
      inputs: { seed: g, steps: ETAPES, cfg: 1.0, sampler_name: "euler", scheduler: "simple", denoise: force,
                model: ["1", 0], positive: ["2", 0], negative: ["3", 0], latent_image: ["5", 0] },
    },
    7: { class_type: "VAEDecode", inputs: { samples: ["6", 0], vae: ["1", 2] } },
    8: { class_type: "SaveImage", inputs: { filename_prefix: "pose", images: ["7", 0] } },
  };
}

console.log(`${unite} : ${poses.length} pose(s) × ${variantes} variante(s), force ${force}`);
let ok = 0;
for (const pose of poses) {
  for (let v = 0; v < variantes; v++) {
    process.stdout.write(`  ${pose.padEnd(7)} v${v} … `);
    const prompt = `${style}\n\n${description}\n\nThe character is ${POSES[pose]}. Same character, same costume, same colours.`;
    const debut = Date.now();
    try {
      const r = await fetch(`${HOTE}/prompt`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ prompt: workflow(prompt, graine + v), client_id: "healer-game" }),
      });
      if (!r.ok) throw new Error((await r.text()).slice(0, 200));
      const { prompt_id } = await r.json();
      const limite = Date.now() + 10 * 60 * 1000;
      let fini = false;
      while (!fini && Date.now() < limite) {
        await dors(2000);
        const h = await (await fetch(`${HOTE}/history/${prompt_id}`)).json();
        const e = h?.[prompt_id];
        if (!e) continue;
        if (e.status?.status_str === "error") throw new Error("erreur dans ComfyUI");
        const images = Object.values(e.outputs ?? {}).flatMap(o => o.images ?? []);
        if (!images.length) continue;
        const im = images[0];
        const vue = await fetch(`${HOTE}/view?filename=${encodeURIComponent(im.filename)}&subfolder=${encodeURIComponent(im.subfolder ?? "")}&type=${im.type ?? "output"}`);
        fs.writeFileSync(path.join(ESSAIS, neuf ? `${unite}_${pose}_n${v}.png` : `${unite}_${pose}_f${String(force).replace(".", "")}_v${v}.png`), Buffer.from(await vue.arrayBuffer()));
        fini = true;
      }
      console.log(fini ? `écrit en ${((Date.now() - debut) / 1000).toFixed(0)} s` : "délai dépassé");
      if (fini) ok++;
    } catch (e) {
      console.log(`échec : ${e.message}`);
    }
  }
}
console.log(`\n${ok} image(s). Comparer : node art-2d/tools/planche.mjs art-2d/essais`);
