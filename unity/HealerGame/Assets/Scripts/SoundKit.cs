using System;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Effets sonores générés par code (aucun fichier audio) : sinusoïdes, balayages et bruit déterministe,
    /// avec enveloppes courtes. Discrets et brefs ; le langage sonore double le langage visuel
    /// (soin = notes montantes, bouclier = tintement, danger = grondement grave, purge = balayage descendant).
    /// </summary>
    public static class SoundKit
    {
        private const int Rate = 44100;
        private const float Tau = 2f * Mathf.PI;

        private static float Noise(int i)
        {
            uint x = (uint)i * 2654435761u;
            x ^= x >> 15; x *= 2246822519u; x ^= x >> 13;
            return (x & 0xFFFF) / 32768f - 1f;
        }

        private static AudioClip Make(string name, float seconds, Func<float, int, float> sample)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(sample(i / (float)Rate, i), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Decay(float t, float rate) => Mathf.Exp(-rate * t);
        private static float Attack(float t) => Mathf.Clamp01(t / 0.004f);

        /// <summary>Balayage de fréquence f0 → f1 sur la durée dur (phase intégrée, sans clic).</summary>
        private static float Sweep(float t, float f0, float f1, float dur) => Mathf.Sin(Tau * (f0 * t + (f1 - f0) * t * t / (2f * dur)));

        public static AudioClip Heal() => Make("heal", 0.5f, (t, i) =>
        {
            float f = t < 0.14f ? 659f : 880f;
            float local = t < 0.14f ? t : t - 0.14f;
            return Attack(local) * Decay(local, 5f) * (0.42f * Mathf.Sin(Tau * f * t) + 0.12f * Mathf.Sin(Tau * f * 2f * t));
        });

        public static AudioClip Shield() => Make("shield", 0.45f, (t, i) =>
            Attack(t) * (0.42f * Decay(t, 9f) * Mathf.Sin(Tau * 1318f * t) + 0.18f * Decay(t, 14f) * Mathf.Sin(Tau * 2637f * t)));

        public static AudioClip Purge() => Make("purge", 0.4f, (t, i) => Attack(t) * Decay(t, 5f) * 0.4f * Sweep(t, 1500f, 400f, 0.4f));

        public static AudioClip Hit() => Make("hit", 0.22f, (t, i) =>
            Attack(t) * (0.7f * Decay(t, 20f) * Mathf.Sin(Tau * 95f * t) + 0.25f * Decay(t, 35f) * Noise(i)));

        public static AudioClip BossTick() => Make("bossTick", 0.12f, (t, i) => Attack(t) * 0.35f * Decay(t, 45f) * Noise(i));

        public static AudioClip Boom() => Make("boom", 1.0f, (t, i) =>
            Attack(t) * (0.85f * Decay(t, 3.2f) * Sweep(t, 62f, 38f, 1f) + 0.3f * Decay(t, 7f) * Noise(i)));

        public static AudioClip Roar() => Make("roar", 1.2f, (t, i) =>
        {
            float saw = 2f * (((85f - 25f * t) * t) % 1f) - 1f;
            return Attack(t) * Decay(t, 1.8f) * (0.45f * saw + 0.25f * Noise(i / 3));
        });

        public static AudioClip Poison() => Make("poison", 0.28f, (t, i) => Attack(t) * 0.32f * Decay(t, 9f) * Sweep(t, 300f, 900f, 0.28f));

        public static AudioClip Warning() => Make("warning", 0.34f, (t, i) =>
        {
            float local = t % 0.17f;
            return t < 0.34f ? Attack(local) * Decay(local, 12f) * 0.3f * Mathf.Sin(Tau * 880f * t) : 0f;
        });

        public static AudioClip Cast() => Make("cast", 0.16f, (t, i) => Attack(t) * 0.22f * Decay(t, 22f) * Mathf.Sin(Tau * 620f * t) * (1f + 0.5f * Mathf.Sin(Tau * 30f * t)));

        public static AudioClip Enrage() => Make("enrage", 1.1f, (t, i) =>
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Tau * 7f * t);
            return Attack(t) * Decay(t, 2.2f) * (0.5f * Sweep(t, 70f, 150f, 1.1f) * (0.6f + 0.4f * pulse) + 0.2f * Noise(i / 4));
        });

        public static AudioClip Death() => Make("death", 0.7f, (t, i) => Attack(t) * (0.6f * Decay(t, 6f) * Sweep(t, 220f, 60f, 0.7f) + 0.2f * Decay(t, 14f) * Noise(i)));

        public static AudioClip Dodge() => Make("dodge", 0.2f, (t, i) => Attack(t) * 0.25f * Decay(t, 14f) * Sweep(t, 1400f, 500f, 0.2f) + 0.05f * Decay(t, 30f) * Noise(i));

        public static AudioClip Crit() => Make("crit", 0.35f, (t, i) => Attack(t) * (0.5f * Decay(t, 12f) * Mathf.Sin(Tau * 1760f * t) + 0.35f * Decay(t, 9f) * Mathf.Sin(Tau * 110f * t) + 0.2f * Decay(t, 30f) * Noise(i)));

        public static AudioClip Click() => Make("click", 0.09f, (t, i) => Attack(t) * 0.3f * Decay(t, 40f) * Mathf.Sin(Tau * 900f * t));

        public static AudioClip Buy() => Make("buy", 0.45f, (t, i) =>
        {
            float f = t < 0.09f ? 988f : 1319f;
            float local = t < 0.09f ? t : t - 0.09f;
            return Attack(local) * Decay(local, 8f) * (0.3f * Mathf.Sin(Tau * f * t) + 0.1f * Mathf.Sin(Tau * f * 3f * t));
        });

        public static AudioClip Refuse() => Make("refuse", 0.25f, (t, i) => Attack(t) * 0.3f * Decay(t, 9f) * Mathf.Sign(Mathf.Sin(Tau * 120f * t)) * 0.6f);

        private static AudioClip Arpeggio(string name, float[] freqs, float step)
        {
            return Make(name, step * freqs.Length + 0.5f, (t, i) =>
            {
                int idx = Mathf.Min(freqs.Length - 1, Mathf.FloorToInt(t / step));
                float local = t - idx * step;
                float f = freqs[idx];
                return Attack(local) * Decay(local, 4f) * (0.36f * Mathf.Sin(Tau * f * t) + 0.1f * Mathf.Sin(Tau * f * 2f * t));
            });
        }

        public static AudioClip Victory() => Arpeggio("victory", new[] { 523f, 659f, 784f, 1046f }, 0.14f);
        public static AudioClip Defeat() => Arpeggio("defeat", new[] { 392f, 330f, 262f, 196f }, 0.2f);
    }
}
