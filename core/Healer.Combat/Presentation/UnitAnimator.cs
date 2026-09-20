using System;

namespace Healer.Combat.Presentation
{
    /// <summary>Attitude d'une unité à un instant : des valeurs de 0 à 1 que le client traduit en mouvements.</summary>
    public readonly struct UnitPose
    {
        /// <summary>Élan vers l'avant (une attaque qui part).</summary>
        public double Lunge { get; }
        /// <summary>Recul dû à un coup reçu.</summary>
        public double Recoil { get; }
        /// <summary>Geste de lancer (bras levé, bâton dressé).</summary>
        public double Cast { get; }
        /// <summary>Halo de soin ou de bouclier reçu.</summary>
        public double Glow { get; }
        /// <summary>Chute : 0 debout, 1 à terre.</summary>
        public double Fall { get; }

        public UnitPose(double lunge, double recoil, double cast, double glow, double fall)
        {
            Lunge = lunge; Recoil = recoil; Cast = cast; Glow = glow; Fall = fall;
        }
    }

    /// <summary>
    /// Machine à états d'animation d'UNE unité, nourrie par les événements du combat (patron Observer) : le combat
    /// ne sait rien des animations, le client ne calcule aucune attitude lui-même. Pure : mêmes événements, même
    /// attitude, à la milliseconde près. Durées en ms.
    /// </summary>
    public sealed class UnitAnimator
    {
        public const double LungeMs = 380;
        public const double RecoilMs = 320;
        public const double CastMs = 500;
        public const double GlowMs = 700;
        public const double FallMs = 750;

        private readonly string _id;
        private readonly bool _isBoss;
        private double _castDurationMs = CastMs;
        private double _lungeAt = double.NegativeInfinity, _recoilAt = double.NegativeInfinity, _castAt = double.NegativeInfinity, _glowAt = double.NegativeInfinity, _diedAt = double.PositiveInfinity;

        public UnitAnimator(string unitId, bool isBoss = false)
        {
            _id = unitId;
            _isBoss = isBoss;
        }

        public void OnEvent(BattleEvent e)
        {
            switch (e.Type)
            {
                case "bossDamaged":
                    if (_isBoss) _recoilAt = e.TimeMs;
                    else if (e.SourceId == _id) _lungeAt = e.TimeMs;
                    break;
                case "bossAction":
                    if (_isBoss) _lungeAt = e.TimeMs;
                    break;
                case "unitDamaged":
                    if (!_isBoss && e.UnitId == _id && e.Amount > 0) _recoilAt = e.TimeMs;
                    break;
                case "castStarted":
                    // Incantation : le geste dure tout le temps du sort (bâton levé jusqu'au lancer).
                    if (!_isBoss && e.CasterId == _id) { _castAt = e.TimeMs; _castDurationMs = Math.Max(CastMs, e.Amount); }
                    break;
                case "skillUsed":
                    // Un sort instantané lance un geste court ; à l'achèvement d'une incantation le geste se termine.
                    if (!_isBoss && e.CasterId == _id)
                    {
                        if (_castDurationMs > CastMs && e.TimeMs - _castAt <= _castDurationMs + 1) { _castAt = double.NegativeInfinity; _castDurationMs = CastMs; }
                        else { _castAt = e.TimeMs; _castDurationMs = CastMs; }
                    }
                    break;
                case "castFailed":
                    if (!_isBoss && e.CasterId == _id) { _castAt = double.NegativeInfinity; _castDurationMs = CastMs; }
                    break;
                case "healed":
                case "shielded":
                    if (!_isBoss && e.UnitId == _id) _glowAt = e.TimeMs;
                    break;
                case "unitDied":
                    if (!_isBoss && e.UnitId == _id) _diedAt = e.TimeMs;
                    break;
                case "battleStarted":
                    Reset();
                    break;
            }
        }

        public void Reset()
        {
            _lungeAt = _recoilAt = _castAt = _glowAt = double.NegativeInfinity;
            _castDurationMs = CastMs;
            _diedAt = double.PositiveInfinity;
        }

        /// <summary>Attitude à l'instant donné (horloge du combat, en ms).</summary>
        public UnitPose Sample(double nowMs) => new UnitPose(
            Pulse(nowMs - _lungeAt, LungeMs),
            Decay(nowMs - _recoilAt, RecoilMs),
            Pulse(nowMs - _castAt, _castDurationMs),
            Decay(nowMs - _glowAt, GlowMs),
            nowMs >= _diedAt ? Math.Min(1, Ease((nowMs - _diedAt) / FallMs)) : 0);

        /// <summary>Monte puis redescend (aller-retour d'un coup porté) : 0 → 1 à mi-durée → 0.</summary>
        private static double Pulse(double sinceMs, double durationMs)
        {
            if (sinceMs < 0 || sinceMs >= durationMs) return 0;
            double t = sinceMs / durationMs;
            return Math.Sin(Math.PI * t);
        }

        /// <summary>Part de 1 et s'éteint (un choc, un halo) : 1 à l'instant, 0 après durationMs.</summary>
        private static double Decay(double sinceMs, double durationMs)
        {
            if (sinceMs < 0 || sinceMs >= durationMs) return 0;
            double t = 1 - sinceMs / durationMs;
            return t * t;
        }

        private static double Ease(double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return t * t * (3 - 2 * t);
        }
    }
}
