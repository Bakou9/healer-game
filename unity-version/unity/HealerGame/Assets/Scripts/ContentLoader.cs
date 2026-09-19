using System.IO;
using Healer.Combat;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Lit le contenu de jeu partagé (core/content/*.json), copié dans StreamingAssets/content par l'outil
    /// d'Éditeur (Healer.EditorTools). Le cœur ne lit jamais de fichier lui-même : c'est le client qui lui
    /// fournit le texte JSON. Une seule source de vérité : core/content.
    /// </summary>
    public static class ContentLoader
    {
        public static GameContent Load()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "content");
            string Read(string name) => File.ReadAllText(Path.Combine(dir, name));
            return GameContent.FromJson(Read("characters.json"), Read("skills.json"), Read("effects.json"), Read("boss1.json"));
        }
    }
}
