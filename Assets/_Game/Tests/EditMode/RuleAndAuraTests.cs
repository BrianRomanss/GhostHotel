using ChuchuGames.GridPuzzle;
using GhostHotel.Model;
using GhostHotel.Model.Rules;
using NUnit.Framework;

namespace GhostHotel.Tests
{
    public class PositionRuleTests
    {
        HotelModel _h;

        [SetUp]
        public void Setup() => _h = new HotelModel(3, 3, autoFloorTags: false);

        GuestState Add(string id, GhostType type = GhostType.Wisp, AuraDef aura = null) =>
            _h.AddGuest(new GuestDef(id, id, type, aura: aura));

        static bool Eval(IRule r, GuestState g, HotelModel h) => r.Evaluate(g, h);

        [Test]
        public void FloorRule_TopAndBottom()
        {
            var g = Add("g");
            var top2 = new FloorRule(FloorEnd.Top, 2);
            var ground = new FloorRule(FloorEnd.Bottom, 1);

            _h.Place(g, new Cell(2, 0));
            Assert.IsTrue(Eval(top2, g, _h));
            Assert.IsFalse(Eval(ground, g, _h));
            _h.Place(g, new Cell(1, 0));
            Assert.IsTrue(Eval(top2, g, _h));
            _h.Place(g, new Cell(0, 0));
            Assert.IsFalse(Eval(top2, g, _h));
            Assert.IsTrue(Eval(ground, g, _h));
        }

        [Test]
        public void NeighbourWant_ByGuestOrType()
        {
            var g = Add("g");
            var ash = Add("ashworth", GhostType.Victorian);
            var byName = new NeighbourWantRule(GuestMatcher.Guest("ashworth", "Lady Ashworth"));
            var byType = new NeighbourWantRule(GuestMatcher.OfType(GhostType.Victorian));
            Assert.AreEqual("Next to Lady Ashworth", byName.Label);

            _h.Place(g, new Cell(1, 1));
            _h.Place(ash, new Cell(2, 2)); // diagonal
            Assert.IsFalse(Eval(byName, g, _h));
            _h.Place(ash, new Cell(1, 2));
            Assert.IsTrue(Eval(byName, g, _h));
            Assert.IsTrue(Eval(byType, g, _h));
        }

        [Test]
        public void IsolationAndCompany_AreOpposites()
        {
            var g = Add("g");
            var other = Add("o");
            _h.Place(g, new Cell(1, 1));
            _h.Place(other, new Cell(0, 0)); // diagonal is not a neighbour
            Assert.IsTrue(Eval(new IsolationRule(), g, _h));
            Assert.IsFalse(Eval(new CompanyRule(), g, _h));

            _h.Place(other, new Cell(0, 1)); // directly below
            Assert.IsFalse(Eval(new IsolationRule(), g, _h));
            Assert.IsTrue(Eval(new CompanyRule(), g, _h));
        }

        [Test]
        public void Vertical_BelowMeansMatchIsDirectlyAbove()
        {
            var knight = Add("knight", GhostType.HeadlessKnight);
            var vic = Add("vic", GhostType.Victorian);
            var below = new VerticalRule(VerticalRelation.Below, GuestMatcher.OfType(GhostType.Victorian));
            Assert.AreEqual("Below a Victorian", below.Label);

            _h.Place(knight, new Cell(0, 1));
            _h.Place(vic, new Cell(1, 1));
            Assert.IsTrue(Eval(below, knight, _h));

            _h.Place(vic, new Cell(2, 1)); // two floors up: not directly above
            Assert.IsFalse(Eval(below, knight, _h));

            _h.Place(vic, new Cell(1, 1));
            Assert.IsTrue(Eval(new VerticalRule(VerticalRelation.Above, GuestMatcher.OfType(GhostType.HeadlessKnight)), vic, _h));
        }

        [Test]
        public void Edge_FirstOrLastColumn()
        {
            var g = Add("g");
            _h.Place(g, new Cell(1, 0));
            Assert.IsTrue(Eval(new EdgeRule(), g, _h));
            _h.Place(g, new Cell(1, 1));
            Assert.IsFalse(Eval(new EdgeRule(), g, _h));
            _h.Place(g, new Cell(2, 2));
            Assert.IsTrue(Eval(new EdgeRule(), g, _h));
        }

        [Test]
        public void FloorCount_IncludesSelf()
        {
            var g = Add("g");
            var a = Add("a");
            var b = Add("b");
            var max2 = new FloorCountRule(2);
            _h.Place(g, new Cell(0, 0));
            _h.Place(a, new Cell(0, 2));
            _h.Place(b, new Cell(1, 1));
            Assert.IsTrue(Eval(max2, g, _h));
            _h.Place(b, new Cell(0, 1));
            Assert.IsFalse(Eval(max2, g, _h));
        }

        [Test]
        public void UnplacedGuest_FailsEveryRule()
        {
            var g = Add("g");
            IRule[] rules =
            {
                new FloorRule(FloorEnd.Top, 3), new NeighbourWantRule(GuestMatcher.OfType(GhostType.Wisp)),
                new IsolationRule(), new CompanyRule(), new EdgeRule(), new FloorCountRule(9),
                new VerticalRule(VerticalRelation.Below, GuestMatcher.OfType(GhostType.Wisp)),
                new RequiresTagRule(Tags.Dark), new ForbidsTagRule(Tags.Dark), new AvoidNeighbourTypeRule(GhostType.Banshee),
            };
            foreach (var r in rules) Assert.IsFalse(r.Evaluate(g, _h), r.Label);
        }
    }

