using System.Collections.Generic;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>Palette du jeu (langage visuel de docs/UX.md et docs/ART_3D.md).</summary>
    public static class Palette
    {
        public static readonly Color Stone = Hex("6B6485");
        public static readonly Color StoneDark = Hex("4A4562");
        public static readonly Color StoneLight = Hex("8A82A8");
        public static readonly Color CoreCalm = Hex("5BE7FF");
        public static readonly Color CoreFury = Hex("FF6A3D");
        public static readonly Color Skin = Hex("F2C6A0");
        public static readonly Color EyeDark = Hex("23202E");
        public static readonly Color Heal = Hex("7CFFB2");
        public static readonly Color Damage = Hex("FF7C7C");
        public static readonly Color Shield = Hex("7CC8FF");
        public static readonly Color Poison = Hex("C78CFF");
        public static readonly Color Danger = Hex("FF5B5B");
        public static readonly Color Wood = Hex("A57B4B");
        public static readonly Color Tank = Hex("4A6FA5");
        public static readonly Color Archer = Hex("B5495B");
        public static readonly Color Mage = Hex("7B5BC4");
        public static readonly Color Healer = Hex("4CAF7D");

        public static Color Hex(string rgb)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out var c);
            return c;
        }
    }

    /// <summary>
    /// Fabrique des modèles 3D « figurine » à partir de formes procédurales (MeshKit). Aucun fichier de
    /// modèle : tout est reproductible par code (docs/ART_3D.md). Origine de chaque modèle = au sol, au centre.
    /// </summary>
    public static class ModelFactory
    {
        private static readonly Dictionary<Color, Material> Lit = new Dictionary<Color, Material>();
        private static readonly Dictionary<Color, Material> Glow = new Dictionary<Color, Material>();

        public static Material LitMaterial(Color color)
        {
            if (!Lit.TryGetValue(color, out var m) || m == null)
            {
                m = new Material(Shader.Find("Standard")) { color = color };
                m.SetFloat("_Glossiness", 0.28f);
                m.SetFloat("_Metallic", 0f);
                Lit[color] = m;
            }
            return m;
        }

        public static Material GlowMaterial(Color color)
        {
            if (!Glow.TryGetValue(color, out var m) || m == null)
            {
                m = new Material(Shader.Find("Unlit/Color")) { color = color };
                Glow[color] = m;
            }
            return m;
        }

        private static GameObject Part(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale, Vector3? euler = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localEulerAngles = euler ?? Vector3.zero;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static Vector3 S(float x, float y, float z) => new Vector3(x, y, z);

        // ---- Boss ------------------------------------------------------------------------------

        /// <summary>Golem Ancestral : pierre trapue aux épaules rondes, cœur et yeux lumineux (cyan, orange en phase 2).</summary>
        public static GameObject Golem()
        {
            var root = new GameObject("Golem");
            var t = root.transform;
            var stone = LitMaterial(Palette.Stone);
            var dark = LitMaterial(Palette.StoneDark);
            var light = LitMaterial(Palette.StoneLight);
            var sphere = MeshKit.Sphere(6, 8);
            var cyl = MeshKit.Cylinder(8);
            var cube = MeshKit.Cube();

            Part(t, "LegL", cyl, dark, new Vector3(-0.5f, 0.45f, 0), S(0.62f, 0.9f, 0.62f));
            Part(t, "LegR", cyl, dark, new Vector3(0.5f, 0.45f, 0), S(0.62f, 0.9f, 0.62f));
            Part(t, "Torso", sphere, stone, new Vector3(0, 1.55f, 0), S(2.0f, 1.8f, 1.5f));
            Part(t, "Belly", sphere, light, new Vector3(0, 1.35f, 0.32f), S(1.25f, 1.0f, 0.9f));
            Part(t, "ShoulderL", sphere, light, new Vector3(-1.25f, 2.2f, 0), S(0.95f, 0.95f, 0.95f));
            Part(t, "ShoulderR", sphere, light, new Vector3(1.25f, 2.2f, 0), S(0.95f, 0.95f, 0.95f));
            Part(t, "ArmL", cyl, stone, new Vector3(-1.4f, 1.35f, 0), S(0.6f, 1.4f, 0.6f));
            Part(t, "ArmR", cyl, stone, new Vector3(1.4f, 1.35f, 0), S(0.6f, 1.4f, 0.6f));
            Part(t, "FistL", sphere, light, new Vector3(-1.4f, 0.6f, 0.1f), S(0.85f, 0.8f, 0.85f));
            Part(t, "FistR", sphere, light, new Vector3(1.4f, 0.6f, 0.1f), S(0.85f, 0.8f, 0.85f));
            Part(t, "Head", sphere, stone, new Vector3(0, 2.85f, 0.05f), S(1.05f, 0.95f, 1.0f));
            Part(t, "Brow", cube, dark, new Vector3(0, 2.98f, 0.46f), S(0.75f, 0.1f, 0.14f));

            var glow = GlowMaterial(Palette.CoreCalm);
            Part(t, "Core", sphere, glow, new Vector3(0, 1.6f, 0.72f), S(0.6f, 0.6f, 0.5f));
            var eyes = new GameObject("Eyes");
            eyes.transform.SetParent(t, false);
            Part(eyes.transform, "EyeL", cube, glow, new Vector3(-0.27f, 2.85f, 0.5f), S(0.22f, 0.13f, 0.1f));
            Part(eyes.transform, "EyeR", cube, glow, new Vector3(0.27f, 2.85f, 0.5f), S(0.22f, 0.13f, 0.1f));
            var runes = new GameObject("Runes");
            runes.transform.SetParent(t, false);
            foreach (float side in new[] { -1f, 1f })
                for (int i = 0; i < 2; i++)
                    Part(runes.transform, "Rune", cube, glow, new Vector3(side * 1.4f, 1.15f + 0.3f * i, 0.32f), S(0.2f, 0.07f, 0.06f));
            return root;
        }

        // ---- Personnages -----------------------------------------------------------------------

        /// <summary>Base « chibi » : grosse tête, corps compact, pieds ronds, yeux sombres.</summary>
        private static GameObject Chibi(string name, Color bodyColor, bool dress = false)
        {
            var root = new GameObject(name);
            var t = root.transform;
            var sphere = MeshKit.Sphere(6, 8);
            var cone = MeshKit.Cone(8);
            var body = LitMaterial(bodyColor);
            var skin = LitMaterial(Palette.Skin);
            var dark = GlowMaterial(Palette.EyeDark);

            if (dress) Part(t, "Body", cone, body, new Vector3(0, 0.72f, 0), S(1.35f, 1.35f, 1.1f));
            else Part(t, "Body", sphere, body, new Vector3(0, 0.75f, 0), S(1.0f, 1.05f, 0.85f));
            Part(t, "FootL", sphere, LitMaterial(Palette.StoneDark), new Vector3(-0.28f, 0.13f, 0.1f), S(0.38f, 0.24f, 0.55f));
            Part(t, "FootR", sphere, LitMaterial(Palette.StoneDark), new Vector3(0.28f, 0.13f, 0.1f), S(0.38f, 0.24f, 0.55f));
            Part(t, "ArmL", sphere, body, new Vector3(-0.62f, 0.85f, 0.05f), S(0.32f, 0.5f, 0.32f));
            Part(t, "ArmR", sphere, body, new Vector3(0.62f, 0.85f, 0.05f), S(0.32f, 0.5f, 0.32f));
            Part(t, "Head", sphere, skin, new Vector3(0, 1.7f, 0), S(0.95f, 0.9f, 0.9f));
            var eyes = new GameObject("Eyes");
            eyes.transform.SetParent(t, false);
            Part(eyes.transform, "EyeL", sphere, dark, new Vector3(-0.2f, 1.72f, 0.4f), S(0.13f, 0.17f, 0.08f));
            Part(eyes.transform, "EyeR", sphere, dark, new Vector3(0.2f, 1.72f, 0.4f), S(0.13f, 0.17f, 0.08f));
            return root;
        }

        /// <summary>Garde : casque, grand bouclier rond devant lui.</summary>
        public static GameObject Tank()
        {
            var root = Chibi("Tank", Palette.Tank);
            var t = root.transform;
            var armor = LitMaterial(Palette.Hex("8FA6C8"));
            Part(t, "Helmet", MeshKit.Sphere(6, 8), armor, new Vector3(0, 2.0f, -0.02f), S(1.0f, 0.55f, 0.95f));
            Part(t, "Crest", MeshKit.Cube(), LitMaterial(Palette.Damage), new Vector3(0, 2.32f, -0.05f), S(0.1f, 0.3f, 0.5f));
            var shield = Part(t, "Shield", MeshKit.Cylinder(10), armor, new Vector3(-0.78f, 0.95f, 0.42f), S(1.1f, 0.14f, 1.1f), new Vector3(90, 0, 20));
            Part(shield.transform, "Emblem", MeshKit.Cylinder(10), GlowMaterial(Palette.Shield), new Vector3(0, 0.6f, 0), S(0.5f, 0.3f, 0.5f));
            return root;
        }

        /// <summary>Archère : fine, queue de cheval, arc et carquois.</summary>
        public static GameObject Archer()
        {
            var root = Chibi("Archer", Palette.Archer);
            var t = root.transform;
            var hair = LitMaterial(Palette.Hex("6B3A2E"));
            Part(t, "Hair", MeshKit.Sphere(6, 8), hair, new Vector3(0, 1.95f, -0.08f), S(1.0f, 0.6f, 0.95f));
            Part(t, "Ponytail", MeshKit.Cone(6), hair, new Vector3(0, 1.75f, -0.62f), S(0.3f, 0.9f, 0.3f), new Vector3(150, 0, 0));
            var wood = LitMaterial(Palette.Wood);
            var bow = new GameObject("Bow");
            bow.transform.SetParent(t, false);
            bow.transform.localPosition = new Vector3(0.85f, 1.0f, 0.3f);
            Part(bow.transform, "Upper", MeshKit.Cylinder(6), wood, new Vector3(0, 0.42f, 0.0f), S(0.09f, 0.62f, 0.09f), new Vector3(0, 0, -18));
            Part(bow.transform, "Lower", MeshKit.Cylinder(6), wood, new Vector3(0, -0.42f, 0.0f), S(0.09f, 0.62f, 0.09f), new Vector3(0, 0, 18));
            Part(bow.transform, "Grip", MeshKit.Cylinder(6), wood, Vector3.zero, S(0.12f, 0.4f, 0.12f));
            Part(bow.transform, "String", MeshKit.Cube(), LitMaterial(Palette.Hex("EDE6D6")), new Vector3(-0.1f, 0, 0), S(0.02f, 1.5f, 0.02f));
            Part(t, "Quiver", MeshKit.Cylinder(8), wood, new Vector3(-0.1f, 1.0f, -0.5f), S(0.3f, 0.9f, 0.3f), new Vector3(0, 0, 18));
            return root;
        }

        /// <summary>Mage : chapeau pointu, bâton à orbe violet.</summary>
        public static GameObject Mage()
        {
            var root = Chibi("Mage", Palette.Mage);
            var t = root.transform;
            var hat = LitMaterial(Palette.Hex("5A3F9E"));
            Part(t, "HatBrim", MeshKit.Cylinder(10), hat, new Vector3(0, 2.0f, 0), S(1.4f, 0.1f, 1.4f));
            Part(t, "Hat", MeshKit.Cone(10), hat, new Vector3(0, 2.6f, 0), S(0.95f, 1.2f, 0.95f), new Vector3(-6, 0, 0));
            Part(t, "HatBand", MeshKit.Cylinder(10), GlowMaterial(Palette.Poison), new Vector3(0, 2.1f, 0), S(1.0f, 0.12f, 1.0f));
            var staff = new GameObject("Staff");
            staff.transform.SetParent(t, false);
            staff.transform.localPosition = new Vector3(0.85f, 0, 0.25f);
            Part(staff.transform, "Shaft", MeshKit.Cylinder(6), LitMaterial(Palette.Wood), new Vector3(0, 1.0f, 0), S(0.09f, 2.0f, 0.09f));
            Part(staff.transform, "Orb", MeshKit.Sphere(6, 8), GlowMaterial(Palette.Poison), new Vector3(0, 2.1f, 0), S(0.42f, 0.42f, 0.42f));
            return root;
        }

        /// <summary>Soigneuse (le joueur) : robe claire, capuche, bâton à croix verte lumineuse.</summary>
        public static GameObject Healer()
        {
            var root = Chibi("Healer", Palette.Healer, dress: true);
            var t = root.transform;
            Part(t, "Hood", MeshKit.Sphere(6, 8), LitMaterial(Palette.Hex("EAF6EE")), new Vector3(0, 1.98f, -0.06f), S(1.05f, 0.62f, 1.0f));
            Part(t, "Sash", MeshKit.Cylinder(10), LitMaterial(Palette.Hex("EAF6EE")), new Vector3(0, 0.98f, 0), S(0.78f, 0.12f, 0.66f));
            var staff = new GameObject("Staff");
            staff.transform.SetParent(t, false);
            staff.transform.localPosition = new Vector3(0.85f, 0, 0.25f);
            Part(staff.transform, "Shaft", MeshKit.Cylinder(6), LitMaterial(Palette.Wood), new Vector3(0, 1.0f, 0), S(0.09f, 2.0f, 0.09f));
            var cross = new GameObject("Cross");
            cross.transform.SetParent(staff.transform, false);
            cross.transform.localPosition = new Vector3(0, 2.15f, 0);
            Part(cross.transform, "Vertical", MeshKit.Cube(), GlowMaterial(Palette.Heal), Vector3.zero, S(0.16f, 0.6f, 0.16f));
            Part(cross.transform, "Horizontal", MeshKit.Cube(), GlowMaterial(Palette.Heal), Vector3.zero, S(0.6f, 0.16f, 0.16f));
            return root;
        }

        public static GameObject ForCharacter(string id, string role)
        {
            switch (id)
            {
                case "tank": return Tank();
                case "dps1": return Archer();
                case "dps2": return Mage();
                case "healer": return Healer();
            }
            return role == "tank" ? Tank() : role == "healer" ? Healer() : Archer();
        }

        public static int TriangleCount(GameObject model)
        {
            int total = 0;
            foreach (var f in model.GetComponentsInChildren<MeshFilter>()) total += MeshKit.TriangleCount(f.sharedMesh);
            return total;
        }
    }
}
