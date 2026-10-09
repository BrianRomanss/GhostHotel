using System.Collections.Generic;
using System.Linq;
using ChuchuGames.GridPuzzle;
using GhostHotel.Model;
using GhostHotel.Model.Content;
using GhostHotel.Model.Rules;
using NUnit.Framework;

namespace GhostHotel.Tests
{
    public class MidnightEventTests
    {
        static readonly AuraDef Noisy = new AuraDef("Noisy", addTag: Tags.Noisy);

        [Test]
        public void RestlessNight_ExtendsPoltergeistNoise()
        {
            var h = new HotelModel(1, 4);
            var p = h.AddGuest(new GuestDef("p", "P", GhostType.Poltergeist, aura: Noisy));
            h.Place(p, new Cell(0, 0));
            Assert.IsFalse(h.HasTag(new Cell(0, 2), Tags.Noisy));
            new MidnightEvent(MidnightKind.RestlessNight, "", "").Fire(h);
            Assert.IsTrue(h.HasTag(new Cell(0, 2), Tags.Noisy));
            Assert.IsFalse(h.HasTag(new Cell(0, 3), Tags.Noisy));
        }

        [Test]
        public void PowerFlicker_RemovesDarkFromOneFloorOnly()
        {
            var h = new HotelModel(2, 2, autoFloorTags: false);
            h.Room(new Cell(0, 0)).Tags.AddBase(Tags.Dark);
            h.Room(new Cell(1, 0)).Tags.AddBase(Tags.Dark);
            new MidnightEvent(MidnightKind.PowerFlicker, "", "", floorRow: 1).Fire(h);
            Assert.IsTrue(h.HasTag(new Cell(0, 0), Tags.Dark));
            Assert.IsFalse(h.HasTag(new Cell(1, 0), Tags.Dark));
        }

        [Test]
        public void BurstPipe_WaterHere_ColdNextDoor_AndColdCostsAStar()
        {
            var h = new HotelModel(1, 3);
            var g = h.AddGuest(new GuestDef("g", "G", GhostType.Wisp));
            h.Place(g, new Cell(0, 0));
            new MidnightEvent(MidnightKind.BurstPipe, "", "", room: new Cell(0, 1)).Fire(h);
            Assert.IsTrue(h.HasTag(new Cell(0, 1), Tags.Water));
            Assert.IsTrue(h.HasTag(new Cell(0, 0), Tags.Cold));
            Assert.AreEqual(1, Scoring.ScoreGuest(g, h));
        }

        [Test]
        public void VanesVisit_CursesARoom()
        {
            var h = new HotelModel(1, 2);
            var g = h.AddGuest(new GuestDef("g", "G", GhostType.Wisp));
            h.Place(g, new Cell(0, 1));
            new MidnightEvent(MidnightKind.VanesVisit, "", "", room: new Cell(0, 1)).Fire(h);
            CollectionAssert.Contains(Scoring.Evaluate(g, h).UnpleasantTags, Tags.Cursed);
        }

        [Test]
        public void Seance_BasementGuestsWantCompany()
        {
            var h = new HotelModel(2, 2);
            var lonely = h.AddGuest(new GuestDef("a", "A", GhostType.Wisp));
            var attic = h.AddGuest(new GuestDef("b", "B", GhostType.Wisp));
            h.Place(lonely, new Cell(0, 0));
            h.Place(attic, new Cell(1, 1));
            new MidnightEvent(MidnightKind.SeanceDownstairs, "", "").Fire(h);
            Assert.AreEqual(1, Scoring.ScoreGuest(lonely, h), "alone in the Basement");
            Assert.AreEqual(2, Scoring.ScoreGuest(attic, h), "Attic guests are unaffected");
        }

        [Test]
        public void UnexpectedGuest_IsOptional_AndTurningAwayDoesNotCount()
        {
            var h = new HotelModel(1, 2);
            var g = h.AddGuest(new GuestDef("g", "G", GhostType.Wisp, likes: new IRule[] { new EdgeRule() }));
            h.Place(g, new Cell(0, 0));
            var walkIn = new MidnightEvent(MidnightKind.UnexpectedGuest, "", "", guest: new GuestDef("w", "W", GhostType.Wisp)).Fire(h);
            Assert.IsTrue(walkIn.Optional);
            Assert.IsTrue(h.AllGuestsPlaced, "walk-ins may stay in the queue");
            Assert.IsTrue(Scoring.IsPerfect(h), "a turned-away walk-in does not drag the rating down");
            h.Place(walkIn, new Cell(0, 1));
            Assert.AreEqual(2.5f, Scoring.NightRating(h));
        }
    }

