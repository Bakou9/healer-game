using System;
using UnityEngine;
using UnityEngine.UIElements;
using Rect = UnityEngine.Rect;

namespace Healer.Client
{
    /// <summary>
    /// Petit kit UI Toolkit du jeu : couleurs, boîtes, textes, boutons et étoiles positionnés sur la grille
    /// LOGIQUE 1280×720 (Healer.Ui.Layout). Toute la mise en page vient du cœur (testée) ; ici on ne fait que
    /// créer des éléments à ces rectangles. Aucun fichier UXML/USS : l'interface se reconstruit par le code.
    /// </summary>
    public static class Ui
    {
        public static readonly Color Panel = Palette.Hex("161926");
        public static readonly Color PanelStroke = Palette.Hex("3A4058");
        public static readonly Color Selected = Palette.Hex("E0BE6A");
        public static readonly Color Muted = Palette.Hex("9A9FB4");
        public static readonly Color Gold = Palette.Hex("E8C25A");
        public static readonly Color ButtonFill = Palette.Hex("22273B");
        public static readonly Color ButtonStroke = Palette.Hex("6F7698");
        public static readonly Color PrimaryFill = Palette.Hex("245C43");
        public static readonly Color PrimaryStroke = Palette.Hex("5FB98D");

        private static Font? _font;
        private static Texture2D? _star;

        /// <summary>Rectangle du cœur (double) vers rectangle Unity (float).</summary>
        public static Rect R(Healer.Ui.Rect r) => new Rect((float)r.X, (float)r.Y, (float)r.W, (float)r.H);

        /// <summary>Texte riche d'une fiche de sort : les valeurs modifiées (Changed) sont en vert, entre parenthèses.</summary>
        public static string Rich(System.Collections.Generic.IReadOnlyList<Healer.Ui.SkillSpan> spans)
        {
            string green = ColorUtility.ToHtmlStringRGB(Palette.Heal);
            var sb = new System.Text.StringBuilder();
            foreach (var span in spans)
                sb.Append(span.Changed ? "<b><color=#" + green + ">(" + span.Text + ")</color></b>" : span.Text);
            return sb.ToString();
        }

        /// <summary>Police d'interface (police intégrée d'Unity) appliquée à la racine : les enfants en héritent.</summary>
        public static void ApplyFont(VisualElement root)
        {
            _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            root.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(_font));
        }

        public static void Place(VisualElement e, Rect r)
        {
            e.style.position = Position.Absolute;
            e.style.left = r.x;
            e.style.top = r.y;
            e.style.width = r.width;
            e.style.height = r.height;
        }

        public static void Round(VisualElement e, float radius)
        {
            e.style.borderTopLeftRadius = radius; e.style.borderTopRightRadius = radius;
            e.style.borderBottomLeftRadius = radius; e.style.borderBottomRightRadius = radius;
        }

        public static void Stroke(VisualElement e, Color c, float width)
        {
            e.style.borderTopColor = c; e.style.borderRightColor = c; e.style.borderBottomColor = c; e.style.borderLeftColor = c;
            e.style.borderTopWidth = width; e.style.borderRightWidth = width; e.style.borderBottomWidth = width; e.style.borderLeftWidth = width;
        }

