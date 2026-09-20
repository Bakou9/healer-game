using System.Collections.Generic;
using System.Linq;
using Healer.Combat;
using Healer.Combat.Presentation;
using Healer.Ui;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Scène 3D du combat : décor, Golem, alliés, animations, particules, chiffres flottants et secousse de
    /// caméra. Ne contient aucune règle : elle lit l'état de Battle et réagit à ses événements (Observer).
    /// Les modèles sont alignés sur les cartes de l'interface (ScreenMap).
    /// </summary>
    public sealed class BattleStage : MonoBehaviour
    {
        private const float BossDepth = 35f;
        private const float AllyDepth = 33f;
        private const float BossFeetY = 452f;
        private const float AllyFeetY = 640f;
        private const float BossScale = 2.1f;
        private const float AllyScale = 1.85f;

        private sealed class UnitView
        {
            public string Id = "";
            public GameObject Root = null!;
            public Vector3 Home;
            public Renderer[] Renderers = null!;
            public Color[] BaseColors = null!;
            public float Phase;
            public float HitFlash;
            public float Lunge;
            public GameObject Bubble = null!;
            public GameObject Ring = null!;
            public float HealGlow;
            public UnitRig? Rig;
        }

        private Camera _cam = null!;
        private BattleController _ctl = null!;
        private readonly Dictionary<string, UnitView> _units = new Dictionary<string, UnitView>();
        private UnitView _boss = null!;
        private Renderer[] _bossGlow = null!;
        private Color _coreCalm = Palette.CoreCalm, _coreFury = Palette.CoreFury;
        private string _bossId = "";
        private float _bossScaleFactor = 1f;
        private Light _coreLight = null!;
        private Transform _backdrop = null!;
        private float _shake;

        // Animation : l'attitude de chaque unité vient de UnitAnimator (cœur, testé), jamais calculée ici.
        private readonly Dictionary<string, UnitAnimator> _anims = new Dictionary<string, UnitAnimator>();
        private readonly UnitAnimator _bossAnim = new UnitAnimator("boss", true);
        private bool _lastBossBig;

        /// <summary>Renseigné par le flux : faux si le joueur a désactivé la secousse de l'écran.</summary>
        public System.Func<bool>? ShakeEnabled { get; set; }

        private void AddShake(float amount)
        {
            if (ShakeEnabled != null && !ShakeEnabled()) return;
            _shake = Mathf.Max(_shake, amount);
        }

        // Effets : anneaux au sol, faisceaux entre le soigneur et sa cible, anneau de danger avant l'attaque de zone.
        private sealed class RingFx { public GameObject Go = null!; public Material Mat = null!; public float Age = 99f; public float Size; public Color Color; }
        private sealed class BeamFx { public LineRenderer Line = null!; public float Age = 99f; public Color Color; }
        private readonly List<RingFx> _rings = new List<RingFx>();
        private readonly List<BeamFx> _beams = new List<BeamFx>();
        private GameObject _dangerRing = null!;
        private Material _dangerMat = null!;
        private float _bossHit;
        private float _bossStrike;
        private float _phaseBurst;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // ---- Effets ----
        private ParticleSystem _heal = null!, _shield = null!, _poison = null!, _purge = null!, _impact = null!;
        private readonly List<FloatingText> _texts = new List<FloatingText>();

        private sealed class FloatingText
        {
            public TextMesh Mesh = null!;
            public float Age = 99f;
            public Vector3 Start;
        }

        public void Build(Camera cam, BattleController ctl)
        {
            _cam = cam;
            _ctl = ctl;
            ConfigureCamera();
            BuildLights();
            BuildBackdrop();
            BuildParticles();
            BuildAtmosphere();
            BuildModels();
            ctl.EventEmitted += OnEvent;
            ctl.Restarted += ResetVisuals;
        }

        private void OnDestroy()
        {
            if (_ctl != null)
            {
                _ctl.EventEmitted -= OnEvent;
                _ctl.Restarted -= ResetVisuals;
            }
        }

        // ---- Construction ------------------------------------------------------------------------

        private void ConfigureCamera()
        {
            _cam.orthographic = false;
            _cam.fieldOfView = 30f;
            _cam.nearClipPlane = 0.3f;
            _cam.farClipPlane = 200f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Palette.Hex("05060B");
            _cam.transform.position = Vector3.zero;
            _cam.transform.rotation = Quaternion.identity;
        }

        private void BuildLights()
        {
            // Ambiance nocturne : ambiance bleu nuit, lune froide, contre-jour violet, deux braseros chauds sur les côtés.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.29f, 0.30f, 0.45f);
            var sun = new GameObject("Moon").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.66f, 0.74f, 1f);
            sun.intensity = 1.3f;
            sun.transform.rotation = Quaternion.Euler(42f, 205f, 0f);
            sun.shadows = LightShadows.None;
            var rim = new GameObject("RimLight").AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = new Color(0.58f, 0.36f, 0.95f);
            rim.intensity = 0.95f;
            rim.transform.rotation = Quaternion.Euler(28f, 175f, 0f);
            rim.shadows = LightShadows.None;
            _coreLight = new GameObject("CoreLight").AddComponent<Light>();
            _coreLight.type = LightType.Point;
            _coreLight.range = 11f;
            _coreLight.intensity = 1.0f;
            _coreLight.color = Palette.CoreCalm;
            _brazierL = new GameObject("BrazierL").AddComponent<Light>();
            _brazierR = new GameObject("BrazierR").AddComponent<Light>();
            foreach (var l in new[] { _brazierL, _brazierR })
            {
                l.type = LightType.Point;
                l.range = 10f;
                l.intensity = 0.9f;
                l.color = new Color(1f, 0.55f, 0.26f);
            }
        }

        // ---- Atmosphère : cercle runique, brume, poussières lumineuses, braseros ------------------------------

        private Light _brazierL = null!, _brazierR = null!;
        private Transform _runeCircle = null!;
        private Material _runeMat = null!;
        private readonly List<Transform> _mist = new List<Transform>();
        private readonly List<Material> _mistMat = new List<Material>();
        private ParticleSystem? _motes;

        private void BuildAtmosphere()
        {
            var circle = new GameObject("RuneCircle");
            circle.transform.SetParent(transform, false);
            circle.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
            _runeMat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = RuneCircleTexture(256) };
            _runeMat.SetColor("_TintColor", new Color(0.4f, 0.8f, 1f, 0.3f));
            circle.AddComponent<MeshRenderer>().sharedMaterial = _runeMat;
            _runeCircle = circle.transform;

            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject("Mist");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
                var mat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = SoftDot(64) };
                mat.SetColor("_TintColor", new Color(0.36f, 0.32f, 0.56f, 0.1f));
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                _mist.Add(go.transform);
                _mistMat.Add(mat);
            }

            var motes = new GameObject("FxMotes");
            motes.transform.SetParent(transform, false);
            _motes = motes.AddComponent<ParticleSystem>();
            var main = _motes.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 7f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.18f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            main.gravityModifier = -0.02f;
            main.maxParticles = 160;
            main.startColor = new Color(0.4f, 0.9f, 1f, 0.65f);
            var emission = _motes.emission;
            emission.enabled = true;
            emission.rateOverTime = 14f;
            var shape = _motes.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 0.2f, 9f);
            var fade = _motes.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0f, 0), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1) });
            fade.color = g;
            var renderer = motes.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = SoftDot(64) };
            _motes.Play();
        }

        private void UpdateAtmosphere(float t)
        {
            var center = WorldAt((float)Layout.GameW / 2f, AllyFeetY, AllyDepth);
            _runeCircle.position = center + new Vector3(0f, 0.02f, 0f);
            _runeCircle.rotation = Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 0f, t * 3.5f);
            _runeCircle.localScale = new Vector3(19f, 19f * 0.42f, 1f);
            var glow = _boss != null ? ((_ctl.Battle != null && _ctl.Battle.GetBossPhase().Index > 0) ? _coreFury : _coreCalm) : Palette.CoreCalm;
            _runeMat.SetColor("_TintColor", new Color(glow.r, glow.g, glow.b, 0.22f + 0.1f * Mathf.Sin(t * 1.4f)));
            if (_motes != null) _motes.transform.position = center + new Vector3(0f, 0.2f, 1.5f);
            for (int i = 0; i < _mist.Count; i++)
            {
                float k = i / (float)_mist.Count;
                var p = WorldAt(240f + 160f * i + Mathf.Sin(t * 0.07f + i * 1.7f) * 120f, AllyFeetY - 15f, AllyDepth + 4f - i * 1.2f);
                _mist[i].position = p + new Vector3(0f, 0.55f + 0.18f * Mathf.Sin(t * 0.3f + i), 0f);
                _mist[i].rotation = _cam.transform.rotation;
                _mist[i].localScale = new Vector3(11f, 2.6f, 1f);
                _mistMat[i].SetColor("_TintColor", new Color(0.36f, 0.32f, 0.56f, 0.07f + 0.04f * Mathf.Sin(t * 0.4f + i * 2f + k)));
            }
            _brazierL.transform.position = WorldAt(70f, 520f, AllyDepth - 2f);
            _brazierR.transform.position = WorldAt((float)Layout.GameW - 70f, 520f, AllyDepth - 2f);
            _brazierL.intensity = 0.85f + 0.25f * Mathf.Sin(t * 11f) * Mathf.Sin(t * 3.7f);
            _brazierR.intensity = 0.85f + 0.25f * Mathf.Sin(t * 9.3f + 1f) * Mathf.Sin(t * 4.1f);
        }

        /// <summary>Cercle runique : deux anneaux, douze graduations et un hexagramme, en blanc à transparence (teinté par la scène).</summary>
        private static Texture2D RuneCircleTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - c) / c, dy = (y - c) / c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float a = 0f;
                    a = Mathf.Max(a, 1f - Mathf.Abs(d - 0.96f) / 0.012f);
                    a = Mathf.Max(a, 1f - Mathf.Abs(d - 0.8f) / 0.008f);
                    float tick = Mathf.Abs(Mathf.Repeat(ang / (Mathf.PI * 2f) * 12f, 1f) - 0.5f);
                    if (d > 0.8f && d < 0.96f) a = Mathf.Max(a, (1f - tick / 0.04f) * 0.9f);
                    // hexagramme : deux triangles inscrits dans le cercle intérieur
                    for (int tri = 0; tri < 2; tri++)
                    {
                        float best = 9f;
                        for (int e = 0; e < 3; e++)
                        {
                            float a0 = (tri * 60f + e * 120f + 90f) * Mathf.Deg2Rad, a1 = (tri * 60f + (e + 1) * 120f + 90f) * Mathf.Deg2Rad;
                            var p0 = 0.78f * new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)); var p1 = 0.78f * new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
                            var q = new Vector2(dx, dy); var ab = p1 - p0;
                            float u = Mathf.Clamp01(Vector2.Dot(q - p0, ab) / ab.sqrMagnitude);
                            best = Mathf.Min(best, (q - (p0 + ab * u)).magnitude);
                        }
                        a = Mathf.Max(a, (1f - best / 0.008f) * 0.7f);
                    }
                    if (d > 1f) a = 0f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(a)));
                }
            tex.Apply();
            return tex;
        }

        private void BuildBackdrop()
        {
            var tex = PaintBackdrop(384, 216);
            var quad = new GameObject("Backdrop");
            quad.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
            var mat = new Material(Shader.Find("Unlit/Texture")) { mainTexture = tex };
            quad.AddComponent<MeshRenderer>().sharedMaterial = mat;
            _backdrop = quad.transform;
            // Halo derrière le boss.
            var halo = new GameObject("Halo");
            halo.transform.SetParent(quad.transform, false);
            halo.transform.localPosition = new Vector3(0, 0.12f, -0.01f);
            halo.transform.localScale = new Vector3(0.7f, 0.5f, 1f);
            halo.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
            var haloMat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = SoftDot(128) };
            haloMat.SetColor("_TintColor", new Color(0.22f, 0.16f, 0.38f, 0.12f));
            halo.AddComponent<MeshRenderer>().sharedMaterial = haloMat;
        }

        /// <summary>
        /// Décor peint par code : ruines sombres (piliers en silhouette, brume à l'horizon), sol de dalles en
        /// perspective légère, lueur froide sous le combat. Aucune image importée (docs/ART_3D.md).
        /// </summary>
        private static Texture2D PaintBackdrop(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[w * h];
            var skyTop = Palette.Hex("04050A");
            var skyLow = Palette.Hex("100F24");
            var haze = Palette.Hex("352B54");
            var floorNear = Palette.Hex("040509");
            var floorFar = Palette.Hex("0E0D1D");
            var pillar = Palette.Hex("080911");
            const float horizon = 0.6f; // fraction depuis le bas
            float[] pillarX = { 0.09f, 0.26f, 0.74f, 0.91f };
            float[] pillarW = { 0.03f, 0.022f, 0.022f, 0.03f };
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1);
                    Color c;
                    if (v >= horizon)
                    {
                        float k = (v - horizon) / (1f - horizon);
                        c = Color.Lerp(skyLow, skyTop, Mathf.Pow(k, 0.7f));
                        float glow = Mathf.Exp(-Mathf.Pow((v - horizon) / 0.09f, 2f)) * Mathf.Exp(-Mathf.Pow((u - 0.5f) / 0.55f, 2f));
                        c = Color.Lerp(c, haze, glow * 0.55f);
                        // Fenêtre gothique en ogive derrière le boss : lueur froide de lune, meneaux sombres.
                        float wy = (v - (horizon + 0.05f)) / 0.3f;
                        if (wy > 0f && wy < 1f)
                        {
                            float half = 0.085f * Mathf.Sqrt(Mathf.Clamp01(1f - Mathf.Pow(Mathf.Max(0f, wy - 0.55f) / 0.45f, 2f)));
                            if (Mathf.Abs(u - 0.5f) < half)
                            {
                                float mull = Mathf.Abs(Mathf.Abs(u - 0.5f) - 0.03f) < 0.004f || Mathf.Abs(wy - 0.42f) < 0.012f ? 0.25f : 1f;
                                c = Color.Lerp(c, Palette.Hex("4B5C8A"), 0.32f * mull * (1f - 0.5f * wy));
                            }
                        }
                        for (int p = 0; p < pillarX.Length; p++)
                        {
                            float d = Mathf.Abs(u - pillarX[p]);
                            if (d < pillarW[p])
                            {
                                float edge = Mathf.Clamp01((pillarW[p] - d) / 0.008f);
                                var body = Color.Lerp(pillar, haze, glow * 0.5f);
                                c = Color.Lerp(c, body, edge);
                            }
                        }
                    }
                    else
                    {
                        float dy = horizon - v; // distance sous l'horizon
                        float depth = Mathf.Clamp01(dy / horizon);
                        c = Color.Lerp(floorFar, floorNear, Mathf.Pow(depth, 0.6f));
                        // lueur froide sous le combat
                        float sheen = Mathf.Exp(-(Mathf.Pow((u - 0.5f) / 0.42f, 2f) + Mathf.Pow((v - 0.36f) / 0.2f, 2f)));
                        c = Color.Lerp(c, Palette.Hex("35315A"), sheen * 0.45f);
                        // joints des dalles en perspective
                        float dx = (u - 0.5f) * 1.78f / Mathf.Max(0.02f, dy);
                        float lineA = Mathf.Abs(Mathf.Repeat(dx * 1.6f + 0.5f, 1f) - 0.5f) * dy;
                        float lineB = Mathf.Abs(Mathf.Repeat(0.05f / Mathf.Max(0.02f, dy) * 4f, 1f) - 0.5f);
                        if (lineA < 0.0035f || lineB < 0.02f) c = Color.Lerp(c, Palette.Hex("05060A"), 0.5f);
                    }
                    pixels[y * w + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Mesh QuadMesh()
        {
            var m = new Mesh { name = "Quad" };
            m.SetVertices(new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) });
            m.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            m.RecalculateNormals();
            return m;
        }

        /// <summary>Anneau doux (allié ciblé) : bande claire autour du bord d'un disque, centre transparent.</summary>
        private static Texture2D RingTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) / 0.1f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a * a));
                }
            tex.Apply();
            return tex;
        }

        private static Texture2D SoftDot(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a * a));
                }
            tex.Apply();
            return tex;
        }

        private ParticleSystem MakeSystem(string name, float size, float lifetime, float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = lifetime;
            main.startSize = size;
            main.gravityModifier = gravity;
            main.maxParticles = 400;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(1f, 0), new GradientAlphaKey(0f, 1) });
            fade.color = g;
            var shrink = ps.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.1f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = SoftDot(64) };
            return ps;
        }

        private void BuildParticles()
        {
            _heal = MakeSystem("FxHeal", 0.9f, 0.9f, -0.05f);
            _shield = MakeSystem("FxShield", 0.8f, 0.6f, 0f);
            _poison = MakeSystem("FxPoison", 0.9f, 1.0f, -0.08f);
            _purge = MakeSystem("FxPurge", 0.8f, 0.7f, -0.04f);
            _impact = MakeSystem("FxImpact", 0.7f, 0.45f, 0.2f);
            var ringTex = RingTexture(128);
            for (int i = 0; i < 10; i++)
            {
                var go = new GameObject("RingFx");
                go.transform.SetParent(transform, false);
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                go.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
                var mat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = ringTex };
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                go.SetActive(false);
                _rings.Add(new RingFx { Go = go, Mat = mat });
            }
            for (int i = 0; i < 8; i++)
            {
                var go = new GameObject("BeamFx");
                go.transform.SetParent(transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.useWorldSpace = true;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startWidth = 0.22f;
                line.endWidth = 0.03f;
                line.enabled = false;
                _beams.Add(new BeamFx { Line = line });
            }
            _dangerRing = new GameObject("DangerRing");
            _dangerRing.transform.SetParent(transform, false);
            _dangerRing.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _dangerRing.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
            _dangerMat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = ringTex };
            _dangerRing.AddComponent<MeshRenderer>().sharedMaterial = _dangerMat;
            _dangerRing.SetActive(false);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < 16; i++)
            {
                var go = new GameObject("FloatingText");
                go.transform.SetParent(transform, false);
                var tm = go.AddComponent<TextMesh>();
                tm.font = font;
                tm.fontSize = 64;
                tm.characterSize = 0.07f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.fontStyle = FontStyle.Bold;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                go.SetActive(false);
                _texts.Add(new FloatingText { Mesh = tm });
            }
        }

        private UnitView MakeView(string id, GameObject model, float scale)
        {
            model.transform.SetParent(transform, false);
            model.transform.localScale = Vector3.one * scale;
            var renderers = model.GetComponentsInChildren<Renderer>().Where(r => r.name != "GroundShadow").ToArray();
            var view = new UnitView
            {
                Id = id,
                Root = model,
                Renderers = renderers,
                BaseColors = renderers.Select(r => r.sharedMaterial.color).ToArray(),
                Phase = Mathf.Abs(id.GetHashCode() % 100) * 0.1f,
            };
            var shadow = new GameObject("GroundShadow");
            shadow.transform.SetParent(model.transform, false);
            shadow.transform.localPosition = new Vector3(0, 0.03f, 0);
            shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.transform.localScale = new Vector3(id == "boss" ? 5.4f : 2.6f, id == "boss" ? 3.4f : 1.7f, 1f);
            shadow.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
            var shadowMat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = SoftDot(64) };
            shadowMat.SetColor("_TintColor", new Color(0f, 0f, 0f, 0.3f));
            shadow.AddComponent<MeshRenderer>().sharedMaterial = shadowMat;
            var ring = new GameObject("SelectRing");
            ring.transform.SetParent(model.transform, false);
            ring.transform.localPosition = new Vector3(0, 0.05f, 0);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localScale = new Vector3(3.1f, 2.1f, 1f);
            ring.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
            var ringMat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = RingTexture(128) };
            ringMat.SetColor("_TintColor", new Color(0.88f, 0.75f, 0.42f, 0.95f));
            ring.AddComponent<MeshRenderer>().sharedMaterial = ringMat;
            ring.SetActive(false);
            var bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(bubble.GetComponent<Collider>());
            bubble.name = "ShieldBubble";
            bubble.transform.SetParent(model.transform, false);
            bubble.transform.localPosition = new Vector3(0, 1.1f, 0);
            bubble.transform.localScale = new Vector3(2.6f, 2.9f, 2.6f);
            bubble.GetComponent<MeshFilter>().sharedMesh = MeshKit.Sphere(8, 12);
            var mat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = SoftDot(64) };
            mat.SetColor("_TintColor", new Color(0.25f, 0.39f, 0.5f, 0.22f));
            bubble.GetComponent<MeshRenderer>().sharedMaterial = mat;
            bubble.SetActive(false);
            view.Bubble = bubble;
            view.Ring = ring;
            view.Rig = model.GetComponent<UnitRig>();
            return view;
        }

        private void BuildModels()
        {
            SetBoss(_ctl.Level?.BossId ?? "boss1");

            BuildAllies();
        }

        private string _looksSignature = "";

        private string LooksSignature() => string.Join("|", _ctl.Battle.GetAllies().Select(a => _ctl.AppearanceOf(a.Id).Signature()));

        /// <summary>(Re)construit les héros avec l'apparence de leur équipement (D-061) : arme et armure donnent la forme et la palette.</summary>
        private void BuildAllies()
        {
            foreach (var old in _units.Values) if (old.Root != null) Destroy(old.Root);
            _units.Clear();
            var allies = _ctl.Battle.GetAllies();
            for (int i = 0; i < allies.Count; i++)
            {
                var a = allies[i];
                var view = MakeView(a.Id, ModelFactory.ForCharacter(a.Id, a.Role, _ctl.AppearanceOf(a.Id)), AllyScale);
                view.Root.name = "Ally_" + a.Id;
                _units[a.Id] = view;
                if (!_anims.ContainsKey(a.Id)) _anims[a.Id] = new UnitAnimator(a.Id);
                _stageX[a.Id] = (float)Layout.AllyStageX(i, allies.Count);
            }
            _looksSignature = LooksSignature();
            Debug.Log("[Healer] apparence : " + _looksSignature);
        }

        /// <summary>L'équipement a pu changer entre deux combats (achat à l'Atelier) : on redessine les héros seulement si leur apparence a changé.</summary>
        private void RefreshLooks()
        {
            if (_ctl.Battle != null && LooksSignature() != _looksSignature) BuildAllies();
        }

        /// <summary>Position logique (x) de chaque allié dans la scène : en ligne devant le boss (les cartes sont dans la colonne gauche).</summary>
        /// <summary>Affiche le boss d'un niveau (modèle, couleurs lumineuses, échelle). Sans effet si c'est déjà lui.</summary>
        public void SetBoss(string bossId)
        {
            if (_boss != null && _bossId == bossId) return;
            if (_boss != null) Destroy(_boss.Root);
            var model = ModelFactory.ForBoss(bossId);
            _bossId = bossId;
            _bossScaleFactor = ModelFactory.BossScaleFactor(bossId);
            (_coreCalm, _coreFury) = ModelFactory.BossColors(bossId);
            _boss = MakeView("boss", model, BossScale * _bossScaleFactor);
            _bossGlow = model.GetComponentsInChildren<Renderer>().Where(r => r.name is "Core" or "EyeL" or "EyeR" or "Rune").ToArray();
            _bossHit = _bossStrike = _phaseBurst = 0;
            if (_motes != null) { var main = _motes.main; main.startColor = new Color(_coreCalm.r, _coreCalm.g, _coreCalm.b, 0.65f); }
        }

        private readonly Dictionary<string, float> _stageX = new Dictionary<string, float>();

        private void ResetVisuals()
        {
            RefreshLooks();
            foreach (var v in _units.Values) { v.HitFlash = 0; v.Lunge = 0; v.HealGlow = 0; v.Root.transform.rotation = Quaternion.identity; }
            _bossHit = _bossStrike = _phaseBurst = _shake = 0;
            _bossAnim.Reset();
            foreach (var a in _anims.Values) a.Reset();
        }

        // ---- Positions ---------------------------------------------------------------------------

        private Vector3 WorldAt(float logicalX, float logicalY, float depth)
        {
            var vp = ScreenMap.LogicalToViewport(logicalX, logicalY);
            return _cam.ViewportToWorldPoint(new Vector3(vp.x, vp.y, depth));
        }

        public Vector3 AllyHead(string id) => _units.TryGetValue(id, out var v) ? v.Root.transform.position + Vector3.up * 2.9f * AllyScale : Vector3.zero;

        // ---- Boucle ------------------------------------------------------------------------------

        private void LateUpdate()
        {
            if (_ctl == null || _ctl.Battle == null) return;
            ScreenMap.Refresh();
            float t = Time.time;
            float dt = Time.deltaTime;

            // Fond : recouvre tout le champ de vision.
            float bd = 90f;
            float h = 2f * bd * Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            _backdrop.position = _cam.transform.position + _cam.transform.forward * bd;
            _backdrop.rotation = _cam.transform.rotation;
            _backdrop.localScale = new Vector3(h * _cam.aspect * 1.02f, h * 1.02f, 1f);

            var battle = _ctl.Battle;
            var states = battle.GetAllies().ToDictionary(a => a.Id);
            var phase = battle.GetBossPhase();
            var telegraph = battle.GetTelegraph();
            bool bigAttack = telegraph != null && telegraph.Type == "bigAttack";
            string? focusId = telegraph != null && telegraph.Type == "focusAttack" ? telegraph.TargetId : null; // victime annoncée d'une attaque ciblée

            // Boss.
            var bp = WorldAt((float)Layout.GameW / 2f, BossFeetY, BossDepth);
            float bossShake = bigAttack ? Mathf.Sin(t * 45f) * 0.06f : 0f;
            double clock = battle.GetClock();
            var bossPose = _bossAnim.Sample(clock);
            _bossHit = (float)bossPose.Recoil;
            _bossStrike = (float)bossPose.Lunge * (_lastBossBig ? 1f : 0.5f);
            float breathe = Mathf.Sin(t * 1.6f);
            var br = _boss.Root.transform;
            br.position = bp + new Vector3(bossShake, breathe * 0.05f, -_bossStrike * 1.6f);
            br.rotation = Quaternion.Euler(_bossStrike * 14f, 180f, 0f);
            _boss.Rig?.Apply(t, 0.4f, 0f, (float)bossPose.Lunge, (float)bossPose.Recoil, 0f);
            br.localScale = new Vector3(1f + _bossHit * 0.04f, 1f - _bossHit * 0.03f + breathe * 0.008f, 1f + _bossHit * 0.04f) * BossScale * _bossScaleFactor;
            Color core = phase.Index > 0 ? _coreFury : _coreCalm;
            float pulse = bigAttack ? 0.7f + 0.3f * Mathf.Sin(t * 22f) : 0.8f + 0.2f * Mathf.Sin(t * 3.2f);
            _phaseBurst = Mathf.Max(0f, _phaseBurst - dt * 1.5f);
            for (int i = 0; i < _boss.Renderers.Length; i++)
            {
                var r = _boss.Renderers[i];
                if (_bossGlow.Contains(r) || r.gameObject == _boss.Bubble) continue;
                Color stone = _boss.BaseColors[i];
                if (phase.Index > 0) stone = Color.Lerp(stone, new Color(0.78f, 0.42f, 0.34f), 0.32f + 0.06f * Mathf.Sin(t * 3f));
                if (_bossHit > 0f) stone = Color.Lerp(stone, Color.white, _bossHit * 0.35f);
                _block.SetColor(ColorId, stone);
                r.SetPropertyBlock(_block);
            }
            foreach (var r in _bossGlow)
            {
                _block.SetColor(ColorId, Color.Lerp(core * pulse, Color.white, _phaseBurst * 0.7f));
                r.SetPropertyBlock(_block);
            }
            _coreLight.color = Color.Lerp(core, Color.white, 0.35f);
            _coreLight.intensity = (bigAttack ? 1.4f : 0.8f) * pulse;
            _coreLight.transform.position = br.position + new Vector3(0, 2.3f, -3.5f);

            // Alliés.
            foreach (var kv in _units)
            {
                var v = kv.Value;
                if (!states.TryGetValue(kv.Key, out var st)) continue;
                var pose = _anims[kv.Key].Sample(clock);
                v.HitFlash = (float)pose.Recoil;
                v.Lunge = (float)pose.Lunge;
                v.HealGlow = (float)pose.Glow;
                Vector3 home = WorldAt(_stageX[kv.Key], AllyFeetY, AllyDepth);
                float bob = st.Alive ? Mathf.Abs(Mathf.Sin(t * 2.4f + v.Phase)) * 0.09f : 0f;
                var tr = v.Root.transform;
                tr.position = home + new Vector3(0, bob - v.HitFlash * 0.12f + (float)pose.Cast * 0.2f - (float)pose.Fall * 0.3f, -v.Lunge * 1.2f + (float)pose.Recoil * 0.25f);
                float side = kv.Key == "healer" ? -10f : (kv.Key == "tank" ? 12f : kv.Key == "dps1" ? -6f : 8f);
                tr.rotation = Quaternion.Euler(-(float)pose.Cast * 10f, 180f + side * (1f - (float)pose.Fall), (float)pose.Fall * 78f);
                v.Rig?.Apply(t, v.Phase, (float)pose.Cast, (float)pose.Lunge, (float)pose.Recoil, (float)pose.Fall);
                v.Bubble.SetActive(st.Alive && st.Shield > 0.5f);
                v.Ring.SetActive(st.Alive && _ctl.Selection.Selected == kv.Key);
                bool poisoned = st.Effects.Count > 0;
                bool marked = st.Alive && kv.Key == focusId;
                for (int i = 0; i < v.Renderers.Length; i++)
                {
                    var r = v.Renderers[i];
                    if (r.gameObject == v.Bubble) continue;
                    Color c = v.BaseColors[i];
                    if (!st.Alive) c = Color.Lerp(c, new Color(0.3f, 0.3f, 0.34f), 0.75f);
                    else
                    {
                        if (poisoned) c = Color.Lerp(c, Palette.Poison, 0.35f + 0.1f * Mathf.Sin(t * 6f));
                        if (marked) c = Color.Lerp(c, Palette.Danger, 0.4f + 0.3f * Mathf.Sin(t * 12f)); // victime annoncée : rouge pulsant
                        if (v.HealGlow > 0f) c = Color.Lerp(c, Palette.Heal, v.HealGlow * 0.6f);
                        if (v.HitFlash > 0f) c = Color.Lerp(c, Palette.Damage, v.HitFlash * 0.85f);
                    }
                    _block.SetColor(ColorId, c);
                    r.SetPropertyBlock(_block);
                }
            }

            // Anneau de danger sous l'équipe pendant que l'attaque de zone est annoncée.
            _dangerRing.SetActive(bigAttack);
            if (bigAttack && telegraph != null)
            {
                float progress = 1f - Mathf.Clamp01((float)(telegraph.MsRemaining / System.Math.Max(1.0, telegraph.TotalMs)));
                float blink = 0.6f + 0.4f * Mathf.Sin(t * 16f);
                _dangerMat.SetColor("_TintColor", new Color(1f, 0.25f, 0.2f, (0.12f + 0.55f * progress) * blink));
                _dangerRing.transform.position = WorldAt((float)Layout.GameW / 2f, AllyFeetY, AllyDepth) + new Vector3(0f, 0.04f, 0f);
                _dangerRing.transform.localScale = new Vector3(15f + 4f * (1f - progress), 6f + 1.6f * (1f - progress), 1f);
            }
            UpdateFx(dt);
            UpdateAtmosphere(t);

            // Secousse de caméra.
            _shake = Mathf.Max(0f, _shake - dt * 2.4f);
            _cam.transform.position = _shake > 0f
                ? new Vector3(Mathf.Sin(t * 60f), Mathf.Cos(t * 53f), 0f) * _shake * 0.35f
                : Vector3.zero;

            UpdateTexts(dt);
        }

        // ---- Événements --------------------------------------------------------------------------

        private void OnEvent(BattleEvent e)
        {
            _bossAnim.OnEvent(e);
            foreach (var a in _anims.Values) a.OnEvent(e);
            switch (e.Type)
            {
                case "skillUsed":
                    if (_units.TryGetValue(e.CasterId, out var cv))
                    {
                        var from = cv.Root.transform.position + Vector3.up * 1.7f;
                        Color sc = e.SkillId == "shield" ? Palette.Shield : e.SkillId == "purge" ? Palette.Poison : Palette.Heal;
                        Ring(cv.Root.transform.position, sc, 2.4f);
                        foreach (var id in e.TargetIds)
                            if (id != e.CasterId && _units.TryGetValue(id, out var beamTarget)) Beam(from, beamTarget.Root.transform.position + Vector3.up * 1.4f, sc);
                    }
                    break;
                case "unitDodged":
                    if (_units.TryGetValue(e.UnitId, out var dgv)) Float(dgv, "Esquive", Palette.Shield, 0.95f);
                    break;
                case "castFailed":
                    if (_units.TryGetValue(e.CasterId, out var cfv)) Float(cfv, "Interrompu", Palette.Damage, 0.85f);
                    break;
                case "bossEnraged":
                    AddShake(0.5f);
                    Ring(_boss.Root.transform.position, _coreFury, 7f);
                    Burst(_impact, _boss.Root.transform.position + Vector3.up * 1.7f, _coreFury, 30, 4f);
                    break;
                case "healed":
                    if (e.Amount > 0 && _units.TryGetValue(e.UnitId, out var hv))
                    {
                        Burst(_heal, hv.Root.transform.position + Vector3.up * 1.0f, Palette.Heal, 10, 1.6f);
                        Float(hv, (e.Crit ? "CRITIQUE +" : "+") + Format.Number(e.Amount) + (e.Crit ? "!" : ""), e.Crit ? Ui.Gold : Palette.Heal, e.Crit ? 1.35f : 1f);
                    }
                    break;
                case "shielded":
                    if (_units.TryGetValue(e.UnitId, out var sv))
                    {
                        Burst(_shield, sv.Root.transform.position + Vector3.up * 1.2f, Palette.Shield, 14, 2.6f);
                        Ring(sv.Root.transform.position, Palette.Shield, 3.2f);
                        Float(sv, "+" + Format.Number(e.Amount), Palette.Shield);
                    }
                    break;
                case "unitDamaged":
                    if (_units.TryGetValue(e.UnitId, out var dv))
                    {
                        if (e.Amount > 0)
                        {
                            Burst(_impact, dv.Root.transform.position + Vector3.up * 1.2f, Palette.Damage, 8, 3f);
                            Float(dv, (e.Crit ? "CRITIQUE -" : "-") + Format.Number(e.Amount) + (e.Crit ? "!" : ""), DamageColor(e), e.Crit ? 1.35f : 1f);
                        }
                        else Float(dv, "Absorbé", Palette.Shield, 0.8f);
                    }
                    break;
                case "effectTick":
                    if (e.Amount > 0 && _units.TryGetValue(e.UnitId, out var tv))
                    {
                        Burst(_poison, tv.Root.transform.position + Vector3.up * 1.0f, Palette.Poison, 3, 1.2f);
                        Float(tv, "-" + Format.Number(e.Amount), Palette.Poison);
                    }
                    break;
                case "effectApplied":
                    if (_units.TryGetValue(e.UnitId, out var av))
                    {
                        Burst(_poison, av.Root.transform.position + Vector3.up * 1.0f, Palette.Poison, 14, 1.8f);
                        Float(av, "Poison", Palette.Poison, 0.8f);
                    }
                    break;
                case "effectEnded":
                    if (e.Reason == "cleansed" && _units.TryGetValue(e.UnitId, out var pv))
                    {
                        Burst(_purge, pv.Root.transform.position + Vector3.up * 1.0f, Color.white, 14, 2.2f);
                        Ring(pv.Root.transform.position, Color.white, 2.8f);
                        Float(pv, "Purgé", Palette.Heal, 0.8f);
                    }
                    break;
                case "bossDamaged":
                    break;
                case "bossAction":
                    _lastBossBig = e.Action == "bigAttack";
                    if (_lastBossBig) AddShake(0.55f);
                    break;
                case "bossPhaseChanged":
                    _phaseBurst = 1f;
                    AddShake(1f);
                    Burst(_impact, _boss.Root.transform.position + Vector3.up * 1.7f, _coreFury, 40, 5f);
                    break;
            }
        }

        /// <summary>Anneau qui s'étend au sol et s'estompe (lancer d'un sort, bouclier, purge, enrage).</summary>
        private void Ring(Vector3 pos, Color color, float size)
        {
            var fx = _rings.OrderByDescending(r => r.Age).First();
            fx.Age = 0f; fx.Size = size; fx.Color = color;
            fx.Go.transform.position = new Vector3(pos.x, pos.y + 0.06f, pos.z);
            fx.Go.SetActive(true);
        }

        /// <summary>Faisceau bref entre le soigneur et sa cible.</summary>
        private void Beam(Vector3 from, Vector3 to, Color color)
        {
            var fx = _beams.OrderByDescending(b => b.Age).First();
            fx.Age = 0f; fx.Color = color;
            fx.Line.SetPosition(0, from);
            fx.Line.SetPosition(1, to);
            fx.Line.enabled = true;
        }

        private void UpdateFx(float dt)
        {
            foreach (var r in _rings)
            {
                if (!r.Go.activeSelf) continue;
                r.Age += dt;
                float k = r.Age / 0.55f;
                if (k >= 1f) { r.Go.SetActive(false); continue; }
                float size = r.Size * (0.35f + 0.65f * (1f - (1f - k) * (1f - k)));
                r.Go.transform.localScale = new Vector3(size, size * 0.62f, 1f);
                r.Mat.SetColor("_TintColor", new Color(r.Color.r, r.Color.g, r.Color.b, 0.85f * (1f - k)));
            }
            foreach (var b in _beams)
            {
                if (!b.Line.enabled) continue;
                b.Age += dt;
                float k = b.Age / 0.35f;
                if (k >= 1f) { b.Line.enabled = false; continue; }
                var c = new Color(b.Color.r, b.Color.g, b.Color.b, 0.9f * (1f - k));
                b.Line.startColor = c;
                b.Line.endColor = new Color(c.r, c.g, c.b, c.a * 0.3f);
            }
        }

        /// <summary>Couleur d'un nombre de dégâts selon le type (feu orange, magie violette, physique rouge).</summary>
        private static Color DamageColor(BattleEvent e)
        {
            switch (e.DamageType)
            {
                case "fire": return Palette.Hex("FF9A4D");
                case "magic": return Palette.Hex("B98CFF");
                case "poison": return Palette.Poison;
                default: return Palette.Damage;
            }
        }

        private void Burst(ParticleSystem ps, Vector3 pos, Color color, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                var p = new ParticleSystem.EmitParams
                {
                    position = pos + Random.insideUnitSphere * 0.5f,
                    velocity = Random.insideUnitSphere * speed + Vector3.up * speed * 0.35f,
                    startColor = color,
                };
                ps.Emit(p, 1);
            }
        }

        private void Float(UnitView v, string text, Color color, float size = 1f)
        {
            var slot = _texts.FirstOrDefault(x => x.Age > 1.1f) ?? _texts.OrderByDescending(x => x.Age).First();
            slot.Age = 0f;
            int stacked = _texts.Count(x => x.Mesh.gameObject.activeSelf && Vector3.Distance(x.Start, v.Root.transform.position) < 3f);
            slot.Start = v.Root.transform.position + Vector3.up * (2.4f + 0.55f * (stacked % 3)) + new Vector3(Random.Range(-0.3f, 0.3f), 0, -1.5f);
            slot.Mesh.characterSize = 0.07f * size;
            slot.Mesh.text = text;
            slot.Mesh.color = color;
            slot.Mesh.gameObject.SetActive(true);
        }

        private void UpdateTexts(float dt)
        {
            foreach (var f in _texts)
            {
                if (!f.Mesh.gameObject.activeSelf) continue;
                f.Age += dt;
                if (f.Age > 1.1f) { f.Mesh.gameObject.SetActive(false); continue; }
                var tr = f.Mesh.transform;
                tr.position = f.Start + Vector3.up * (f.Age * 0.9f);
                tr.rotation = _cam.transform.rotation;
                var c = f.Mesh.color;
                c.a = Mathf.Clamp01(1.1f - f.Age) * 1.4f;
                f.Mesh.color = c;
            }
        }
    }
}
