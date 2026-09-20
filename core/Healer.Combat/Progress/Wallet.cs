using System;
using System.Collections.Generic;

namespace Healer.Combat.Progress
{
    /// <summary>Une écriture du registre : qui a gagné ou dépensé quoi, et pourquoi.</summary>
    public sealed class LedgerEntry
    {
        public string Currency { get; set; } = "";
        public int Amount { get; set; }
        public string Reason { get; set; } = "";
        public int BalanceAfter { get; set; }
    }

    /// <summary>
    /// Portefeuille du joueur : UNIQUE point d'entrée des gains et dépenses (« Grant » et « TrySpend »).
    /// Tout gain ou coût du jeu passe ici, avec une raison : c'est ce qui permettra, plus tard, de brancher
    /// une boutique, des tirages ou un serveur sans réécrire le jeu (voir D-047). Pur, sans horloge.
    /// </summary>
    public sealed class Wallet
    {
        public const int MaxBalance = 999_999_999;
        public const int MaxLedgerEntries = 200;
        public const string Gold = "gold";

        private readonly Dictionary<string, int> _balances = new Dictionary<string, int>();
        private readonly List<LedgerEntry> _ledger = new List<LedgerEntry>();

        public IReadOnlyList<LedgerEntry> Ledger => _ledger;
        public IReadOnlyDictionary<string, int> Balances => _balances;

        public int Balance(string currency) => _balances.TryGetValue(currency, out var v) ? v : 0;

        public void Grant(string currency, int amount, string reason)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Un gain doit être strictement positif.");
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Un gain doit avoir une raison.", nameof(reason));
            long next = Math.Min((long)Balance(currency) + amount, MaxBalance);
            _balances[currency] = (int)next;
            Record(currency, amount, reason, (int)next);
        }

        /// <summary>Dépense si le solde suffit ; sinon ne fait rien et renvoie false (jamais de solde négatif).</summary>
        public bool TrySpend(string currency, int amount, string reason)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Une dépense doit être strictement positive.");
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Une dépense doit avoir une raison.", nameof(reason));
            int balance = Balance(currency);
            if (balance < amount) return false;
            _balances[currency] = balance - amount;
            Record(currency, -amount, reason, balance - amount);
            return true;
        }

        /// <summary>Restaure un solde depuis une sauvegarde (borné, jamais négatif).</summary>
        public void Restore(string currency, int balance, IEnumerable<LedgerEntry>? ledger)
        {
            _balances[currency] = Math.Max(0, Math.Min(balance, MaxBalance));
            if (ledger == null) return;
            foreach (var e in ledger) { _ledger.Add(e); Trim(); }
        }

        private void Record(string currency, int amount, string reason, int after)
        {
            _ledger.Add(new LedgerEntry { Currency = currency, Amount = amount, Reason = reason, BalanceAfter = after });
            Trim();
        }

        private void Trim()
        {
            while (_ledger.Count > MaxLedgerEntries) _ledger.RemoveAt(0);
        }
    }
}
