using ChuchuGames.Core;
using ChuchuGames.GridPuzzle;
using GhostHotel.Model;
using GhostHotel.Model.Rules;
using NUnit.Framework;

namespace GhostHotel.Tests
{
    public class ScoringTests
    {
        static GuestDef Def(GhostType type = GhostType.Drowned, IRule[] needs = null, IRule[] likes = null, IRule[] dislikes = null) =>
            new GuestDef("g", "Guest", type, needs, likes, dislikes);

        static (HotelModel hotel, GuestState guest) OneGuest(GuestDef def, params string[] roomTags)
        {
            var h = new HotelModel(1, 3);
            foreach (var t in roomTags) h.Room(new Cell(0, 0)).Tags.AddBase(t);
            var g = h.AddGuest(def);
            h.Place(g, new Cell(0, 0));
            return (h, g);
        }

        [Test]
        public void NoRules_TwoStars()
        {
            var (h, g) = OneGuest(Def());
            Assert.AreEqual(2, Scoring.ScoreGuest(g, h));
        }

        [Test]
        public void Unplaced_ZeroStars()
        {
            var h = new HotelModel(1, 3);
            var g = h.AddGuest(Def());
            Assert.AreEqual(0, Scoring.ScoreGuest(g, h));
        }

        [Test]
        public void BrokenNeed_CapsAtZero_EvenWithLike()
        {
            var (h, g) = OneGuest(Def(needs: new IRule[] { new RequiresTagRule(Tags.Water) },
                                      likes: new IRule[] { new RequiresTagRule(Tags.Dark) }), Tags.Dark);
            Assert.AreEqual(0, Scoring.ScoreGuest(g, h));
            Assert.AreEqual(Feedback.Red, Scoring.Evaluate(g, h).Feedback);
        }

        [Test]
        public void MetLike_ThreeStars_CountsOnlyOnce()
        {
            var (h, g) = OneGuest(Def(likes: new IRule[] { new RequiresTagRule(Tags.Dark), new RequiresTagRule(Tags.Water) }),
                Tags.Dark, Tags.Water);
            Assert.AreEqual(3, Scoring.ScoreGuest(g, h));
        }

        [Test]
        public void EachBrokenDislike_CostsAStar_ClampedAtZero()
        {
            var (h, g) = OneGuest(Def(dislikes: new IRule[]
            {
                new ForbidsTagRule(Tags.Dark), new ForbidsTagRule(Tags.Water), new ForbidsTagRule(Tags.Mirror),
            }), Tags.Dark, Tags.Water, Tags.Mirror);
            Assert.AreEqual(0, Scoring.ScoreGuest(g, h));
            Assert.AreEqual(Feedback.Yellow, Scoring.Evaluate(g, h).Feedback);
        }

        [Test]
        public void LikeAndDislike_Cancel()
        {
            var (h, g) = OneGuest(Def(likes: new IRule[] { new RequiresTagRule(Tags.Water) },
                                      dislikes: new IRule[] { new ForbidsTagRule(Tags.Dark) }), Tags.Water, Tags.Dark);
            Assert.AreEqual(2, Scoring.ScoreGuest(g, h));
        }
    }

    public class RuleTests
    {
        [Test]
        public void AvoidNeighbourType_OnlyOrthogonal()
        {
            var h = new HotelModel(2, 2, autoFloorTags: false);
            var weeper = h.AddGuest(new GuestDef("w", "Gloria", GhostType.Weeper,
                dislikes: new IRule[] { new AvoidNeighbourTypeRule(GhostType.Poltergeist) }));
            var polt = h.AddGuest(new GuestDef("p", "Rattles", GhostType.Poltergeist));

            h.Place(weeper, new Cell(0, 0));
            h.Place(polt, new Cell(1, 1)); // diagonal
            Assert.AreEqual(2, Scoring.ScoreGuest(weeper, h));

            h.Place(polt, new Cell(1, 0)); // directly above
            Assert.AreEqual(1, Scoring.ScoreGuest(weeper, h));
        }

