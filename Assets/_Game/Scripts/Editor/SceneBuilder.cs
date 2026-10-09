using System.IO;
using System.Linq;
using GhostHotel.Data;
using GhostHotel.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GhostHotel.EditorTools
{
    /// <summary>
    /// Builds scenes from code so they are reproducible. Menu: Tools/Ghost Hotel/...
    /// Batchmode: -executeMethod GhostHotel.EditorTools.SceneBuilder.BuildAll
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenesFolder = "Assets/_Game/Scenes";
        public const string MainScene = ScenesFolder + "/Main.unity";

        [MenuItem("Tools/Ghost Hotel/Build Scenes")]
        public static void BuildAll()
        {
            BuildMain();
        }

        public static void BuildMain()
        {
            Directory.CreateDirectory(ScenesFolder);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.NightNavy;
            cam.transform.position = new Vector3(0, 0, -10);

            var flow = new GameObject("GameFlow").AddComponent<GameFlow>();
            flow.Catalog = AssetDatabase.LoadAssetAtPath<NightCatalogSO>(ContentImporter.CatalogPath);
            flow.Art = AssetDatabase.LoadAssetAtPath<ArtLibrarySO>(ArtGenMenu.LibraryPath);
            if (flow.Art == null)
                Debug.LogWarning($"[SceneBuilder] No art library at {ArtGenMenu.LibraryPath}; run Generate Placeholder Art first.");
            if (flow.Catalog == null)
                Debug.LogWarning($"[SceneBuilder] No catalog at {ContentImporter.CatalogPath}; run Import Content first.");

            EditorSceneManager.SaveScene(scene, MainScene);
            SetBuildScenes(MainScene);
            Debug.Log($"[SceneBuilder] Saved {MainScene}");
        }

        /// <summary>Only the game's own scenes ship; template scenes (SampleScene) and removed scenes are dropped.</summary>
        static void SetBuildScenes(params string[] paths)
        {
            var scenes = paths.Select(p => new EditorBuildSettingsScene(p, true))
                .Concat(EditorBuildSettings.scenes.Where(s =>
                    !paths.Contains(s.path) && s.path.StartsWith(ScenesFolder + "/") && File.Exists(s.path)))
                .ToArray();
            EditorBuildSettings.scenes = scenes;
        }
    }
}
