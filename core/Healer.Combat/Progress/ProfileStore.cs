using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Healer.Combat.Progress
{
    /// <summary>
    /// Sérialisation du profil (JSON, versionné, tolérant). Le cœur ne touche jamais au disque : le client lit
    /// et écrit le texte. Un fichier illisible n'est jamais une erreur fatale : on repart d'une partie neuve
    /// (le client garde une copie du fichier abîmé).
    /// </summary>
    public static class ProfileStore
    {
        public static string ToJson(PlayerProfile p)
        {
            var levels = new JObject();
            foreach (var kv in p.Levels.OrderBy(k => k.Key, StringComparer.Ordinal))
                levels[kv.Key] = new JObject
                {
                    ["completed"] = kv.Value.Completed,
                    ["bestStars"] = kv.Value.BestStars,
                    ["bestTimeMs"] = kv.Value.BestTimeMs,
                    ["clears"] = kv.Value.Clears,
                };
            var balances = new JObject();
            foreach (var kv in p.Wallet.Balances.OrderBy(k => k.Key, StringComparer.Ordinal)) balances[kv.Key] = kv.Value;
            var root = new JObject
            {
                ["version"] = PlayerProfile.CurrentVersion,
                ["levels"] = levels,
                ["owned"] = new JArray(p.OwnedCharacters),
                ["wallet"] = balances,
                ["ledger"] = JArray.FromObject(p.Wallet.Ledger),
                ["settings"] = new JObject
                {
                    ["muted"] = p.Settings.Muted,
                    ["musicVolume"] = p.Settings.MusicVolume,
                    ["sfxVolume"] = p.Settings.SfxVolume,
                    ["screenShake"] = p.Settings.ScreenShake,
                },
                ["equipment"] = new JObject(p.Loadout.Equipment.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => new JProperty(k.Key, k.Value))),
                ["talents"] = new JObject(p.Loadout.Talents.OrderBy(k => k.Key).Select(k => new JProperty(k.Key.ToString(), k.Value))),
            };
            return root.ToString(Formatting.Indented);
        }

        private static int Clamp(int volume) => Math.Max(0, Math.Min(100, volume));

        /// <summary>Lit un profil. Renvoie false (et un profil neuf) si le texte est vide, illisible ou d'une version future.</summary>
        public static bool TryLoad(string? json, GameContent content, out PlayerProfile profile, out string error)
        {
            profile = PlayerProfile.NewGame(content);
            error = "";
            if (string.IsNullOrWhiteSpace(json)) { error = "sauvegarde vide"; return false; }
            try
            {
                var root = JObject.Parse(json);
                int version = root.Value<int?>("version") ?? 0;
                if (version < 1 || version > PlayerProfile.CurrentVersion) { error = $"version de sauvegarde non gérée : {version}"; return false; }

                var loaded = new PlayerProfile { Version = version };
                if (root["levels"] is JObject levels)
                    foreach (var kv in levels)
                        if (kv.Value is JObject o)
                            loaded.Levels[kv.Key] = new LevelRecord
                            {
                                Completed = o.Value<bool?>("completed") ?? false,
                                BestStars = o.Value<int?>("bestStars") ?? 0,
                                BestTimeMs = o.Value<double?>("bestTimeMs") ?? 0,
                                Clears = o.Value<int?>("clears") ?? 0,
                            };
                if (root["owned"] is JArray owned) loaded.OwnedCharacters.AddRange(owned.Values<string>().Where(s => !string.IsNullOrEmpty(s))!);
                var ledger = root["ledger"] is JArray l ? l.ToObject<List<LedgerEntry>>() : null;
                if (root["wallet"] is JObject wallet)
                    foreach (var kv in wallet) loaded.Wallet.Restore(kv.Key, kv.Value?.Value<int>() ?? 0, kv.Key == Wallet.Gold ? ledger : null);
                loaded.Settings.Muted = root["settings"]?.Value<bool?>("muted") ?? false;
                loaded.Settings.MusicVolume = Clamp(root["settings"]?.Value<int?>("musicVolume") ?? Settings.DefaultMusic);
                loaded.Settings.SfxVolume = Clamp(root["settings"]?.Value<int?>("sfxVolume") ?? Settings.DefaultSfx);
                loaded.Settings.ScreenShake = root["settings"]?.Value<bool?>("screenShake") ?? true;
                if (root["equipment"] is JObject equipment)
                    foreach (var kv in equipment) loaded.Loadout.Equipment[kv.Key] = kv.Value?.Value<int>() ?? 0;
                if (root["talents"] is JObject talents)
                    foreach (var kv in talents)
                        if (int.TryParse(kv.Key, out int tier) && kv.Value?.Value<string>() is string option) loaded.Loadout.Talents[tier] = option;

                loaded.Repair(content);
                profile = loaded;
                return true;
            }
            catch (Exception e) when (e is JsonException || e is InvalidCastException || e is FormatException || e is ArgumentException)
            {
                error = "sauvegarde illisible : " + e.Message;
                profile = PlayerProfile.NewGame(content);
                return false;
            }
        }
    }
}
