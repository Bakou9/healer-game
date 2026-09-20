using UnityEngine;

namespace Healer.Client
{
    /// <summary>Primitives de dessin IMGUI partagées par les écrans (menu, choix du niveau, combat).</summary>
    public static class UiKit
    {
        public static readonly Color Panel = Palette.Hex("161926");
        public static readonly Color PanelStroke = Palette.Hex("3A4058");
        public static readonly Color Selected = Palette.Hex("E0BE6A");
        public static readonly Color Muted = Palette.Hex("9A9FB4");
        public static readonly Color Gold = Palette.Hex("E8C25A");

        private static GUIStyle? _label;
        private static Texture2D? _star;

        public static void Fill(Rect r, Color c, float radius = 8f) =>
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, Vector4.zero, new Vector4(radius, radius, radius, radius));

        public static void Outline(Rect r, Color c, float width, float radius = 8f) =>
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, new Vector4(width, width, width, width), new Vector4(radius, radius, radius, radius));

        public static void Label(Rect r, string s, int size, Color c, TextAnchor anchor, bool bold = false)
        {
            _label ??= new GUIStyle(GUI.skin.label) { wordWrap = false, clipping = TextClipping.Overflow };
            _label.fontSize = size;
            _label.alignment = anchor;
            _label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            _label.normal.textColor = new Color(0, 0, 0, 0.75f);
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), s, _label);
            _label.normal.textColor = c;
            GUI.Label(r, s, _label);
        }

        /// <summary>Bouton plein avec libellé centré ; renvoie sa zone pour la détection de clic.</summary>
        public static void Button(Rect r, string text, int size, Color fill, Color stroke, bool enabled = true)
        {
            Fill(r, enabled ? fill : Palette.Hex("1B1E2C"), 8);
            Outline(r, enabled ? stroke : PanelStroke, 2, 8);
            Label(r, text, size, enabled ? Color.white : Muted, TextAnchor.MiddleCenter, true);
        }

        /// <summary>Étoile pleine (couleur or) ou vide (contour sombre), dessinée avec une texture générée par code.</summary>
        public static void Star(Rect r, bool filled)
        {
            _star ??= MakeStarTexture(96);
            GUI.DrawTexture(r, _star, ScaleMode.ScaleToFit, true, 0f, filled ? Gold : new Color(0.28f, 0.3f, 0.4f, 0.9f), 0f, 0f);
        }

        /// <summary>Rangée de 3 étoiles centrée sur un rectangle.</summary>
        public static void Stars(Rect area, int earned, float size)
        {
            float total = 3 * size + 2 * size * 0.15f;
            float x = area.x + (area.width - total) / 2f;
            for (int i = 0; i < 3; i++)
                Star(new Rect(x + i * size * 1.15f, area.y + (area.height - size) / 2f, size, size), i < earned);
        }

        private static Texture2D MakeStarTexture(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float rad = i % 2 == 0 ? 0.47f : 0.2f;
                pts[i] = new Vector2(0.5f + Mathf.Cos(a) * rad, 0.5f + Mathf.Sin(a) * rad);
            }
            const int ss = 3;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                            if (Inside(pts, new Vector2((x + (sx + 0.5f) / ss) / n, (y + (sy + 0.5f) / ss) / n))) inside++;
                    tex.SetPixel(x, y, new Color(1, 1, 1, inside / (float)(ss * ss)));
                }
            tex.Apply();
            return tex;
        }

        private static bool Inside(Vector2[] poly, Vector2 p)
        {
            bool c = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    c = !c;
            return c;
        }
    }
}
