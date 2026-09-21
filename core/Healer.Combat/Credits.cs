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
        /// <summary>Dossier des fichiers sous Assets/Resources/Imported (ressources seulement) : une ressource importée sans entrée est refusée par les tests.</summary>
        public string? Folder { get; set; }
        /// <summary>Vrai si nous avons modifié la ressource (à indiquer avec une licence CC-BY).</summary>
        public bool Modified { get; set; }
        /// <summary>Contenu généré par IA seulement : le prompt employé, conservé pour la déclaration exigée par les boutiques (D-075).</summary>
        public string? Prompt { get; set; }
    }

    /// <summary>Contenu de core/content/credits.json : les ressources externes (assets) et les outils remerciés au générique.</summary>
    public sealed class CreditsData
    {
        public string Title { get; set; } = "Healer Game";
        public List<CreditEntry> Assets { get; set; } = new List<CreditEntry>();
        public List<CreditEntry> Tools { get; set; } = new List<CreditEntry>();
        /// <summary>Contenus générés par IA (D-075) : ni ressource tierce sous licence, ni simple outil. Déclaration exigée par Steam et Google Play.</summary>
        public List<CreditEntry> Ai { get; set; } = new List<CreditEntry>();

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

        /// <summary>
        /// Licences de nos outils : pas de fichiers importés, seulement un remerciement. Deux cas (D-074) :
        /// un outil LIÉ au jeu (bibliothèque distribuée dans l'exécutable, comme Json.NET) exige une licence permissive ;
        /// un outil de PRODUCTION, utilisé sur notre machine et jamais distribué (Blender sous GPL, FFmpeg sous LGPL),
        /// peut être sous copyleft sans que cela touche le jeu ni les fichiers qu'il produit.
        /// </summary>
        public static readonly IReadOnlyList<string> ToolLicenses = new[] { "Unity-Engine", "MIT", "Apache-2.0", "BSD-3-Clause", "GPL-3.0", "LGPL-2.1" };

        /// <summary>
        /// Règles d'un contenu généré par IA (D-075). Ce n'est pas une ressource tierce sous licence : les conditions de l'outil
        /// nous cèdent la sortie, donc elle nous appartient. Ce qui compte ici, c'est la TRAÇABILITÉ : quel outil, quelle adresse,
        /// pour quoi, et avec quel prompt — c'est ce que Steam et Google Play demandent de déclarer.
        /// </summary>
        public static IReadOnlyList<string> AiProblems(CreditEntry e)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(e.Name)) list.Add("nom de l'outil manquant");
            if (string.IsNullOrWhiteSpace(e.Author)) list.Add("éditeur de l'outil manquant");
            if (string.IsNullOrWhiteSpace(e.Url)) list.Add("source (URL) manquante");
            else if (!e.Url.StartsWith("https://", StringComparison.Ordinal)) list.Add("l'URL doit commencer par https://");
            if (string.IsNullOrWhiteSpace(e.UsedFor)) list.Add("usage manquant");
            if (string.IsNullOrWhiteSpace(e.Prompt)) list.Add("prompt manquant (exigé pour la déclaration des boutiques)");
            return list;
        }

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
            if (isAsset && string.IsNullOrWhiteSpace(e.Folder)) list.Add("dossier sous Assets/Resources/Imported manquant");
            return list;
        }
    }
}
