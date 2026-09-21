using System.Collections.Generic;
using System.Globalization;
using Healer.Combat.Presentation;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Unité affichée par une illustration 2D articulée (D-074, D-075 ; option -healer-art2d). L'illustration est découpée en
    /// quelques parties (corps, tête, bras) par art-2d/tools/cutout.py ; chacune pivote autour de son articulation, comme un
    /// pantin de papier. Les gestes viennent uniquement de l'attitude calculée par le cœur (UnitPose) : rien n'est dessiné
    /// image par image, et aucune règle de jeu n'est ici.
    ///
    /// Sans fichier de découpe, l'illustration entière sert de pièce unique : une unité peut donc arriver avant d'être découpée.
    /// </summary>
    public sealed class Art2DUnit : MonoBehaviour
    {
        /// <summary>Posé par GameBootstrap depuis la ligne de commande (-healer-art2d).</summary>
        public static bool Enabled;

        private const string Dossier = "Art2D/";
        /// <summary>Hauteur locale de l'illustration, en unités : la même que les modèles 3D, pour garder les échelles de la scène.</summary>
        private const float HauteurUnites = 3.1f;
        private const int Colonnes = 5, Rangs = 7;

        private static readonly Dictionary<string, Texture2D?> Cache = new Dictionary<string, Texture2D?>();

        public static bool Has(string unitId) => Texture(unitId + "_idle") != null;

        private static Texture2D? Texture(string nom)
        {
            if (!Cache.TryGetValue(nom, out var tex))
            {
                tex = Resources.Load<Texture2D>(Dossier + nom);
                Cache[nom] = tex;
            }
            return tex;
        }

        /// <summary>Une partie articulée : son pivot (qui tourne) et son image (posée en décalé sous ce pivot).</summary>
        private sealed class Partie
        {
            public string Nom = "";
            public Transform Pivot = null!;
            public Mesh? Grille;            // seulement pour le corps : ondulation du tissu
            /// <summary>Degrés de rotation par canal : souffle, coup, recul, lancer, lâcher, chute (lus dans le fichier de découpe).</summary>
            public float[] Anim = new float[6];
            public Vector3[]? Repos;
            public Vector3[]? Travail;
            public float Hauteur;
        }

        private readonly List<Partie> _parties = new List<Partie>();
        private readonly Dictionary<string, float[]> _animations = new Dictionary<string, float[]>();
        private Transform _plateau = null!;   // porte l'orientation face caméra : tout ce qui est dessous est du pur 2D
        private float _phase;
        private UnitPose _pose;

        /// <summary>Construit l'unité avec le même contrat qu'un modèle 3D : un UnitRig (que la scène pilote) et des Renderer à teinter.</summary>
        public static GameObject Create(string unitId)
        {
            var racine = new GameObject("Art2D_" + unitId);
            var rig = racine.AddComponent<UnitRig>();
            var vue = racine.AddComponent<Art2DUnit>();
            vue._phase = Mathf.Abs(unitId.GetHashCode() % 100) * 0.1f;

            var plateau = new GameObject("Plateau");
            plateau.transform.SetParent(racine.transform, false);
            vue._plateau = plateau.transform;

            var entiere = Texture(unitId + "_idle")!;
            float largeurTotale = HauteurUnites * entiere.width / Mathf.Max(1, entiere.height);
            var decoupe = Resources.Load<TextAsset>(Dossier + unitId + "_rig");
            if (decoupe != null) vue.Articuler(unitId, decoupe.text, largeurTotale);
            else vue.PieceUnique(entiere, largeurTotale);

            rig.PoseHook = vue.OnPose;
            return racine;
        }

        // ---- Construction ---------------------------------------------------------------------------------------

        private void PieceUnique(Texture2D tex, float largeur)
        {
            _parties.Add(Ajouter("body", null, 0, new Vector2(0.5f, 0f), Vector2.zero, new Rect(0f, 0f, 1f, 1f), tex, largeur, true));
        }

        /// <summary>Lit le fichier produit par cutout.py : « partie nom parent ordre x y largeur hauteur pivotX pivotY » (fractions, origine en bas à gauche).</summary>
        private void Articuler(string unitId, string texte, float largeur)
        {
            var pivots = new Dictionary<string, Partie>();
            var ancres = new Dictionary<string, Vector2>();
            var lignes = texte.Split('\n');
            // Première passe : les animations, car dans le fichier chaque ligne « anim » SUIT la partie qu'elle décrit.
            foreach (var brute in lignes)
            {
                var t = brute.Trim().Split(' ');
                if (t.Length != 8 || t[0] != "anim") continue;
                var v = new float[6];
                for (int k = 0; k < 6; k++) v[k] = float.Parse(t[k + 2], CultureInfo.InvariantCulture);
                _animations[t[1]] = v;
            }
            foreach (var brute in lignes)
            {
                var t = brute.Trim().Split(' ');
                if (t.Length != 10 || t[0] != "partie") continue;
                string nom = t[1], parent = t[2];
                int ordre = int.Parse(t[3], CultureInfo.InvariantCulture);
                float F(int i) => float.Parse(t[i], CultureInfo.InvariantCulture);
                var zone = new Rect(F(4), F(5), F(6), F(7));
                var pivot = new Vector2(F(8), F(9));
                var tex = Texture("parts/" + unitId + "_" + nom);
                if (tex == null) continue;
                var parente = parent != "-" && pivots.TryGetValue(parent, out var pp) ? pp : null;
                var pivotParent = parent != "-" && ancres.TryGetValue(parent, out var ap) ? ap : Vector2.zero;
                var partie = Ajouter(nom, parente, ordre, pivot, pivotParent, zone, tex, largeur, nom == "body");
                if (_animations.TryGetValue(nom, out var anim)) partie.Anim = anim;
                pivots[nom] = partie;
                ancres[nom] = pivot;
                _parties.Add(partie);
            }
        }

        /// <summary>
        /// Place une partie. Tout est exprimé en fractions de l'illustration entière (origine en bas à gauche), converties ici
        /// en unités : x depuis le milieu de l'illustration, y depuis le sol. Le pivot porte la rotation, l'image est posée
        /// en décalé sous lui, et un petit écart en z met les parties dans le bon ordre d'affichage.
        /// </summary>
        private Partie Ajouter(string nom, Partie? parent, int ordre, Vector2 pivotFrac, Vector2 pivotParentFrac, Rect zone, Texture2D tex, float largeur, bool deformable)
        {
            float X(float frac) => (frac - 0.5f) * largeur;
            float Y(float frac) => frac * HauteurUnites;

            var pivot = new GameObject(nom);
            pivot.transform.SetParent(parent != null ? parent.Pivot : _plateau, false);
            pivot.transform.localPosition = parent != null
                ? new Vector3(X(pivotFrac.x) - X(pivotParentFrac.x), Y(pivotFrac.y) - Y(pivotParentFrac.y), 0f)
                : new Vector3(X(pivotFrac.x), Y(pivotFrac.y), 0f);

            float w = zone.width * largeur, h = zone.height * HauteurUnites;
            var image = new GameObject("Image");
            image.transform.SetParent(pivot.transform, false);
            image.transform.localPosition = new Vector3(
                X(zone.x + zone.width / 2f) - X(pivotFrac.x),
                Y(zone.y + zone.height / 2f) - Y(pivotFrac.y),
                -0.02f * ordre);

            var mesh = deformable ? Grille(w, h) : Quad(w, h);
            var partie = new Partie { Nom = nom, Pivot = pivot.transform, Hauteur = h };
            if (deformable)
            {
                partie.Grille = mesh;
                partie.Repos = mesh.vertices;
                partie.Travail = new Vector3[partie.Repos.Length];
            }
            image.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = image.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex, color = Color.white };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return partie;
        }

        private static Mesh Quad(float w, float h)
        {
            var mesh = new Mesh { name = "Art2DQuad" };
            mesh.vertices = new[] { new Vector3(-w / 2, -h / 2, 0), new Vector3(w / 2, -h / 2, 0), new Vector3(-w / 2, h / 2, 0), new Vector3(w / 2, h / 2, 0) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateNormals();
            return mesh;
        }

        /// <summary>Grille du corps : permet de faire onduler le bas de la robe sans toucher au haut (où sont la tête et les bras).</summary>
        private static Mesh Grille(float w, float h)
        {
            var sommets = new Vector3[Colonnes * Rangs];
            var uv = new Vector2[sommets.Length];
            for (int r = 0; r < Rangs; r++)
                for (int c = 0; c < Colonnes; c++)
                {
                    float u = c / (float)(Colonnes - 1), v = r / (float)(Rangs - 1);
                    int i = r * Colonnes + c;
                    sommets[i] = new Vector3((u - 0.5f) * w, (v - 0.5f) * h, 0f);
                    uv[i] = new Vector2(u, v);
                }
            var tris = new List<int>();
            for (int r = 0; r < Rangs - 1; r++)
                for (int c = 0; c < Colonnes - 1; c++)
                {
                    int i = r * Colonnes + c;
                    tris.AddRange(new[] { i, i + Colonnes, i + 1, i + 1, i + Colonnes, i + Colonnes + 1 });
                }
            var mesh = new Mesh { name = "Art2DGrid" };
            mesh.vertices = sommets;
            mesh.uv = uv;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }

        // ---- Animation ------------------------------------------------------------------------------------------

        private void OnPose(in UnitPose pose) => _pose = pose;

        private Partie? Trouver(string nom) => _parties.Find(p => p.Nom == nom);

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            float t = Time.time;
            float chute = (float)_pose.Fall, vivant = 1f - chute;
            float elan = (float)_pose.Lunge, recul = (float)_pose.Recoil, lache = (float)_pose.Release;
            float lancer = _pose.CastDurationMs > 0 ? Mathf.Clamp01((float)_pose.CastElapsedMs / 260f) : 0f;
            float souffle = Mathf.Sin(t * 1.7f + _phase);

            // Le plateau porte l'orientation face caméra et la bascule de la chute (autour des pieds).
            _plateau.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, -74f * chute);
            _plateau.localPosition = Vector3.zero;

            // Le coup porté est une SÉQUENCE, pas un simple aller-retour : on arme en arrière (valeur négative),
            // on frappe vers l'avant (positive), puis on revient. C'est ce qui fait lire une attaque plutôt qu'un tremblement.
            float u = (float)_pose.LungeProgress;
            float coup = u > 0f ? Courbe(u) : 0f;

            // Le plateau porte l'orientation face caméra et la bascule de la chute (autour des pieds).
            _plateau.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, -74f * chute);
            _plateau.localPosition = Vector3.zero;

            // Sans fichier d'animation (illustration non découpée), on garde un mouvement d'ensemble simple.
            bool articule = _animations.Count > 0;
            float penche = (-elan * 13f - lache * 8f + recul * 11f + lancer * 3f) * vivant;

            foreach (var p in _parties)
            {
                float angle;
                if (articule)
                {
                    var a = p.Anim;
                    // souffle, coup, recul, lancer, lâcher, chute : chaque partie a son amplitude, lue dans les données.
                    angle = (a[0] * souffle + a[1] * coup + a[2] * recul + a[3] * lancer + a[4] * lache) * vivant + a[5] * chute;
                }
                else angle = p.Nom == "body" ? penche : souffle * 1.5f * vivant;

                p.Pivot.localRotation = Quaternion.Euler(0f, 0f, angle);
                if (p.Nom == "body")
                    p.Pivot.localScale = new Vector3(1f - 0.008f * souffle, 1f + 0.014f * souffle - recul * 0.03f, 1f);
                if (p.Grille != null && p.Repos != null && p.Travail != null) Onduler(p, t, vivant);
            }
        }

        /// <summary>Courbe du coup porté : 0 au départ, -1 quand l'arme est armée en arrière (à 30 %), +1 à la frappe (55 %), 0 au retour.</summary>
        private static float Courbe(float u)
        {
            if (u < 0.30f) return -Lisse(u / 0.30f);
            if (u < 0.55f) return Mathf.Lerp(-1f, 1f, Lisse((u - 0.30f) / 0.25f));
            return Mathf.Lerp(1f, 0f, Lisse((u - 0.55f) / 0.45f));
        }

        private static float Lisse(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        /// <summary>Ondulation du tissu : forte en bas de la robe, nulle en haut (là où sont accrochées la tête et les bras).</summary>
        private void Onduler(Partie p, float t, float vivant)
        {
            float demi = p.Hauteur / 2f;
            for (int i = 0; i < p.Repos!.Length; i++)
            {
                var s = p.Repos[i];
                float v = 1f - (s.y + demi) / p.Hauteur;                 // 0 en haut, 1 en bas
                float prise = v * v;
                p.Travail![i] = new Vector3(s.x + Mathf.Sin(t * 1.4f + _phase + v * 3.1f) * 0.035f * prise * vivant, s.y, s.z);
            }
            p.Grille!.vertices = p.Travail;
            p.Grille.RecalculateBounds();
        }
    }
}
