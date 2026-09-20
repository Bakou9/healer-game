using System.Collections.Generic;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>Palette du jeu (langage visuel de docs/UX.md et docs/ART_3D.md).</summary>
    public static class Palette
    {
        public static readonly Color Stone = Hex("6E6A8C");
        public static readonly Color StoneDark = Hex("3E3B52");
        public static readonly Color StoneLight = Hex("8B86AA");
        public static readonly Color CoreCalm = Hex("5BE7FF");
        public static readonly Color CoreFury = Hex("FF6A3D");
        public static readonly Color Skin = Hex("D2A886");
        public static readonly Color EyeDark = Hex("23202E");
        public static readonly Color Heal = Hex("7CFFB2");
        public static readonly Color Damage = Hex("FF7C7C");
        public static readonly Color Shield = Hex("7CC8FF");
        public static readonly Color Poison = Hex("C78CFF");
        public static readonly Color Danger = Hex("FF5B5B");
        public static readonly Color Wood = Hex("7D5A38");
        public static readonly Color Tank = Hex("4A6A9A");
        public static readonly Color Archer = Hex("A44557");
        public static readonly Color Mage = Hex("6E58B4");
        public static readonly Color Healer = Hex("48A078");

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

        private static readonly Dictionary<Color, Material> Metal = new Dictionary<Color, Material>();

        /// <summary>Matériau métallique (armures, bouclier) : plus dur et plus brillant que le tissu.</summary>
        public static Material MetalMaterial(Color color)
        {
            if (!Metal.TryGetValue(color, out var m) || m == null)
            {
                m = new Material(Shader.Find("Standard")) { color = color };
                m.SetFloat("_Glossiness", 0.62f);
                m.SetFloat("_Metallic", 0.4f);
                Metal[color] = m;
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

        /// <summary>Base stylisée aux proportions adultes : tête modeste, buste long, bras fins, regard en fente.</summary>
        private static GameObject Chibi(string name, Color bodyColor, bool dress = false)
        {
            var root = new GameObject(name);
            var t = root.transform;
            var sphere = MeshKit.Sphere(6, 8);
            var cone = MeshKit.Cone(8);
            var body = LitMaterial(bodyColor);
            var skin = LitMaterial(Palette.Skin);
            var dark = GlowMaterial(Palette.EyeDark);

            if (dress) Part(t, "Body", cone, body, new Vector3(0, 0.85f, 0), S(1.2f, 1.6f, 0.95f));
            else Part(t, "Body", sphere, body, new Vector3(0, 0.92f, 0), S(0.9f, 1.5f, 0.7f));
            Part(t, "FootL", sphere, LitMaterial(Palette.StoneDark), new Vector3(-0.24f, 0.12f, 0.1f), S(0.3f, 0.22f, 0.5f));
            Part(t, "FootR", sphere, LitMaterial(Palette.StoneDark), new Vector3(0.24f, 0.12f, 0.1f), S(0.3f, 0.22f, 0.5f));
            Part(t, "ArmL", sphere, body, new Vector3(-0.58f, 1.02f, 0.05f), S(0.22f, 0.8f, 0.24f));
            Part(t, "ArmR", sphere, body, new Vector3(0.58f, 1.02f, 0.05f), S(0.22f, 0.8f, 0.24f));
            Part(t, "Head", sphere, skin, new Vector3(0, 1.8f, 0), S(0.68f, 0.74f, 0.68f));
            var eyes = new GameObject("Eyes");
            eyes.transform.SetParent(t, false);
            Part(eyes.transform, "EyeL", sphere, dark, new Vector3(-0.15f, 1.82f, 0.31f), S(0.11f, 0.05f, 0.06f));
            Part(eyes.transform, "EyeR", sphere, dark, new Vector3(0.15f, 1.82f, 0.31f), S(0.11f, 0.05f, 0.06f));
            return root;
        }

        /// <summary>Garde : casque, grand bouclier rond devant lui.</summary>
        public static GameObject Tank()
        {
            var root = Chibi("Tank", Palette.Tank);
            var t = root.transform;
            var armor = MetalMaterial(Palette.Hex("8C9AB0"));
            Part(t, "Helmet", MeshKit.Sphere(6, 8), armor, new Vector3(0, 2.03f, -0.02f), S(0.74f, 0.44f, 0.72f));
            Part(t, "Crest", MeshKit.Cube(), LitMaterial(Palette.Damage), new Vector3(0, 2.3f, -0.05f), S(0.08f, 0.24f, 0.44f));
            var shield = Part(t, "Shield", MeshKit.Cylinder(10), armor, new Vector3(-0.74f, 1.05f, 0.4f), S(0.95f, 0.12f, 0.95f), new Vector3(90, 0, 20));
            Part(shield.transform, "Emblem", MeshKit.Cylinder(10), GlowMaterial(Palette.Shield), new Vector3(0, 0.6f, 0), S(0.5f, 0.3f, 0.5f));
            return root;
        }

        /// <summary>Archère : fine, queue de cheval, arc et carquois.</summary>
        public static GameObject Archer()
        {
            var root = Chibi("Archer", Palette.Archer);
            var t = root.transform;
            var hair = LitMaterial(Palette.Hex("6B3A2E"));
            Part(t, "Hair", MeshKit.Sphere(6, 8), hair, new Vector3(0, 2.02f, -0.06f), S(0.72f, 0.46f, 0.72f));
            Part(t, "Ponytail", MeshKit.Cone(6), hair, new Vector3(0, 1.85f, -0.5f), S(0.22f, 0.85f, 0.22f), new Vector3(150, 0, 0));
            var wood = LitMaterial(Palette.Wood);
            var bow = new GameObject("Bow");
            bow.transform.SetParent(t, false);
            bow.transform.localPosition = new Vector3(0.8f, 1.1f, 0.3f);
            Part(bow.transform, "Upper", MeshKit.Cylinder(6), wood, new Vector3(0, 0.42f, 0.0f), S(0.09f, 0.62f, 0.09f), new Vector3(0, 0, -18));
            Part(bow.transform, "Lower", MeshKit.Cylinder(6), wood, new Vector3(0, -0.42f, 0.0f), S(0.09f, 0.62f, 0.09f), new Vector3(0, 0, 18));
            Part(bow.transform, "Grip", MeshKit.Cylinder(6), wood, Vector3.zero, S(0.12f, 0.4f, 0.12f));
            Part(bow.transform, "String", MeshKit.Cube(), LitMaterial(Palette.Hex("EDE6D6")), new Vector3(-0.1f, 0, 0), S(0.02f, 1.5f, 0.02f));
            Part(t, "Quiver", MeshKit.Cylinder(8), wood, new Vector3(-0.1f, 1.15f, -0.45f), S(0.26f, 0.85f, 0.26f), new Vector3(0, 0, 18));
            return root;
        }

        /// <summary>Mage : chapeau pointu, bâton à orbe violet.</summary>
        public static GameObject Mage()
        {
            var root = Chibi("Mage", Palette.Mage);
            var t = root.transform;
            var hat = LitMaterial(Palette.Hex("5A3F9E"));
            Part(t, "HatBrim", MeshKit.Cylinder(10), hat, new Vector3(0, 2.08f, 0), S(1.1f, 0.08f, 1.1f));
            Part(t, "Hat", MeshKit.Cone(10), hat, new Vector3(0, 2.68f, 0), S(0.72f, 1.2f, 0.72f), new Vector3(-6, 0, 0));
            Part(t, "HatBand", MeshKit.Cylinder(10), GlowMaterial(Palette.Poison), new Vector3(0, 2.14f, 0), S(0.76f, 0.1f, 0.76f));
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
            Part(t, "Hood", MeshKit.Sphere(6, 8), LitMaterial(Palette.Hex("EAF6EE")), new Vector3(0, 2.05f, -0.05f), S(0.78f, 0.5f, 0.76f));
            Part(t, "Sash", MeshKit.Cylinder(10), LitMaterial(Palette.Hex("EAF6EE")), new Vector3(0, 1.2f, 0), S(0.66f, 0.09f, 0.56f));
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

        // ---- Autres boss -----------------------------------------------------------------------

        /// <summary>Couleurs lumineuses d'un boss : (calme, fureur).</summary>
        public static (Color calm, Color fury) BossColors(string id)
        {
            switch (id)
            {
                case "boss2": return (Palette.Hex("9CFF5A"), Palette.Hex("E45CFF"));
                case "boss3": return (Palette.Hex("FFA13D"), Palette.Hex("FF3B2A"));
                default: return (Palette.CoreCalm, Palette.CoreFury);
            }
        }

        public static float BossScaleFactor(string id) => id == "boss2" ? 0.95f : 1f;

        public static GameObject ForBoss(string id)
        {
            switch (id)
            {
                case "boss2": return SwampQueen();
                case "boss3": return AshLord();
                default: return Golem();
            }
        }

        /// <summary>Reine des Marais : bulbe de marais, buste élancé, couronne d'épines, tentacules et cœur toxique.</summary>
        public static GameObject SwampQueen()
        {
            var root = new GameObject("SwampQueen");
            var t = root.transform;
            var moss = LitMaterial(Palette.Hex("3F5A3A"));
            var dark = LitMaterial(Palette.Hex("263A2B"));
            var light = LitMaterial(Palette.Hex("6C8A52"));
            var bark = LitMaterial(Palette.Hex("4B3A2C"));
            var sphere = MeshKit.Sphere(6, 8);
            var cyl = MeshKit.Cylinder(8);
            var cone = MeshKit.Cone(6);
            var cube = MeshKit.Cube();

            Part(t, "Bulb", sphere, dark, new Vector3(0, 0.9f, 0), S(2.7f, 1.8f, 2.5f));
            Part(t, "Torso", sphere, moss, new Vector3(0, 2.15f, 0), S(1.5f, 1.9f, 1.2f));
            Part(t, "Chest", sphere, light, new Vector3(0, 2.3f, 0.28f), S(0.95f, 0.8f, 0.7f));
            Part(t, "Head", sphere, moss, new Vector3(0, 3.4f, 0.05f), S(0.9f, 0.85f, 0.85f));
            for (int i = 0; i < 5; i++)
            {
                int d = Mathf.Abs(i - 2);
                Part(t, "Thorn", cone, bark, new Vector3((i - 2) * 0.28f, 3.9f + (2 - d) * 0.1f, 0f), S(0.16f, 0.55f + 0.12f * (2 - d), 0.16f), new Vector3(0, 0, (i - 2) * -12f));
            }
            foreach (float side in new[] { -1f, 1f })
            {
                Part(t, "Tentacle", cyl, moss, new Vector3(side * 1.15f, 2.4f, 0.2f), S(0.3f, 1.7f, 0.3f), new Vector3(0, 0, side * -22f));
                Part(t, "Tentacle", cyl, dark, new Vector3(side * 1.75f, 1.3f, 0.45f), S(0.24f, 1.6f, 0.24f), new Vector3(0, 0, side * -52f));
                Part(t, "TentacleTip", sphere, light, new Vector3(side * 2.2f, 0.55f, 0.55f), S(0.34f, 0.34f, 0.34f));
            }
            var glow = GlowMaterial(Palette.Hex("9CFF5A"));
            Part(t, "Core", sphere, glow, new Vector3(0, 2.25f, 0.72f), S(0.55f, 0.55f, 0.4f));
            var eyes = new GameObject("Eyes");
            eyes.transform.SetParent(t, false);
            Part(eyes.transform, "EyeL", cube, glow, new Vector3(-0.25f, 3.42f, 0.42f), S(0.22f, 0.1f, 0.08f));
            Part(eyes.transform, "EyeR", cube, glow, new Vector3(0.25f, 3.42f, 0.42f), S(0.22f, 0.1f, 0.08f));
            var runes = new GameObject("Runes");
            runes.transform.SetParent(t, false);
            for (int i = 0; i < 6; i++)
            {
                float a = (i - 2.5f) * 0.42f;
                Part(runes.transform, "Rune", sphere, glow, new Vector3(Mathf.Sin(a) * 1.15f, 0.75f + 0.12f * (i % 2), Mathf.Cos(a) * 1.05f), S(0.2f, 0.2f, 0.2f));
            }
            return root;
        }

        /// <summary>Seigneur de Cendre : géant de roche noire fissurée de lave, cornes, épaules hérissées.</summary>
        public static GameObject AshLord()
        {
            var root = new GameObject("AshLord");
            var t = root.transform;
            var stone = LitMaterial(Palette.Hex("3A3540"));
            var dark = LitMaterial(Palette.Hex("231F27"));
            var light = LitMaterial(Palette.Hex("55505F"));
            var horn = LitMaterial(Palette.Hex("C9BFAE"));
            var sphere = MeshKit.Sphere(6, 8);
            var cyl = MeshKit.Cylinder(8);
            var cone = MeshKit.Cone(6);
            var cube = MeshKit.Cube();

            Part(t, "LegL", cyl, dark, new Vector3(-0.55f, 0.55f, 0), S(0.7f, 1.1f, 0.7f));
            Part(t, "LegR", cyl, dark, new Vector3(0.55f, 0.55f, 0), S(0.7f, 1.1f, 0.7f));
            Part(t, "Torso", sphere, stone, new Vector3(0, 1.95f, 0), S(2.1f, 2.1f, 1.5f));
            Part(t, "Belly", sphere, dark, new Vector3(0, 1.45f, 0.35f), S(1.3f, 1.1f, 0.8f));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(t, "Shoulder", sphere, light, new Vector3(side * 1.35f, 2.65f, 0), S(1.1f, 1.0f, 1.1f));
                Part(t, "ShoulderSpike", cone, horn, new Vector3(side * 1.5f, 3.2f, 0), S(0.3f, 0.75f, 0.3f), new Vector3(0, 0, side * -18f));
                Part(t, "Arm", cyl, stone, new Vector3(side * 1.55f, 1.6f, 0), S(0.65f, 1.6f, 0.65f));
                Part(t, "Fist", sphere, light, new Vector3(side * 1.55f, 0.6f, 0.1f), S(0.95f, 0.9f, 0.95f));
                Part(t, "Horn", cone, horn, new Vector3(side * 0.45f, 4.05f, 0), S(0.22f, 0.85f, 0.22f), new Vector3(0, 0, side * -26f));
            }
            Part(t, "Head", sphere, stone, new Vector3(0, 3.4f, 0.05f), S(1.0f, 0.95f, 0.95f));
            Part(t, "Brow", cube, dark, new Vector3(0, 3.55f, 0.46f), S(0.8f, 0.1f, 0.14f));

            var glow = GlowMaterial(Palette.Hex("FFA13D"));
            Part(t, "Core", sphere, glow, new Vector3(0, 2.0f, 0.74f), S(0.7f, 0.7f, 0.5f));
            var eyes = new GameObject("Eyes");
            eyes.transform.SetParent(t, false);
            Part(eyes.transform, "EyeL", cube, glow, new Vector3(-0.28f, 3.4f, 0.5f), S(0.24f, 0.11f, 0.1f));
            Part(eyes.transform, "EyeR", cube, glow, new Vector3(0.28f, 3.4f, 0.5f), S(0.24f, 0.11f, 0.1f));
            var runes = new GameObject("Runes");
            runes.transform.SetParent(t, false);
            foreach (float side in new[] { -1f, 1f })
                for (int i = 0; i < 3; i++)
                    Part(runes.transform, "Rune", cube, glow, new Vector3(side * (0.55f + 0.12f * i), 1.5f + 0.35f * i, 0.72f), S(0.08f, 0.5f, 0.06f), new Vector3(0, 0, side * 14f));
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