        [Test]
        public void AutoFloorTags_OnlyWithTwoOrMoreFloors()
        {
            var one = new HotelModel(1, 3);
            Assert.IsFalse(one.HasTag(new Cell(0, 0), Tags.Attic));
            Assert.IsFalse(one.HasTag(new Cell(0, 0), Tags.Basement));

            var two = new HotelModel(2, 3);
            Assert.IsTrue(two.HasTag(new Cell(1, 2), Tags.Attic));
            Assert.IsTrue(two.HasTag(new Cell(0, 2), Tags.Basement));
            Assert.IsFalse(two.HasTag(new Cell(0, 2), Tags.Attic));
        }

        [Test]
        public void RoomNumbers_FloorThenRoom()
        {
            var h = new HotelModel(3, 4);
            Assert.AreEqual(101, h.Room(new Cell(0, 0)).Number);
            Assert.AreEqual(304, h.Room(new Cell(2, 3)).Number);
        }
    }

    public class PlacementTests
    {
        HotelModel _h;
        GuestState _a, _b;

        [SetUp]
        public void Setup()
        {
            _h = new HotelModel(1, 4);
            _a = _h.AddGuest(new GuestDef("a", "A", GhostType.Wisp));
            _b = _h.AddGuest(new GuestDef("b", "B", GhostType.Banshee));
        }

        [Test]
        public void DropOntoOccupied_Swaps()
        {
            _h.Place(_a, new Cell(0, 0));
            _h.Place(_b, new Cell(0, 1));
            _h.Place(_a, new Cell(0, 1));
            Assert.AreEqual(new Cell(0, 1), _a.Room);
            Assert.AreEqual(new Cell(0, 0), _b.Room);
        }

        [Test]
        public void QueueGuestOntoOccupied_SendsOccupantToQueue()
        {
            _h.Place(_b, new Cell(0, 2));
            _h.Place(_a, new Cell(0, 2));
            Assert.AreEqual(new Cell(0, 2), _a.Room);
            Assert.IsFalse(_b.IsPlaced);
        }

        [Test]
        public void Commands_UndoSwapAndClearAll()
        {
            var stack = new CommandStack();
            stack.Execute(new PlaceGuestCommand(_h, _a, new Cell(0, 0)));
            stack.Execute(new PlaceGuestCommand(_h, _b, new Cell(0, 3)));
            stack.Execute(new PlaceGuestCommand(_h, _a, new Cell(0, 3))); // swap
            stack.Execute(new ClearAllCommand(_h));
            Assert.IsFalse(_a.IsPlaced || _b.IsPlaced);

            stack.Undo(); // clear
            Assert.AreEqual(new Cell(0, 3), _a.Room);
            Assert.AreEqual(new Cell(0, 0), _b.Room);

            stack.Undo(); // swap
            Assert.AreEqual(new Cell(0, 0), _a.Room);
            Assert.AreEqual(new Cell(0, 3), _b.Room);
            Assert.AreSame(_a, _h.GuestAt(new Cell(0, 0)));
        }

        [Test]
        public void Preview_DoesNotChangeState()
        {
            var picky = _h.AddGuest(new GuestDef("p", "Pip", GhostType.ChildGhost,
                needs: new IRule[] { new ForbidsTagRule(Tags.Dark) }));
            _h.Room(new Cell(0, 1)).Tags.AddBase(Tags.Dark);
            _h.Place(_a, new Cell(0, 1));
            int changes = 0;
            _h.Changed += () => changes++;

            Assert.AreEqual(Feedback.Red, Scoring.Preview(picky, new Cell(0, 1), _h));
            Assert.AreEqual(Feedback.Green, Scoring.Preview(picky, new Cell(0, 2), _h));

            Assert.IsFalse(picky.IsPlaced);
            Assert.AreEqual(new Cell(0, 1), _a.Room);
            Assert.AreEqual(0, changes);
        }

        [Test]
        public void NightRating_AndPerfect()
        {
            Assert.IsFalse(_h.AllGuestsPlaced);
            _h.Place(_a, new Cell(0, 0));
            _h.Place(_b, new Cell(0, 2));
            Assert.IsTrue(_h.AllGuestsPlaced);
            Assert.AreEqual(2f, Scoring.NightRating(_h));
            Assert.IsFalse(Scoring.IsPerfect(_h));
        }
    }
}
