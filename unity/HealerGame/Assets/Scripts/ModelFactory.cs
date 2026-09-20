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
    /// Fabrique des modèles 3D « dark fantasy » à partir de formes procédurales à facettes (MeshKit) : armures à arêtes
    /// vives, capuches, cornes, cristaux et failles lumineuses. Aucun fichier de modèle : tout est reproductible par code
    /// (docs/ART_3D.md, D-058). Origine de chaque modèle = au sol, au centre, face vers +Z. Chaque modèle porte un UnitRig
    /// (tête, bras, cape, buste) que la scène anime. Les parties lumineuses d'un boss s'appellent « Core », « EyeL »,
    /// « EyeR » et « Rune » : la scène les teinte selon la phase.
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
                m.SetFloat("_Glossiness", 0.18f);
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
                m.SetFloat("_Glossiness", 0.55f);
                m.SetFloat("_Metallic", 0.55f);
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

        private static Transform Pivot(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go.transform;
        }

        private static Vector3 S(float x, float y, float z) => new Vector3(x, y, z);
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        private static Color H(string hex) => Palette.Hex(hex);

        // Palette dark fantasy : acier terni, cuir noir, sang, os, or vieilli.
        private static readonly Color Steel = Palette.Hex("59606E");
        private static readonly Color SteelLight = Palette.Hex("8A93A6");
        private static readonly Color BlackCloth = Palette.Hex("15161D");
        private static readonly Color Crimson = Palette.Hex("8E2B34");
        private static readonly Color Bone = Palette.Hex("CFC6B0");
        private static readonly Color Gold = Palette.Hex("B8924A");

        // ---- Personnages -----------------------------------------------------------------------

        private static UnitRig NewRig(GameObject root) => root.AddComponent<UnitRig>();

        private static void Legs(Transform t, Material leg, Material boot, float spread = 0.26f, float width = 0.34f)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                Part(t, "Leg", MeshKit.Prism(6, 0.8f), leg, V(side * spread, 0.44f, 0), S(width, 0.88f, width + 0.04f));
                Part(t, "Boot", MeshKit.Wedge(), boot, V(side * spread, 0.1f, 0.1f), S(width + 0.06f, 0.24f, 0.66f));
            }
        }

        /// <summary>Garde : armure de plaques noircies, casque à cornes et visière lumineuse, grand bouclier runique, épée, cape sang.</summary>
        public static GameObject Tank()
        {
            var root = new GameObject("Tank");
            var t = root.transform;
            var rig = NewRig(root);
            var steel = MetalMaterial(Steel); var plate = MetalMaterial(SteelLight); var cloth = LitMaterial(BlackCloth);
            var red = LitMaterial(Crimson); var bone = LitMaterial(Bone); var glow = GlowMaterial(Palette.Shield);
            Legs(t, steel, cloth);
            Part(t, "Skirt", MeshKit.Prism(6, 1.25f), cloth, V(0, 0.98f, 0), S(0.95f, 0.5f, 0.8f));
            var torso = Pivot(t, "Torso", V(0, 1.52f, 0)); rig.Torso = torso;
            Part(torso, "Chest", MeshKit.Prism(6, 1.3f), steel, V(0, 0, 0), S(0.9f, 0.92f, 0.72f));
            Part(torso, "Plate", MeshKit.Wedge(), plate, V(0, 0.08f, 0.3f), S(0.72f, 0.7f, 0.32f));
            Part(torso, "Belt", MeshKit.Prism(6, 1f), red, V(0, -0.5f, 0), S(0.86f, 0.12f, 0.7f));
            var head = Pivot(torso, "Head", V(0, 0.72f, 0)); rig.Head = head;
            Part(head, "Helm", MeshKit.Prism(8, 0.9f), steel, V(0, 0.05f, 0), S(0.6f, 0.62f, 0.6f));
            Part(head, "Visor", MeshKit.Cube(), glow, V(0, 0.05f, 0.31f), S(0.44f, 0.07f, 0.05f));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(head, "Horn", MeshKit.Cone(4), bone, V(side * 0.3f, 0.32f, 0), S(0.13f, 0.6f, 0.13f), V(0, 0, side * -34f));
                Part(torso, "Pauldron", MeshKit.Crystal(5, 0.35f), plate, V(side * 0.62f, 0.34f, 0), S(0.62f, 0.5f, 0.64f));
                Part(torso, "PauldronSpike", MeshKit.Cone(4), red, V(side * 0.72f, 0.72f, 0), S(0.16f, 0.5f, 0.16f), V(0, 0, side * -22f));
            }
            Part(head, "Crest", MeshKit.Blade(), red, V(0, 0.42f, -0.08f), S(0.55f, 0.42f, 0.5f), V(0, 90, 0));
            var armL = Pivot(torso, "ArmL", V(-0.62f, 0.3f, 0)); rig.ArmL = armL;
            Part(armL, "Upper", MeshKit.Prism(6, 0.8f), steel, V(0, -0.3f, 0), S(0.24f, 0.62f, 0.24f));
            Part(armL, "Fore", MeshKit.Prism(6, 0.9f), plate, V(0, -0.7f, 0.02f), S(0.27f, 0.5f, 0.29f));
            Part(armL, "Shield", MeshKit.Prism(4, 2.4f, 45f), plate, V(-0.2f, -0.55f, 0.36f), S(0.42f, 1.25f, 0.16f));
            Part(armL, "ShieldRim", MeshKit.Prism(4, 2.4f, 45f), steel, V(-0.2f, -0.55f, 0.28f), S(0.5f, 1.35f, 0.1f));
            Part(armL, "Emblem", MeshKit.Crystal(6), glow, V(-0.2f, -0.42f, 0.47f), S(0.3f, 0.44f, 0.16f));
            var armR = Pivot(torso, "ArmR", V(0.62f, 0.3f, 0)); rig.ArmR = armR;
            Part(armR, "Upper", MeshKit.Prism(6, 0.8f), steel, V(0, -0.3f, 0), S(0.24f, 0.62f, 0.24f));
            Part(armR, "Fore", MeshKit.Prism(6, 0.9f), plate, V(0, -0.7f, 0.02f), S(0.27f, 0.5f, 0.29f));
            var weapon = Pivot(armR, "Weapon", V(0, -0.9f, 0.06f)); rig.Weapon = weapon;
            Part(weapon, "Blade", MeshKit.Blade(), plate, V(0, 0, 0.78f), S(0.22f, 1.55f, 0.55f), V(90, 0, 0));
            Part(weapon, "Guard", MeshKit.Cube(), red, V(0, 0, 0.05f), S(0.5f, 0.1f, 0.1f));
            var cape = Pivot(torso, "Cape", V(0, 0.42f, -0.36f)); rig.Cape = cape;
            Part(cape, "Cloth", MeshKit.Prism(4, 1.5f, 45f), red, V(0, -0.85f, -0.04f), S(0.95f, 1.7f, 0.06f));
            rig.Capture();
            return root;
        }

        /// <summary>Archère : silhouette capuchonnée, cape en lambeaux, yeux ambrés sous la capuche, arc à branches en lames, carquois.</summary>
        public static GameObject Archer()
        {
            var root = new GameObject("Archer");
            var t = root.transform;
            var rig = NewRig(root);
            var cloth = LitMaterial(H("41544A")); var leather = LitMaterial(H("5A4834")); var dark = LitMaterial(BlackCloth);
            var red = LitMaterial(Crimson); var skin = LitMaterial(Palette.Skin); var wood = LitMaterial(H("5B4330"));
            var amber = GlowMaterial(H("FFB347"));
            Legs(t, leather, dark, 0.22f, 0.3f);
            var torso = Pivot(t, "Torso", V(0, 1.46f, 0)); rig.Torso = torso;
            Part(torso, "Tunic", MeshKit.Prism(6, 1.15f), cloth, V(0, 0, 0), S(0.74f, 0.98f, 0.56f));
            Part(torso, "Belt", MeshKit.Prism(6, 1f), leather, V(0, -0.5f, 0), S(0.7f, 0.1f, 0.52f));
            Part(torso, "Scarf", MeshKit.Prism(6, 1.25f), red, V(0, 0.52f, 0.02f), S(0.58f, 0.18f, 0.5f));
            var head = Pivot(torso, "Head", V(0, 0.78f, 0)); rig.Head = head;
            Part(head, "Face", MeshKit.Sphere(5, 6), skin, V(0, -0.02f, 0.05f), S(0.4f, 0.44f, 0.4f));
            Part(head, "Hood", MeshKit.Cone(6), cloth, V(0, 0.2f, -0.03f), S(0.7f, 1.0f, 0.7f), V(-8, 0, 0));
            Part(head, "Shadow", MeshKit.Prism(6, 1f), dark, V(0, 0.02f, 0.14f), S(0.5f, 0.3f, 0.3f));
            foreach (float side in new[] { -1f, 1f }) Part(head, "Eye", MeshKit.Cube(), amber, V(side * 0.11f, 0.02f, 0.24f), S(0.1f, 0.04f, 0.04f));
            var cape = Pivot(torso, "Cape", V(0, 0.5f, -0.3f)); rig.Cape = cape;
            Part(cape, "Cloak", MeshKit.Prism(4, 1.6f, 45f), cloth, V(0, -0.8f, -0.05f), S(0.85f, 1.7f, 0.07f));
            for (int i = -1; i <= 1; i++) Part(cape, "Tatter", MeshKit.Blade(), cloth, V(i * 0.32f, -1.78f, -0.06f), S(0.3f, 0.55f, 0.2f), V(180, 0, 0));
            Part(torso, "Quiver", MeshKit.Prism(6, 1.1f), leather, V(0.2f, 0.05f, -0.36f), S(0.22f, 0.9f, 0.22f), V(0, 0, -16f));
            for (int i = 0; i < 3; i++) Part(torso, "Arrow", MeshKit.Cone(4), red, V(0.26f + i * 0.05f, 0.58f + i * 0.02f, -0.36f), S(0.08f, 0.24f, 0.08f), V(0, 0, -16f));
            var armR = Pivot(torso, "ArmR", V(0.44f, 0.42f, 0)); rig.ArmR = armR;
            Part(armR, "Upper", MeshKit.Prism(5, 0.8f), leather, V(0, -0.28f, 0), S(0.2f, 0.56f, 0.2f));
            Part(armR, "Fore", MeshKit.Prism(5, 0.9f), cloth, V(0, -0.62f, 0.02f), S(0.22f, 0.48f, 0.22f));
            var armL = Pivot(torso, "ArmL", V(-0.44f, 0.42f, 0)); rig.ArmL = armL;
            Part(armL, "Upper", MeshKit.Prism(5, 0.8f), leather, V(0, -0.28f, 0), S(0.2f, 0.56f, 0.2f));
            Part(armL, "Fore", MeshKit.Prism(5, 0.9f), cloth, V(0, -0.62f, 0.02f), S(0.22f, 0.48f, 0.22f));
            var bow = Pivot(armL, "Bow", V(-0.06f, -0.8f, 0.24f)); rig.Weapon = bow;
            Part(bow, "Grip", MeshKit.Cube(), leather, V(0, 0, 0), S(0.1f, 0.4f, 0.1f));
            foreach (float s in new[] { -1f, 1f })
                Part(bow, "Limb", MeshKit.Blade(), wood, V(0, s * 0.62f, 0.05f), S(0.14f, 1.0f, 0.55f), V(0, 0, s > 0 ? 180f + 10f : -10f));
            Part(bow, "String", MeshKit.Cube(), LitMaterial(H("D9D2C0")), V(0, 0, -0.14f), S(0.015f, 2.0f, 0.015f));
            rig.Capture();
            return root;
        }

        /// <summary>Mage : robe violette à liserés, chapeau tordu, yeux violets, bâton à cristal flottant entouré d'éclats.</summary>
        public static GameObject Mage()
        {
            var root = new GameObject("Mage");
            var t = root.transform;
            var rig = NewRig(root);
            var robe = LitMaterial(H("3B2C66")); var trim = LitMaterial(H("1A1530")); var gold = MetalMaterial(Gold);
            var skin = LitMaterial(Palette.Skin); var wood = LitMaterial(H("3D2E24")); var violet = GlowMaterial(Palette.Poison);
            Part(t, "Robe", MeshKit.Prism(6, 0.55f), robe, V(0, 0.86f, 0), S(1.08f, 1.7f, 0.86f));
            Part(t, "Hem", MeshKit.Prism(6, 1.02f), trim, V(0, 0.08f, 0), S(1.1f, 0.14f, 0.88f));
            var torso = Pivot(t, "Torso", V(0, 1.6f, 0)); rig.Torso = torso;
            Part(torso, "Mantle", MeshKit.Prism(6, 0.6f), trim, V(0, 0.08f, 0), S(0.98f, 0.42f, 0.74f));
            Part(torso, "Stud", MeshKit.Crystal(4), gold, V(0, 0.02f, 0.36f), S(0.16f, 0.2f, 0.12f));
            var head = Pivot(torso, "Head", V(0, 0.6f, 0)); rig.Head = head;
            Part(head, "Face", MeshKit.Sphere(5, 6), skin, V(0, -0.02f, 0.05f), S(0.4f, 0.44f, 0.4f));
            Part(head, "Brim", MeshKit.Prism(8, 1f), robe, V(0, 0.2f, 0), S(1.15f, 0.07f, 1.15f));
            Part(head, "Hat", MeshKit.Cone(6), robe, V(0, 0.82f, -0.04f), S(0.78f, 1.3f, 0.78f), V(-12, 0, 6));
            Part(head, "Band", MeshKit.Prism(8, 1f), gold, V(0, 0.27f, 0), S(0.8f, 0.08f, 0.8f));
            foreach (float side in new[] { -1f, 1f }) Part(head, "Eye", MeshKit.Cube(), violet, V(side * 0.1f, 0.03f, 0.22f), S(0.1f, 0.04f, 0.04f));
            var armR = Pivot(torso, "ArmR", V(0.5f, 0.2f, 0)); rig.ArmR = armR;
            Part(armR, "Sleeve", MeshKit.Prism(5, 1.4f), robe, V(0, -0.36f, 0), S(0.3f, 0.72f, 0.3f));
            Part(armR, "Hand", MeshKit.Sphere(4, 5), skin, V(0, -0.78f, 0.04f), S(0.15f, 0.15f, 0.15f));
            var staff = Pivot(armR, "Staff", V(0, -0.78f, 0.1f)); rig.Weapon = staff;
            Part(staff, "Shaft", MeshKit.Prism(5, 1f), wood, V(0, 0.5f, 0.24f), S(0.09f, 2.5f, 0.09f));
            Part(staff, "Orb", MeshKit.Crystal(6, 0.4f), violet, V(0, 2.0f, 0.24f), S(0.34f, 0.66f, 0.34f));
            foreach (float side in new[] { -1f, 1f }) Part(staff, "Claw", MeshKit.Blade(), wood, V(side * 0.16f, 1.78f, 0.24f), S(0.1f, 0.5f, 0.3f), V(0, 0, side * -20f));
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.1f;
                Part(staff, "Shard", MeshKit.Crystal(4), violet, V(Mathf.Cos(a) * 0.36f, 1.7f + i * 0.2f, 0.24f + Mathf.Sin(a) * 0.3f), S(0.09f, 0.22f, 0.09f), V(0, 0, 20f * i));
            }
            var armL = Pivot(torso, "ArmL", V(-0.5f, 0.2f, 0)); rig.ArmL = armL;
            Part(armL, "Sleeve", MeshKit.Prism(5, 1.4f), robe, V(0, -0.36f, 0), S(0.3f, 0.72f, 0.3f));
            Part(armL, "Book", MeshKit.Cube(), LitMaterial(Crimson), V(-0.02f, -0.8f, 0.2f), S(0.34f, 0.4f, 0.08f));
            Part(armL, "Page", MeshKit.Cube(), violet, V(-0.02f, -0.8f, 0.25f), S(0.2f, 0.26f, 0.02f));
            var cape = Pivot(torso, "Cape", V(0, 0.3f, -0.34f)); rig.Cape = cape;
            Part(cape, "Drape", MeshKit.Prism(4, 1.3f, 45f), trim, V(0, -0.8f, -0.03f), S(0.8f, 1.6f, 0.05f));
            rig.Capture();
            return root;
        }

        /// <summary>Soigneuse (le joueur) : robe ivoire et mantelet vert sombre, capuche, halo derrière la tête, bâton à croix de cristal.</summary>
        public static GameObject Healer()
        {
            var root = new GameObject("Healer");
            var t = root.transform;
            var rig = NewRig(root);
            var ivory = LitMaterial(H("D8E0DA")); var green = LitMaterial(H("2E5A48")); var gold = MetalMaterial(Gold);
            var skin = LitMaterial(Palette.Skin); var wood = LitMaterial(H("4A3A2A")); var heal = GlowMaterial(Palette.Heal);
            Part(t, "Robe", MeshKit.Prism(6, 0.52f), ivory, V(0, 0.84f, 0), S(1.02f, 1.68f, 0.82f));
            Part(t, "Hem", MeshKit.Prism(6, 1.02f), gold, V(0, 0.07f, 0), S(1.04f, 0.1f, 0.84f));
            var torso = Pivot(t, "Torso", V(0, 1.58f, 0)); rig.Torso = torso;
            Part(torso, "Mantle", MeshKit.Prism(6, 0.56f), green, V(0, 0.1f, 0), S(1.0f, 0.44f, 0.76f));
            Part(torso, "Sash", MeshKit.Prism(6, 1f), gold, V(0, -0.34f, 0), S(0.66f, 0.09f, 0.56f));
            foreach (float side in new[] { -1f, 1f }) Part(torso, "Stud", MeshKit.Crystal(4), gold, V(side * 0.46f, 0.3f, 0.1f), S(0.14f, 0.22f, 0.14f));
            var head = Pivot(torso, "Head", V(0, 0.62f, 0)); rig.Head = head;
            Part(head, "Face", MeshKit.Sphere(5, 6), skin, V(0, -0.02f, 0.06f), S(0.4f, 0.44f, 0.4f));
            Part(head, "Hood", MeshKit.Cone(6), ivory, V(0, 0.2f, -0.04f), S(0.74f, 0.98f, 0.74f), V(-6, 0, 0));
            Part(head, "Trim", MeshKit.Prism(6, 1f), gold, V(0, -0.14f, 0.1f), S(0.52f, 0.06f, 0.4f));
            Part(head, "Halo", MeshKit.Prism(14, 1f), heal, V(0, 0.1f, -0.34f), S(1.0f, 0.04f, 1.0f), V(90, 0, 0));
            var armR = Pivot(torso, "ArmR", V(0.5f, 0.24f, 0)); rig.ArmR = armR;
            Part(armR, "Sleeve", MeshKit.Prism(5, 1.4f), ivory, V(0, -0.36f, 0), S(0.3f, 0.72f, 0.3f));
            Part(armR, "Hand", MeshKit.Sphere(4, 5), skin, V(0, -0.78f, 0.04f), S(0.15f, 0.15f, 0.15f));
            var staff = Pivot(armR, "Staff", V(0, -0.78f, 0.1f)); rig.Weapon = staff;
            Part(staff, "Shaft", MeshKit.Prism(5, 1f), wood, V(0, 0.5f, 0.24f), S(0.09f, 2.6f, 0.09f));
            var cross = Pivot(staff, "Cross", V(0, 2.2f, 0.24f));
            Part(cross, "Vertical", MeshKit.Crystal(4, 0.5f), heal, Vector3.zero, S(0.2f, 0.9f, 0.2f));
            Part(cross, "Horizontal", MeshKit.Crystal(4, 0.5f), heal, Vector3.zero, S(0.2f, 0.62f, 0.2f), V(0, 0, 90f));
            Part(cross, "Ring", MeshKit.Prism(10, 1f), gold, Vector3.zero, S(0.62f, 0.04f, 0.62f), V(90, 0, 0));
            var armL = Pivot(torso, "ArmL", V(-0.5f, 0.24f, 0)); rig.ArmL = armL;
            Part(armL, "Sleeve", MeshKit.Prism(5, 1.4f), ivory, V(0, -0.36f, 0), S(0.3f, 0.72f, 0.3f));
            Part(armL, "Hand", MeshKit.Sphere(4, 5), skin, V(0, -0.78f, 0.04f), S(0.15f, 0.15f, 0.15f));
            var cape = Pivot(torso, "Cape", V(0, 0.3f, -0.34f)); rig.Cape = cape;
            Part(cape, "Drape", MeshKit.Prism(4, 1.4f, 45f), green, V(0, -0.8f, -0.03f), S(0.86f, 1.6f, 0.05f));
            rig.Capture();
            return root;
        }

        // ---- Boss ------------------------------------------------------------------------------

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

        private static void BossArm(Transform torso, UnitRig rig, bool left, Vector3 shoulder, Material stone, Material light, Material glow, int seed, float length = 1.5f)
        {
            float side = left ? -1f : 1f;
            var arm = Pivot(torso, left ? "ArmL" : "ArmR", shoulder);
            if (left) rig.ArmL = arm; else rig.ArmR = arm;
            Part(arm, "Shoulder", MeshKit.Rock(seed, 0.22f, 4, 7), light, V(0, 0.1f, 0), S(1.05f, 0.95f, 1.0f));
            Part(arm, "Upper", MeshKit.Prism(6, 0.85f), stone, V(0, -0.2f - length * 0.5f, 0), S(0.7f, length, 0.7f));
            Part(arm, "Fist", MeshKit.Rock(seed + 3, 0.18f, 4, 7), light, V(0, -0.35f - length, 0.1f), S(0.95f, 0.88f, 0.95f));
            Part(arm, "Rune", MeshKit.Blade(), glow, V(side * 0.02f, -0.75f, 0.37f), S(0.16f, 0.7f, 0.4f));
            Part(arm, "Knuckle", MeshKit.Cone(4), light, V(0, -0.2f - length, 0.5f), S(0.22f, 0.34f, 0.22f), V(90, 0, 0));
        }

        /// <summary>Golem Ancestral : masse de pierre taillée à grosses facettes, mousse, failles et cœur de cristal lumineux (cyan, orange en phase 2).</summary>
        public static GameObject Golem()
        {
            var root = new GameObject("Golem");
            var t = root.transform;
            var rig = NewRig(root);
            var stone = LitMaterial(Palette.Hex("5C5A78")); var dark = LitMaterial(Palette.Hex("34324A")); var light = LitMaterial(Palette.Hex("7C7899"));
            var moss = LitMaterial(Palette.Hex("3F5A44")); var glow = GlowMaterial(Palette.CoreCalm);
            foreach (float side in new[] { -1f, 1f })
            {
                Part(t, "Leg", MeshKit.Prism(6, 0.85f), dark, V(side * 0.6f, 0.6f, 0), S(0.9f, 1.2f, 0.9f));
                Part(t, "Foot", MeshKit.Rock(11, 0.2f, 3, 6), stone, V(side * 0.6f, 0.15f, 0.2f), S(1.0f, 0.4f, 1.3f));
            }
            var torso = Pivot(t, "Torso", V(0, 1.95f, 0)); rig.Torso = torso;
            Part(torso, "Chest", MeshKit.Rock(3, 0.22f, 4, 7), stone, V(0, 0, 0), S(2.3f, 2.1f, 1.65f));
            Part(torso, "Belly", MeshKit.Rock(5, 0.2f, 4, 7), dark, V(0, -0.6f, 0.25f), S(1.35f, 0.95f, 0.95f));
            Part(torso, "Slab", MeshKit.Wedge(), light, V(0, 0.25f, 0.62f), S(1.3f, 0.95f, 0.5f));
            Part(torso, "Moss", MeshKit.Rock(7, 0.3f, 3, 6), moss, V(-0.7f, 0.85f, 0.15f), S(0.9f, 0.4f, 0.8f));
            Part(torso, "Moss", MeshKit.Rock(8, 0.3f, 3, 6), moss, V(0.85f, 0.75f, -0.1f), S(0.7f, 0.35f, 0.7f));
            Part(torso, "Core", MeshKit.Crystal(6, 0.5f), glow, V(0, 0.12f, 0.8f), S(0.62f, 0.9f, 0.5f));
            foreach (float side in new[] { -1f, 1f })
                Part(torso, "Rune", MeshKit.Blade(), glow, V(side * 0.42f, 0.05f, 0.74f), S(0.09f, 1.0f, 0.4f), V(0, 0, side * 14f));
            var head = Pivot(torso, "Head", V(0, 1.25f, 0.05f)); rig.Head = head;
            Part(head, "Skull", MeshKit.Rock(9, 0.2f, 4, 7), stone, V(0, 0, 0), S(1.05f, 0.92f, 0.98f));
            Part(head, "Jaw", MeshKit.Wedge(), dark, V(0, -0.34f, 0.36f), S(0.72f, 0.28f, 0.42f), V(180, 0, 0));
            Part(head, "Brow", MeshKit.Prism(4, 1f, 45f), dark, V(0, 0.13f, 0.44f), S(0.85f, 0.1f, 0.16f));
            Part(head, "EyeL", MeshKit.Cube(), glow, V(-0.26f, 0.06f, 0.5f), S(0.22f, 0.11f, 0.08f));
            Part(head, "EyeR", MeshKit.Cube(), glow, V(0.26f, 0.06f, 0.5f), S(0.22f, 0.11f, 0.08f));
            for (int i = -1; i <= 1; i++)
                Part(head, "Crest", MeshKit.Cone(4), dark, V(i * 0.32f, 0.55f - Mathf.Abs(i) * 0.1f, -0.12f), S(0.2f, 0.8f - Mathf.Abs(i) * 0.15f, 0.2f), V(-12, 0, i * -14f));
            BossArm(torso, rig, true, V(-1.4f, 0.75f, 0), stone, light, glow, 21);
            BossArm(torso, rig, false, V(1.4f, 0.75f, 0), stone, light, glow, 31);
            rig.Capture();
            return root;
        }

        /// <summary>Reine des Marais : silhouette élancée, couronne de ronces, voile en lambeaux, racines en jupe, cœur toxique.</summary>
        public static GameObject SwampQueen()
        {
            var root = new GameObject("SwampQueen");
            var t = root.transform;
            var rig = NewRig(root);
            var moss = LitMaterial(H("3F5A3A")); var dark = LitMaterial(H("1E2E24")); var light = LitMaterial(H("6C8A52"));
            var bark = LitMaterial(H("3B2D22")); var bone = LitMaterial(Bone); var glow = GlowMaterial(H("9CFF5A"));
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f;
                var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                Part(t, "Root", MeshKit.Prism(5, 0.3f), i % 2 == 0 ? bark : dark, dir * 0.95f + V(0, 0.75f, 0), S(0.5f, 1.55f, 0.5f), V(dir.z * 26f, 0, -dir.x * 26f));
                Part(t, "RootTip", MeshKit.Cone(4), bark, dir * 1.45f + V(0, 0.18f, 0), S(0.22f, 0.6f, 0.22f), V(dir.z * 100f, 0, -dir.x * 100f));
            }
            Part(t, "Bulb", MeshKit.Rock(41, 0.25f, 4, 8), dark, V(0, 1.1f, 0), S(2.2f, 1.5f, 2.0f));
            var torso = Pivot(t, "Torso", V(0, 2.3f, 0)); rig.Torso = torso;
            Part(torso, "Ribs", MeshKit.Prism(6, 1.3f), moss, V(0, 0, 0), S(1.2f, 1.7f, 0.9f));
            Part(torso, "Chest", MeshKit.Wedge(), light, V(0, 0.15f, 0.3f), S(0.9f, 0.8f, 0.4f));
            Part(torso, "Core", MeshKit.Crystal(6, 0.5f), glow, V(0, 0.1f, 0.62f), S(0.5f, 0.78f, 0.4f));
            foreach (float side in new[] { -1f, 1f })
                Part(torso, "Rune", MeshKit.Blade(), glow, V(side * 0.32f, -0.2f, 0.5f), S(0.08f, 0.7f, 0.3f), V(0, 0, side * 16f));
            var head = Pivot(torso, "Head", V(0, 1.2f, 0.05f)); rig.Head = head;
            Part(head, "Skull", MeshKit.Rock(43, 0.15f, 4, 7), moss, V(0, 0, 0), S(0.85f, 0.95f, 0.82f));
            Part(head, "EyeL", MeshKit.Cube(), glow, V(-0.2f, 0.04f, 0.38f), S(0.2f, 0.09f, 0.08f), V(0, 0, -12f));
            Part(head, "EyeR", MeshKit.Cube(), glow, V(0.2f, 0.04f, 0.38f), S(0.2f, 0.09f, 0.08f), V(0, 0, 12f));
            for (int i = 0; i < 7; i++)
            {
                float a = (i - 3) * 0.5f;
                Part(head, "Thorn", MeshKit.Cone(4), bark, V(Mathf.Sin(a) * 0.5f, 0.45f + (3 - Mathf.Abs(i - 3)) * 0.13f, -0.05f), S(0.13f, 0.6f + (3 - Mathf.Abs(i - 3)) * 0.18f, 0.13f), V(0, 0, -a * 45f));
            }
            foreach (float side in new[] { -1f, 1f })
                Part(head, "Antler", MeshKit.Blade(), bone, V(side * 0.55f, 0.7f, -0.05f), S(0.2f, 1.0f, 0.5f), V(0, 0, side * -32f));
            var veil = Pivot(torso, "Cape", V(0, 0.6f, -0.45f)); rig.Cape = veil;
            Part(veil, "Veil", MeshKit.Prism(4, 1.7f, 45f), dark, V(0, -1.4f, -0.05f), S(2.0f, 2.8f, 0.07f));
            for (int i = -2; i <= 2; i++) Part(veil, "Tatter", MeshKit.Blade(), dark, V(i * 0.42f, -2.95f, -0.06f), S(0.4f, 0.8f, 0.2f), V(180, 0, i * 6f));
            foreach (bool left in new[] { true, false })
            {
                float side = left ? -1f : 1f;
                var arm = Pivot(torso, left ? "ArmL" : "ArmR", V(side * 0.85f, 0.55f, 0));
                if (left) rig.ArmL = arm; else rig.ArmR = arm;
                Part(arm, "Upper", MeshKit.Prism(5, 0.7f), moss, V(0, -0.65f, 0), S(0.36f, 1.3f, 0.36f));
                Part(arm, "Fore", MeshKit.Prism(5, 0.6f), bark, V(side * 0.05f, -1.5f, 0.1f), S(0.28f, 1.0f, 0.28f));
                for (int f = -1; f <= 1; f++) Part(arm, "Claw", MeshKit.Cone(4), bone, V(side * 0.05f + f * 0.14f, -2.15f, 0.2f), S(0.09f, 0.5f, 0.09f), V(-20f, 0, f * 14f));
                for (int s = 0; s < 4; s++) Part(arm, "Vine", MeshKit.Crystal(4), s % 2 == 0 ? light : moss, V(side * (0.2f + 0.05f * s), -0.4f - s * 0.45f, 0.24f), S(0.16f, 0.32f, 0.16f));
            }
            rig.Capture();
            return root;
        }

        /// <summary>Seigneur de Cendre : chevalier d'obsidienne fissuré de lave, casque à grandes cornes, épaulières hérissées, cape de cendre.</summary>
        public static GameObject AshLord()
        {
            var root = new GameObject("AshLord");
            var t = root.transform;
            var rig = NewRig(root);
            var obsidian = LitMaterial(H("2E2A36")); var dark = LitMaterial(H("1A171F")); var light = LitMaterial(H("4B4658"));
            var horn = LitMaterial(Bone); var ash = LitMaterial(H("4A2A2A")); var glow = GlowMaterial(H("FFA13D"));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(t, "Leg", MeshKit.Prism(6, 0.8f), dark, V(side * 0.58f, 0.62f, 0), S(0.78f, 1.25f, 0.8f));
                Part(t, "Greave", MeshKit.Wedge(), light, V(side * 0.58f, 0.75f, 0.42f), S(0.6f, 0.9f, 0.3f));
                Part(t, "Foot", MeshKit.Wedge(), dark, V(side * 0.58f, 0.13f, 0.22f), S(0.85f, 0.3f, 1.2f));
            }
            var torso = Pivot(t, "Torso", V(0, 2.05f, 0)); rig.Torso = torso;
            Part(torso, "Chest", MeshKit.Rock(51, 0.16f, 4, 7), obsidian, V(0, 0, 0), S(2.3f, 2.2f, 1.65f));
            Part(torso, "Plate", MeshKit.Wedge(), light, V(0, 0.3f, 0.6f), S(1.5f, 0.95f, 0.5f));
            Part(torso, "Belt", MeshKit.Prism(6, 1f), dark, V(0, -0.85f, 0), S(1.7f, 0.22f, 1.25f));
            Part(torso, "Core", MeshKit.Crystal(6, 0.5f), glow, V(0, 0.16f, 0.83f), S(0.7f, 1.0f, 0.5f));
            for (int i = 0; i < 3; i++)
                foreach (float side in new[] { -1f, 1f })
                    Part(torso, "Rune", MeshKit.Blade(), glow, V(side * (0.55f + 0.14f * i), -0.1f + 0.28f * i, 0.78f), S(0.08f, 0.6f, 0.4f), V(0, 0, side * 16f));
            var head = Pivot(torso, "Head", V(0, 1.35f, 0.05f)); rig.Head = head;
            Part(head, "Helm", MeshKit.Prism(8, 0.85f), dark, V(0, 0, 0), S(1.0f, 1.05f, 1.0f));
            Part(head, "Visor", MeshKit.Prism(4, 1f, 45f), obsidian, V(0, 0.02f, 0.4f), S(0.7f, 0.5f, 0.22f));
            Part(head, "EyeL", MeshKit.Cube(), glow, V(-0.24f, 0.08f, 0.53f), S(0.26f, 0.1f, 0.08f), V(0, 0, -10f));
            Part(head, "EyeR", MeshKit.Cube(), glow, V(0.24f, 0.08f, 0.53f), S(0.26f, 0.1f, 0.08f), V(0, 0, 10f));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(head, "Horn", MeshKit.Cone(4), horn, V(side * 0.55f, 0.75f, 0), S(0.3f, 1.4f, 0.3f), V(0, 0, side * -34f));
                Part(head, "HornLow", MeshKit.Cone(4), horn, V(side * 0.6f, 0.2f, 0.05f), S(0.2f, 0.85f, 0.2f), V(0, 0, side * -70f));
            }
            foreach (float side in new[] { -1f, 1f })
            {
                Part(torso, "Pauldron", MeshKit.Crystal(5, 0.35f), light, V(side * 1.35f, 0.85f, 0), S(1.5f, 1.0f, 1.4f));
                for (int i = 0; i < 3; i++)
                    Part(torso, "PauldronSpike", MeshKit.Cone(4), horn, V(side * (1.3f + 0.22f * i), 1.35f + 0.1f * i, 0), S(0.24f, 0.8f - 0.12f * i, 0.24f), V(0, 0, side * (-14f - 12f * i)));
            }
            BossArm(torso, rig, true, V(-1.55f, 0.55f, 0), obsidian, light, glow, 61, 1.6f);
            BossArm(torso, rig, false, V(1.55f, 0.55f, 0), obsidian, light, glow, 71, 1.6f);
            var cape = Pivot(torso, "Cape", V(0, 0.9f, -0.85f)); rig.Cape = cape;
            Part(cape, "Cloth", MeshKit.Prism(4, 1.5f, 45f), ash, V(0, -1.6f, -0.05f), S(2.5f, 3.3f, 0.08f));
            for (int i = -2; i <= 2; i++) Part(cape, "Tatter", MeshKit.Blade(), ash, V(i * 0.5f, -3.4f, -0.06f), S(0.45f, 0.9f, 0.2f), V(180, 0, i * 5f));
            rig.Capture();
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