    public class SwapTokenTests
    {
        static NightSession Session(int tokens, params MidnightEvent[] events)
        {
            var h = new HotelModel(1, 4);
            h.AddGuest(new GuestDef("a", "A", GhostType.Wisp));
            h.AddGuest(new GuestDef("b", "B", GhostType.Wisp));
            var s = new NightSession(1, "t", h, new GameProgress(), events, tokens);
            s.BeginCheckIn();
            s.Move(h.Guests[0], new Cell(0, 0));
            s.Move(h.Guests[1], new Cell(0, 1));
            s.OpenDoors();
            return s;
        }

        [Test]
        public void MidnightMovesCostTokens_UndoRefunds_SwapIsOneToken()
        {
            var s = Session(1, new MidnightEvent(MidnightKind.SeanceDownstairs, "", ""));
            var h = s.Hotel;
            Assert.AreEqual(NightPhase.Midnight, s.Phase);
            Assert.AreEqual(1, s.TokensLeft);
            s.Move(h.Guests[0], new Cell(0, 1)); // swap A and B: one token
            Assert.AreEqual(0, s.TokensLeft);
            Assert.AreEqual(new Cell(0, 0), h.Guests[1].Room);
            Assert.IsFalse(s.CanMove(h.Guests[0], new Cell(0, 3)));
            s.Commands.Undo();
            Assert.AreEqual(1, s.TokensLeft);
        }

        [Test]
        public void PlacingTheWalkInIsFree()
        {
            var s = Session(0, new MidnightEvent(MidnightKind.UnexpectedGuest, "", "", guest: new GuestDef("w", "W", GhostType.Wisp)));
            var walkIn = s.WalkIns.Single();
            Assert.IsTrue(s.CanMove(walkIn, new Cell(0, 3)));
            Assert.IsFalse(s.CanMove(walkIn, new Cell(0, 0)), "bumping a booked guest would cost a token");
            s.Move(walkIn, new Cell(0, 3));
            Assert.AreEqual(0, s.TokensUsed);
            s.EndMidnight();
            Assert.IsNotNull(s.Dawn);
        }
    }

    public class MidnightSolverTests
    {
        [Test]
        public void Moves_CountsSwapsOnce()
        {
            var a = new Dictionary<string, int> { ["x"] = 101, ["y"] = 102, ["z"] = 103 };
            var b = new Dictionary<string, int> { ["x"] = 102, ["y"] = 101, ["z"] = 104 };
            Assert.AreEqual(2, MidnightSolver.Moves(a, b));
        }

        [Test]
        public void BurstPipeNight_CanBeAvoidedByPlanningAhead()
        {
            // Gloria needs Dark and likes a corner. The pipe bursts next to 101, so 101 turns Cold;
            // starting her in 104 means midnight never bothers her.
            HotelModel Build()
            {
                var h = new HotelModel(1, 4);
                h.Room(new Cell(0, 0)).Tags.AddBase(Tags.Dark);
                h.Room(new Cell(0, 3)).Tags.AddBase(Tags.Dark);
                h.AddGuest(new GuestDef("g", "Gloria", GhostType.Weeper,
                    needs: new IRule[] { new RequiresTagRule(Tags.Dark) }, likes: new IRule[] { new EdgeRule() }));
                return h;
            }
            var events = new[] { new MidnightEvent(MidnightKind.BurstPipe, "", "", room: new Cell(0, 1)) };
            var r = MidnightSolver.Analyse(Build, events, 1);
            Assert.IsTrue(r.Before.HasPerfect);
            Assert.IsTrue(r.After.HasPerfect);
            Assert.AreEqual(0, r.MinMoves, "starting in 104, midnight never bothers her");
            Assert.AreEqual(1, r.WorstCaseMoves, "starting in 101, she must move once");
            Assert.IsTrue(r.Ok, r.ToString());
            Assert.IsTrue(r.Matters);
        }

        [Test]
        public void FactoryBuildsEventsAndRejectsBadOnes()
        {
            var f = new ContentFactory(new[] { new GuestData { id = "w", name = "W", type = "Wisp" } });
            var night = new NightData
            {
                number = 11, floors = 2, roomsPerFloor = 2, guests = new string[0],
                midnight = new[]
                {
                    new MidnightEventData { kind = "BurstPipe", room = "201" },
                    new MidnightEventData { kind = "PowerFlicker", floor = 2 },
                    new MidnightEventData { kind = "UnexpectedGuest", guestId = "w" },
                    new MidnightEventData { kind = "VanesVisit", room = "305" },
                    new MidnightEventData { kind = "Earthquake" },
                },
            };
            var events = f.Midnight(night);
            Assert.AreEqual(3, events.Count);
            Assert.AreEqual("Burst Pipe", events[0].Title);
            Assert.AreEqual(2, f.Errors.Count, string.Join("\n", f.Errors));
        }
    }
}
