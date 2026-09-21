using System.Collections.Generic;
using System.Globalization;
using Healer.Combat.Presentation;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Pixel-art hero (experiment D-071 / D-073, option -healer-sprites) : the 3D model is replaced by a camera-facing quad that plays
    /// the sprite sheets of art-pixel/ (Resources/Sprites/Druid/druid_&lt;animation&gt;.png, frames side by side, point filtering, listed in
    /// druid_anims.txt). Which animation and which frame come only from the pose computed by the core (UnitPose) : no game rule here.
    /// </summary>
    public sealed class SpriteHero : MonoBehaviour
    {
        /// <summary>Set by GameBootstrap from the command line (-healer-sprites).</summary>
        public static bool Enabled;

        /// <summary>Sprite set folder and file prefix of each hero (Resources/Sprites/&lt;Folder&gt;/&lt;prefix&gt;_&lt;animation&gt;.png and &lt;prefix&gt;_anims.txt).</summary>
        private static readonly Dictionary<string, (string Folder, string Prefix)> Sets = new Dictionary<string, (string, string)>
        {
            ["healer"] = ("Druid", "druid"),
            ["dps1"] = ("Archer", "archer"),
        };

        private sealed class Clip
        {
            public Texture2D Sheet = null!;
            public int Frames, Fps;
            public bool Loop;
        }

        private static readonly Dictionary<string, Dictionary<string, Clip>> ClipSets = new Dictionary<string, Dictionary<string, Clip>>();
        private static readonly Dictionary<string, int> FrameSizes = new Dictionary<string, int>();

        /// <summary>World height (units) of the frame : the pipeline renders 4.3 m of the raw model, the game scales it by 0.79 (DruidHeight / raw height).</summary>
        private const float FrameUnits = 4.3f * 0.785f;
        /// <summary>The feet sit 0.2 m above the bottom of the frame (camera centred at 1.95 m, frame 4.3 m).</summary>
        private const float FeetOffsetUnits = 0.2f * 0.785f;

        private Material _material = null!;
        private Transform _quad = null!;
        private Mesh _mesh = null!;
        private string _heroId = "";
        private int _framePixels = 128;
        private string _clip = "";
        private int _frame = -1;

        public static bool Has(string heroId) => Sets.ContainsKey(heroId) && Clips(heroId).ContainsKey("idle");

        private static Dictionary<string, Clip> Clips(string heroId)
        {
            if (ClipSets.TryGetValue(heroId, out var cached)) return cached;
            var clips = new Dictionary<string, Clip>();
            ClipSets[heroId] = clips;
            if (!Sets.TryGetValue(heroId, out var set)) return clips;
            var text = Resources.Load<TextAsset>($"Sprites/{set.Folder}/{set.Prefix}_anims");
            if (text == null) return clips;
            FrameSizes[heroId] = 128;
            foreach (var raw in text.text.Split('\n'))
            {
                var t = raw.Trim().Split(' ');
                if (t.Length == 2 && t[0] == "size") { FrameSizes[heroId] = int.Parse(t[1], CultureInfo.InvariantCulture); continue; }
                if (t.Length != 4) continue;
                var sheet = Resources.Load<Texture2D>($"Sprites/{set.Folder}/{set.Prefix}_{t[0]}");
                if (sheet == null) continue;
                clips[t[0]] = new Clip { Sheet = sheet, Frames = int.Parse(t[1], CultureInfo.InvariantCulture), Fps = int.Parse(t[2], CultureInfo.InvariantCulture), Loop = t[3] == "1" };
            }
            return clips;
        }

        /// <summary>Builds the sprite hero with the same root contract as a 3D model : a UnitRig (which the stage drives) and renderers to tint.</summary>
        public static GameObject Create(string heroId)
        {
            var root = new GameObject("SpriteHero_" + heroId);
            var rig = root.AddComponent<UnitRig>();
            var hero = root.AddComponent<SpriteHero>();
            hero._heroId = heroId;
            hero._framePixels = FrameSizes.TryGetValue(heroId, out var px) ? px : 128;
            Clips(heroId);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.name = "Sprite";
            quad.transform.SetParent(root.transform, false);
            quad.transform.localScale = new Vector3(-FrameUnits, FrameUnits, 1f);   // sprites are drawn facing left : mirrored so the hero faces the boss on the right
            quad.transform.localPosition = new Vector3(0f, FrameUnits / 2f - FeetOffsetUnits, 0f);
            var mr = quad.GetComponent<MeshRenderer>();
            hero._material = new Material(Shader.Find("Sprites/Default")) { color = Color.white };
            mr.sharedMaterial = hero._material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            hero._quad = quad.transform;
            hero._mesh = quad.GetComponent<MeshFilter>().mesh;   // instance : its UVs pick the frame (Sprites/Default ignores texture scale and offset)
            rig.PoseHook = hero.OnPose;
            hero.Show("idle", 0f);
            return root;
        }

        /// <summary>Shows frame `progress` (0 to 1) of a clip : the sheet is one row of frames, so a texture offset and scale pick the frame.</summary>
        private void Show(string clipName, float progress)
        {
            if (!Clips(_heroId).TryGetValue(clipName, out var clip)) return;
            int frame = Mathf.Clamp(Mathf.FloorToInt(progress * clip.Frames), 0, clip.Frames - 1);
            if (clipName == _clip && frame == _frame) return;
            if (clipName != _clip) _material.mainTexture = clip.Sheet;
            float u0 = frame / (float)clip.Frames, u1 = (frame + 1) / (float)clip.Frames;
            _mesh.uv = new[] { new Vector2(u0, 0f), new Vector2(u1, 0f), new Vector2(u0, 1f), new Vector2(u1, 1f) };   // Unity quad vertex order : bottom-left, bottom-right, top-left, top-right
            _clip = clipName;
            _frame = frame;
        }

        /// <summary>Looping clip : frame from the clock, at the clip's own speed.</summary>
        private void Play(string clipName)
        {
            if (!Clips(_heroId).TryGetValue(clipName, out var clip)) return;
            Show(clipName, Mathf.Repeat(Time.time * clip.Fps / clip.Frames, 1f));
        }

        /// <summary>Which clip and frame for this pose : death, hit, cast (raise, hold, release), attack, otherwise idle.</summary>
        private void OnPose(in UnitPose p)
        {
            if (p.Fall > 0.001) { Show("death", (float)p.Fall * 0.999f); return; }
            if (p.Recoil > 0.02) { Show("hit", 1f - Mathf.Sqrt((float)p.Recoil)); return; }                       // Recoil decays as (1 - t)^2
            if (p.CastDurationMs > 0)
            {
                float e = (float)p.CastElapsedMs;
                if (p.Channeling)
                {
                    if (e < 280f) Show("cast_raise", e / 280f); else Play("cast_hold");
                }
                else if (e < 150f) Show("cast_raise", e / 150f);
                else Show("cast_release", (e - 150f) / 350f);                                                       // instant spell : quick raise, then the release
                return;
            }
            if (p.Release > 0.02) { Show("cast_release", 1f - Mathf.Sqrt((float)p.Release)); return; }             // channeled spell finished : release
            if (p.LungeProgress > 0) { Show("attack", (float)p.LungeProgress); return; }
            Play("idle");
        }

        /// <summary>Reference height of the pixel-art grid : one sprite pixel = round(Screen.height / 360) screen pixels (2 at 1280x720, 3 at 1080p).</summary>
        private const float ReferenceHeight = 360f;

        /// <summary>
        /// Pixel-perfect placement (what a PixelPerfectCamera does, done here because the battle camera is still perspective) : the quad always
        /// faces the camera, is sized so that each sprite pixel covers a whole number of screen pixels, and its corner is snapped to the screen
        /// pixel grid, so the sprite stays crisp, never shimmers and never gets fractional pixels, whatever the depth or the root scale.
        /// </summary>
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var ct = cam.transform;
            _quad.rotation = ct.rotation;

            float texel = Mathf.Max(1f, Mathf.Round(Screen.height / ReferenceHeight));
            Vector3 feet = transform.position;
            float depth = Vector3.Dot(feet - ct.position, ct.forward);
            if (depth < 0.1f) return;
            float unitsPerPixel = 2f * depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;
            float worldSize = _framePixels * texel * unitsPerPixel;
            float root = Mathf.Max(0.0001f, transform.lossyScale.x);
            _quad.localScale = new Vector3(-worldSize / root, worldSize / root, 1f);

            // frame centre = feet + up by (half a frame - the margin under the feet), then snap the frame's bottom-left corner to a whole screen pixel
            float centerLift = worldSize * (0.5f - FeetMarginFraction);
            Vector3 center = feet + ct.up * centerLift;
            Vector3 sp = cam.WorldToScreenPoint(center);
            float half = _framePixels * texel * 0.5f;
            sp.x = Mathf.Round(sp.x - half) + half;
            sp.y = Mathf.Round(sp.y - half) + half;
            _quad.position = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, depth));
        }

        /// <summary>Margin under the feet, as a fraction of the frame (0.2 m of 4.3 m in the pipeline framing).</summary>
        private const float FeetMarginFraction = 0.2f / 4.3f;
    }
}
