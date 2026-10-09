using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GhostHotel.Model;
using GhostHotel.Model.Content;
using NUnit.Framework;

namespace GhostHotel.Tests
{
    /// <summary>Loads Assets/_Game/Content/*.json in Unity (JsonUtility) or .NET (System.Text.Json).</summary>
    public static class ContentFiles
    {
        public static string ContentDir()
        {
#if UNITY_5_3_OR_NEWER
            return Path.Combine(UnityEngine.Application.dataPath, "_Game", "Content");
#else
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets"))) dir = dir.Parent;
            Assert.IsNotNull(dir, "Couldn't find the Unity project root");
            return Path.Combine(dir.FullName, "Assets", "_Game", "Content");
#endif
        }

        public static T Load<T>(string file)
        {
            var json = File.ReadAllText(Path.Combine(ContentDir(), file));
#if UNITY_5_3_OR_NEWER
            return UnityEngine.JsonUtility.FromJson<T>(json);
#else
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
#endif
        }

        public static GuestFile Guests() => Load<GuestFile>("guests.json");
        public static NightFile Nights() => Load<NightFile>("nights.json");
    }

    /// <summary>The M2 gate as a test: every authored night loads without errors and has a 3★ solution.</summary>
    public class ContentValidationTests
    {
        static GuestFile Guests() => ContentFiles.Guests();
        static NightFile Nights() => ContentFiles.Nights();

        [Test]
        public void AllGuestsBuildWithoutErrors()
        {
            var f = new ContentFactory(Guests().guests);
            foreach (var id in f.GuestIds) f.Guest(id);
            Assert.IsEmpty(f.Errors, string.Join("\n", f.Errors));
        }

        [Test]
        public void EveryNightHasAPerfectSolution()
        {
            var guests = Guests().guests;
            var failures = new StringBuilder();
            foreach (var night in Nights().nights)
            {
                var f = new ContentFactory(guests);
                var hotel = f.Night(night);
                if (f.Errors.Count > 0)
                {
                    failures.AppendLine($"Night {night.number}: " + string.Join("; ", f.Errors));
                    continue;
                }
                var events = f.Midnight(night);
                if (events.Count > 0)
                {
                    var n = night;
                    var mr = MidnightSolver.Analyse(() => new ContentFactory(guests).Night(n), events, night.swapTokens);
                    TestContext.WriteLine($"Night {night.number,2} {night.title}: {mr}");
                    if (!mr.Ok) failures.AppendLine($"Night {night.number}: {mr}");
                    continue;
                }
                var report = NightSolver.Analyse(hotel);
                TestContext.WriteLine($"Night {night.number,2} {night.title}: {report}");
                if (!report.HasPerfect) failures.AppendLine($"Night {night.number}: {report}");
            }
            Assert.IsEmpty(failures.ToString(), failures.ToString());
        }

        [Test]
        public void NightNumbersAreSequential()
        {
            var nights = Nights().nights;
            for (int i = 0; i < nights.Length; i++) Assert.AreEqual(i + 1, nights[i].number);
        }
    }

    /// <summary>Design rules for Act 1 (GDD §6), checked automatically so content edits can't break them.</summary>
    public class Act1DesignTests
    {
        static readonly string[] Act1RuleKinds = { "RequiresTag", "ForbidsTag", "AvoidNeighbour", "Company", "Floor" };

        [Test]
        public void EveryStoryCanBeFinished()
        {
            // Act 1 arcs fit exactly inside nights 1-10. From Act 2 on, freed guests may return to help
            // (GDD §2), so later guests just need at least as many nights as stays, starting with their debut.
            var nights = ContentFiles.Nights().nights;
            var problems = new StringBuilder();
            foreach (var g in ContentFiles.Guests().guests)
            {
                if (g.stays >= 10) continue; // staff (Bartholomew)
                int act1 = 0, all = 0, debut = int.MaxValue;
                foreach (var n in nights)
                {
                    if (Array.IndexOf(n.guests, g.id) < 0) continue;
                    all++;
                    debut = Math.Min(debut, n.number);
                    if (n.number <= 10) act1++;
                }
                if (all == 0) continue;
                if (debut <= 10 && act1 != g.stays) problems.AppendLine($"{g.id}: {act1} Act 1 nights but {g.stays} stays");
                if (debut > 10 && all < g.stays) problems.AppendLine($"{g.id}: {all} nights but {g.stays} stays");
            }
            Assert.IsEmpty(problems.ToString(), problems.ToString());
        }

        [Test]
        public void Act1UsesOnlyAct1RulesAndHotelSizes()
        {
            var guests = new Dictionary<string, GuestData>();
            foreach (var g in ContentFiles.Guests().guests) guests[g.id] = g;
            var problems = new StringBuilder();
            foreach (var n in ContentFiles.Nights().nights)
            {
                if (n.number > 10) continue;
                if (n.floors > 2 || n.roomsPerFloor > 4) problems.AppendLine($"Night {n.number}: hotel {n.floors}x{n.roomsPerFloor} is bigger than Act 1's 2x4");
                foreach (var id in n.guests)
                {
                    var g = guests[id];
                    if (g.aura != null && !g.aura.IsEmpty) problems.AppendLine($"Night {n.number}: {id} has an aura (Act 2)");
                    foreach (var list in new[] { g.needs, g.likes, g.dislikes })
                    foreach (var r in list ?? new RuleData[0])
                        if (Array.IndexOf(Act1RuleKinds, r.kind) < 0) problems.AppendLine($"Night {n.number}: {id} uses {r.kind}");
                }
            }
            Assert.IsEmpty(problems.ToString(), problems.ToString());
        }

        [Test]
        public void EveryBookedGuestHasAStoryLinePerStay()
        {
            var booked = new HashSet<string>();
            foreach (var n in ContentFiles.Nights().nights) booked.UnionWith(n.guests);
            var problems = new StringBuilder();
            foreach (var g in ContentFiles.Guests().guests)
            {
                if (g.stays >= 10 || !booked.Contains(g.id)) continue; // staff, or drafted for a later act
                if (string.IsNullOrEmpty(g.arrival)) problems.AppendLine($"{g.id}: no arrival line");
                if ((g.story?.Length ?? 0) < g.stays) problems.AppendLine($"{g.id}: {g.story?.Length ?? 0} story beats for {g.stays} stays");
                if (string.IsNullOrEmpty(g.checkout)) problems.AppendLine($"{g.id}: no checkout line");
            }
            Assert.IsEmpty(problems.ToString(), problems.ToString());
        }
    }
}
