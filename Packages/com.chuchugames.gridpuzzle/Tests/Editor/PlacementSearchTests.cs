using System.Collections.Generic;
using NUnit.Framework;

namespace ChuchuGames.GridPuzzle.Tests
{
    public class PlacementSearchTests
    {
        static IReadOnlyList<int>[] All(int items, int slots)
        {
            var d = new IReadOnlyList<int>[items];
            for (int i = 0; i < items; i++)
            {
                var l = new List<int>();
                for (int s = 0; s < slots; s++) l.Add(s);
                d[i] = l;
            }
            return d;
        }

        [Test]
        public void CountsAllPermutations()
        {
            var r = PlacementSearch.Run(4, All(3, 4), a => 0);
            Assert.AreEqual(24, r.Leaves); // 4P3
            Assert.IsTrue(r.Exhaustive);
            Assert.AreEqual(24, r.CountAtBest);
        }

        [Test]
        public void FindsUniqueBest_AndTargets()
        {
            // Score = number of items sitting in the slot equal to their index.
            var r = PlacementSearch.Run(3, All(3, 3), a =>
            {
                int s = 0;
                for (int i = 0; i < a.Length; i++) if (a[i] == i) s++;
                return s;
            }, new SearchOptions { Target = 3 });
            Assert.AreEqual(3, r.BestScore);
            Assert.AreEqual(1, r.CountAtBest);
            Assert.AreEqual(1, r.TargetCount);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, r.FirstTarget);
        }

        [Test]
        public void RespectsDomains_AndDistinctSlots()
        {
            var domains = new IReadOnlyList<int>[] { new[] { 0 }, new[] { 0, 1 } };
            var r = PlacementSearch.Run(2, domains, a => 0);
            Assert.AreEqual(1, r.Leaves);
            CollectionAssert.AreEqual(new[] { 0, 1 }, r.BestAssignment);
        }

        [Test]
        public void EmptyDomain_NoArrangements()
        {
            var domains = new IReadOnlyList<int>[] { new int[0], new[] { 0 } };
            var r = PlacementSearch.Run(2, domains, a => 0);
            Assert.AreEqual(0, r.Leaves);
            Assert.IsNull(r.BestAssignment);
        }

        [Test]
        public void LeafCap_MarksNotExhaustive()
        {
            var r = PlacementSearch.Run(6, All(6, 6), a => 0, new SearchOptions { MaxLeaves = 10 });
            Assert.AreEqual(10, r.Leaves);
            Assert.IsFalse(r.Exhaustive);
        }

        [Test]
        public void StopAfterTargets_StopsEarly()
        {
            var r = PlacementSearch.Run(5, All(5, 5), a => 1, new SearchOptions { Target = 1, StopAfterTargets = 1 });
            Assert.AreEqual(1, r.Leaves);
            Assert.IsTrue(r.HasTarget);
        }

        [Test]
        public void KeepBranch_Prunes()
        {
            // Forbid item 0 in slot 0 via pruning.
            var r = PlacementSearch.Run(2, All(2, 2), a => 0,
                new SearchOptions { KeepBranch = (item, a) => !(item == 0 && a[0] == 0) });
            Assert.AreEqual(1, r.Leaves);
            CollectionAssert.AreEqual(new[] { 1, 0 }, r.BestAssignment);
        }

        [Test]
        public void CollectsTargetsUpToCap()
        {
            var r = PlacementSearch.Run(3, All(2, 3), a => 1, new SearchOptions { Target = 1, CollectTargets = 4 });
            Assert.AreEqual(6, r.TargetCount);
            Assert.AreEqual(4, r.Targets.Count);
        }
    }
}
