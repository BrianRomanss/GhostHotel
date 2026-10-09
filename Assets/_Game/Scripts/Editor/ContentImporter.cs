using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChuchuGames.ContentImport.Editor;
using GhostHotel.Data;
using GhostHotel.Model;
using GhostHotel.Model.Content;
using UnityEditor;
using UnityEngine;

namespace GhostHotel.EditorTools
{
    /// <summary>
    /// guests.json + nights.json → GuestSO / NightSO / TagSO assets and the NightCatalog.
    /// Everything is validated first; nothing is written if there are errors.
    /// Batchmode: -executeMethod GhostHotel.EditorTools.ContentImporter.ImportBatch
    /// </summary>
    public static class ContentImporter
    {
        public const string ContentFolder = "Assets/_Game/Content";
        public const string DataFolder = "Assets/_Game/Data";
        public const string CatalogPath = DataFolder + "/NightCatalog.asset";

        [MenuItem("Tools/Ghost Hotel/Import Content (JSON → assets)")]
        public static void ImportMenu()
        {
            var report = Import();
            if (report.Ok) Debug.Log(report);
            else Debug.LogError(report);
        }

        public static void ImportBatch()
        {
            var report = Import();
            Debug.Log(report);
            EditorApplication.Exit(report.Ok ? 0 : 1);
        }

        public static ImportReport Import()
        {
            var report = new ImportReport();
            var guestFile = Read<GuestFile>("guests.json", report);
            var nightFile = Read<NightFile>("nights.json", report);
            if (!report.Ok) return report;

            // Validate everything before touching assets.
            var factory = new ContentFactory(guestFile.guests);
            foreach (var id in factory.GuestIds) factory.Guest(id);
            foreach (var n in nightFile.nights) factory.Night(n);
            report.Errors.AddRange(factory.Errors.Distinct());
            if (!report.Ok) return report;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var tag in Tags.All)
                    AssetUpsert.Upsert<TagSO>(DataFolder + "/Tags", tag, a =>
                    {
                        a.id = tag;
                        if (string.IsNullOrEmpty(a.displayName)) a.displayName = tag;
                        a.isVirtual = tag == Tags.Noisy || tag == Tags.Cold;
                    }, report);

                var guests = new Dictionary<string, GuestSO>();
                foreach (var g in guestFile.guests)
                    guests[g.id] = AssetUpsert.Upsert<GuestSO>(DataFolder + "/Guests", g.id, a => a.data = g, report);

                var nights = new List<NightSO>();
                foreach (var n in nightFile.nights.OrderBy(n => n.number))
                    nights.Add(AssetUpsert.Upsert<NightSO>(DataFolder + "/Nights", $"Night{n.number:00}", a =>
                    {
                        a.data = n;
                        a.guests = n.guests.Select(id => guests[id]).ToArray();
                    }, report));

                AssetUpsert.Upsert<NightCatalogSO>(DataFolder, "NightCatalog", a =>
                {
                    a.nights = nights.ToArray();
                    a.guests = guestFile.guests.Select(g => guests[g.id]).ToArray();
                }, report);

                foreach (var orphan in AssetUpsert.FindOrphans<GuestSO>(DataFolder + "/Guests", guests.Keys))
                    report.Warnings.Add($"{orphan} is not in guests.json (left untouched)");
                var nightIds = new HashSet<string>(nightFile.nights.Select(n => $"Night{n.number:00}"));
                foreach (var orphan in AssetUpsert.FindOrphans<NightSO>(DataFolder + "/Nights", nightIds))
                    report.Warnings.Add($"{orphan} is not in nights.json (left untouched)");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }
            return report;
        }

        static T Read<T>(string file, ImportReport report) where T : class
        {
            var path = Path.Combine(ContentFolder, file);
            if (!File.Exists(path))
            {
                report.Errors.Add($"Missing {path}");
                return null;
            }
            try
            {
                return JsonUtility.FromJson<T>(File.ReadAllText(path));
            }
            catch (System.Exception e)
            {
                report.Errors.Add($"{path}: {e.Message}");
                return null;
            }
        }
    }
}
