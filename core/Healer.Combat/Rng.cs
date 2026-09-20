using System;
using System.Collections.Generic;

namespace Healer.Combat
{
    /// <summary>
    /// PRNG déterministe (mulberry32), identique à la version TypeScript : à seed égale et
    /// commandes égales, un combat rejoue exactement de la même façon. C'est le SEUL générateur
    /// aléatoire autorisé dans le cœur (System.Random est interdit, vérifié par test).
    /// </summary>
    public sealed class Rng
    {
        private uint _a;

        public Rng(uint seed)
        {
            _a = seed;
        }

        /// <summary>Nombre dans [0, 1).</summary>
        public double Next()
        {
            unchecked
            {
                _a += 0x6D2B79F5u;
                uint t = (_a ^ (_a >> 15)) * (1u | _a);
                t = (t + (t ^ (t >> 7)) * (61u | t)) ^ t;
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }

        public T PickRandom<T>(IReadOnlyList<T> items)
        {
            if (items.Count == 0) throw new InvalidOperationException("PickRandom : la liste est vide");
            int index = (int)Math.Floor(Next() * items.Count);
            return items[Math.Min(index, items.Count - 1)];
        }
    }
}