        /// <summary>Boîte pleine (fond, coins arrondis, contour facultatif). Ne capte pas les clics.</summary>
        public static VisualElement Box(VisualElement? parent, Rect r, Color fill, float radius = 10f, Color? stroke = null, float strokeWidth = 0f)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            Place(e, r);
            e.style.backgroundColor = fill;
            Round(e, radius);
            if (stroke.HasValue && strokeWidth > 0) Stroke(e, stroke.Value, strokeWidth);
            parent?.Add(e);
            return e;
        }

        /// <summary>Texte sur une ligne (ou plusieurs avec wrap) dans un rectangle, avec une ombre portée lisible sur la scène 3D.</summary>
        public static Label Text(VisualElement? parent, Rect r, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false, bool wrap = false)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            Place(l, r);
            SetText(l, text, size, color, anchor, bold, wrap);
            parent?.Add(l);
            return l;
        }

        public static void SetText(Label l, string text, int size, Color color, TextAnchor anchor, bool bold = false, bool wrap = false)
        {
            l.text = text;
            l.style.fontSize = size;
            l.style.color = color;
            l.style.unityTextAlign = anchor;
            l.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            l.style.whiteSpace = wrap ? WhiteSpace.Normal : WhiteSpace.NoWrap;
            l.style.overflow = Overflow.Visible;
            l.style.marginLeft = 0; l.style.marginRight = 0; l.style.marginTop = 0; l.style.marginBottom = 0;
            l.style.paddingLeft = 0; l.style.paddingRight = 0; l.style.paddingTop = 0; l.style.paddingBottom = 0;
            l.style.textShadow = new TextShadow { offset = new Vector2(1.2f, 1.2f), blurRadius = 0f, color = new Color(0, 0, 0, 0.75f) };
        }

        /// <summary>Bouton plein avec libellé : un clic complet (appui et relâchement dans le bouton) appelle onClick.</summary>
        public static VisualElement Button(VisualElement parent, Rect r, string text, int size, Color fill, Color stroke, Action onClick, bool enabled = true)
        {
            var b = Box(parent, r, enabled ? fill : Palette.Hex("1B1E2C"), 8f, enabled ? stroke : PanelStroke, 2f);
            b.pickingMode = PickingMode.Position;
            Text(b, new Rect(0, 0, r.width, r.height), text, size, enabled ? Color.white : Muted, TextAnchor.MiddleCenter, true);
            if (enabled)
            {
                var hover = Color.Lerp(fill, Color.white, 0.12f);
                b.RegisterCallback<PointerEnterEvent>(_ => b.style.backgroundColor = hover);
                b.RegisterCallback<PointerLeaveEvent>(_ => b.style.backgroundColor = fill);
            }
            b.RegisterCallback<ClickEvent>(_ => onClick());
            return b;
        }

        /// <summary>Zone cliquable transparente (carte entière).</summary>
        public static void OnClick(VisualElement e, Action onClick)
        {
            e.pickingMode = PickingMode.Position;
            e.RegisterCallback<ClickEvent>(_ => onClick());
        }

        /// <summary>Étoile pleine (or) ou vide (contour sombre).</summary>
        public static VisualElement Star(VisualElement parent, Rect r, bool filled)
        {
            _star ??= MakeStarTexture(96);
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            Place(e, r);
            e.style.backgroundImage = new StyleBackground(_star);
            e.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            e.style.unityBackgroundImageTintColor = filled ? Gold : new Color(0.28f, 0.3f, 0.4f, 0.9f);
            parent.Add(e);
            return e;
        }

        /// <summary>Rangée de 3 étoiles centrée sur une zone.</summary>
        public static void Stars(VisualElement parent, Rect area, int earned, float size)
        {
            float total = 3 * size + 2 * size * 0.15f;
            float x = area.x + (area.width - total) / 2f;
            for (int i = 0; i < 3; i++)
                Star(parent, new Rect(x + i * size * 1.15f, area.y + (area.height - size) / 2f, size, size), i < earned);
        }

        /// <summary>Élément plein écran (hors grille logique) : fonds, voiles, vignettes.</summary>
        public static VisualElement Fullscreen(VisualElement parent)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute;
            e.style.left = 0; e.style.top = 0; e.style.right = 0; e.style.bottom = 0;
            parent.Add(e);
            return e;
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
                            if (IconKit.InsidePoly(pts, new Vector2((x + (sx + 0.5f) / ss) / n, (y + (sy + 0.5f) / ss) / n))) inside++;
                    tex.SetPixel(x, y, new Color(1, 1, 1, inside / (float)(ss * ss)));
                }
            tex.Apply();
            return tex;
        }
    }
}
