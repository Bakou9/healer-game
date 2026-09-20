using System;
using System.Collections;
using System.IO;
using System.Linq;
using Healer.Combat.Progress;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Point d'entrée : assemble contenu, profil, contrôleur de combat, scène 3D, interface et navigation.
    /// Options de ligne de commande (vérifications automatiques, jamais utilisées en jeu normal) :
    ///   -healer-shots 8,30,46   captures d'écran à ces secondes de combat (le bot de référence joue)
    ///   -healer-screen menu|levels|workshop|settings|credits   avec -healer-shots : capture d'un écran hors combat (secondes réelles)
    ///   -healer-level l2        démarre directement ce niveau
    ///   -healer-out DOSSIER     dossier des captures
    ///   -healer-speed 6         accélération du temps pendant les captures
    ///   -healer-timescale 30    accélération du temps en jeu normal (tests de bout en bout : tools/unity-e2e.ps1)
    ///   -healer-notes-dir D     dossier des notes de playtest (F8) ; par défaut playtest/ dans les données du jeu
    ///   -healer-dev             mode développeur : Atelier avec niveaux d'équipement ± et or à 99 999, sauvegarde séparée (dev-profile)
    ///   -healer-e2e FICHIER     entrées injectées par le script de test (souris et clavier virtuels, sans focus ; voir E2eInput)
    ///   -healer-sound           avec -healer-e2e : laisse le son (par défaut les tests sont silencieux)
    ///   -healer-autoplay        le bot de référence joue le soin (tests de bout en bout)
    ///   -healer-profile-dir D   dossier de la sauvegarde (par défaut : dossier de données du jeu)
    ///   -healer-progress l1:3,l2:2   précharge une progression (captures)
    ///   -healer-gold 400        ajoute de l'or au profil (tests de l'atelier)
    ///   -healer-equip tank_weapon:3,t1:quick_heal   précharge équipement et talents (captures)
    /// Avec -healer-shots, la sauvegarde va dans un dossier temporaire : on ne touche jamais à la vraie.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Start()
        {
            Application.targetFrameRate = 60;
            string[] args = Environment.GetCommandLineArgs();
            string? Arg(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
            string? shots = Arg("-healer-shots");

            var content = ContentLoader.Load();
            bool dev = args.Contains("-healer-dev");
            // Mode développeur : sauvegarde SÉPARÉE (dev-profile) pour ne jamais fausser la vraie progression.
            string profileDir = Arg("-healer-profile-dir") ?? (shots != null ? Path.Combine(Application.temporaryCachePath, "capture-profile") : dev ? Path.Combine(Application.persistentDataPath, "dev-profile") : Application.persistentDataPath);
            var storage = new ProfileStorage(profileDir);
            var profile = shots != null && Arg("-healer-profile-dir") == null ? PlayerProfile.NewGame(content) : storage.Load(content);
            ApplyProgress(profile, content, Arg("-healer-progress"));
            if (int.TryParse(Arg("-healer-gold"), out int gold) && gold > 0) profile.Wallet.Grant(Wallet.Gold, gold, "option de test");
            ApplyEquip(profile, content, Arg("-healer-equip"));

            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            var controller = gameObject.AddComponent<BattleController>();
            controller.StartLevel(content, content.Levels[0], profile.OwnedCharacters, profile.Loadout, (uint)(Environment.TickCount & 0x7fffffff));
            var stage = gameObject.AddComponent<BattleStage>();
            stage.Build(cam, controller);
            var flow = gameObject.AddComponent<GameFlow>();
            flow.Init(content, storage, profile, controller, stage);
            flow.Credits = ContentLoader.LoadCredits();
            flow.DevMode = dev;
            if (dev) Debug.Log("[Healer] mode développeur : sauvegarde dans " + profileDir);
            var ui = gameObject.AddComponent<UiRoot>();
            ui.Init(flow);
            gameObject.AddComponent<PlaytestNotes>().Init(ui, flow, Arg("-healer-notes-dir") ?? Path.Combine(Application.persistentDataPath, "playtest"));
            var audio = gameObject.AddComponent<BattleAudio>();
            audio.Init(controller, flow);
            flow.Audio = audio;
            gameObject.AddComponent<BattleMusic>().Init(flow);
            gameObject.AddComponent<BattleKeys>().Init(flow);
            string? e2e = Arg("-healer-e2e");
            if (e2e != null) gameObject.AddComponent<E2eInput>().Init(e2e);
            if (e2e != null && !args.Contains("-healer-sound")) AudioListener.volume = 0f; // tests de bout en bout silencieux, sauf -healer-sound

            Debug.Log("[Healer] jeu prêt"); // signal lu par les tests de bout en bout (le démarrage à froid est plus long)
            if (float.TryParse(Arg("-healer-timescale"), out var fast)) controller.TimeScale = fast;
            if (args.Contains("-healer-autoplay")) controller.Autoplay = true;
            string? level = Arg("-healer-level");

            if (shots != null)
            {
                float speed = float.TryParse(Arg("-healer-speed"), out var s) ? s : 6f;
                var seconds = shots.Split(',').Select(x => float.Parse(x.Trim())).OrderBy(x => x).ToList();
                string dir = Arg("-healer-out") ?? Path.Combine(Application.persistentDataPath, "captures");
                Directory.CreateDirectory(dir);
                string? screen = Arg("-healer-screen");
                if (screen == "menu" || screen == "levels" || screen == "workshop" || screen == "settings" || screen == "credits")
                {
                    if (screen == "levels") flow.OpenLevels();
                    if (screen == "workshop") flow.OpenWorkshop();
                    if (screen == "settings") flow.OpenSettings();
                    if (screen == "credits") flow.OpenCredits();
                    StartCoroutine(RealtimeCaptureRoutine(seconds, dir));
                    return;
                }
                flow.StartAt(level ?? "l1");
                controller.StartFight();
                controller.Autoplay = true;
                controller.TimeScale = speed;
                controller.TapAlly("tank");
                StartCoroutine(CaptureRoutine(controller, seconds, dir));
            }
            else if (level != null) flow.StartAt(level);
        }

        /// <summary>Précharge une progression pour les captures : « l1:3,l2:2 » = niveau 1 à 3 étoiles, niveau 2 à 2 étoiles.</summary>
        private static void ApplyProgress(PlayerProfile profile, Healer.Combat.GameContent content, string? spec)
        {
            if (string.IsNullOrEmpty(spec)) return;
            foreach (var part in spec.Split(','))
            {
                var kv = part.Split(':');
                if (kv.Length != 2 || !int.TryParse(kv[1], out int stars)) continue;
                profile.Levels[kv[0]] = new LevelRecord { Completed = true, BestStars = stars, BestTimeMs = 85000, Clears = 2 };
            }
            profile.Repair(content);
        }

        /// <summary>Précharge équipement et talents : « tank_weapon:3,t1:quick_heal » (captures uniquement).</summary>
        private static void ApplyEquip(PlayerProfile profile, Healer.Combat.GameContent content, string? spec)
        {
            if (string.IsNullOrEmpty(spec)) return;
            foreach (var part in spec.Split(','))
            {
                var kv = part.Split(':');
                if (kv.Length != 2) continue;
                if (kv[0].StartsWith("t") && int.TryParse(kv[0].Substring(1), out int tier)) profile.Loadout.Talents[tier] = kv[1];
                else if (int.TryParse(kv[1], out int level)) profile.Loadout.Equipment[kv[0]] = level;
            }
            profile.Repair(content);
        }

        private static IEnumerator RealtimeCaptureRoutine(System.Collections.Generic.List<float> seconds, string dir)
        {
            foreach (float sec in seconds)
            {
                while (Time.realtimeSinceStartup < sec) yield return null;
                yield return new WaitForEndOfFrame();
                string path = Path.Combine(dir, $"capture_{sec:0}s.png");
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log("[Healer] capture : " + path);
                yield return new WaitForSeconds(0.4f);
            }
            yield return new WaitForSeconds(0.3f);
            Application.Quit();
        }

        private static IEnumerator CaptureRoutine(BattleController controller, System.Collections.Generic.List<float> seconds, string dir)
        {
            foreach (float sec in seconds)
            {
                while (controller.Battle.GetClock() < sec * 1000.0 && controller.Battle.GetResult() == Healer.Combat.BattleResults.Ongoing)
                    yield return null;
                controller.TimeScale = 1f; // laisse les animations et les particules s'afficher normalement
                yield return new WaitForSeconds(0.35f);
                yield return new WaitForEndOfFrame();
                string path = Path.Combine(dir, $"capture_{sec:0}s.png");
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log("[Healer] capture : " + path);
                yield return new WaitForSeconds(0.5f);
                controller.TimeScale = 6f;
            }
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }
    }
}
