using System.Collections.Generic;
using Healer.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Healer.Client
{
    /// <summary>Sons du combat : réagit aux événements (Observer). Touche M = son coupé / rétabli.</summary>
    public sealed class BattleAudio : MonoBehaviour
    {
        private BattleController _ctl = null!;
        private AudioSource _source = null!;
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
        private bool _wasTelegraphing;
        public bool Muted { get; private set; }

        public void Init(BattleController controller)
        {
            _ctl = controller;
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _clips["heal"] = SoundKit.Heal();
            _clips["shield"] = SoundKit.Shield();
            _clips["purge"] = SoundKit.Purge();
            _clips["hit"] = SoundKit.Hit();
            _clips["bossTick"] = SoundKit.BossTick();
            _clips["boom"] = SoundKit.Boom();
            _clips["roar"] = SoundKit.Roar();
            _clips["poison"] = SoundKit.Poison();
            _clips["warning"] = SoundKit.Warning();
            _clips["victory"] = SoundKit.Victory();
            _clips["defeat"] = SoundKit.Defeat();
            controller.EventEmitted += OnEvent;
        }

        private void OnDestroy()
        {
            if (_ctl != null) _ctl.EventEmitted -= OnEvent;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.mKey.wasPressedThisFrame) Muted = !Muted;
            if (_ctl == null || _ctl.Battle == null) return;
            var telegraph = _ctl.Battle.GetTelegraph();
            bool telegraphing = telegraph != null && telegraph.Type == "bigAttack";
            if (telegraphing && !_wasTelegraphing) Play("warning", 0.5f);
            _wasTelegraphing = telegraphing;
        }

        /// <summary>Joue un son ; un même son ne se répète pas plus d'une fois toutes les 60 ms (les soins de zone touchent quatre alliés).</summary>
        private void Play(string key, float volume)
        {
            if (Muted || !_clips.TryGetValue(key, out var clip)) return;
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(key, out var last) && now - last < 0.06f) return;
            _lastPlayed[key] = now;
            _source.PlayOneShot(clip, volume);
        }

        private void OnEvent(BattleEvent e)
        {
            switch (e.Type)
            {
                case "healed": if (e.Amount > 0) Play("heal", 0.55f); break;
                case "shielded": Play("shield", 0.5f); break;
                case "effectEnded": if (e.Reason == "cleansed") Play("purge", 0.5f); break;
                case "effectApplied": Play("poison", 0.5f); break;
                case "unitDamaged": if (e.Amount > 0) Play("hit", 0.6f); break;
                case "bossAction": Play(e.Action == "bigAttack" ? "boom" : "bossTick", e.Action == "bigAttack" ? 0.9f : 0.5f); break;
                case "bossPhaseChanged": Play("roar", 0.8f); break;
                case "battleEnded": Play(e.Result == BattleResults.Victory ? "victory" : "defeat", 0.7f); break;
            }
        }
    }
}
