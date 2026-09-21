using System.Collections.Generic;
using Healer.Combat.Presentation;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Pixel-art hero (experiment D-071, option -healer-sprites) : the 3D model is replaced by a camera-facing quad that shows the
    /// sprite frame matching the pose computed by the core (UnitPose). Sprites come from the 3D-to-pixel pipeline in art-pixel/
    /// (Resources/Sprites/Druid/druid_&lt;pose&gt;.png, point filtering). No game rule here : the pose selection only reads UnitPose.
    /// </summary>
    public sealed class SpriteHero : MonoBehaviour
    {
        /// <summary>Set by GameBootstrap from the command line (-healer-sprites).</summary>
        public static bool Enabled;

        private const string Folder = "Sprites/Druid/druid_";
        private static readonly Dictionary<string, Texture2D?> Cache = new Dictionary<string, Texture2D?>();

        /// <summary>World height (units) of the frame : the pipeline renders 4.3 m of the raw model, the game scales it by 0.79 (DruidHeight / raw height).</summary>
        private const float FrameUnits = 4.3f * 0.785f;
        /// <summary>The feet sit 0.2 m above the bottom of the frame (camera centred at 1.95 m, frame 4.3 m).</summary>
        private const float FeetOffsetUnits = 0.2f * 0.785f;

        private MeshRenderer _renderer = null!;
        private Transform _quad = null!;
        private string _current = "";

        public static bool Has(string heroId) => heroId == "healer" && Load("idle") != null;

        private static Texture2D? Load(string pose)
        {
            if (!Cache.TryGetValue(pose, out var tex))
            {
                tex = Resources.Load<Texture2D>(Folder + pose);
                Cache[pose] = tex;
            }
            return tex;
        }

        /// <summary>Builds the sprite hero with the same root contract as a 3D model : a UnitRig (which the stage drives) and renderers to tint.</summary>
        public static GameObject Create(string heroId)
        {
            var root = new GameObject("SpriteHero_" + heroId);
            var rig = root.AddComponent<UnitRig>();
            var hero = root.AddComponent<SpriteHero>();

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.name = "Sprite";
            quad.transform.SetParent(root.transform, false);
            quad.transform.localScale = new Vector3(-FrameUnits, FrameUnits, 1f);   // sprites are drawn facing left : mirrored so the hero faces the boss on the right
            quad.transform.localPosition = new Vector3(0f, FrameUnits / 2f - FeetOffsetUnits, 0f);
            var mr = quad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = Color.white };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            hero._renderer = mr;
            hero._quad = quad.transform;
            rig.PoseHook = hero.OnPose;
            hero.Show("idle");
            return root;
        }

        private void Show(string pose)
        {
            if (pose == _current) return;
            var tex = Load(pose) ?? Load("idle");
            if (tex != null) _renderer.sharedMaterial.mainTexture = tex;
            _current = pose;
        }

        /// <summary>Which frame for this pose : fall, hit, cast (raise then hold), release, otherwise idle.</summary>
        private void OnPose(in UnitPose p)
        {
            string frame = "idle";
            if (p.Fall > 0.5) frame = "fall";
            else if (p.Recoil > 0.25) frame = "hit";
            else if (p.CastDurationMs > 0)
            {
                if (p.Channeling) frame = p.CastElapsedMs < 260 ? "cast_raise" : "cast_hold";
                else frame = p.CastElapsedMs >= 140 && p.CastElapsedMs <= 340 ? "cast_release" : "cast_raise";
            }
            else if (p.Release > 0.25) frame = "cast_release";
            Show(frame);
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) _quad.rotation = cam.transform.rotation;   // always faces the camera, whatever the root rotation
        }
    }
}
