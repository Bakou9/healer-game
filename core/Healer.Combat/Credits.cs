using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Healer.Combat
{
    /// <summary>Une ressource ou un outil crédité dans le générique du jeu.</summary>
    public sealed class CreditEntry
    {
        public string Name { get; set; } = "";
        public string Author { get; set; } = "";
        /// <summary>Identifiant SPDX de la licence (voir CreditPolicy.AllowedLicenses).</summary>
        public string License { get; set; } = "";
        public string Url { get; set; } = "";
        /// <summary>À quoi la ressource sert dans le jeu (« Modèles des personnages »).</summary>
        public string UsedFor { get; set; } = "";
        /// <summary>Dossier des fichiers sous Assets/Art/Imported (ressources seulement) : une ressource importée sans entrée est refusée par les tests.</summary>
        public string? Folder { get; set; }
        /// <summary>Vrai si nous avons modifié la ressource (à indiquer avec une licence CC-BY).</summary>
        public bool Modified { get; set; }
    }

    /// <summary>Contenu de core/content/credits.json : les ressources externes (assets) et les outils remerciés au générique.</summary>
    public sealed class CreditsData
    {
        public string Title { get; set; } = "Healer Game";
        public List<CreditEntry> Assets { get; set; } = new List<CreditEntry>();
        public List<CreditEntry> Tools { get; set; } = new List<CreditEntry>();

        public static CreditsData FromJson(string json) => JsonConvert.DeserializeObject<CreditsData>(json) ?? new CreditsData();
    }

    /// <summary>
    /// Règles d'entrée d'une ressource externe (D-060) : licence libre autorisant l'usage commercial, auteur et source
    /// obligatoires, jamais de ressource sans crédit. Vérifié par des tests ; à respecter AVANT d'importer quoi que ce soit.
    /// </summary>
    public static class CreditPolicy
    {
        /// <summary>Licences acceptées pour les ressources : usage commercial permis. Toute autre licence demande l'accord de l'utilisateur.</summary>
        public static readonly IReadOnlyList<string> AllowedLicenses = new[] { "CC0-1.0", "CC-BY-4.0", "CC-BY-3.0", "MIT", "OFL-1.1", "Apache-2.0" };

        /// <summary>Licences de nos outils (le moteur) : pas de fichiers importés, seulement un remerciement.</summary>
        public static readonly IReadOnlyList<string> ToolLicenses = new[] { "Unity-Engine", "MIT", "Apache-2.0", "BSD-3-Clause" };

        public static IReadOnlyList<string> Problems(CreditEntry e, bool isAsset)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(e.Name)) list.Add("nom manquant");
            if (string.IsNullOrWhiteSpace(e.Author)) list.Add("auteur manquant");
            if (string.IsNullOrWhiteSpace(e.Url)) list.Add("source (URL) manquante");
            else if (!e.Url.StartsWith("https://", StringComparison.Ordinal)) list.Add("l'URL doit commencer par https://");
            if (string.IsNullOrWhiteSpace(e.UsedFor)) list.Add("usage manquant");
            var allowed = isAsset ? AllowedLicenses : ToolLicenses;
            if (!allowed.Contains(e.License)) list.Add($"licence « {e.License} » non autorisée (autorisées : {string.Join(", ", allowed)})");
            if (isAsset && string.IsNullOrWhiteSpace(e.Folder)) list.Add("dossier sous Assets/Art/Imported manquant");
            return list;
        }
    }
}
