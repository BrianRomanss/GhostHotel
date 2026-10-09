using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using GhostHotel.Data;
using GhostHotel.Model;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GhostHotel.EditorTools
{
    /// <summary>
    /// GDD §9 Night Validator: brute-forces every night in the catalog and reports solution counts,
    /// best score and whether a 3★ arrangement exists.
    /// Batchmode: -executeMethod GhostHotel.EditorTools.NightValidatorWindow.ValidateBatch (exit 1 on failure)
    /// </summary>
    public sealed class NightValidatorWindow : EditorWindow
    {
        sealed class Row
        {
            public int Number;
            public string Title;
            public NightReport Report;
            public MidnightReport Midnight;
            public string Error;
            public long Ms;
            public bool Expanded;
        }

        readonly List<Row> _rows = new List<Row>();
        Vector2 _scroll;

        [MenuItem("Tools/Ghost Hotel/Night Validator")]
        static void Open() => GetWindow<NightValidatorWindow>("Night Validator");

        public static void ValidateBatch()
        {
            var rows = ValidateAll(out var catalogError);
            var sb = new StringBuilder("[NightValidator]\n");
            bool ok = catalogError == null;
            if (catalogError != null) sb.AppendLine(catalogError);
            foreach (var r in rows)
            {
                bool pass = r.Error == null && r.Report.HasPerfect && (r.Midnight == null || r.Midnight.Ok);
                ok &= pass;
                sb.AppendLine($"{(pass ? "PASS" : "FAIL")} Night {r.Number,2} {r.Title}: {r.Error ?? (r.Midnight != null ? r.Midnight.ToString() : r.Report.ToString())} ({r.Ms} ms)");
            }
            Debug.Log(sb.ToString());
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static List<Row> ValidateAll(out string catalogError)
        {
            var rows = new List<Row>();
            var catalog = AssetDatabase.LoadAssetAtPath<NightCatalogSO>(ContentImporter.CatalogPath);
            catalogError = catalog == null ? $"No catalog at {ContentImporter.CatalogPath}. Run Tools/Ghost Hotel/Import Content first." : null;
            if (catalog == null) return rows;

            for (int i = 1; i <= catalog.Count; i++)
            {
                var night = catalog.Night(i);
                var row = new Row { Number = i, Title = night != null ? night.data.title : "(missing)" };
                var factory = catalog.CreateFactory();
                var hotel = night != null ? factory.Night(night.data) : null;
                if (hotel == null || factory.Errors.Count > 0)
                {
                    row.Error = string.Join("; ", factory.Errors);
                }
                else
                {
                    var sw = Stopwatch.StartNew();
                    var events = factory.Midnight(night.data);
                    if (events.Count > 0)
                    {
                        row.Midnight = MidnightSolver.Analyse(() => catalog.CreateFactory().Night(night.data), events, night.data.swapTokens);
                        row.Report = row.Midnight.Before;
                    }
                    else row.Report = NightSolver.Analyse(hotel);
                    row.Ms = sw.ElapsedMilliseconds;
                }
                rows.Add(row);
            }
            return rows;
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Import Content", EditorStyles.toolbarButton, GUILayout.Width(110)))
                    ContentImporter.ImportMenu();
                if (GUILayout.Button("Validate All", EditorStyles.toolbarButton, GUILayout.Width(90)))
                {
                    _rows.Clear();
                    _rows.AddRange(ValidateAll(out var err));
                    if (err != null) ShowNotification(new GUIContent(err));
                }
                GUILayout.FlexibleSpace();
            }

            if (_rows.Count == 0)
            {
                EditorGUILayout.HelpBox("Press Validate All. Every night must have at least one 3★ (perfect) solution.", MessageType.Info);
                return;
            }

            int pass = 0;
            foreach (var r in _rows) if (r.Error == null && r.Report.HasPerfect && (r.Midnight == null || r.Midnight.Ok)) pass++;
            EditorGUILayout.HelpBox($"{pass} / {_rows.Count} nights have a perfect solution.",
                pass == _rows.Count ? MessageType.Info : MessageType.Error);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var r in _rows)
            {
                bool ok = r.Error == null && r.Report.HasPerfect && (r.Midnight == null || r.Midnight.Ok);
                var label = $"{(ok ? "✓" : "✗")}  Night {r.Number}: {r.Title}";
                r.Expanded = EditorGUILayout.Foldout(r.Expanded, label, true);
                if (!r.Expanded) continue;
                EditorGUI.indentLevel++;
                if (r.Error != null) EditorGUILayout.HelpBox(r.Error, MessageType.Error);
                else
                {
                    EditorGUILayout.LabelField(r.Report.ToString().Split('\n')[0], EditorStyles.wordWrappedLabel);
                    if (r.Midnight != null) EditorGUILayout.LabelField("Midnight: " + r.Midnight, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField($"{r.Report.ArrangementsChecked:N0} arrangements in {r.Ms} ms");
                    if (r.Report.ExamplePerfect != null)
                        foreach (var kv in r.Report.ExamplePerfect)
                            EditorGUILayout.LabelField($"{kv.Key} → room {kv.Value}");
                    foreach (var line in r.Report.BestBreakdown)
                        EditorGUILayout.LabelField(line);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
