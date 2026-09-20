using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Musique générée par code (aucun fichier audio) : une boucle de 8 mesures à 96 battements par minute en la
    /// mineur (La - Fa - Do - Sol), en QUATRE couches synchrones jouées ensemble à des volumes différents par le
    /// directeur musical du cœur : nappe, pulsation grave, mélodie, alarme aiguë. Toutes ont exactement la même durée
    /// pour rester alignées.
    /// </summary>
    public static class MusicKit
    {
        public const int Rate = 32000;
        private const float Bpm = 96f;
        private const int Bars = 8;
        private const float Tau = 2f * Mathf.PI;
        private static float Beat => 60f / Bpm;
        private static float BarSec => Beat * 4f;
        public static float LoopSec => BarSec * Bars;

        // Accords (fondamentales et tierces/quintes en Hz), deux mesures chacun.
        private static readonly float[][] Chords =
        {
            new[] { 110.00f, 130.81f, 164.81f }, // La mineur
            new[] { 87.31f, 110.00f, 130.81f },  // Fa majeur
            new[] { 130.81f, 164.81f, 196.00f }, // Do majeur
            new[] { 98.00f, 123.47f, 146.83f },  // Sol majeur
        };
        private static readonly float[] Roots = { 55.00f, 43.65f, 65.41f, 49.00f };
        private static readonly float[] Pentatonic = { 440.00f, 523.25f, 587.33f, 659.25f, 783.99f, 880.00f };

        public static AudioClip[] Generate()
        {
            return new[] { Layer("musique_nappe", Pad), Layer("musique_pulsation", Pulse), Layer("musique_melodie", Melody), Layer("musique_alarme", Alarm) };
        }

        private static AudioClip Layer(string name, System.Func<float, float> sample)
        {
            int n = Mathf.RoundToInt(LoopSec * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(sample(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static int ChordAt(float t) => Mathf.FloorToInt(t / (BarSec * 2f)) % 4;

        /// <summary>Poids d'un accord : 1 pendant sa fenêtre, fondu enchaîné doux aux frontières (boucle comprise).</summary>
        private static float ChordWeight(int chord, float t)
        {
            float window = BarSec * 2f, start = chord * window, fade = 0.45f;
            float d = t - start;
            if (d < 0) d += LoopSec;
            if (d >= window + fade) return 0f;
            float rise = Mathf.Clamp01(d / fade);
            float fall = d > window ? 1f - Mathf.Clamp01((d - window) / fade) : 1f;
            return Mathf.SmoothStep(0, 1, Mathf.Min(rise, fall));
        }

        private static float Pad(float t)
        {
            float s = 0f;
            for (int c = 0; c < 4; c++)
            {
                float wgt = ChordWeight(c, t);
                if (wgt <= 0f) continue;
                for (int k = 0; k < 3; k++)
                {
                    float f = Chords[c][k];
                    s += wgt * (0.10f * Mathf.Sin(Tau * f * t) + 0.05f * Mathf.Sin(Tau * f * 1.004f * t) + 0.03f * Mathf.Sin(Tau * f * 2f * t));
                }
            }
            return s * (0.85f + 0.15f * Mathf.Sin(Tau * t / (BarSec * 2f)));
        }

        private static float Pulse(float t)
        {
            float eighth = Beat / 2f;
            int idx = Mathf.FloorToInt(t / eighth);
            float local = t - idx * eighth;
            float root = Roots[ChordAt(t)] * ((idx % 8) == 3 || (idx % 8) == 7 ? 1.5f : 1f);
            float env = Mathf.Exp(-7f * local) * Mathf.Clamp01(local / 0.004f);
            float bass = env * (0.34f * Mathf.Sin(Tau * root * local) + 0.10f * Mathf.Sin(Tau * root * 2f * local));
            float beatLocal = t % Beat;
            int beat = Mathf.FloorToInt(t / Beat) % 4;
            float kick = (beat == 0 || beat == 2) ? Mathf.Exp(-18f * beatLocal) * Mathf.Sin(Tau * (95f * beatLocal - 220f * beatLocal * beatLocal)) * 0.32f : 0f;
            return bass + kick;
        }

        private static float Melody(float t)
        {
            float eighth = Beat / 2f;
            int idx = Mathf.FloorToInt(t / eighth);
            float local = t - idx * eighth;
            int chord = ChordAt(t);
            int[][] patterns =
            {
                new[] { 0, 2, 3, 2, 4, 3, 2, 0 },
                new[] { 1, 3, 4, 3, 5, 4, 3, 1 },
                new[] { 2, 4, 5, 4, 3, 2, 1, 2 },
                new[] { 0, 1, 3, 1, 2, 3, 4, 3 },
            };
            float f = Pentatonic[patterns[chord][idx % 8]];
            float env = Mathf.Exp(-5.5f * local) * Mathf.Clamp01(local / 0.004f);
            return env * (0.16f * Mathf.Sin(Tau * f * local) + 0.06f * Mathf.Sin(Tau * f * 2f * local) + 0.03f * Mathf.Sin(Tau * f * 3f * local));
        }

        private static float Alarm(float t)
        {
            float sixteenth = Beat / 4f;
            int idx = Mathf.FloorToInt(t / sixteenth);
            float local = t - idx * sixteenth;
            float f = (idx % 4 < 2) ? 1318.5f : 987.8f;
            float env = Mathf.Exp(-16f * local) * Mathf.Clamp01(local / 0.002f);
            float gate = (idx % 8 < 6) ? 1f : 0.4f;
            return gate * env * 0.10f * Mathf.Sign(Mathf.Sin(Tau * f * local)) * 0.6f;
        }
    }
}
