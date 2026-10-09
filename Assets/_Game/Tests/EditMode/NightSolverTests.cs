using System.Linq;
using ChuchuGames.GridPuzzle;
using GhostHotel.Model;
using GhostHotel.Model.Content;
using GhostHotel.Model.Rules;
using NUnit.Framework;

namespace GhostHotel.Tests
{
    public class NightSolverTests
    {
        [Test]
        public void Greybox_ExactlyOnePerfect_AndHotelLeftClean()
        {
            var h = GreyboxNight.Create();
            var r = NightSolver.Analyse(h);
            Assert.AreEqual(1, r.PerfectSolutions, r.ToString());
            Assert.IsTrue(r.PerfectExhaustive);
            Assert.AreEqual(101, r.ExamplePerfect["gloria"]);
            Assert.AreEqual(104, r.ExamplePerfect["ashworth"]);
            Assert.IsTrue(h.Guests.All(g => !g.IsPlaced));
        }

        [Test]
        public void StaticPruning_CheckedFarFewerThanAllArrangements()
        {
            var r = NightSolver.Analyse(GreyboxNight.Create());
            Assert.Less(r.ArrangementsChecked, 24);
        }

        [Test]
        public void GuestWithoutLikes_ReportsBestInstead()
        {
            var h = new HotelModel(1, 2);
            h.AddGuest(new GuestDef("a", "Plain", GhostType.Wisp));
            var r = NightSolver.Analyse(h);
            Assert.IsFalse(r.HasPerfect);
            Assert.AreEqual(1, r.Notes.Count);
            Assert.AreEqual(2, r.BestTotalStars);
            StringAssert.Contains("NO perfect", r.ToString());
        }

        [Test]
        public void AuraTouchedTags_AreNotPrunedAway()
        {
            // Gloria needs Dark; only 102 is dark. The Wisp's Glow removes Dark next to it,
            // so the Wisp must sit far away (104), never in 101/103.
            var h = new HotelModel(1, 4);
            h.Room(new Cell(0, 1)).Tags.AddBase(Tags.Dark);
            h.AddGuest(new GuestDef("g", "Gloria", GhostType.Weeper,
                needs: new IRule[] { new RequiresTagRule(Tags.Dark) }, likes: new IRule[] { new IsolationRule() }));
            h.AddGuest(new GuestDef("w", "Wisp", GhostType.Wisp, likes: new IRule[] { new EdgeRule() },
                aura: new AuraDef("Glow", removeTag: Tags.Dark)));
            var r = NightSolver.Analyse(h);
            Assert.AreEqual(1, r.PerfectSolutions, r.ToString());
            Assert.AreEqual(102, r.ExamplePerfect["g"]);
            Assert.AreEqual(104, r.ExamplePerfect["w"]);
        }

        [Test]
        public void LockedGuests_StayPut()
        {
            var factory = new ContentFactory(new[]
            {
                new GuestData { id = "a", name = "A", type = "Wisp", likes = new[] { new RuleData { kind = "Company" } } },
                new GuestData { id = "b", name = "B", type = "Wisp", likes = new[] { new RuleData { kind = "Company" } } },
            });
            var h = factory.Night(new NightData
            {
                number = 1, floors = 1, roomsPerFloor = 3, guests = new[] { "a", "b" },
                prePlaced = new[] { new PlacementData { guest = "a", room = "101" } },
            });
            CollectionAssert.IsEmpty(factory.Errors);
            var r = NightSolver.Analyse(h);
            Assert.AreEqual(1, r.PerfectSolutions);
            Assert.AreEqual(102, r.ExamplePerfect["b"]);
            Assert.AreEqual(new Cell(0, 0), h.Guests[0].Room);
            Assert.IsTrue(h.Guests[0].Locked);
        }
    }

    public class ContentFactoryTests
    {
        [Test]
        public void ReportsEveryProblem()
        {
            var f = new ContentFactory(new[]
            {
                new GuestData
                {
                    id = "x", name = "X", type = "Ghosty",
                    needs = new[] { new RuleData { kind = "RequiresTag", tag = "Lava" }, new RuleData { kind = "Teleport" } },
                    likes = new[] { new RuleData { kind = "WantNeighbour", guestId = "nobody" } },
                },
            });
            f.Night(new NightData { number = 7, floors = 1, roomsPerFloor = 2, guests = new[] { "x", "ghost" },
                rooms = new[] { new RoomData { room = "305", tags = new[] { "Dark" } } } });

            var all = string.Join("\n", f.Errors);
            StringAssert.Contains("'Ghosty'", all);
            StringAssert.Contains("unknown tag 'Lava'", all);
            StringAssert.Contains("unknown rule kind 'Teleport'", all);
            StringAssert.Contains("unknown guestId 'nobody'", all);
            StringAssert.Contains("Unknown guest 'ghost'", all);
            StringAssert.Contains("room '305'", all);
        }

        [Test]
        public void BuildsEveryRuleKind()
        {
            RuleData R(string kind) => new RuleData { kind = kind };
            var rules = new[]
            {
                new RuleData { kind = "RequiresTag", tag = "Water" }, new RuleData { kind = "ForbidsTag", tag = "Attic" },
                new RuleData { kind = "Floor", end = "Top", count = 2 }, new RuleData { kind = "AvoidNeighbour", ghostType = "Poltergeist" },
                new RuleData { kind = "WantNeighbour", guestId = "y" }, R("Isolation"), R("Company"),
                new RuleData { kind = "Vertical", relation = "Below", ghostType = "Victorian" }, R("Edge"),
                new RuleData { kind = "FloorCount", count = 2 },
            };
            var f = new ContentFactory(new[]
            {
                new GuestData { id = "x", name = "X", type = "Wisp", likes = rules,
                    aura = new AuraData { name = "Screech", addTag = "Noisy", directions = "Vertical" } },
                new GuestData { id = "y", name = "Lady Ashworth", type = "Victorian" },
            });
            var def = f.Guest("x");
            CollectionAssert.IsEmpty(f.Errors);
            Assert.AreEqual(10, def.Likes.Count);
            Assert.AreEqual("Next to Lady Ashworth", def.Likes[4].Label);
            Assert.AreEqual(Direction.Vertical, def.Aura.Directions);
        }

        [Test]
        public void RoomNumbers_MapToFloorAndColumn()
        {
            var f = new ContentFactory(new GuestData[0]);
            var h = f.Night(new NightData { number = 1, floors = 2, roomsPerFloor = 4,
                rooms = new[] { new RoomData { room = "203", tags = new[] { "Mirror" } } } });
            Assert.IsTrue(h.HasTag(new Cell(1, 2), Tags.Mirror));
            Assert.IsTrue(h.HasTag(new Cell(1, 2), Tags.Attic)); // automatic on the top floor
        }
    }
}
