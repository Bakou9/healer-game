using System.IO;
using Healer.Combat;
using UnityEngine;
using UnityEngine.Networking;

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
#if UNITY_ANDROID && !UNITY_EDITOR
            // Sur Android, StreamingAssets est dans l'APK (jar:file://) : lecture par UnityWebRequest.
            string Read(string name)
            {
                using var request = UnityWebRequest.Get(Path.Combine(dir, name));
                var op = request.SendWebRequest();
                while (!op.isDone) { }
                if (request.result != UnityWebRequest.Result.Success) throw new IOException("Lecture impossible : " + name + " (" + request.error + ")");
                return request.downloadHandler.text;
            }
#else
            string Read(string name) => File.ReadAllText(Path.Combine(dir, name));
#endif
            return GameContent.FromJson(Read("characters.json"), Read("skills.json"), Read("effects.json"), Read("boss1.json"));
        }
    }
}
