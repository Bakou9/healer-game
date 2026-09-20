using System.Collections.Generic;
using Healer.Combat;
using Healer.Combat.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Healer.Client
{
    /// <summary>
    /// Effets sonores : écoute les événements du combat (Observer) et joue le son que la table du cœur (AudioCues)
    /// désigne, à travers le limiteur (CueLimiter) et le volume réglé par le joueur (Mix). Touche M = son coupé.
    /// </summary>
    public sealed class BattleAudio : MonoBehaviour
    {
        private BattleController _ctl = null!;
        private GameFlow? _flow;
        private AudioSource _source = null!;
        private readonly Dictionary<SoundCue, AudioClip> _clips = new Dictionary<SoundCue, AudioClip>();
        private readonly CueLimiter _limiter = new CueLimiter();
        private bool _wasTelegraphing;

        public bool Muted => _flow != null && _flow.Profile.Settings.Muted;

        public void Init(BattleController controller, GameFlow flow)
        {
            _ctl = controller;
            _flow = flow;
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _clips[SoundCue.Cast] = SoundKit.Cast();
            _clips[SoundCue.Heal] = SoundKit.Heal();
            _clips[SoundCue.Shield] = SoundKit.Shield();
            _clips[SoundCue.Purge] = SoundKit.Purge();
            _clips[SoundCue.Hit] = SoundKit.Hit();
            _clips[SoundCue.BossTick] = SoundKit.BossTick();
            _clips[SoundCue.Boom] = SoundKit.Boom();
            _clips[SoundCue.Roar] = SoundKit.Roar();
            _clips[SoundCue.Poison] = SoundKit.Poison();
            _clips[SoundCue.Warning] = SoundKit.Warning();
            _clips[SoundCue.Enrage] = SoundKit.Enrage();
            _clips[SoundCue.Death] = SoundKit.Death();
            _clips[SoundCue.Victory] = SoundKit.Victory();
            _clips[SoundCue.Defeat] = SoundKit.Defeat();
            _clips[SoundCue.Dodge] = SoundKit.Dodge();
            _clips[SoundCue.Crit] = SoundKit.Crit();
            _clips[SoundCue.Click] = SoundKit.Click();
            _clips[SoundCue.Buy] = SoundKit.Buy();
            _clips[SoundCue.Refuse] = SoundKit.Refuse();
            controller.EventEmitted += OnEvent;
        }

        private void OnDestroy()
        {
            if (_ctl != null) _ctl.EventEmitted -= OnEvent;
        }

        private void Update()
        {
            var keyboard = PlaytestNotes.Open ? null : Keyboard.current;
            if (keyboard != null && keyboard.mKey.wasPressedThisFrame && _flow != null) _flow.ToggleMute();
            if (_ctl == null || _ctl.Battle == null) return;
            var telegraph = _ctl.Battle.GetTelegraph();
            bool telegraphing = telegraph != null && (telegraph.Type == "bigAttack" || telegraph.Type == "focusAttack");
            if (telegraphing && !_wasTelegraphing) Play(SoundCue.Warning, 0.5f);
            _wasTelegraphing = telegraphing;
        }

        /// <summary>Joue un son (combat ou interface) au volume des effets réglé par le joueur ; un même son ne se répète pas trop vite.</summary>
        public void Play(SoundCue cue, double volume)
        {
            if (_flow == null || !_clips.TryGetValue(cue, out var clip)) return;
            var settings = _flow.Profile.Settings;
            double gain = Mix.Gain(settings.SfxVolume, settings.Muted);
            if (gain <= 0 || !_limiter.TryPlay(cue, Time.unscaledTime)) return;
            _source.PlayOneShot(clip, (float)(volume * gain));
        }

        private void OnEvent(BattleEvent e)
        {
            var request = AudioCues.ForEvent(e);
            if (request != null) Play(request.Value.Cue, request.Value.Volume);
        }
    }
}
