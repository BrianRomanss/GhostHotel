using System;
using System.Collections.Generic;
using ChuchuGames.GridPuzzle;
using GhostHotel.Model;
using GhostHotel.Model.Rules;
using NUnit.Framework;

namespace GhostHotel.Tests
{
    /// <summary>
    /// The solver prunes aggressively (static domains + "permanently unhappy" branches). These tests
    /// prove the pruning is sound: on random small nights it finds exactly as many perfect
    /// arrangements as a dumb brute force over every arrangement.
    /// </summary>
    public class SolverSoundnessTests
    {
        static readonly string[] TagPool = { Tags.Dark, Tags.Water, Tags.Mirror, Tags.Warm };
        static readonly GhostType[] TypePool = { GhostType.Poltergeist, GhostType.Weeper, GhostType.Victorian, GhostType.Wisp };

        static IRule RandomRule(Random rng)
        {
            switch (rng.Next(9))
            {
                case 0: return new RequiresTagRule(TagPool[rng.Next(TagPool.Length)]);
                case 1: return new ForbidsTagRule(TagPool[rng.Next(TagPool.Length)]);
                case 2: return new AvoidNeighbourTypeRule(TypePool[rng.Next(TypePool.Length)]);
                case 3: return new IsolationRule();
                case 4: return new CompanyRule();
                case 5: return new FloorRule(rng.Next(2) == 0 ? FloorEnd.Top : FloorEnd.Bottom, 1);
                case 6: return new EdgeRule();
                case 7: return new FloorCountRule(1 + rng.Next(2));
                default: return new NeighbourWantRule(GuestMatcher.OfType(TypePool[rng.Next(TypePool.Length)]));
            }
        }

        static HotelModel RandomNight(int seed)
        {
            var rng = new Random(seed);
            var h = new HotelModel(2, 3);
            foreach (var c in h.Rooms.Cells())
                if (rng.NextDouble() < 0.5) h.Room(c).Tags.AddBase(TagPool[rng.Next(TagPool.Length)]);
            int guests = 3 + rng.Next(2);
            for (int i = 0; i < guests; i++)
            {
                var type = TypePool[rng.Next(TypePool.Length)];
                AuraDef aura = null;
                if (type == GhostType.Poltergeist && rng.NextDouble() < 0.6) aura = new AuraDef("Noisy", addTag: Tags.Noisy);
                if (type == GhostType.Wisp && rng.NextDouble() < 0.6) aura = new AuraDef("Glow", removeTag: Tags.Dark);
                IRule[] Few(int max)
                {
                    var list = new List<IRule>();
                    for (int k = 0, n = rng.Next(max + 1); k < n; k++) list.Add(RandomRule(rng));
                    return list.ToArray();
                }
                var likes = Few(2);
                if (likes.Length == 0) likes = new[] { RandomRule(rng) };
                var def = new GuestDef($"g{i}", $"G{i}", type, Few(1), likes, Few(2), aura);
                if (rng.NextDouble() < 0.25) def.Hidden = RandomRule(rng);
                h.AddGuest(def);
            }
            return h;
        }

        static long BruteForcePerfect(HotelModel h)
        {
            var cells = new List<Cell>(h.Rooms.Cells());
            long count = 0;
            var used = new bool[cells.Count];
            void Go(int i)
            {
                if (i == h.Guests.Count)
                {
                    if (Scoring.IsPerfect(h, truth: true)) count++;
                    return;
                }
                for (int c = 0; c < cells.Count; c++)
                {
                    if (used[c]) continue;
                    used[c] = true;
                    h.Place(h.Guests[i], cells[c]);
                    Go(i + 1);
                    h.Place(h.Guests[i], null);
                    used[c] = false;
                }
            }
            Go(0);
            return count;
        }

        [Test]
        public void PrunedSolverMatchesBruteForce_On200RandomNights()
        {
            int withSolutions = 0;
            for (int seed = 1; seed <= 200; seed++)
            {
                long expected = BruteForcePerfect(RandomNight(seed));
                var report = NightSolver.Analyse(RandomNight(seed));
                long actual = report.PerfectSolutions;
                Assert.AreEqual(expected, actual, $"seed {seed}: solver {actual} vs brute force {expected}");
                if (expected > 0) withSolutions++;
            }
            Assert.Greater(withSolutions, 10, "the generator should produce some solvable nights");
        }
    }
}
