using System;

namespace Healer.Combat.Presentation
{
    /// <summary>Ce que le directeur musical regarde à chaque image.</summary>
    public sealed class MusicInput
    {
        /// <summary>Plus petit ratio de PV parmi les alliés vivants (1 = tous en pleine forme).</summary>
        public double LowestAllyRatio { get; set; } = 1;
        public bool AttackTelegraphed { get; set; }
        public int BossPhase { get; set; }
        public int EnrageLevel { get; set; }
        /// <summary>Faux hors combat (menus) ou après la fin : la musique se calme.</summary>
        public bool InCombat { get; set; }
    }

    /// <summary>
    /// Musique adaptative : calcule une INTENSITÉ de 0 à 1 à partir de l'état du combat, puis le volume de chaque
    /// couche (nappe, pulsation, mélodie, alarme). Pur et testé ; le client ne fait que jouer les couches à ces
    /// volumes, en lissant les changements pour qu'on n'entende jamais de saut.
    /// </summary>
    public static class MusicDirector
    {
        public const int Layers = 4;

        /// <summary>0 = calme (menus, début de combat), 1 = danger maximal.</summary>
        public static double Intensity(MusicInput s)
        {
            if (!s.InCombat) return 0;
            double danger = 1 - Math.Max(0, Math.Min(1, s.LowestAllyRatio));
            double value = 0.25 + 0.5 * danger + (s.AttackTelegraphed ? 0.12 : 0) + 0.08 * Math.Min(3, Math.Max(0, s.BossPhase)) + 0.05 * Math.Min(4, Math.Max(0, s.EnrageLevel));
            return Math.Max(0, Math.Min(1, value));
        }

        /// <summary>Volume de chaque couche (0 à 1) pour une intensité donnée. La nappe est toujours là en combat, les autres arrivent avec le danger.</summary>
        public static double[] LayerVolumes(double intensity, bool inCombat)
        {
            double i = Math.Max(0, Math.Min(1, intensity));
            if (!inCombat) return new[] { 0.7, 0, 0, 0 }; // menus : nappe seule
            return new[]
            {
                0.55,                              // nappe
                Ramp(i, 0.25, 0.5),                // pulsation grave
                Ramp(i, 0.5, 0.75),                // mélodie
                Ramp(i, 0.78, 0.95),               // alarme aiguë
            };
        }

        private static double Ramp(double x, double from, double to)
        {
            if (x <= from) return 0;
            if (x >= to) return 1;
            double t = (x - from) / (to - from);
            return t * t * (3 - 2 * t);
        }

        /// <summary>Lissage exponentiel : monte en riseSec, redescend en fallSec (les montées de danger se ressentent vite, les retours au calme lentement).</summary>
        public static double Smooth(double current, double target, double dtSec, double riseSec = 0.6, double fallSec = 2.5)
        {
            double tau = target > current ? riseSec : fallSec;
            if (tau <= 0 || dtSec <= 0) return dtSec > 0 ? target : current;
            double k = 1 - Math.Exp(-dtSec / tau);
            return current + (target - current) * k;
        }
    }
}
