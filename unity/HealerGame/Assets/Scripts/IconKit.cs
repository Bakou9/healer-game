using System;
using System.Collections.Generic;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Icônes de sorts dessinées par le code (aucun fichier d'image) : formes remplies avec anti-crénelage,
    /// empilées sur un médaillon sombre au liseré de la couleur du sort. Reproductible et versionné avec le code.
    /// Les mêmes icônes servent aux boutons de combat et à la fiche de sorts du menu de pause.
    /// </summary>
    public static class IconKit
    {
        private const int Size = 128;
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        private readonly struct Layer
        {
            public readonly Func<Vector2, bool> Inside;
            public readonly Color Color;
            public Layer(Func<Vector2, bool> inside, Color color) { Inside = inside; Color = color; }
        }

        /// <summary>Couleur d'accent d'un sort (liseré du médaillon, coût en mana, recharge).</summary>
        public static Color Accent(string skillId) => skillId switch
        {
            "shield" => Palette.Shield,
            "purge" => Palette.Poison,
            _ => Palette.Heal,
        };

        public static Texture2D For(string skillId)
        {
            if (Cache.TryGetValue(skillId, out var cached)) return cached;
            var tex = Render(Layers(skillId));
            Cache[skillId] = tex;
            return tex;
        }

        private static List<Layer> Layers(string id)
        {
            var accent = Accent(id);
            var c = new Vector2(64, 64);
            var layers = new List<Layer>
            {
                new Layer(p => (p - c).sqrMagnitude <= 62 * 62, new Color(0.05f, 0.06f, 0.1f, 1f)),
                new Layer(p => { float d = (p - c).magnitude; return d <= 61 && d >= 55; }, accent),
                new Layer(p => { float d = (p - c).magnitude; return d <= 51 && d >= 50; }, new Color(accent.r, accent.g, accent.b, 0.35f)),
            };
            switch (id)
            {
                case "heal_single":
                    layers.Add(new Layer(p => Cross(p, c, 1f), Color.Lerp(accent, Color.white, 0.35f)));
                    layers.Add(new Layer(p => Cross(p, c, 0.55f), Color.white));
                    break;
                case "heal_aoe":
                    layers.Add(new Layer(p => { float d = (p - c).magnitude; return d <= 42 && d >= 37; }, accent));
                    layers.Add(new Layer(p => Cross(p, c, 0.62f), Color.Lerp(accent, Color.white, 0.35f)));
                    foreach (var a in new[] { 90f, 210f, 330f })
                    {
                        var s = c + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * 40f;
                        layers.Add(new Layer(p => (p - s).sqrMagnitude <= 7 * 7, Color.white));
                    }
                    break;
                case "shield":
                    var outer = new[] { new Vector2(64, 104), new Vector2(98, 92), new Vector2(98, 62), new Vector2(64, 22), new Vector2(30, 62), new Vector2(30, 92) };
                    var inner = new[] { new Vector2(64, 94), new Vector2(90, 85), new Vector2(90, 63), new Vector2(64, 32), new Vector2(38, 63), new Vector2(38, 85) };
                    layers.Add(new Layer(p => InsidePoly(outer, p), Color.Lerp(accent, Color.white, 0.25f)));
                    layers.Add(new Layer(p => InsidePoly(inner, p), Color.Lerp(accent, Color.black, 0.45f)));
                    layers.Add(new Layer(p => Cross(p, new Vector2(64, 66), 0.5f), Color.white));
                    break;
                case "purge":
                    var tri = new[] { new Vector2(64, 106), new Vector2(43, 66), new Vector2(85, 66) };
                    layers.Add(new Layer(p => InsidePoly(tri, p) || (p - new Vector2(64, 54)).sqrMagnitude <= 24 * 24, Color.Lerp(accent, Color.white, 0.2f)));
                    layers.Add(new Layer(p => (p - new Vector2(56, 60)).sqrMagnitude <= 7 * 7, Color.white));
                    layers.Add(new Layer(p => { var q = p - c; float u = (q.x + q.y) * 0.7071f, v = (q.y - q.x) * 0.7071f; return Mathf.Abs(v) <= 3.2f && Mathf.Abs(u) <= 50f; }, new Color(1f, 0.35f, 0.4f, 1f)));
                    break;
                default:
                    layers.Add(new Layer(p => (p - c).sqrMagnitude <= 22 * 22, accent));
                    break;
            }
            return layers;
        }

        /// <summary>Croix centrée (y vers le haut), mise à l'échelle.</summary>
        private static bool Cross(Vector2 p, Vector2 c, float scale)
        {
            var q = p - c;
            float arm = 30f * scale, half = 9f * scale;
            return (Mathf.Abs(q.x) <= half && Mathf.Abs(q.y) <= arm) || (Mathf.Abs(q.y) <= half && Mathf.Abs(q.x) <= arm);
        }

        private static Texture2D Render(List<Layer> layers)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            const int ss = 3;
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    Color result = new Color(0, 0, 0, 0);
                    foreach (var layer in layers)
                    {
                        int inside = 0;
                        for (int sy = 0; sy < ss; sy++)
                            for (int sx = 0; sx < ss; sx++)
                                if (layer.Inside(new Vector2(x + (sx + 0.5f) / ss, y + (sy + 0.5f) / ss))) inside++;
                        if (inside == 0) continue;
                        float a = layer.Color.a * inside / (ss * ss);
                        float outA = a + result.a * (1 - a);
                        result = outA <= 0 ? result : new Color(
                            (layer.Color.r * a + result.r * result.a * (1 - a)) / outA,
                            (layer.Color.g * a + result.g * result.a * (1 - a)) / outA,
                            (layer.Color.b * a + result.b * result.a * (1 - a)) / outA, outA);
                    }
                    pixels[y * Size + x] = result;
                }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        public static bool InsidePoly(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }
    }
}
