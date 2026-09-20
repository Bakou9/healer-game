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

        /// <summary>
        /// Apparence d'un héros selon son équipement (D-061) : palier et palette de l'arme et de l'armure, venus du cœur
        /// (Healer.Combat.Progress.AppearanceSet). Sans données, la version de base est dessinée. Les modèles sont MODULAIRES :
        /// un corps commun (jambes, torse, tête, bras) et des pièces d'équipement (arme, armure) qui changent de forme et de
        /// couleur selon le palier — c'est la même mécanique que pour des pièces importées plus tard.
        /// </summary>
        public sealed class Look
        {
            public readonly int WeaponTier, ArmorTier;
            /// <summary>Modèle importé de l'arme, ou null (pièce dessinée par le code).</summary>
            public readonly Healer.Combat.Progress.AppearanceModel? WeaponModel;
            private readonly IReadOnlyDictionary<string, string>? _weapon, _armor;

            public Look(Healer.Combat.Progress.AppearanceSet? set)
            {
                WeaponTier = set?.TierOf("weapon") ?? 0;
                ArmorTier = set?.TierOf("armor") ?? 0;
                if (set != null && set.Parts.TryGetValue("weapon", out var w)) { _weapon = w.Palette; WeaponModel = w.Model; }
                if (set != null && set.Parts.TryGetValue("armor", out var a)) _armor = a.Palette;
            }

            /// <summary>Couleur de l'arme pour un rôle (primary, secondary, accent, glow, trim).</summary>
            public Color W(string role, string fallback) => Pick(_weapon, role, fallback);
            /// <summary>Couleur de l'armure pour un rôle.</summary>
            public Color A(string role, string fallback) => Pick(_armor, role, fallback);

            private static Color Pick(IReadOnlyDictionary<string, string>? palette, string role, string fallback) =>
                Palette.Hex(palette != null && palette.TryGetValue(role, out var hex) ? hex : fallback);
        }

        /// <summary>
        /// Remplace l'arme dessinée par le code par un modèle importé (D-062) si les données en désignent un et qu'il existe ; sinon la
        /// version dessinée reste (jamais de héros sans arme). Le modèle est rattaché au pivot d'arme : il suit donc l'animation.
        /// </summary>
        private static void ApplyImportedWeapon(UnitRig rig, Look look)
        {
            var model = look.WeaponModel;
            if (model == null || rig.Weapon == null) return;
            var prefab = Resources.Load<GameObject>(model.Path);
            if (prefab == null)
            {
                Debug.LogWarning("[Healer] modèle introuvable, version dessinée conservée : " + model.Path);
                return;
            }
            // Destruction IMMÉDIATE : la scène liste ensuite les rendus du héros, elle ne doit pas y trouver des objets détruits en fin d'image.
            var keep = new HashSet<string>(model.Keep);
            for (int i = rig.Weapon.childCount - 1; i >= 0; i--)
                if (!keep.Contains(rig.Weapon.GetChild(i).name)) Object.DestroyImmediate(rig.Weapon.GetChild(i).gameObject);
            var instance = Object.Instantiate(prefab, rig.Weapon, false);
            instance.name = "ImportedWeapon";
            instance.transform.localPosition = Vector3.zero;
            var native = BoundsIn(instance.transform, instance.transform);
            Debug.Log($"[Healer] modèle natif {model.Path} : taille {native.size.x:0.###} x {native.size.y:0.###} x {native.size.z:0.###}, de {native.min.x:0.###},{native.min.y:0.###},{native.min.z:0.###} à {native.max.x:0.###},{native.max.y:0.###},{native.max.z:0.###}");
            instance.transform.localEulerAngles = new Vector3(model.Euler[0], model.Euler[1], model.Euler[2]);
            foreach (var collider in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            // Taille normalisée : l'unité du fichier (mètres, centimètres…) ne compte pas, seule la taille voulue compte.
            instance.transform.localScale = Vector3.one;
            var measured = BoundsIn(instance.transform, rig.Weapon);
            float longest = Mathf.Max(measured.size.x, Mathf.Max(measured.size.y, measured.size.z));
            if (longest > 0.0001f) instance.transform.localScale = Vector3.one * (model.Size / 100f / longest);
            // Recentrage : l'origine du fichier ne compte pas, le CENTRE du modèle va où les données le demandent.
            var scaled = BoundsIn(instance.transform, rig.Weapon);
            instance.transform.localPosition = new Vector3(model.Offset[0], model.Offset[1], model.Offset[2]) / 100f - scaled.center;
            Debug.Log("[Healer] modèle importé : " + model.Path);
        }

        /// <summary>Boîte englobante d'un modèle exprimée dans le repère `space` (mesures des maillages : indépendante de l'unité du fichier et de l'échelle des parents).</summary>
        private static Bounds BoundsIn(Transform root, Transform space)
        {
            bool any = false;
            var bounds = new Bounds();
            void Add(Mesh? mesh, Transform node)
            {
                if (mesh == null) return;
                var toRoot = space.worldToLocalMatrix * node.localToWorldMatrix;
                var b = mesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = toRoot.MultiplyPoint3x4(corner);
                    if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; } else bounds.Encapsulate(p);
                }
            }
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>()) Add(mf.sharedMesh, mf.transform);
            foreach (var sk in root.GetComponentsInChildren<SkinnedMeshRenderer>()) Add(sk.sharedMesh, sk.transform);
            return bounds;
        }

        /// <summary>Garde : armure de plaques noircies ; corne, crête et cape sang aux paliers supérieurs ; épée et bouclier runiques.</summary>
        public static GameObject Tank(Look? look = null)
        {
            look ??= new Look(null);
            int at = look.ArmorTier, wt = look.WeaponTier;
            var root = new GameObject("Tank");
            var t = root.transform;
            var rig = NewRig(root);
            var steel = MetalMaterial(look.A("secondary", "59606E")); var plate = MetalMaterial(look.A("primary", "8A93A6")); var cloth = LitMaterial(BlackCloth);
            var red = LitMaterial(look.A("accent", "8E2B34")); var bone = LitMaterial(Bone); var trim = MetalMaterial(look.A("trim", "B8924A"));
            var visorGlow = GlowMaterial(look.A("glow", "7CC8FF"));
            var bladeMat = MetalMaterial(look.W("primary", "8A93A6")); var shieldMat = MetalMaterial(look.W("primary", "8A93A6")); var rimMat = MetalMaterial(look.W("secondary", "59606E"));
            var weaponGlow = GlowMaterial(look.W("glow", "7CC8FF")); var weaponRed = LitMaterial(look.W("accent", "8E2B34"));
            Legs(t, steel, cloth);
            Part(t, "Skirt", MeshKit.Prism(6, 1.25f), cloth, V(0, 0.98f, 0), S(0.95f, 0.5f, 0.8f));
            var torso = Pivot(t, "Torso", V(0, 1.52f, 0)); rig.Torso = torso;
            Part(torso, "Chest", MeshKit.Prism(6, 1.3f), steel, V(0, 0, 0), S(0.9f, 0.92f, 0.72f));
            Part(torso, "Plate", MeshKit.Wedge(), plate, V(0, 0.08f, 0.3f), S(0.72f, 0.7f, 0.32f));
            Part(torso, "Belt", MeshKit.Prism(6, 1f), red, V(0, -0.5f, 0), S(0.86f, 0.12f, 0.7f));
            var head = Pivot(torso, "Head", V(0, 0.72f, 0)); rig.Head = head;
            Part(head, "Helm", MeshKit.Prism(8, 0.9f), steel, V(0, 0.05f, 0), S(0.6f, 0.62f, 0.6f));
            Part(head, "Visor", MeshKit.Cube(), visorGlow, V(0, 0.05f, 0.31f), S(0.4f + 0.06f * at, 0.06f + 0.02f * at, 0.05f));
            float pauldron = 0.5f + 0.1f * at;
            foreach (float side in new[] { -1f, 1f })
            {
                Part(torso, "Pauldron", MeshKit.Crystal(5, 0.35f), plate, V(side * 0.62f, 0.34f, 0), S(pauldron + 0.12f, pauldron, pauldron + 0.14f));
                if (at >= 1)
                {
                    Part(head, "Horn", MeshKit.Cone(4), bone, V(side * 0.3f, 0.32f, 0), S(0.13f, 0.5f + 0.15f * at, 0.13f), V(0, 0, side * -34f));
                    Part(torso, "PauldronSpike", MeshKit.Cone(4), red, V(side * 0.72f, 0.72f, 0), S(0.16f, 0.4f + 0.12f * at, 0.16f), V(0, 0, side * -22f));
                }
            }
            if (at >= 1) Part(head, "Crest", MeshKit.Blade(), red, V(0, 0.42f, -0.08f), S(0.55f, 0.42f, 0.5f), V(0, 90, 0));
            if (at >= 2)
            {
                Part(torso, "TrimBand", MeshKit.Prism(6, 1.3f), trim, V(0, 0.42f, 0), S(0.95f, 0.06f, 0.77f));
                Part(torso, "PlateRune", MeshKit.Crystal(4), visorGlow, V(0, 0.1f, 0.48f), S(0.16f, 0.42f, 0.1f));
                Part(head, "HelmRing", MeshKit.Prism(8, 1f), trim, V(0, 0.27f, 0), S(0.62f, 0.05f, 0.62f));
            }
            var armL = Pivot(torso, "ArmL", V(-0.62f, 0.3f, 0)); rig.ArmL = armL;
            Part(armL, "Upper", MeshKit.Prism(6, 0.8f), steel, V(0, -0.3f, 0), S(0.24f, 0.62f, 0.24f));
            Part(armL, "Fore", MeshKit.Prism(6, 0.9f), plate, V(0, -0.7f, 0.02f), S(0.27f, 0.5f, 0.29f));
            Part(armL, "Shield", MeshKit.Prism(4, 2.4f, 45f), shieldMat, V(-0.2f, -0.55f, 0.36f), S(0.42f + 0.05f * wt, 1.25f + 0.1f * wt, 0.16f));
            Part(armL, "ShieldRim", MeshKit.Prism(4, 2.4f, 45f), rimMat, V(-0.2f, -0.55f, 0.28f), S(0.5f + 0.05f * wt, 1.35f + 0.1f * wt, 0.1f));
            Part(armL, "Emblem", MeshKit.Crystal(6), weaponGlow, V(-0.2f, -0.42f, 0.47f), S(0.3f, 0.44f, 0.16f));
            if (wt >= 2) foreach (float dx in new[] { -0.55f, 0.15f }) Part(armL, "ShieldSpike", MeshKit.Cone(4), weaponRed, V(dx, -0.1f, 0.4f), S(0.14f, 0.36f, 0.14f), V(90, 0, 0));
            var armR = Pivot(torso, "ArmR", V(0.62f, 0.3f, 0)); rig.ArmR = armR;
            Part(armR, "Upper", MeshKit.Prism(6, 0.8f), steel, V(0, -0.3f, 0), S(0.24f, 0.62f, 0.24f));
            Part(armR, "Fore", MeshKit.Prism(6, 0.9f), plate, V(0, -0.7f, 0.02f), S(0.27f, 0.5f, 0.29f));
            var weapon = Pivot(armR, "Weapon", V(0, -0.9f, 0.06f)); rig.Weapon = weapon;
            float length = 1.2f + 0.3f * wt;
            Part(weapon, "Blade", MeshKit.Blade(), bladeMat, V(0, 0, 0.3f + length * 0.5f), S(0.2f + 0.05f * wt, length, 0.55f), V(90, 0, 0));
            if (wt >= 1) Part(weapon, "BladeRune", MeshKit.Blade(), weaponGlow, V(0, 0, 0.3f + length * 0.5f), S(0.06f, length * 0.85f, 0.7f), V(90, 0, 0));
            Part(weapon, "Guard", MeshKit.Cube(), weaponRed, V(0, 0, 0.05f), S(0.5f + 0.1f * wt, 0.1f, 0.1f));
            if (wt >= 2) foreach (float side in new[] { -1f, 1f }) Part(weapon, "GuardSpike", MeshKit.Cone(4), weaponRed, V(side * 0.35f, 0, 0.05f), S(0.1f, 0.32f, 0.1f), V(0, 0, side * -90f));
            var cape = Pivot(torso, "Cape", V(0, 0.42f, -0.36f)); rig.Cape = cape;
            Part(cape, "Cloth", MeshKit.Prism(4, 1.5f, 45f), at >= 1 ? red : cloth, V(0, -0.5f - 0.2f * at, -0.04f), S(0.95f, 1.0f + 0.35f * at, 0.06f));
            if (at >= 2) for (int i = -1; i <= 1; i++) Part(cape, "Tatter", MeshKit.Blade(), red, V(i * 0.32f, -1.55f, -0.05f), S(0.3f, 0.5f, 0.2f), V(180, 0, 0));
            ApplyImportedWeapon(rig, look);
            rig.Capture();
            return root;
        }

        /// <summary>Archère : silhouette capuchonnée ; foulard, cape en lambeaux puis mantelet hérissé aux paliers supérieurs ; arc de plus en plus grand et lumineux.</summary>
        public static GameObject Archer(Look? look = null)
        {
            look ??= new Look(null);
            int at = look.ArmorTier, wt = look.WeaponTier;
            var root = new GameObject("Archer");
            var t = root.transform;
            var rig = NewRig(root);
            var cloth = LitMaterial(look.A("primary", "41544A")); var leather = LitMaterial(look.A("trim", "5A4834")); var dark = LitMaterial(BlackCloth);
            var red = LitMaterial(look.A("accent", "8E2B34")); var skin = LitMaterial(Palette.Skin); var wood = LitMaterial(look.W("primary", "5B4330"));
            var eyes = GlowMaterial(look.A("glow", "FFB347")); var stringGlow = GlowMaterial(look.W("glow", "FFB347")); var woodDark = LitMaterial(look.W("secondary", "3D2E24"));
            Legs(t, leather, dark, 0.22f, 0.3f);
            var torso = Pivot(t, "Torso", V(0, 1.46f, 0)); rig.Torso = torso;
            Part(torso, "Tunic", MeshKit.Prism(6, 1.15f), cloth, V(0, 0, 0), S(0.74f, 0.98f, 0.56f));
            Part(torso, "Belt", MeshKit.Prism(6, 1f), leather, V(0, -0.5f, 0), S(0.7f, 0.1f, 0.52f));
            if (at >= 1) Part(torso, "Scarf", MeshKit.Prism(6, 1.25f), red, V(0, 0.52f, 0.02f), S(0.58f, 0.18f, 0.5f));
            if (at >= 2) foreach (float side in new[] { -1f, 1f })
                {
                    Part(torso, "Mantle", MeshKit.Wedge(), cloth, V(side * 0.42f, 0.5f, 0), S(0.42f, 0.28f, 0.5f), V(0, 0, side * -18f));
                    Part(torso, "MantleSpike", MeshKit.Cone(4), leather, V(side * 0.52f, 0.7f, 0), S(0.1f, 0.4f, 0.1f), V(0, 0, side * -30f));
                }
            var head = Pivot(torso, "Head", V(0, 0.78f, 0)); rig.Head = head;
            Part(head, "Face", MeshKit.Sphere(5, 6), skin, V(0, -0.02f, 0.05f), S(0.4f, 0.44f, 0.4f));
            Part(head, "Hood", MeshKit.Cone(6), cloth, V(0, 0.2f, -0.03f), S(0.7f, 1.0f + 0.12f * at, 0.7f), V(-8, 0, 0));
            Part(head, "Shadow", MeshKit.Prism(6, 1f), dark, V(0, 0.02f, 0.14f), S(0.5f, 0.3f, 0.3f));
            foreach (float side in new[] { -1f, 1f }) Part(head, "Eye", MeshKit.Cube(), eyes, V(side * 0.11f, 0.02f, 0.24f), S(0.1f + 0.02f * at, 0.04f, 0.04f));
            var cape = Pivot(torso, "Cape", V(0, 0.5f, -0.3f)); rig.Cape = cape;
            Part(cape, "Cloak", MeshKit.Prism(4, 1.6f, 45f), cloth, V(0, -0.8f, -0.05f), S(0.85f, 1.7f, 0.07f));
            if (at >= 1) for (int i = -1; i <= 1; i++) Part(cape, "Tatter", MeshKit.Blade(), at >= 2 ? red : cloth, V(i * 0.32f, -1.78f, -0.06f), S(0.3f, 0.55f, 0.2f), V(180, 0, 0));
            Part(torso, "Quiver", MeshKit.Prism(6, 1.1f), leather, V(0.2f, 0.05f, -0.36f), S(0.22f, 0.9f, 0.22f), V(0, 0, -16f));
            for (int i = 0; i < 2 + wt; i++) Part(torso, "Arrow", MeshKit.Cone(4), red, V(0.24f + i * 0.04f, 0.58f + i * 0.02f, -0.36f), S(0.08f, 0.24f, 0.08f), V(0, 0, -16f));
            var armR = Pivot(torso, "ArmR", V(0.44f, 0.42f, 0)); rig.ArmR = armR;
            Part(armR, "Upper", MeshKit.Prism(5, 0.8f), leather, V(0, -0.28f, 0), S(0.2f, 0.56f, 0.2f));
            Part(armR, "Fore", MeshKit.Prism(5, 0.9f), cloth, V(0, -0.62f, 0.02f), S(0.22f, 0.48f, 0.22f));
            var armL = Pivot(torso, "ArmL", V(-0.44f, 0.42f, 0)); rig.ArmL = armL;
            Part(armL, "Upper", MeshKit.Prism(5, 0.8f), leather, V(0, -0.28f, 0), S(0.2f, 0.56f, 0.2f));
            Part(armL, "Fore", MeshKit.Prism(5, 0.9f), cloth, V(0, -0.62f, 0.02f), S(0.22f, 0.48f, 0.22f));
            var bow = Pivot(armL, "Bow", V(-0.06f, -0.8f, 0.24f)); rig.Weapon = bow;
            float limb = 1.0f + 0.2f * wt;
            Part(bow, "Grip", MeshKit.Cube(), leather, V(0, 0, 0), S(0.1f, 0.4f, 0.1f));
            foreach (float s in new[] { -1f, 1f })
            {
                Part(bow, "Limb", MeshKit.Blade(), wt >= 2 ? woodDark : wood, V(0, s * limb * 0.62f, 0.05f), S(0.14f + 0.03f * wt, limb, 0.55f), V(0, 0, s > 0 ? 180f + 10f : -10f));
                if (wt >= 2) Part(bow, "Tip", MeshKit.Crystal(4), stringGlow, V(0, s * limb * 1.12f, 0.05f), S(0.1f, 0.24f, 0.1f));
            }
            Part(bow, "String", MeshKit.Cube(), wt >= 1 ? stringGlow : LitMaterial(H("D9D2C0")), V(0, 0, -0.14f), S(0.015f + 0.008f * wt, 2.0f + 0.4f * wt, 0.015f));
            ApplyImportedWeapon(rig, look);
            rig.Capture();
            return root;
        }

        /// <summary>Mage : robe violette ; liserés dorés puis runes lumineuses et cristal de chapeau aux paliers supérieurs ; bâton dont le cristal grossit et s'entoure d'éclats.</summary>
        public static GameObject Mage(Look? look = null)
        {
            look ??= new Look(null);
            int at = look.ArmorTier, wt = look.WeaponTier;
            var root = new GameObject("Mage");
            var t = root.transform;
            var rig = NewRig(root);
            var robe = LitMaterial(look.A("primary", "3B2C66")); var trim = LitMaterial(look.A("secondary", "1A1530")); var gold = MetalMaterial(look.A("trim", "B8924A"));
            var skin = LitMaterial(Palette.Skin); var wood = LitMaterial(look.W("primary", "3D2E24")); var orbGlow = GlowMaterial(look.W("glow", "C78CFF"));
            var runeGlow = GlowMaterial(look.A("glow", "C78CFF")); var red = LitMaterial(look.A("accent", "8E2B34"));
            Part(t, "Robe", MeshKit.Prism(6, 0.55f), robe, V(0, 0.86f, 0), S(1.08f, 1.7f, 0.86f));
            Part(t, "Hem", MeshKit.Prism(6, 1.02f), at >= 1 ? gold : trim, V(0, 0.08f, 0), S(1.1f, 0.14f, 0.88f));
            if (at >= 2) for (int i = 0; i < 5; i++)
                {
                    float ang = (i - 2) * 0.5f;
                    Part(t, "HemRune", MeshKit.Blade(), runeGlow, V(Mathf.Sin(ang) * 0.5f, 0.45f, Mathf.Cos(ang) * 0.42f), S(0.07f, 0.5f, 0.3f), V(0, ang * -57f, 0));
                }
            var torso = Pivot(t, "Torso", V(0, 1.6f, 0)); rig.Torso = torso;
            Part(torso, "Mantle", MeshKit.Prism(6, 0.6f), trim, V(0, 0.08f, 0), S(0.98f, 0.42f, 0.74f));
            Part(torso, "Stud", MeshKit.Crystal(4), gold, V(0, 0.02f, 0.36f), S(0.16f, 0.2f, 0.12f));
            if (at >= 1) foreach (float side in new[] { -1f, 1f }) Part(torso, "ShoulderStud", MeshKit.Crystal(4), gold, V(side * 0.46f, 0.26f, 0.08f), S(0.14f, 0.22f, 0.14f));
            var head = Pivot(torso, "Head", V(0, 0.6f, 0)); rig.Head = head;
            Part(head, "Face", MeshKit.Sphere(5, 6), skin, V(0, -0.02f, 0.05f), S(0.4f, 0.44f, 0.4f));
            Part(head, "Brim", MeshKit.Prism(8, 1f), robe, V(0, 0.2f, 0), S(1.15f, 0.07f, 1.15f));
            Part(head, "Hat", MeshKit.Cone(6), robe, V(0, 0.82f + 0.08f * at, -0.04f), S(0.78f, 1.3f + 0.16f * at, 0.78f), V(-12, 0, 6));
            Part(head, "Band", MeshKit.Prism(8, 1f), at >= 1 ? gold : trim, V(0, 0.27f, 0), S(0.8f, 0.08f, 0.8f));
            if (at >= 2) Part(head, "HatCrystal", MeshKit.Crystal(4), runeGlow, V(0.12f, 0.36f, 0.4f), S(0.12f, 0.24f, 0.12f));
            foreach (float side in new[] { -1f, 1f }) Part(head, "Eye", MeshKit.Cube(), runeGlow, V(side * 0.1f, 0.03f, 0.22f), S(0.1f, 0.04f, 0.04f));
            var armR = Pivot(torso, "ArmR", V(0.5f, 0.2f, 0)); rig.ArmR = armR;
            Part(armR, "Sleeve", MeshKit.Prism(5, 1.4f), robe, V(0, -0.36f, 0), S(0.3f, 0.72f, 0.3f));
            Part(armR, "Hand", MeshKit.Sphere(4, 5), skin, V(0, -0.78f, 0.04f), S(0.15f, 0.15f, 0.15f));
            var staff = Pivot(armR, "Staff", V(0, -0.78f, 0.1f)); rig.Weapon = staff;
            Part(staff, "Shaft", MeshKit.Prism(5, 1f), wood, V(0, 0.5f, 0.24f), S(0.09f, 2.5f, 0.09f));
            float orb = 1f + 0.3f * wt;
            Part(staff, "Orb", MeshKit.Crystal(6, 0.4f), orbGlow, V(0, 2.0f, 0.24f), S(0.3f * orb, 0.6f * orb, 0.3f * orb));
            if (wt >= 1) foreach (float side in new[] { -1f, 1f }) Part(staff, "Claw", MeshKit.Blade(), wood, V(side * 0.16f * orb, 1.78f, 0.24f), S(0.1f, 0.5f, 0.3f), V(0, 0, side * -20f));
            for (int i = 0; i < wt * 2 - (wt > 0 ? 1 : 0); i++)
            {
                float ang = i * 2.1f;
                Part(staff, "Shard", MeshKit.Crystal(4), orbGlow, V(Mathf.Cos(ang) * 0.4f, 1.7f + i * 0.16f, 0.24f + Mathf.Sin(ang) * 0.3f), S(0.09f, 0.22f, 0.09f), V(0, 0, 20f * i));
            }
            var armL = Pivot(torso, "ArmL", V(-0.5f, 0.2f, 0)); rig.ArmL = armL;
            Part(armL, "Sleeve", MeshKit.Prism(5, 1.4f), robe, V(0, -0.36f, 0), S(0.3f, 0.72f, 0.3f));
            Part(armL, "Book", MeshKit.Cube(), red, V(-0.02f, -0.8f, 0.2f), S(0.34f, 0.4f, 0.08f));
            Part(armL, "Page", MeshKit.Cube(), runeGlow, V(-0.02f, -0.8f, 0.25f), S(0.2f, 0.26f, 0.02f));
            var cape = Pivot(torso, "Cape", V(0, 0.3f, -0.34f)); rig.Cape = cape;
            Part(cape, "Drape", MeshKit.Prism(4, 1.3f, 45f), trim, V(0, -0.8f, -0.03f), S(0.8f, 1.6f, 0.05f));
            ApplyImportedWeapon(rig, look);
            rig.Capture();
            return root;
        }

        /// <summary>Soigneuse (le joueur) : robe ivoire ; ceinture et mantelet aux paliers supérieurs, puis halo et ailes de lumière ; bâton dont la croix grossit et s'entoure d'un anneau.</summary>
        public static GameObject Healer(Look? look = null)
        {
            look ??= new Look(null);
            int at = look.ArmorTier, wt = look.WeaponTier;
            var root = new GameObject("Healer");
            var t = root.transform;
            var rig = NewRig(root);
            var ivory = LitMaterial(look.A("primary", "D8E0DA")); var green = LitMaterial(look.A("secondary", "2E5A48")); var gold = MetalMaterial(look.A("trim", "B8924A"));
            var skin = LitMaterial(Palette.Skin); var wood = LitMaterial(look.W("primary", "4A3A2A")); var armorGlow = GlowMaterial(look.A("glow", "7CFFB2"));
            var crossGlow = GlowMaterial(look.W("glow", "7CFFB2")); var weaponGold = MetalMaterial(look.W("trim", "B8924A"));
            Part(t, "Robe", MeshKit.Prism(6, 0.52f), ivory, V(0, 0.84f, 0), S(1.02f, 1.68f, 0.82f));
            Part(t, "Hem", MeshKit.Prism(6, 1.02f), gold, V(0, 0.07f, 0), S(1.04f, 0.1f, 0.84f));
            var torso = Pivot(t, "Torso", V(0, 1.58f, 0)); rig.Torso = torso;
            Part(torso, "Mantle", MeshKit.Prism(6, 0.56f), green, V(0, 0.1f, 0), S(1.0f, 0.44f, 0.76f));
            if (at >= 1)
            {
                Part(torso, "Sash", MeshKit.Prism(6, 1f), gold, V(0, -0.34f, 0), S(0.66f, 0.09f, 0.56f));
                foreach (float side in new[] { -1f, 1f }) Part(torso, "Stud", MeshKit.Crystal(4), gold, V(side * 0.46f, 0.3f, 0.1f), S(0.14f, 0.22f, 0.14f));
            }
            var head = Pivot(torso, "Head", V(0, 0.62f, 0)); rig.Head = head;
            Part(head, "Face", MeshKit.Sphere(5, 6), skin, V(0, -0.02f, 0.06f), S(0.4f, 0.44f, 0.4f));
            Part(head, "Hood", MeshKit.Cone(6), ivory, V(0, 0.2f, -0.04f), S(0.74f, 0.98f, 0.74f), V(-6, 0, 0));
            if (at >= 1) Part(head, "Trim", MeshKit.Prism(6, 1f), gold, V(0, -0.14f, 0.1f), S(0.52f, 0.06f, 0.4f));
            if (at >= 2) Part(head, "Halo", MeshKit.Prism(14, 1f), armorGlow, V(0, 0.1f, -0.34f), S(1.0f, 0.04f, 1.0f), V(90, 0, 0));
            if (at >= 2) foreach (float side in new[] { -1f, 1f })
                    for (int i = 0; i < 3; i++)
                        Part(torso, "Wing", MeshKit.Blade(), armorGlow, V(side * (0.5f + 0.12f * i), 0.3f - 0.18f * i, -0.42f), S(0.14f, 0.9f - 0.15f * i, 0.3f), V(20, 0, side * (-50f - 14f * i)));
            var armR = Pivot(torso, "ArmR", V(0.5f, 0.24f, 0)); rig.ArmR = armR;
            Part(armR, "Sleeve", MeshKit.Prism(5, 1.4f), ivory, V(0, -0.36f, 0), S(0.3f, 0.72f, 0.3f));
            Part(armR, "Hand", MeshKit.Sphere(4, 5), skin, V(0, -0.78f, 0.04f), S(0.15f, 0.15f, 0.15f));
            var staff = Pivot(armR, "Staff", V(0, -0.78f, 0.1f)); rig.Weapon = staff;
            Part(staff, "Shaft", MeshKit.Prism(5, 1f), wt >= 1 ? weaponGold : wood, V(0, 0.5f, 0.24f), S(0.09f, 2.6f, 0.09f));
            var cross = Pivot(staff, "Cross", V(0, 2.2f, 0.24f));
            float size = 1f + 0.25f * wt;
            Part(cross, "Vertical", MeshKit.Crystal(4, 0.5f), crossGlow, Vector3.zero, S(0.2f * size, 0.9f * size, 0.2f * size));
            Part(cross, "Horizontal", MeshKit.Crystal(4, 0.5f), crossGlow, Vector3.zero, S(0.2f * size, 0.62f * size, 0.2f * size), V(0, 0, 90f));
            if (wt >= 1) Part(cross, "Ring", MeshKit.Prism(10, 1f), weaponGold, Vector3.zero, S(0.62f * size, 0.04f, 0.62f * size), V(90, 0, 0));
            if (wt >= 2) Part(cross, "RingOuter", MeshKit.Prism(12, 1f), crossGlow, Vector3.zero, S(1.1f, 0.03f, 1.1f), V(90, 0, 0));
            var armL = Pivot(torso, "ArmL", V(-0.5f, 0.24f, 0)); rig.ArmL = armL;
            Part(armL, "Sleeve", MeshKit.Prism(5, 1.4f), ivory, V(0, -0.36f, 0), S(0.3f, 0.72f, 0.3f));
            Part(armL, "Hand", MeshKit.Sphere(4, 5), skin, V(0, -0.78f, 0.04f), S(0.15f, 0.15f, 0.15f));
            var cape = Pivot(torso, "Cape", V(0, 0.3f, -0.34f)); rig.Cape = cape;
            Part(cape, "Drape", MeshKit.Prism(4, 1.4f, 45f), green, V(0, -0.8f, -0.03f), S(0.86f, 1.6f, 0.05f));
            ApplyImportedWeapon(rig, look);
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

        public static GameObject ForCharacter(string id, string role, Healer.Combat.Progress.AppearanceSet? appearance = null)
        {
            var look = new Look(appearance);
            switch (id)
            {
                case "tank": return Tank(look);
                case "dps1": return Archer(look);
                case "dps2": return Mage(look);
                case "healer": return Healer(look);
            }
            return role == "tank" ? Tank(look) : role == "healer" ? Healer(look) : Archer(look);
        }

        public static int TriangleCount(GameObject model)
        {
            int total = 0;
            foreach (var f in model.GetComponentsInChildren<MeshFilter>()) total += MeshKit.TriangleCount(f.sharedMesh);
            return total;
        }
    }
}
