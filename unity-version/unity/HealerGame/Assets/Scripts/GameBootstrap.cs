using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Point d'entrée : assemble contenu, contrôleur, scène 3D et interface. Options de ligne de commande
    /// (vérifications automatiques, jamais utilisées en jeu normal) :
    ///   -healer-shots 8,30,46   captures d'écran à ces secondes de combat (le bot de référence joue)
    ///   -healer-out DOSSIER     dossier des captures (défaut : Documents/…/captures)
    ///   -healer-speed 6         accélération du temps pendant les captures
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Start()
        {
            Application.targetFrameRate = 60;
            var content = ContentLoader.Load();
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            var controller = gameObject.AddComponent<BattleController>();
            controller.Begin(content, (uint)(Environment.TickCount & 0x7fffffff));
            gameObject.AddComponent<BattleStage>().Build(cam, controller);
            var hud = gameObject.AddComponent<BattleHud>();
            hud.Init(controller);
            gameObject.AddComponent<BattleAudio>().Init(controller);
            gameObject.AddComponent<BattleKeys>().Init(controller);

            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
            string shots = Arg("-healer-shots");
            if (shots != null)
            {
                float speed = float.TryParse(Arg("-healer-speed"), out var s) ? s : 6f;
                controller.StartFight();
                controller.Autoplay = true;
                controller.TimeScale = speed;
                controller.TapAlly("tank");
                var seconds = shots.Split(',').Select(x => float.Parse(x.Trim())).OrderBy(x => x).ToList();
                string dir = Arg("-healer-out") ?? Path.Combine(Application.persistentDataPath, "captures");
                Directory.CreateDirectory(dir);
                StartCoroutine(CaptureRoutine(controller, seconds, dir));
            }
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
