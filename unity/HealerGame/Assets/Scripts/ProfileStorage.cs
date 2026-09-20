using System;
using System.IO;
using Healer.Combat;
using Healer.Combat.Progress;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Lecture et écriture du profil sur le disque (le cœur ne touche jamais au disque). Écriture atomique :
    /// on écrit un fichier temporaire puis on le substitue, pour qu'une coupure ne laisse jamais un fichier
    /// à moitié écrit. Un fichier illisible est mis de côté (profile.corrupt-…) et jamais écrasé en silence.
    /// </summary>
    public sealed class ProfileStorage
    {
        private readonly string _dir;
        public string Path { get; }

        public ProfileStorage(string dir)
        {
            _dir = dir;
            Path = System.IO.Path.Combine(dir, "profile.json");
        }

        public PlayerProfile Load(GameContent content)
        {
            try
            {
                if (!File.Exists(Path)) return PlayerProfile.NewGame(content);
                if (ProfileStore.TryLoad(File.ReadAllText(Path), content, out var profile, out var error))
                {
                    Debug.Log($"[Healer] sauvegarde : chargée ({profile.TotalStars} étoiles, {profile.Wallet.Balance(Wallet.Gold)} or)");
                    return profile;
                }
                string aside = System.IO.Path.Combine(_dir, "profile.corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
                File.Copy(Path, aside, true);
                Debug.LogWarning($"[Healer] sauvegarde illisible ({error}) : copie conservée dans {aside}, nouvelle partie");
                return PlayerProfile.NewGame(content);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Healer] sauvegarde : lecture impossible (" + e.Message + "), nouvelle partie");
                return PlayerProfile.NewGame(content);
            }
        }

        public void Save(PlayerProfile profile)
        {
            try
            {
                Directory.CreateDirectory(_dir);
                string tmp = Path + ".tmp";
                File.WriteAllText(tmp, ProfileStore.ToJson(profile));
                if (File.Exists(Path))
                {
                    try { File.Replace(tmp, Path, Path + ".bak"); }
                    catch (Exception) { File.Copy(tmp, Path, true); File.Delete(tmp); }
                }
                else File.Move(tmp, Path);
                Debug.Log($"[Healer] sauvegarde : écrite ({profile.TotalStars} étoiles, {profile.Wallet.Balance(Wallet.Gold)} or)");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Healer] sauvegarde : écriture impossible (" + e.Message + ")");
            }
        }
    }
}
