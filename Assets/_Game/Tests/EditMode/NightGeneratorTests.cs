using GhostHotel.Model;
using GhostHotel.Model.Content;
using NUnit.Framework;

namespace GhostHotel.Tests
{
    public class NightGeneratorTests
    {
        static GuestData[] Guests() => new[]
        {
            new GuestData { id = "a", name = "A", type = "Weeper",
                needs = new[] { new RuleData { kind = "RequiresTag", tag = "Dark" } }, likes = new[] { new RuleData { kind = "Isolation" } } },
            new GuestData { id = "b", name = "B", type = "Drowned",
                needs = new[] { new RuleData { kind = "RequiresTag", tag = "Water" } }, likes = new[] { new RuleData { kind = "Edge" } } },
            new GuestData { id = "c", name = "C", type = "ChildGhost",
                needs = new[] { new RuleData { kind = "ForbidsTag", tag = "Dark" } }, likes = new[] { new RuleData { kind = "Company" } } },
        };

        static DesignNight Design() => new DesignNight
        {
            number = 99, title = "Generated", floors = 2, roomsPerFloor = 3, guests = new[] { "a", "b", "c" },
            tagPool = new[] { "Dark", "Water", "Mirror" }, minTags = 2, maxTags = 4, minPerfect = 1, maxPerfect = 3,
        };

        [Test]
        public void FindsAValidatedLayout_Deterministically()
        {
            var r1 = NightGenerator.Generate(Design(), Guests(), seed: 7);
            Assert.IsTrue(r1.Ok, r1.Report);
            var check = NightSolver.Analyse(new ContentFactory(Guests()).Night(r1.Night));
            Assert.That(check.PerfectSolutions, Is.InRange(1, 3));

            var r2 = NightGenerator.Generate(Design(), Guests(), seed: 7);
            Assert.AreEqual(Layout(r1.Night), Layout(r2.Night), "same seed, same night");
        }

        [Test]
        public void MidnightNights_NeedAFixWithinTheTokens()
        {
            var d = Design();
            d.swapTokens = 1;
            d.maxPerfect = 10;
            d.midnight = new[] { new MidnightEventData { kind = "BurstPipe" } };
            var r = NightGenerator.Generate(d, Guests(), seed: 3);
            Assert.IsTrue(r.Ok, r.Report);
            var f = new ContentFactory(Guests());
            var mr = MidnightSolver.Analyse(() => new ContentFactory(Guests()).Night(r.Night), f.Midnight(r.Night), 1);
            Assert.IsTrue(mr.Ok, mr.ToString());
            Assert.IsTrue(mr.Matters, "midnight should disturb at least one perfect check-in");
        }

        [Test]
        public void BadDesign_ReportsContentErrors()
        {
            var d = Design();
            d.guests = new[] { "nobody" };
            var r = NightGenerator.Generate(d, Guests(), seed: 1);
            Assert.IsFalse(r.Ok);
            StringAssert.Contains("Unknown guest 'nobody'", r.Report);
        }

        static string Layout(NightData n) =>
            string.Join("|", System.Array.ConvertAll(n.rooms, r => r.room + ":" + string.Join(",", r.tags)));
    }
}
