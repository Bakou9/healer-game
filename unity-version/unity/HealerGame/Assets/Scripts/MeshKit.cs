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

        public static int TriangleCount(Mesh mesh) => mesh.triangles.Length / 3;
    }
}
