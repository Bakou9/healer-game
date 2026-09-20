using System.Collections.Generic;
using System.Linq;
using Healer.Combat.Progress;
using Healer.Ui;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Galerie de modèles du mode développeur (D-065) : affiche seul, au centre, chaque héros (avec l'aspect de son arme et de
    /// son armure à chaque palier) et chaque boss (calme ou fureur), en rotation, avec les poses d'animation du jeu (repos,
    /// incantation, attaque, coup reçu, chute). Le combat est masqué pendant la visite et rétabli à la sortie. Aucune règle de
    /// jeu ici : uniquement de la présentation, avec les mêmes fabriques de modèles que le combat.
    /// </summary>
    public sealed class ModelGallery : MonoBehaviour
    {
        public struct Entry { public string Id, Name; public bool IsBoss; }

        public static readonly string[] PoseNames = { "Repos", "Incantation", "Attaque", "Coup reçu", "Chute" };
        public static readonly string[] TierLabels = { "Ordinaire", "Raffiné", "Légendaire" };
        /// <summary>Niveau d'équipement qui donne chaque palier (0-1, 2-3, 4-5) : voir appearance.json.</summary>
        private static readonly int[] LevelOfTier = { 0, 2, 4 };

        public List<Entry> Entries { get; } = new List<Entry>();
        public int Index { get; private set; }
        public int WeaponTier { get; private set; }
        public int ArmorTier { get; private set; }
        public int Pose { get; private set; }
        public bool Fury { get; private set; }
        public bool AutoRotate { get; private set; } = true;
        public string Info { get; private set; } = "";

        private GameFlow _flow = null!;
        private BattleStage _stage = null!;
        private Camera _cam = null!;
        private GameObject? _model;
        private UnitRig? _rig;
        private bool _active;
        private float _yaw, _zoom = 1f;
        private readonly List<GameObject> _hidden = new List<GameObject>();
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public Entry Current => Entries[Mathf.Clamp(Index, 0, Entries.Count - 1)];

        /// <summary>Change quand l'interface doit être redessinée (sélection, paliers, pose, rotation automatique).</summary>
        public string Signature => $"{Index}|{WeaponTier}|{ArmorTier}|{Pose}|{Fury}|{AutoRotate}|{Info}";

        public void Init(GameFlow flow, BattleStage stage, Camera cam)
        {
            _flow = flow; _stage = stage; _cam = cam;
            foreach (var c in flow.Content.Characters) Entries.Add(new Entry { Id = c.Id, Name = c.Role == "healer" ? c.Name + " (soigneuse)" : c.Name, IsBoss = false });
            foreach (var b in flow.Content.Bosses) Entries.Add(new Entry { Id = b.Id, Name = b.Name, IsBoss = true });
        }

        // ---- Commandes (appelées par l'interface) ----------------------------------------------------

        public void Select(int index) { Index = Mathf.Clamp(index, 0, Entries.Count - 1); Fury = false; Pose = 0; Rebuild(); }
        public void SetWeaponTier(int tier) { WeaponTier = tier; Rebuild(); }
        public void SetArmorTier(int tier) { ArmorTier = tier; Rebuild(); }
        public void SetPose(int pose) { Pose = pose; Info = Describe(); }
        public void SetFury(bool fury) { Fury = fury; }
        public void ToggleAutoRotate() { AutoRotate = !AutoRotate; }
        public void Turn(float degrees) { AutoRotate = false; _yaw += degrees; }
        public void Zoom(float factor) { _zoom = Mathf.Clamp(_zoom * factor, 0.5f, 2f); }

        // ---- Cycle de vie ------------------------------------------------------------------------------

        private void Update()
        {
            if (_flow == null) return;
            bool wanted = _flow.Screen == AppScreen.Gallery;
            if (wanted && !_active) Enter();
            else if (!wanted && _active) Exit();
            if (_active && _model != null) Animate();
        }

        private void Enter()
        {
            _active = true;
            _hidden.Clear();
            foreach (Transform child in _stage.transform)
                // Le système d'événements de l'interface est aussi un enfant de cet objet : le masquer couperait tous les clics.
                if (child.gameObject.activeSelf && child.GetComponent<UnityEngine.EventSystems.EventSystem>() == null) { _hidden.Add(child.gameObject); child.gameObject.SetActive(false); }
            Rebuild();
        }

        private void Exit()
        {
            _active = false;
            if (_model != null) Destroy(_model);
            _model = null; _rig = null;
            foreach (var g in _hidden) if (g != null) g.SetActive(true);
            _hidden.Clear();
        }

        private void Rebuild()
        {
            if (!_active) return;
            if (_model != null) DestroyImmediate(_model);
            var entry = Current;
            if (entry.IsBoss) _model = ModelFactory.ForBoss(entry.Id);
            else
            {
                var loadout = new Loadout();
                loadout.Equipment[entry.Id + "_weapon"] = LevelOfTier[WeaponTier];
                loadout.Equipment[entry.Id + "_armor"] = LevelOfTier[ArmorTier];
                var role = _flow.Content.Characters.FirstOrDefault(c => c.Id == entry.Id)?.Role ?? "dps";
                _model = ModelFactory.ForCharacter(entry.Id, role, _flow.Content.Appearance.Resolve(entry.Id, loadout));
            }
            _model.name = "Gallery_" + entry.Id;
            _rig = _model.GetComponent<UnitRig>();
            Info = Describe();
            Debug.Log($"[Healer] galerie : {entry.Id} {ModelFactory.TriangleCount(_model)} triangles");
        }

        private string Describe()
        {
            var entry = Current;
            int tris = _model != null ? ModelFactory.TriangleCount(_model) : 0;
            string text = $"{entry.Name} · {Format.Number(tris)} triangles · pose : {PoseNames[Pose]}";
            if (!entry.IsBoss)
            {
                var look = _flow.Content.Appearance.Resolve(entry.Id, LoadoutFor(entry.Id));
                var model = look.Parts.TryGetValue("weapon", out var w) ? w.Model : null;
                text += model != null ? " · arme importée : " + model.Path : " · arme dessinée par le code";
            }
            return text;
        }

        private Loadout LoadoutFor(string id)
        {
            var l = new Loadout();
            l.Equipment[id + "_weapon"] = LevelOfTier[WeaponTier];
            l.Equipment[id + "_armor"] = LevelOfTier[ArmorTier];
            return l;
        }

        // ---- Affichage -----------------------------------------------------------------------------------

        private void Animate()
        {
            var entry = Current;
            float t = Time.unscaledTime;
            if (AutoRotate) _yaw += 24f * Time.unscaledDeltaTime;

            float cast = Pose == 1 ? 0.5f + 0.5f * Mathf.Sin(t * 2.2f) : 0f;
            float lunge = Pose == 2 ? Mathf.Max(0f, Mathf.Sin(t * 3f)) : 0f;
            float recoil = Pose == 3 ? Mathf.Max(0f, Mathf.Sin(t * 3f)) : 0f;
            float fall = Pose == 4 ? 1f : 0f;
            _rig?.Apply(t, 0.3f, cast, lunge, recoil, fall);

            float scale = (entry.IsBoss ? 2.1f * ModelFactory.BossScaleFactor(entry.Id) : 2.2f) * _zoom;
            float depth = entry.IsBoss ? 32f : 20f;
            ScreenMap.Refresh();
            var vp = ScreenMap.LogicalToViewport((float)Layout.GameW / 2f, entry.IsBoss ? 640f : 620f);
            var feet = _cam.ViewportToWorldPoint(new Vector3(vp.x, vp.y, depth));
            _model!.transform.position = feet + new Vector3(0f, -fall * 0.3f * scale, 0f);
            _model.transform.localScale = Vector3.one * scale;
            _model.transform.rotation = Quaternion.Euler(-cast * 10f + lunge * 10f, 180f + _yaw, fall * 78f);

            if (entry.IsBoss) TintBoss(entry.Id);
        }

        /// <summary>Teinte les parties lumineuses d'un boss (Core, yeux, runes) : couleur calme ou de fureur.</summary>
        private void TintBoss(string bossId)
        {
            var (calm, fury) = ModelFactory.BossColors(bossId);
            var color = Fury ? fury : calm;
            foreach (var r in _model!.GetComponentsInChildren<Renderer>())
                if (r.name is "Core" or "EyeL" or "EyeR" or "Rune")
                {
                    _block.SetColor(ColorId, color * (0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 3f)));
                    r.SetPropertyBlock(_block);
                }
        }
    }
}
