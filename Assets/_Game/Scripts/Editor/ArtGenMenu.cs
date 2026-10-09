using System.IO;
using System.Linq;
using ChuchuGames.ContentImport.Editor;
using GhostHotel.Data;
using GhostHotel.EditorTools.ArtGen;
using UnityEditor;
using UnityEngine;

namespace GhostHotel.EditorTools
{
    /// <summary>
    /// Tools/Ghost Hotel/Generate Placeholder Art: writes art + audio, then links everything under
    /// Art/ and Audio/ into the ArtLibrary asset. "Link Art" alone re-links after you swap in final files.
    /// Batchmode: -executeMethod GhostHotel.EditorTools.ArtGenMenu.GenerateBatch
    /// </summary>
    public static class ArtGenMenu
    {
        public const string ArtRoot = "Assets/_Game/Art";
        public const string AudioRoot = "Assets/_Game/Audio";
        public const string LibraryPath = ContentImporter.DataFolder + "/ArtLibrary.asset";

        [MenuItem("Tools/Ghost Hotel/Generate Placeholder Art + Audio")]
        public static void Generate()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Ghost Hotel", "Generating art…", 0.2f);
                GhostHotelArt.GenerateAll(Path.Combine(ArtRoot, "Generated"));
                EditorUtility.DisplayProgressBar("Ghost Hotel", "Generating audio…", 0.7f);
                GhostHotelAudio.GenerateAll(Path.Combine(AudioRoot, "Generated"));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.Refresh();
            Link();
        }

        public static void GenerateBatch()
        {
            Generate();
            EditorApplication.Exit(0);
        }

        public static void LinkBatch()
        {
            Link();
            EditorApplication.Exit(0);
        }

        [MenuItem("Tools/Ghost Hotel/Link Art Library")]
        public static void Link()
        {
            var report = new ImportReport();
            AssetUpsert.Upsert<ArtLibrarySO>(ContentImporter.DataFolder, "ArtLibrary", lib =>
            {
                lib.sprites = AssetDatabase.FindAssets("t:Sprite", new[] { ArtRoot })
                    .Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(s => s != null).OrderBy(s => s.name).ToArray();
                lib.clips = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot })
                    .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(c => c != null).OrderBy(c => c.name).ToArray();
                lib.serif = AssetDatabase.LoadAssetAtPath<Font>(ArtRoot + "/Fonts/Lora.ttf");
                lib.sans = AssetDatabase.LoadAssetAtPath<Font>(ArtRoot + "/Fonts/Inter.ttf");
            }, report);
            AssetDatabase.SaveAssets();
            var lib2 = AssetDatabase.LoadAssetAtPath<ArtLibrarySO>(LibraryPath);
            Debug.Log($"[ArtLibrary] {lib2.sprites.Length} sprites, {lib2.clips.Length} clips, fonts: {(lib2.serif ? "Lora" : "MISSING")}/{(lib2.sans ? "Inter" : "MISSING")}");
        }
    }
}
