using System.Collections.Generic;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Maillages procéduraux low-poly, à facettes (une normale par triangle : l'aspect « figurine » du guide
    /// docs/ART_3D.md). Toutes les formes sont centrées sur l'origine et tiennent dans un cube de côté 1.
    /// Le nombre de triangles est maîtrisé pour tenir les budgets (personnage ≤ 3 000, boss ≤ 8 000).
    /// </summary>
    public static class MeshKit
    {
        private sealed class MeshBuilder
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector3> _normals = new List<Vector3>();
            private readonly List<int> _triangles = new List<int>();

            /// <summary>Ajoute un triangle en corrigeant l'orientation pour que la face soit tournée vers l'extérieur.</summary>
            public void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                Vector3 normal = Vector3.Cross(b - a, c - a);
                Vector3 centroid = (a + b + c) / 3f;
                if (Vector3.Dot(normal, centroid) < 0f)
                {
                    (b, c) = (c, b);
                    normal = -normal;
                }
                normal.Normalize();
                int i = _vertices.Count;
                _vertices.Add(a); _vertices.Add(b); _vertices.Add(c);
                _normals.Add(normal); _normals.Add(normal); _normals.Add(normal);
                _triangles.Add(i); _triangles.Add(i + 1); _triangles.Add(i + 2);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                Tri(a, b, c);
                Tri(a, c, d);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        private static Mesh Cached(string key, System.Func<Mesh> create)
        {
            if (!Cache.TryGetValue(key, out var mesh) || mesh == null)
            {
                mesh = create();
                Cache[key] = mesh;
            }
            return mesh;
        }

        /// <summary>Sphère de diamètre 1 : segs × (2 × rings − 2) triangles.</summary>
        public static Mesh Sphere(int rings = 6, int segs = 8) => Cached($"sphere{rings}x{segs}", () =>
        {
            var b = new MeshBuilder();
            Vector3 P(int r, int s)
            {
                float phi = Mathf.PI * r / rings;
                float theta = 2f * Mathf.PI * s / segs;
                return 0.5f * new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
            }
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segs; s++)
                {
                    Vector3 a = P(r, s), bb = P(r, s + 1), c = P(r + 1, s + 1), d = P(r + 1, s);
                    if (r == 0) b.Tri(a, c, d);
                    else if (r == rings - 1) b.Tri(a, bb, c);
                    else b.Quad(a, bb, c, d);
                }
            }
            return b.ToMesh("Sphere");
        });

        /// <summary>Cylindre de diamètre 1 et de hauteur 1 (axe Y), avec bouchons.</summary>
        public static Mesh Cylinder(int segs = 8) => Cached($"cyl{segs}", () =>
        {
            var b = new MeshBuilder();
            for (int s = 0; s < segs; s++)
            {
                float t0 = 2f * Mathf.PI * s / segs, t1 = 2f * Mathf.PI * (s + 1) / segs;
                var b0 = new Vector3(0.5f * Mathf.Cos(t0), -0.5f, 0.5f * Mathf.Sin(t0));
                var b1 = new Vector3(0.5f * Mathf.Cos(t1), -0.5f, 0.5f * Mathf.Sin(t1));
                var t0v = b0 + Vector3.up; var t1v = b1 + Vector3.up;
                b.Quad(b0, b1, t1v, t0v);
                b.Tri(new Vector3(0, 0.5f, 0), t0v, t1v);
                b.Tri(new Vector3(0, -0.5f, 0), b0, b1);
            }
            return b.ToMesh("Cylinder");
        });

        /// <summary>Cône de diamètre de base 1 et de hauteur 1 (pointe en +Y).</summary>
        public static Mesh Cone(int segs = 8) => Cached($"cone{segs}", () =>
        {
            var b = new MeshBuilder();
            var apex = new Vector3(0, 0.5f, 0);
            for (int s = 0; s < segs; s++)
            {
                float t0 = 2f * Mathf.PI * s / segs, t1 = 2f * Mathf.PI * (s + 1) / segs;
                var b0 = new Vector3(0.5f * Mathf.Cos(t0), -0.5f, 0.5f * Mathf.Sin(t0));
                var b1 = new Vector3(0.5f * Mathf.Cos(t1), -0.5f, 0.5f * Mathf.Sin(t1));
                b.Tri(apex, b0, b1);
                b.Tri(new Vector3(0, -0.5f, 0), b0, b1);
            }
            return b.ToMesh("Cone");
        });

        /// <summary>Cube de côté 1.</summary>
        public static Mesh Cube() => Cached("cube", () =>
        {
            var b = new MeshBuilder();
            Vector3 v(float x, float y, float z) => new Vector3(x, y, z) * 0.5f;
            b.Quad(v(-1, -1, 1), v(1, -1, 1), v(1, 1, 1), v(-1, 1, 1));
            b.Quad(v(1, -1, -1), v(-1, -1, -1), v(-1, 1, -1), v(1, 1, -1));
            b.Quad(v(-1, -1, -1), v(-1, -1, 1), v(-1, 1, 1), v(-1, 1, -1));
            b.Quad(v(1, -1, 1), v(1, -1, -1), v(1, 1, -1), v(1, 1, 1));
            b.Quad(v(-1, 1, 1), v(1, 1, 1), v(1, 1, -1), v(-1, 1, -1));
            b.Quad(v(-1, -1, -1), v(1, -1, -1), v(1, -1, 1), v(-1, -1, 1));
            return b.ToMesh("Cube");
        });

        // ---- Formes « dark fantasy » (D-058) : facettes marquées, silhouettes anguleuses ---------------------

        /// <summary>Tronc à facettes : base de rayon 0,5, sommet de rayon 0,5 × topScale (plus large en haut si &gt; 1), hauteur 1. rotDeg tourne les arêtes.</summary>
        public static Mesh Prism(int sides = 6, float topScale = 0.7f, float rotDeg = 0f) => Cached($"prism{sides}_{topScale}_{rotDeg}", () =>
        {
            var b = new MeshBuilder();
            float rot = rotDeg * Mathf.Deg2Rad;
            Vector3 Bot(int s) { float a = 2f * Mathf.PI * s / sides + rot; return new Vector3(0.5f * Mathf.Cos(a), -0.5f, 0.5f * Mathf.Sin(a)); }
            Vector3 Top(int s) { float a = 2f * Mathf.PI * s / sides + rot; return new Vector3(0.5f * topScale * Mathf.Cos(a), 0.5f, 0.5f * topScale * Mathf.Sin(a)); }
            for (int s = 0; s < sides; s++)
            {
                b.Quad(Bot(s), Bot(s + 1), Top(s + 1), Top(s));
                b.Tri(new Vector3(0, 0.5f, 0), Top(s), Top(s + 1));
                b.Tri(new Vector3(0, -0.5f, 0), Bot(s), Bot(s + 1));
            }
            return b.ToMesh("Prism");
        });

        /// <summary>Cristal : double pyramide de hauteur 1 ; waist (0 à 1) place la ceinture, de rayon 0,5.</summary>
        public static Mesh Crystal(int sides = 6, float waist = 0.5f) => Cached($"crystal{sides}_{waist}", () =>
        {
            var b = new MeshBuilder();
            float y = waist - 0.5f;
            Vector3 R(int s) { float a = 2f * Mathf.PI * s / sides; return new Vector3(0.5f * Mathf.Cos(a), y, 0.5f * Mathf.Sin(a)); }
            for (int s = 0; s < sides; s++)
            {
                b.Tri(new Vector3(0, 0.5f, 0), R(s), R(s + 1));
                b.Tri(new Vector3(0, -0.5f, 0), R(s), R(s + 1));
            }
            return b.ToMesh("Crystal");
        });

        /// <summary>Rocher taillé : sphère à facettes dont chaque sommet est déplacé de façon déterministe (roughness ≈ 0,15 à 0,35).</summary>
        public static Mesh Rock(int seed = 1, float roughness = 0.25f, int rings = 4, int segs = 7) => Cached($"rock{seed}_{roughness}_{rings}_{segs}", () =>
        {
            var b = new MeshBuilder();
            float Hash(int r, int s) => Mathf.Repeat(Mathf.Sin(seed * 12.9898f + r * 78.233f + (s % segs) * 37.719f) * 43758.5453f, 1f);
            Vector3 P(int r, int s)
            {
                float phi = Mathf.PI * r / rings;
                float theta = 2f * Mathf.PI * s / segs;
                float k = r == 0 || r == rings ? 1f : 1f + roughness * (Hash(r, s) - 0.5f) * 2f;
                return 0.5f * k * new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segs; s++)
                {
                    Vector3 a = P(r, s), bb = P(r, s + 1), c = P(r + 1, s + 1), d = P(r + 1, s);
                    if (r == 0) b.Tri(a, c, d);
                    else if (r == rings - 1) b.Tri(a, bb, c);
                    else b.Quad(a, bb, c, d);
                }
            return b.ToMesh("Rock");
        });

        /// <summary>Toit à deux pans (plaque d'armure, pointe d'épaulière) : base 1×1, arête en haut le long de X.</summary>
        public static Mesh Wedge() => Cached("wedge", () =>
        {
            var b = new MeshBuilder();
            var a = new Vector3(-0.5f, -0.5f, -0.5f); var bb = new Vector3(0.5f, -0.5f, -0.5f);
            var c = new Vector3(0.5f, -0.5f, 0.5f); var d = new Vector3(-0.5f, -0.5f, 0.5f);
            var e = new Vector3(-0.5f, 0.5f, 0f); var f = new Vector3(0.5f, 0.5f, 0f);
            b.Quad(a, bb, c, d);
            b.Quad(a, bb, f, e);
            b.Quad(d, c, f, e);
            b.Tri(a, e, d);
            b.Tri(bb, f, c);
            return b.ToMesh("Wedge");
        });

        /// <summary>Lame plate en losange (épée, plume, épine) : largeur 1 (X), hauteur 1 (Y, pointe en haut), épaisseur 0,32 (Z).</summary>
        public static Mesh Blade() => Cached("blade", () =>
        {
            var b = new MeshBuilder();
            var tip = new Vector3(0, 0.5f, 0); var bottom = new Vector3(0, -0.5f, 0);
            var l = new Vector3(-0.5f, 0.05f, 0); var r = new Vector3(0.5f, 0.05f, 0);
            var front = new Vector3(0, 0.05f, 0.16f); var back = new Vector3(0, 0.05f, -0.16f);
            b.Tri(tip, l, front); b.Tri(tip, front, r); b.Tri(bottom, front, l); b.Tri(bottom, r, front);
            b.Tri(tip, back, l); b.Tri(tip, r, back); b.Tri(bottom, l, back); b.Tri(bottom, back, r);
            return b.ToMesh("Blade");
        });

        public static int TriangleCount(Mesh mesh) => CountIndices(mesh) / 3;

        private static int CountIndices(Mesh mesh) // fonctionne aussi pour les maillages importés, non lisibles dans un exécutable
        {
            long n = 0;
            for (int i = 0; i < mesh.subMeshCount; i++) n += mesh.GetIndexCount(i);
            return (int)n;
        }
    }
}
