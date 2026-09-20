using System.Collections.Generic;
using System.Linq;
using Healer.Combat;
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
        private const float AllyScale = 1.65f;

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
            _cam.backgroundColor = Palette.Hex("0A0C14");
            _cam.transform.position = Vector3.zero;
            _cam.transform.rotation = Quaternion.identity;
        }

        private void BuildLights()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.40f, 0.42f, 0.54f);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.9f, 0.78f);
            sun.intensity = 1.25f;
            sun.transform.rotation = Quaternion.Euler(42f, 205f, 0f);
            sun.shadows = LightShadows.None;
            // Contre-jour froid : détache les silhouettes du décor sombre.
            var rim = new GameObject("RimLight").AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = new Color(0.45f, 0.6f, 1f);
            rim.intensity = 0.8f;
            rim.transform.rotation = Quaternion.Euler(28f, 175f, 0f);
            rim.shadows = LightShadows.None;
            _coreLight = new GameObject("CoreLight").AddComponent<Light>();
            _coreLight.type = LightType.Point;
            _coreLight.range = 9f;
            _coreLight.intensity = 0.8f;
            _coreLight.color = Palette.CoreCalm;
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
            var skyTop = Palette.Hex("080A12");
            var skyLow = Palette.Hex("1E1C34");
            var haze = Palette.Hex("4A3F66");
            var floorNear = Palette.Hex("07080D");
            var floorFar = Palette.Hex("17162A");
            var pillar = Palette.Hex("0C0D18");
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
            return view;
        }

        private void BuildModels()
        {
            SetBoss(_ctl.Level?.BossId ?? "boss1");

            var allies = _ctl.Battle.GetAllies();
            for (int i = 0; i < allies.Count; i++)
            {
                var a = allies[i];
                var view = MakeView(a.Id, ModelFactory.ForCharacter(a.Id, a.Role), AllyScale);
                view.Root.name = "Ally_" + a.Id;
                _units[a.Id] = view;
                _stageX[a.Id] = (float)Layout.AllyStageX(i, allies.Count);
            }
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
        }

        private readonly Dictionary<string, float> _stageX = new Dictionary<string, float>();

        private void ResetVisuals()
        {
            foreach (var v in _units.Values) { v.HitFlash = 0; v.Lunge = 0; v.HealGlow = 0; v.Root.transform.rotation = Quaternion.identity; }
            _bossHit = _bossStrike = _phaseBurst = _shake = 0;
        }

        // ---- Positions ---------------------------------------------------------------------------

        private Vector3 WorldAt(float logicalX, float logicalY, float depth)
        {
            var vp = ScreenMap.LogicalToViewport(logicalX, logicalY);
            return _cam.ViewportToWorldPoint(new Vector3(vp.x, vp.y, depth));
        }

        public Vector3 AllyHead(string id) => _units.TryGetValue(id, out var v) ? v.Root.transform.position + Vector3.up * 2.2f * AllyScale : Vector3.zero;

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

            // Boss.
            var bp = WorldAt((float)Layout.GameW / 2f, BossFeetY, BossDepth);
            float bossShake = bigAttack ? Mathf.Sin(t * 45f) * 0.06f : 0f;
            _bossHit = Mathf.Max(0f, _bossHit - dt * 4f);
            _bossStrike = Mathf.Max(0f, _bossStrike - dt * 3.2f);
            float breathe = Mathf.Sin(t * 1.6f);
            var br = _boss.Root.transform;
            br.position = bp + new Vector3(bossShake, breathe * 0.05f, -_bossStrike * 1.6f);
            br.rotation = Quaternion.Euler(_bossStrike * 14f, 180f, 0f);
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
                v.HitFlash = Mathf.Max(0f, v.HitFlash - dt * 3.5f);
                v.Lunge = Mathf.Max(0f, v.Lunge - dt * 4f);
                v.HealGlow = Mathf.Max(0f, v.HealGlow - dt * 2.5f);
                Vector3 home = WorldAt(_stageX[kv.Key], AllyFeetY, AllyDepth);
                float bob = st.Alive ? Mathf.Abs(Mathf.Sin(t * 2.4f + v.Phase)) * 0.09f : 0f;
                var tr = v.Root.transform;
                tr.position = home + new Vector3(0, bob - v.HitFlash * 0.12f, -v.Lunge * 1.2f);
                float side = kv.Key == "healer" ? -10f : (kv.Key == "tank" ? 12f : kv.Key == "dps1" ? -6f : 8f);
                tr.rotation = st.Alive ? Quaternion.Euler(0f, 180f + side, 0f) : Quaternion.Euler(0f, 180f, 78f);
                v.Bubble.SetActive(st.Alive && st.Shield > 0.5f);
                v.Ring.SetActive(st.Alive && _ctl.Selection.Selected == kv.Key);
                bool poisoned = st.Effects.Count > 0;
                for (int i = 0; i < v.Renderers.Length; i++)
                {
                    var r = v.Renderers[i];
                    if (r.gameObject == v.Bubble) continue;
                    Color c = v.BaseColors[i];
                    if (!st.Alive) c = Color.Lerp(c, new Color(0.3f, 0.3f, 0.34f), 0.75f);
                    else
                    {
                        if (poisoned) c = Color.Lerp(c, Palette.Poison, 0.35f + 0.1f * Mathf.Sin(t * 6f));
                        if (v.HealGlow > 0f) c = Color.Lerp(c, Palette.Heal, v.HealGlow * 0.6f);
                        if (v.HitFlash > 0f) c = Color.Lerp(c, Palette.Damage, v.HitFlash * 0.85f);
                    }
                    _block.SetColor(ColorId, c);
                    r.SetPropertyBlock(_block);
                }
            }

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
            switch (e.Type)
            {
                case "healed":
                    if (e.Amount > 0 && _units.TryGetValue(e.UnitId, out var hv))
                    {
                        hv.HealGlow = 1f;
                        Burst(_heal, hv.Root.transform.position + Vector3.up * 1.0f, Palette.Heal, 10, 1.6f);
                        Float(hv, "+" + Format.Number(e.Amount), Palette.Heal);
                    }
                    break;
                case "shielded":
                    if (_units.TryGetValue(e.UnitId, out var sv))
                    {
                        Burst(_shield, sv.Root.transform.position + Vector3.up * 1.2f, Palette.Shield, 14, 2.6f);
                        Float(sv, "+" + Format.Number(e.Amount), Palette.Shield);
                    }
                    break;
                case "unitDamaged":
                    if (_units.TryGetValue(e.UnitId, out var dv))
                    {
                        if (e.Amount > 0)
                        {
                            dv.HitFlash = 1f;
                            Burst(_impact, dv.Root.transform.position + Vector3.up * 1.2f, Palette.Damage, 8, 3f);
                            Float(dv, "-" + Format.Number(e.Amount), Palette.Damage);
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
                        Float(pv, "Purgé", Palette.Heal, 0.8f);
                    }
                    break;
                case "bossDamaged":
                    _bossHit = 1f;
                    if (_units.TryGetValue(e.SourceId, out var lv)) lv.Lunge = 1f;
                    break;
                case "bossAction":
                    _bossStrike = e.Action == "bigAttack" ? 1f : 0.55f;
                    if (e.Action == "bigAttack") _shake = Mathf.Max(_shake, 0.55f);
                    break;
                case "bossPhaseChanged":
                    _phaseBurst = 1f;
                    _shake = 1f;
                    Burst(_impact, _boss.Root.transform.position + Vector3.up * 1.7f, _coreFury, 40, 5f);
                    break;
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
