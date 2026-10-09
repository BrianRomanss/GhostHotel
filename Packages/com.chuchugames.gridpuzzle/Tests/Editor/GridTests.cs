using System.Linq;
using NUnit.Framework;

namespace ChuchuGames.GridPuzzle.Tests
{
    public class GridTests
    {
        [Test]
        public void Neighbours_CornerCell_HasTwo()
        {
            var g = new Grid<int>(3, 4);
            var n = g.Neighbours(new Cell(0, 0)).ToList();
            CollectionAssert.AreEquivalent(new[] { new Cell(0, 1), new Cell(1, 0) }, n);
        }

        [Test]
        public void Neighbours_CentreCell_HasFourAndNoDiagonals()
        {
            var g = new Grid<int>(3, 3);
            var n = g.Neighbours(new Cell(1, 1)).ToList();
            CollectionAssert.AreEquivalent(
                new[] { new Cell(1, 0), new Cell(1, 2), new Cell(2, 1), new Cell(0, 1) }, n);
        }

        [Test]
        public void Neighbours_VerticalOnly_SkipsSides()
        {
            var g = new Grid<int>(3, 3);
            var n = g.Neighbours(new Cell(1, 1), Direction.Vertical).ToList();
            CollectionAssert.AreEquivalent(new[] { new Cell(2, 1), new Cell(0, 1) }, n);
        }

        [Test]
        public void Neighbours_RangeTwo_StopsAtEdge()
        {
            var g = new Grid<int>(1, 4);
            var n = g.Neighbours(new Cell(0, 1), Direction.Sides, range: 2).ToList();
            CollectionAssert.AreEquivalent(new[] { new Cell(0, 0), new Cell(0, 2), new Cell(0, 3) }, n);
        }

        [Test]
        public void UpIsHigherRow()
        {
            var g = new Grid<int>(2, 1);
            Assert.AreEqual(new Cell(1, 0), g.Neighbours(new Cell(0, 0), Direction.Up).Single());
            Assert.IsTrue(g.IsTopRow(new Cell(1, 0)));
            Assert.IsTrue(g.IsBottomRow(new Cell(0, 0)));
        }

        [Test]
        public void IndexOf_RoundTrips()
        {
            var g = new Grid<int>(3, 5);
            foreach (var c in g.Cells())
                Assert.AreEqual(c, g.CellAt(g.IndexOf(c)));
        }

        [Test]
        public void Factory_FillsEveryCell()
        {
            var g = new Grid<string>(2, 2, c => c.ToString());
            Assert.AreEqual("(1,0)", g[1, 0]);
        }
    }

    public class TagLayersTests
    {
        [Test]
        public void RemovalBeatsBaseAndAdded()
        {
            var t = new TagLayers(new[] { "Dark" });
            t.AddModifier("Dark");
            t.RemoveModifier("Dark");
            Assert.IsFalse(t.Has("Dark"));
        }

        [Test]
        public void ClearModifiers_RestoresBase()
        {
            var t = new TagLayers(new[] { "Dark" });
            t.RemoveModifier("Dark");
            t.AddModifier("Noisy");
            t.ClearModifiers();
            Assert.IsTrue(t.Has("Dark"));
            Assert.IsFalse(t.Has("Noisy"));
        }

        [Test]
        public void Effective_NoDuplicates_AndTemporaryFlag()
        {
            var t = new TagLayers(new[] { "Water" });
            t.AddModifier("Water");
            t.AddModifier("Cold");
            CollectionAssert.AreEquivalent(new[] { "Water", "Cold" }, t.Effective().ToList());
            Assert.IsTrue(t.IsTemporary("Cold"));
            Assert.IsFalse(t.IsTemporary("Water"));
        }
    }
}
