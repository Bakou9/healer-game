using System;

namespace Healer.Combat
{
    /// <summary>
    /// Accumulateur de temps (Game Loop à pas fixe). Le rendu tourne à la cadence de l'écran
    /// (delta variable) ; la simulation avance par pas identiques, ce qui la garde déterministe
    /// quel que soit le FPS de l'appareil.
    /// </summary>
    public sealed class FixedStepper
    {
        /// <summary>
        /// Pas de simulation fixe, en ms. Doit rester nettement inférieur aux plus petits intervalles
        /// des données : Battle.Step ne traite qu'une action par appel. Vérifié par un test.
        /// </summary>
        public const double DefaultStepMs = 50;

        private readonly double _stepMs;
        private readonly int _maxStepsPerFrame;
        private double _accumulatorMs;

        /// <param name="maxStepsPerFrame">Garde-fou : après un gros ralentissement, on abandonne le retard.</param>
        public FixedStepper(double stepMs = DefaultStepMs, int maxStepsPerFrame = 5)
        {
            _stepMs = stepMs;
            _maxStepsPerFrame = maxStepsPerFrame;
        }

        /// <summary>Appelle step(stepMs) autant de fois que nécessaire. Renvoie le nombre de pas exécutés.</summary>
        public int Advance(double deltaMs, Action<double> step)
        {
            _accumulatorMs += Math.Max(0, deltaMs);
            int steps = 0;
            while (_accumulatorMs >= _stepMs && steps < _maxStepsPerFrame)
            {
                step(_stepMs);
                _accumulatorMs -= _stepMs;
                steps++;
            }
            if (steps == _maxStepsPerFrame) _accumulatorMs = 0;
            return steps;
        }
    }
}
