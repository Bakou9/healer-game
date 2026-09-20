using System;
using System.Collections.Generic;

namespace Healer.Combat.Presentation
{
    /// <summary>Les sons du jeu. Un son = une intention (« un soin passe »), jamais un fichier : le client choisit comment le jouer.</summary>
    public enum SoundCue
    {
        Cast,
        Heal,
        Shield,
        Purge,
        Hit,
        BossTick,
        Boom,
        Roar,
        Poison,
        Warning,
        Enrage,
        Death,
        Victory,
        Defeat,
        Click,
        Buy,
        Refuse,
    }

    public readonly struct CueRequest
    {
        public SoundCue Cue { get; }
        public double Volume { get; }

        public CueRequest(SoundCue cue, double volume)
        {
            Cue = cue;
            Volume = volume;
        }
    }

    /// <summary>
    /// Table événement → son (patron Observer : le son écoute le combat, le combat ne sait rien du son). Pure et
    /// testée : un même événement donne toujours le même son, quel que soit le moteur audio.
    /// </summary>
    public static class AudioCues
    {
        public static CueRequest? ForEvent(BattleEvent e)
        {
            switch (e.Type)
            {
                case "skillUsed": return new CueRequest(SoundCue.Cast, 0.35);
                case "healed": return e.Amount > 0 ? new CueRequest(SoundCue.Heal, 0.55) : (CueRequest?)null;
                case "shielded": return new CueRequest(SoundCue.Shield, 0.5);
                case "effectEnded": return e.Reason == "cleansed" ? new CueRequest(SoundCue.Purge, 0.5) : (CueRequest?)null;
                case "effectApplied": return new CueRequest(SoundCue.Poison, 0.5);
                case "unitDamaged": return e.Amount > 0 ? new CueRequest(SoundCue.Hit, 0.6) : (CueRequest?)null;
                case "unitDied": return new CueRequest(SoundCue.Death, 0.7);
                case "bossAction": return e.Action == "bigAttack" ? new CueRequest(SoundCue.Boom, 0.9) : new CueRequest(SoundCue.BossTick, 0.5);
                case "bossPhaseChanged": return new CueRequest(SoundCue.Roar, 0.8);
                case "bossEnraged": return new CueRequest(SoundCue.Enrage, 0.7);
                case "battleEnded": return new CueRequest(e.Result == BattleResults.Victory ? SoundCue.Victory : SoundCue.Defeat, 0.7);
                default: return null;
            }
        }
    }

    /// <summary>
    /// Empêche qu'un même son se répète trop vite (un soin de zone touche quatre alliés : un seul son, pas quatre).
    /// L'intervalle dépend du son. Pur : le client fournit le temps.
    /// </summary>
    public sealed class CueLimiter
    {
        private readonly Dictionary<SoundCue, double> _last = new Dictionary<SoundCue, double>();

        public static double MinIntervalSec(SoundCue cue)
        {
            switch (cue)
            {
                case SoundCue.BossTick: return 0.15;
                case SoundCue.Hit: return 0.05;
                case SoundCue.Victory:
                case SoundCue.Defeat:
                case SoundCue.Roar:
                case SoundCue.Enrage: return 1.5;
                default: return 0.06;
            }
        }

        /// <summary>Renvoie vrai (et mémorise l'instant) si le son peut être joué maintenant.</summary>
        public bool TryPlay(SoundCue cue, double nowSec)
        {
            if (_last.TryGetValue(cue, out var last) && nowSec >= last && nowSec - last < MinIntervalSec(cue)) return false;
            _last[cue] = nowSec;
            return true;
        }

        public void Reset() => _last.Clear();
    }

    /// <summary>Volumes choisis par le joueur (0 à 100) : le volume final d'un son est son volume × celui-ci.</summary>
    public static class Mix
    {
        public const int Step = 10;

        public static double Gain(int volumePercent, bool muted) => muted ? 0 : Math.Max(0, Math.Min(100, volumePercent)) / 100.0;

        /// <summary>Change un réglage de volume d'un pas (borné à 0-100, multiples de 10).</summary>
        public static int Adjust(int current, int direction)
        {
            int snapped = (int)Math.Round(Math.Max(0, Math.Min(100, current)) / (double)Step) * Step;
            return Math.Max(0, Math.Min(100, snapped + Math.Sign(direction) * Step));
        }
    }
}
