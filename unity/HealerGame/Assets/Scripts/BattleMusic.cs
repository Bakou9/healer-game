using Healer.Combat.Presentation;
using Healer.Ui;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Joue les quatre couches de MusicKit en boucle et règle leurs volumes avec le directeur musical du cœur
    /// (MusicDirector) : rien ici ne décide de l'intensité, on ne fait que lire l'état et lisser les changements.
    /// </summary>
    public sealed class BattleMusic : MonoBehaviour
    {
        private GameFlow _flow = null!;
        private AudioSource[] _sources = null!;
        private readonly double[] _current = new double[MusicDirector.Layers];
        private double _intensity;

        public void Init(GameFlow flow)
        {
            _flow = flow;
            var clips = MusicKit.Generate();
            _sources = new AudioSource[clips.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.clip = clips[i];
                s.loop = true;
                s.playOnAwake = false;
                s.volume = 0f;
                s.spatialBlend = 0f;
                _sources[i] = s;
            }
            // Démarrage simultané au même instant : les couches restent alignées.
            double start = AudioSettings.dspTime + 0.2;
            foreach (var s in _sources) s.PlayScheduled(start);
        }

        private void Update()
        {
            if (_flow == null || _sources == null) return;
            var ctl = _flow.Ctl;
            bool inBattle = _flow.Screen == AppScreen.Battle && ctl.Battle != null;
            var state = inBattle ? ctl.State : ScreenState.Start;
            bool playing = inBattle && (state == ScreenState.Playing || state == ScreenState.Paused);

            var input = new MusicInput { InCombat = playing };
            if (playing)
            {
                var battle = ctl.Battle;
                double lowest = 1;
                foreach (var a in battle.GetAllies()) if (a.Alive) lowest = System.Math.Min(lowest, a.Hp / a.MaxHp);
                var tele = battle.GetTelegraph();
                input.LowestAllyRatio = lowest;
                input.AttackTelegraphed = tele != null && tele.Type == "bigAttack";
                input.BossPhase = battle.GetBossPhase().Index;
                input.EnrageLevel = battle.GetEnrageLevel();
            }

            float dt = Time.unscaledDeltaTime;
            _intensity = MusicDirector.Smooth(_intensity, MusicDirector.Intensity(input), dt);
            var targets = MusicDirector.LayerVolumes(_intensity, playing);
            var settings = _flow.Profile.Settings;
            double master = Mix.Gain(settings.MusicVolume, settings.Muted) * 0.55 * (state == ScreenState.Paused ? 0.4 : 1.0);
            for (int i = 0; i < _sources.Length; i++)
            {
                _current[i] = MusicDirector.Smooth(_current[i], targets[i], dt, 0.8, 1.6);
                _sources[i].volume = (float)(_current[i] * master);
            }
        }
    }
}
