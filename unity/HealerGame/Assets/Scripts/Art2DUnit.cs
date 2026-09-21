using System.Collections.Generic;
using System.Globalization;
using Healer.Combat.Presentation;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Unité affichée par une illustration 2D (D-074, option -healer-art2d) : une seule image par unité
    /// (Resources/Art2D/&lt;id&gt;_idle.png, préparée par art-2d/tools/process.py) posée sur une grille face caméra, animée
    /// PAR LE CODE à partir de l'attitude calculée par le cœur (UnitPose) : respiration, ondulation du tissu, élan d'attaque,
    /// recul, geste d'incantation, chute. Aucune règle de jeu ici, et aucune image d'animation à dessiner.
    ///
    /// La grille (quelques dizaines de triangles) permet de déformer l'illustration : le bas reste planté au sol, le haut
    /// respire et s'incline, ce qui donne de la vie sans découper le personnage en morceaux.
    /// </summary>
    public sealed class Art2DUnit : MonoBehaviour
    {
        /// <summary>Posé par GameBootstrap depuis la ligne de commande (-healer-art2d).</summary>
        public static bool Enabled;

        private const string Dossier = "Art2D/";
        /// <summary>Hauteur locale de l'illustration, en unités : la même que les modèles 3D (ModelFactory.DruidHeight), pour garder les échelles de la scène.</summary>
        private const float HauteurUnites = 3.1f;
        private const int Colonnes = 5, Rangs = 9;

        private static readonly Dictionary<string, Texture2D?> Cache = new Dictionary<string, Texture2D?>();

        public static bool Has(string unitId) => Charger(unitId) != null;

        private static Texture2D? Charger(string unitId)
        {
            if (!Cache.TryGetValue(unitId, out var tex))
            {
                tex = Resources.Load<Texture2D>(Dossier + unitId + "_idle");
                Cache[unitId] = tex;
            }
            return tex;
        }

        private Transform _plan = null!;
        private Mesh _mesh = null!;
        private Vector3[] _repos = null!, _travail = null!;
        private float _hauteur, _largeur, _phase;
        private UnitPose _pose;

        /// <summary>Construit l'unité 2D avec le même contrat qu'un modèle 3D : un UnitRig (que la scène pilote) et des Renderer à teinter.</summary>
        public static GameObject Create(string unitId)
        {
            var racine = new GameObject("Art2D_" + unitId);
            var rig = racine.AddComponent<UnitRig>();
            var vue = racine.AddComponent<Art2DUnit>();
            var tex = Charger(unitId)!;

            vue._hauteur = HauteurUnites;
            vue._largeur = HauteurUnites * tex.width / Mathf.Max(1, tex.height);
            vue._phase = Mathf.Abs(unitId.GetHashCode() % 100) * 0.1f;

            var plan = new GameObject("Illustration");
            plan.transform.SetParent(racine.transform, false);
            vue._plan = plan.transform;

            vue._mesh = Grille(vue._largeur, vue._hauteur);
            vue._repos = vue._mesh.vertices;
            vue._travail = new Vector3[vue._repos.Length];
            plan.AddComponent<MeshFilter>().sharedMesh = vue._mesh;
            var mr = plan.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex, color = Color.white };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            rig.PoseHook = vue.OnPose;
            return racine;
        }

        /// <summary>Grille dont l'origine est au MILIEU DU BAS (les pieds) : la chute tourne alors autour des pieds.</summary>
        private static Mesh Grille(float largeur, float hauteur)
        {
            var sommets = new Vector3[Colonnes * Rangs];
            var uv = new Vector2[sommets.Length];
            for (int r = 0; r < Rangs; r++)
                for (int c = 0; c < Colonnes; c++)
                {
                    float u = c / (float)(Colonnes - 1), v = r / (float)(Rangs - 1);
                    int i = r * Colonnes + c;
                    sommets[i] = new Vector3((u - 0.5f) * largeur, v * hauteur, 0f);
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

        private void OnPose(in UnitPose pose) => _pose = pose;

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            float t = Time.time;
            float chute = (float)_pose.Fall, vivant = 1f - chute;
            float elan = (float)_pose.Lunge, recul = (float)_pose.Recoil;
            float lancer = _pose.CastDurationMs > 0 ? Mathf.Clamp01((float)_pose.CastElapsedMs / 260f) : 0f;
            float lache = (float)_pose.Release;

            // Face caméra ; la chute bascule le personnage autour de ses pieds.
            _plan.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, -78f * chute);
            _plan.position = transform.position;

            float souffle = Mathf.Sin(t * 1.7f + _phase);
            // Inclinaison du haut du corps : vers le boss quand il frappe ou lance, en arrière quand il encaisse.
            float penche = (elan * 0.20f + lache * 0.12f - recul * 0.14f - lancer * 0.05f) * vivant;
            float etire = (1f + 0.016f * souffle - elan * 0.03f - recul * 0.04f + lancer * 0.02f) * vivant + chute;

            for (int i = 0; i < _repos.Length; i++)
            {
                var p = _repos[i];
                float v = p.y / _hauteur;                       // 0 aux pieds, 1 au sommet
                float prise = v * v;                            // le bas reste planté au sol
                float onde = Mathf.Sin(t * 1.5f + _phase + v * 2.4f) * 0.022f * _hauteur * prise * vivant;
                _travail[i] = new Vector3(
                    p.x * (1f - 0.02f * souffle * v) + (penche * _hauteur * prise) + onde,
                    p.y * etire,
                    p.z);
            }
            _mesh.vertices = _travail;
            _mesh.RecalculateBounds();
        }
    }
}