    public class AuraTests
    {
        static readonly AuraDef Noisy = new AuraDef("Noisy", addTag: Tags.Noisy);
        static readonly AuraDef Screech = new AuraDef("Screech", addTag: Tags.Noisy, directions: Direction.Vertical);
        static readonly AuraDef Glow = new AuraDef("Glow", removeTag: Tags.Dark);

        [Test]
        public void NoisyAura_TagsNeighboursNotSelf_AndCostsAStar()
        {
            var h = new HotelModel(1, 3);
            var polt = h.AddGuest(new GuestDef("p", "Rattles", GhostType.Poltergeist, aura: Noisy));
            var weeper = h.AddGuest(new GuestDef("w", "Gloria", GhostType.Weeper));
            h.Place(polt, new Cell(0, 0));
            h.Place(weeper, new Cell(0, 1));

            Assert.IsTrue(h.HasTag(new Cell(0, 1), Tags.Noisy));
            Assert.IsFalse(h.HasTag(new Cell(0, 0), Tags.Noisy));
            Assert.IsTrue(h.Room(new Cell(0, 1)).Tags.IsTemporary(Tags.Noisy));

            var e = Scoring.Evaluate(weeper, h);
            Assert.AreEqual(1, e.Stars);
            CollectionAssert.AreEqual(new[] { Tags.Noisy }, e.UnpleasantTags);
            Assert.AreEqual(Feedback.Yellow, e.Feedback);

            h.Place(weeper, new Cell(0, 2));
            Assert.AreEqual(2, Scoring.ScoreGuest(weeper, h));
        }

        [Test]
        public void GuestWhoLikesNoise_IsNotPenalised()
        {
            var h = new HotelModel(1, 2);
            var polt = h.AddGuest(new GuestDef("p", "Rattles", GhostType.Poltergeist, aura: Noisy));
            var fan = h.AddGuest(new GuestDef("f", "Fan", GhostType.Banshee, likes: new IRule[] { new RequiresTagRule(Tags.Noisy) }));
            h.Place(polt, new Cell(0, 0));
            h.Place(fan, new Cell(0, 1));
            Assert.AreEqual(3, Scoring.ScoreGuest(fan, h));
        }

        [Test]
        public void Screech_OnlyAboveAndBelow()
        {
            var h = new HotelModel(3, 3, autoFloorTags: false);
            var banshee = h.AddGuest(new GuestDef("b", "B", GhostType.Banshee, aura: Screech));
            h.Place(banshee, new Cell(1, 1));
            Assert.IsTrue(h.HasTag(new Cell(2, 1), Tags.Noisy));
            Assert.IsTrue(h.HasTag(new Cell(0, 1), Tags.Noisy));
            Assert.IsFalse(h.HasTag(new Cell(1, 0), Tags.Noisy));
            Assert.IsFalse(h.HasTag(new Cell(1, 2), Tags.Noisy));
        }

        [Test]
        public void Glow_RemovesDark_FromNeighbours_AndIsUndoneWhenWispLeaves()
        {
            var h = new HotelModel(1, 3);
            h.Room(new Cell(0, 1)).Tags.AddBase(Tags.Dark);
            var wisp = h.AddGuest(new GuestDef("w", "Wisp", GhostType.Wisp, aura: Glow));
            var weeper = h.AddGuest(new GuestDef("g", "Gloria", GhostType.Weeper, needs: new IRule[] { new RequiresTagRule(Tags.Dark) }));
            h.Place(weeper, new Cell(0, 1));
            Assert.AreEqual(2, Scoring.ScoreGuest(weeper, h));

            h.Place(wisp, new Cell(0, 0));
            Assert.IsFalse(h.HasTag(new Cell(0, 1), Tags.Dark));
            Assert.AreEqual(0, Scoring.ScoreGuest(weeper, h));

            h.Place(wisp, null);
            Assert.AreEqual(2, Scoring.ScoreGuest(weeper, h));
        }

        [Test]
        public void RangeTwo_ReachesTwoRooms()
        {
            var h = new HotelModel(1, 4);
            var polt = h.AddGuest(new GuestDef("p", "P", GhostType.Poltergeist,
                aura: new AuraDef("Restless", addTag: Tags.Noisy, directions: Direction.Sides, range: 2)));
            h.Place(polt, new Cell(0, 0));
            Assert.IsTrue(h.HasTag(new Cell(0, 2), Tags.Noisy));
            Assert.IsFalse(h.HasTag(new Cell(0, 3), Tags.Noisy));
        }

        [Test]
        public void Preview_SeesAuraOfTheGuestBeingPlaced()
        {
            // The preview is for the dragged guest: dragging a Weeper next to a Poltergeist is yellow.
            var h = new HotelModel(1, 3);
            var polt = h.AddGuest(new GuestDef("p", "P", GhostType.Poltergeist, aura: Noisy));
            var weeper = h.AddGuest(new GuestDef("w", "W", GhostType.Weeper));
            h.Place(polt, new Cell(0, 0));
            Assert.AreEqual(Feedback.Yellow, Scoring.Preview(weeper, new Cell(0, 1), h));
            Assert.AreEqual(Feedback.Green, Scoring.Preview(weeper, new Cell(0, 2), h));
            Assert.IsFalse(h.HasTag(new Cell(0, 2), Tags.Noisy));
        }
    }
}
