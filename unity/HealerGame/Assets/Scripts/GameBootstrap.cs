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
    ///   -healer-screen menu|levels   avec -healer-shots : capture d'un écran hors combat (secondes réelles)
    ///   -healer-level l2        démarre directement ce niveau
    ///   -healer-out DOSSIER     dossier des captures
    ///   -healer-speed 6         accélération du temps pendant les captures
    ///   -healer-timescale 30    accélération du temps en jeu normal (tests de bout en bout : tools/unity-e2e.ps1)
    ///   -healer-autoplay        le bot de référence joue le soin (tests de bout en bout)
    ///   -healer-profile-dir D   dossier de la sauvegarde (par défaut : dossier de données du jeu)
    ///   -healer-progress l1:3,l2:2   précharge une progression (captures)
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
            string profileDir = Arg("-healer-profile-dir") ?? (shots != null ? Path.Combine(Application.temporaryCachePath, "capture-profile") : Application.persistentDataPath);
            var storage = new ProfileStorage(profileDir);
            var profile = shots != null && Arg("-healer-profile-dir") == null ? PlayerProfile.NewGame(content) : storage.Load(content);
            ApplyProgress(profile, content, Arg("-healer-progress"));

            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            var controller = gameObject.AddComponent<BattleController>();
            controller.StartLevel(content, content.Levels[0], profile.OwnedCharacters, (uint)(Environment.TickCount & 0x7fffffff));
            var stage = gameObject.AddComponent<BattleStage>();
            stage.Build(cam, controller);
            var flow = gameObject.AddComponent<GameFlow>();
            flow.Init(content, storage, profile, controller, stage);
            gameObject.AddComponent<BattleHud>().Init(controller, flow);
            gameObject.AddComponent<AppHud>().Init(flow);
            gameObject.AddComponent<BattleAudio>().Init(controller, flow);
            gameObject.AddComponent<BattleKeys>().Init(flow);

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
                if (screen == "menu" || screen == "levels")
                {
                    if (screen == "levels") flow.OpenLevels();
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
