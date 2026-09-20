using System;
using System.IO;
using System.Linq;
using Healer.Client;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Healer.EditorTools
{
    /// <summary>
    /// Outils d'Éditeur reproductibles (règle du projet : tout ce qui est fait via l'Éditeur ou le MCP doit
    /// pouvoir être reconstruit par script). Menu « Healer » ; ou en ligne de commande :
    ///   Unity -batchmode -quit -projectPath … -executeMethod Healer.EditorTools.Builder.BuildWindows
    /// </summary>
    public static class Builder
    {
        public const string ScenePath = "Assets/Scenes/Battle.unity";
        private const string Company = "Healer";
        private const string Product = "Healer Game";

        // Budgets de triangles de docs/ART_3D.md.
        private const int CharacterBudget = 3000;
        private const int BossBudget = 8000;

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string CoreContent => Path.GetFullPath(Path.Combine(Application.dataPath, "../../../core/content"));
        public static string BuildDir => Path.Combine(ProjectRoot, "Builds", "Windows");

        // ---- Contenu partagé ---------------------------------------------------------------------

        /// <summary>Copie core/content/*.json (source de vérité) dans StreamingAssets/content. Renvoie true si quelque chose a changé.</summary>
        [MenuItem("Healer/Synchroniser le contenu")]
        public static bool SyncContent()
        {
            string target = Path.Combine(Application.dataPath, "StreamingAssets", "content");
            Directory.CreateDirectory(target);
            bool changed = false;
            foreach (var file in Directory.GetFiles(CoreContent, "*.json"))
            {
                string dest = Path.Combine(target, Path.GetFileName(file));
                byte[] src = File.ReadAllBytes(file);
                if (!File.Exists(dest) || !File.ReadAllBytes(dest).SequenceEqual(src))
                {
                    File.WriteAllBytes(dest, src);
                    changed = true;
                }
            }
            if (changed) AssetDatabase.Refresh();
            return changed;
        }

        // ---- Réglages du projet ------------------------------------------------------------------

        [MenuItem("Healer/Configurer le projet")]
        public static void ConfigureProject()
        {
            PlayerSettings.companyName = Company;
            PlayerSettings.productName = Product;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Disabled);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.healer.game");
            AlwaysInclude("Standard", "Unlit/Color", "Unlit/Texture", "Legacy Shaders/Particles/Alpha Blended", "Sprites/Default");
            // Les assemblies du jeu ne doivent jamais être élaguées (lecture JSON par réflexion).
            File.WriteAllText(Path.Combine(Application.dataPath, "link.xml"),
                "<linker>\n  <assembly fullname=\"Healer.Combat\" preserve=\"all\"/>\n  <assembly fullname=\"Healer.Ui\" preserve=\"all\"/>\n  <assembly fullname=\"Newtonsoft.Json\" preserve=\"all\"/>\n</linker>\n");
            AssetDatabase.SaveAssets();
        }

        private static void AlwaysInclude(params string[] shaderNames)
        {
            var graphics = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/GraphicsSettings.asset");
            var so = new SerializedObject(graphics);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            foreach (string name in shaderNames)
            {
                var shader = Shader.Find(name);
                if (shader == null) { Debug.LogWarning("[Healer] shader introuvable : " + name); continue; }
                bool present = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) present = true;
                if (present) continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            }
            so.ApplyModifiedProperties();
        }

        // ---- Scène -------------------------------------------------------------------------------

        [MenuItem("Healer/Créer la scène de combat")]
        public static void CreateScene()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Game");
            go.AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        // ---- Budget de polygones -----------------------------------------------------------------

        /// <summary>Vérifie les budgets de triangles (docs/ART_3D.md) et la présence des sous-objets requis. Lève une exception sinon.</summary>
        [MenuItem("Healer/Vérifier les modèles 3D")]
        public static void CheckModels()
        {
            var report = new System.Text.StringBuilder("[Healer] Modèles 3D :\n");
            void Check(string name, GameObject model, int budget, params string[] requiredParts)
            {
                int tris = ModelFactory.TriangleCount(model);
                var names = model.GetComponentsInChildren<Transform>().Select(t => t.name).ToHashSet();
                var missing = requiredParts.Where(p => !names.Contains(p)).ToList();
                report.AppendLine($"  {name} : {tris} triangles (budget {budget}), parties manquantes : {(missing.Count == 0 ? "aucune" : string.Join(", ", missing))}");
                UnityEngine.Object.DestroyImmediate(model);
                if (tris > budget) throw new Exception($"{name} dépasse son budget de triangles : {tris} > {budget}");
                if (missing.Count > 0) throw new Exception($"{name} : parties manquantes : {string.Join(", ", missing)}");
            }
            Check("Golem", ModelFactory.Golem(), BossBudget, "Core", "Eyes", "Runes", "Head", "Torso");
            Check("Garde", ModelFactory.Tank(), CharacterBudget, "Shield", "Helmet", "Eyes");
            Check("Archère", ModelFactory.Archer(), CharacterBudget, "Bow", "Quiver", "Eyes");
            Check("Mage", ModelFactory.Mage(), CharacterBudget, "Hat", "Staff", "Orb", "Eyes");
            Check("Soigneuse", ModelFactory.Healer(), CharacterBudget, "Staff", "Cross", "Eyes");
            Debug.Log(report.ToString());
        }

        // ---- Tout reconstruire / build -----------------------------------------------------------

        [MenuItem("Healer/Tout reconstruire")]
        public static void BuildAll()
        {
            SyncContent();
            ConfigureProject();
            CheckModels();
            CreateScene();
        }

        /// <summary>APK de test pour téléphone Android (IL2CPP arm64, portrait). Sortie : Builds/Android/HealerGame.apk.</summary>
        public static void BuildAndroid()
        {
            try
            {
                BuildAll();
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.healer.game");
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Disabled);
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
                PlayerSettings.bundleVersion = "0.1.0";
                EditorUserBuildSettings.buildAppBundle = false;
                string dir = Path.Combine(ProjectRoot, "Builds", "Android");
                Directory.CreateDirectory(dir);
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = Path.Combine(dir, "HealerGame.apk"),
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None,
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                Debug.Log($"[Healer] build Android : {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} Mo, {report.summary.totalTime.TotalSeconds:0} s, erreurs {report.summary.totalErrors}");
                if (report.summary.result != BuildResult.Succeeded) throw new Exception("Le build Android a échoué : " + report.summary.result);
            }
            catch (Exception e)
            {
                Debug.LogError("[Healer] ÉCHEC Android : " + e);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildWindows()
        {
            try
            {
                BuildAll();
                Directory.CreateDirectory(BuildDir);
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = Path.Combine(BuildDir, "HealerGame.exe"),
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None,
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                Debug.Log($"[Healer] build : {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} Mo, {report.summary.totalTime.TotalSeconds:0} s, erreurs {report.summary.totalErrors}");
                if (report.summary.result != BuildResult.Succeeded) throw new Exception("Le build a échoué : " + report.summary.result);
            }
            catch (Exception e)
            {
                Debug.LogError("[Healer] ÉCHEC : " + e);
                EditorApplication.Exit(1);
            }
        }
    }

    /// <summary>Garde le contenu à jour à l'ouverture de l'Éditeur, pour que « Play » utilise toujours core/content.</summary>
    [InitializeOnLoad]
    public static class ContentAutoSync
    {
        static ContentAutoSync() => EditorApplication.delayCall += () => { try { Builder.SyncContent(); } catch (Exception e) { Debug.LogWarning("[Healer] synchronisation du contenu impossible : " + e.Message); } };
    }
}
