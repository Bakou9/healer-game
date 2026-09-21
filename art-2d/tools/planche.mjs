/**
 * Assemble une planche de comparaison à partir d'un dossier d'images (D-078) : sert à juger plusieurs variantes
 * d'un coup, sur le fond sombre du jeu, plutôt que de les ouvrir une par une.
 *
 * Lancer :  node art-2d/tools/planche.mjs [dossier] [--hauteur 480] [--sortie chemin.png]
 *   dossier   art-2d/essais par défaut (ou art-2d/inbox, art-2d/out…)
 *
 * Passe par Blender (numpy) : c'est déjà l'outil d'image du projet, et il lit les PNG de toutes provenances.
 */
import { execFileSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const ICI = path.dirname(fileURLToPath(import.meta.url));
const RACINE = path.resolve(ICI, "..", "..");
const BLENDER = "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe";

const args = process.argv.slice(2);
const opt = (nom, defaut) => { const i = args.indexOf("--" + nom); return i >= 0 ? args[i + 1] : defaut; };
const dossier = path.resolve(RACINE, args.find((a, i) => !a.startsWith("--") && !args[i - 1]?.startsWith("--")) ?? "art-2d/essais");
const hauteur = Number(opt("hauteur", 480));
const sortie = path.resolve(RACINE, opt("sortie", path.join(os.tmpdir(), "planche.png")));

if (!fs.existsSync(dossier)) { console.error(`Dossier introuvable : ${dossier}`); process.exit(1); }
const images = fs.readdirSync(dossier).filter(f => f.toLowerCase().endsWith(".png") && !f.includes("_apercu") && !f.includes("grille")).sort();
if (images.length === 0) { console.error(`Aucune image dans ${dossier}`); process.exit(1); }

const script = `
import os, struct, zlib, sys
import bpy, numpy as np

dossier = r"${dossier.replace(/\\/g, "\\\\")}"
sortie = r"${sortie.replace(/\\/g, "\\\\")}"
hauteur = ${hauteur}
noms = ${JSON.stringify(images)}
FOND = np.array([26, 24, 40, 255], dtype=np.uint8)

def lire(chemin):
    img = bpy.data.images.load(chemin)
    img.colorspace_settings.name = "Non-Color"
    w, h = img.size
    a = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1]
    bpy.data.images.remove(img)
    return a

def redim(a, h):
    ah, aw = a.shape[:2]
    w = max(1, int(round(aw * h / ah)))
    pre = a.copy(); pre[..., :3] *= pre[..., 3:4]
    img = bpy.data.images.new("t", width=aw, height=ah, alpha=True)
    img.colorspace_settings.name = "Non-Color"
    img.pixels = pre[::-1].ravel().tolist()
    img.scale(w, h)
    petit = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1]
    bpy.data.images.remove(img)
    al = petit[..., 3:4]
    petit[..., :3] = np.clip(petit[..., :3] / np.maximum(al, 1e-4), 0, 1)
    return petit

vignettes = [redim(lire(os.path.join(dossier, n)), hauteur) for n in noms]
marge = 8
largeur = sum(v.shape[1] for v in vignettes) + marge * (len(vignettes) + 1)
toile = np.tile(FOND, (hauteur + marge * 2, largeur, 1))
x = marge
for v in vignettes:
    o8 = (np.clip(v, 0, 1) * 255 + 0.5).astype(np.uint8)
    zone = toile[marge:marge + hauteur, x:x + o8.shape[1]]
    al = o8[..., 3:4].astype(np.float32) / 255
    zone[..., :3] = (o8[..., :3] * al + zone[..., :3] * (1 - al)).astype(np.uint8)
    x += o8.shape[1] + marge

h, w = toile.shape[:2]
lignes = b"".join(b"\\x00" + toile[y].tobytes() for y in range(h))
def bloc(tag, data):
    c = struct.pack(">I", len(data)) + tag + data
    return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)
with open(sortie, "wb") as f:
    f.write(b"\\x89PNG\\r\\n\\x1a\\n" + bloc(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
            + bloc(b"IDAT", zlib.compress(lignes, 9)) + bloc(b"IEND", b""))
print("PLANCHE", sortie, w, "x", h, ":", ", ".join(noms))
`;

const fichier = path.join(os.tmpdir(), "planche_tmp.py");
fs.writeFileSync(fichier, script, "utf8");
try {
  const sortieTexte = execFileSync(BLENDER, ["--background", "--python", fichier], { encoding: "utf8", maxBuffer: 1 << 24 });
  const ligne = sortieTexte.split("\n").find(l => l.startsWith("PLANCHE"));
  console.log(ligne ?? sortieTexte.slice(-400));
} catch (e) {
  console.error("Échec :", (e.stdout ?? "").slice(-600) || e.message);
  process.exit(1);
}
