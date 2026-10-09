using System;
using System.Collections.Generic;
using System.Linq;
using ChuchuGames.GridPuzzle;
using GhostHotel.Model.Rules;

namespace GhostHotel.Model
{
    /// <summary>What the Night Validator reports for one night (GDD §9 "Tools to build early").</summary>
    public sealed class NightReport
    {
        /// <summary>Number of perfect (every guest 3★) arrangements; exact only if <see cref="PerfectExhaustive"/>.</summary>
        public long PerfectSolutions;
        public bool PerfectExhaustive;
        /// <summary>Guest id → room number for one perfect arrangement, or null.</summary>
        public Dictionary<string, int> ExamplePerfect;

        /// <summary>Highest total stars over all arrangements; only computed when no perfect exists.</summary>
        public int? BestTotalStars;
        public bool BestExhaustive;
        public int MaxTotalStars;
        public long ArrangementsChecked;

        /// <summary>Up to <see cref="NightSolver.CollectCap"/> perfect arrangements (guest id → room number).</summary>
        public List<Dictionary<string, int>> PerfectList = new List<Dictionary<string, int>>();

        /// <summary>Per-guest breakdown of the best arrangement when there is no perfect one, for authors.</summary>
        public List<string> BestBreakdown = new List<string>();

        /// <summary>Why a perfect night is impossible before searching, e.g. a guest with no likes.</summary>
        public List<string> Notes = new List<string>();

        public bool HasPerfect => PerfectSolutions > 0;

        public override string ToString()
        {
            if (HasPerfect)
                return $"{PerfectSolutions}{(PerfectExhaustive ? "" : "+")} perfect solution(s), {ArrangementsChecked:N0} arrangements checked";
            var best = BestTotalStars.HasValue ? $"best {BestTotalStars}/{MaxTotalStars}★{(BestExhaustive ? "" : " (search capped)")}" : "best unknown";
            return $"NO perfect solution{(PerfectExhaustive ? "" : " found (search capped)")}; {best}" +
                   (Notes.Count > 0 ? " — " + string.Join("; ", Notes) : "") +
                   (BestBreakdown.Count > 0 ? "\n      best: " + string.Join("\n            ", BestBreakdown) : "");
        }
    }

    /// <summary>
    /// Brute-force solver over a night's hotel (GDD §9 Night Validator). Leaves the hotel with all
    /// movable guests back in the queue when done.
    /// </summary>
    public static class NightSolver
    {
        /// <summary>How many perfect arrangements to keep for midnight analysis.</summary>
        public const int CollectCap = 400;

        /// <param name="maxLeaves">Cap for the perfect-solution search.</param>
        /// <param name="maxLeavesBest">Cap for the fallback best-score search (only runs when no perfect exists).</param>
        /// <param name="stopAfterPerfect">Stop counting after this many perfect solutions (the generator only needs "too many").</param>
        public static NightReport Analyse(HotelModel hotel, long maxLeaves = 20_000_000, long maxLeavesBest = 2_000_000,
            long stopAfterPerfect = long.MaxValue)
        {
            var report = new NightReport();
            var movable = hotel.Guests.Where(g => !g.Locked).ToList();
            var free = hotel.Rooms.Cells().Where(c => hotel.GuestAt(c) == null || !hotel.GuestAt(c).Locked).ToList();
            report.MaxTotalStars = hotel.Guests.Count * Scoring.MaxStars;

            var rooms = new Cell?[movable.Count];
            int Evaluate(int[] assignment)
            {
                for (int i = 0; i < movable.Count; i++) rooms[i] = free[assignment[i]];
                hotel.ApplyArrangement(movable, rooms);
                int total = 0;
                foreach (var g in hotel.Guests) total += Scoring.ScoreGuest(g, hotel, truth: true);
                return total;
            }

            try
            {
                // 1. Perfect search with statically pruned domains.
                var touched = TagsTouchedByModifiers(hotel);
                foreach (var g in hotel.Guests)
                    if (g.Def.Likes.Count == 0) report.Notes.Add($"{g.Def.Name} has no likes, so can't reach 3★");

                var perfectDomains = new List<IReadOnlyList<int>>();
                for (int i = 0; i < movable.Count; i++)
                    perfectDomains.Add(StaticCandidates(hotel, movable, i, free, touched));

                var removable = TagsRemovedByModifiers(hotel);
                bool KeepBranch(int item, int[] assignment)
                {
                    for (int i = 0; i < movable.Count; i++) rooms[i] = assignment[i] >= 0 ? free[assignment[i]] : (Cell?)null;
                    hotel.ApplyArrangement(movable, rooms);
                    foreach (var g in hotel.Guests)
                        if (g.IsPlaced && PermanentlyUnhappy(g, hotel, removable)) return false;
                    return true;
                }

                var perfect = report.Notes.Count > 0
                    ? new SearchResult { Exhaustive = true }
                    : PlacementSearch.Run(free.Count, perfectDomains, Evaluate,
                        new SearchOptions { Target = report.MaxTotalStars, MaxLeaves = maxLeaves, CollectTargets = CollectCap, KeepBranch = KeepBranch,
                            StopAfterTargets = stopAfterPerfect });

                report.PerfectSolutions = perfect.TargetCount;
                report.PerfectExhaustive = perfect.Exhaustive;
                report.ArrangementsChecked = perfect.Leaves;
                if (perfect.FirstTarget != null)
                {
                    foreach (var t in perfect.Targets)
                    {
                        Evaluate(t);
                        report.PerfectList.Add(hotel.Guests.Where(g => g.IsPlaced).ToDictionary(g => g.Def.Id, g => hotel.Room(g.Room.Value).Number));
                    }
                    Evaluate(perfect.FirstTarget);
                    report.ExamplePerfect = hotel.Guests.Where(g => g.IsPlaced)
                        .ToDictionary(g => g.Def.Id, g => hotel.Room(g.Room.Value).Number);
                    return report;
                }

                // 2. No perfect: find the best possible total over unpruned domains.
                var all = Enumerable.Range(0, free.Count).ToList();
                var full = PlacementSearch.Run(free.Count, movable.Select(_ => (IReadOnlyList<int>)all).ToList(), Evaluate,
                    new SearchOptions { MaxLeaves = maxLeavesBest });
                report.ArrangementsChecked += full.Leaves;
                if (full.BestAssignment != null)
                {
                    Evaluate(full.BestAssignment);
                    foreach (var g in hotel.Guests)
                        report.BestBreakdown.Add(Describe(g, hotel));
                }
                if (full.BestAssignment != null || movable.Count == 0)
                {
                    report.BestTotalStars = movable.Count == 0 ? Evaluate(new int[0]) : full.BestScore;
                    report.BestExhaustive = full.Exhaustive;
                }
                return report;
            }
            finally
            {
                System.Array.Clear(rooms, 0, rooms.Length);
                hotel.ApplyArrangement(movable, rooms);
            }
        }

        /// <summary>"Gloria @203 2★ (−No neighbours, −Noisy)".</summary>
        public static string Describe(GuestState g, HotelModel hotel)
        {
            if (!g.IsPlaced) return $"{g.Def.Name} unplaced";
            var e = Scoring.Evaluate(g, hotel, truth: true);
            var why = new List<string>();
            foreach (var r in e.BrokenNeeds) why.Add("NEED " + r.Label);
            foreach (var r in e.BrokenDislikes) why.Add("−" + r.Label);
            foreach (var t in e.UnpleasantTags) why.Add("−" + t);
            if (e.MetLikes.Count == 0 && g.Def.Likes.Count > 0) why.Add("no like met");
            return $"{g.Def.Name} @{hotel.Room(g.Room.Value).Number} {e.Stars}★" + (why.Count > 0 ? $" ({string.Join(", ", why)})" : "");
        }

        /// <summary>
        /// True when a placed guest can no longer reach 3★ however the remaining guests are placed:
        /// a need or dislike broken in a way that more guests can only make worse (a neighbour that will
        /// stay, a floor that only fills up, an unpleasant tag nothing tonight can take away). Sound
        /// pruning for the perfect search.
        /// </summary>
        static bool PermanentlyUnhappy(GuestState g, HotelModel hotel, HashSet<string> removable)
        {
            bool Monotone(IRule r) => r is IsolationRule || r is AvoidNeighbourTypeRule || r is FloorCountRule;
            foreach (var r in g.Def.Needs)
                if (Monotone(r) && !r.Evaluate(g, hotel)) return true;
            foreach (var r in g.Def.Dislikes)
                if (Monotone(r) && !r.Evaluate(g, hotel)) return true;
            if (g.Def.Hidden != null && Monotone(g.Def.Hidden) && !g.Def.Hidden.Evaluate(g, hotel)) return true;
            foreach (var tag in Tags.Unpleasant)
                if (!removable.Contains(tag) && hotel.HasTag(g.Room.Value, tag) && !Scoring.Welcomes(g.Def, tag)) return true;
            return false;
        }

        /// <summary>Tags something tonight could take away again (aura removals; any night modifier, conservatively).</summary>
        static HashSet<string> TagsRemovedByModifiers(HotelModel hotel)
        {
            var set = new HashSet<string>();
            foreach (var g in hotel.Guests)
                if (g.Def.Aura != null && !string.IsNullOrEmpty(g.Def.Aura.RemoveTag)) set.Add(g.Def.Aura.RemoveTag);
            foreach (var m in hotel.Modifiers) set.UnionWith(m.RemovedTags);
            return set;
        }

        /// <summary>Tags that an aura or night modifier could add or remove somewhere tonight.</summary>
        static HashSet<string> TagsTouchedByModifiers(HotelModel hotel)
        {
            var set = new HashSet<string>();
            foreach (var g in hotel.Guests)
            {
                if (g.Def.Aura == null) continue;
                if (!string.IsNullOrEmpty(g.Def.Aura.AddTag)) set.Add(g.Def.Aura.AddTag);
                if (!string.IsNullOrEmpty(g.Def.Aura.RemoveTag)) set.Add(g.Def.Aura.RemoveTag);
            }
            foreach (var m in hotel.Modifiers)
            {
                set.UnionWith(m.AddedTags);
                set.UnionWith(m.RemovedTags);
            }
            return set;
        }

        /// <summary>
        /// Rooms where this guest could possibly be at 3★, judged only by rules that depend on the
        /// room alone: every static need and dislike must hold, and if every like is static, one must hold.
        /// </summary>
        static IReadOnlyList<int> StaticCandidates(HotelModel hotel, List<GuestState> movable, int index,
            List<Cell> free, HashSet<string> touched)
        {
            var guest = movable[index];
            var rooms = new Cell?[movable.Count];
            var result = new List<int>();
            bool allLikesStatic = guest.Def.Likes.All(r => IsStatic(r, touched));

            for (int s = 0; s < free.Count; s++)
            {
                rooms[index] = free[s];
                hotel.ApplyArrangement(movable, rooms);

                bool ok = guest.Def.Needs.Where(r => IsStatic(r, touched)).All(r => r.Evaluate(guest, hotel))
                          && guest.Def.Dislikes.Where(r => IsStatic(r, touched)).All(r => r.Evaluate(guest, hotel));
                if (ok && allLikesStatic) ok = guest.Def.Likes.Any(r => r.Evaluate(guest, hotel));
                // Unpleasant base tags (e.g. a Cursed room) cost a star no matter who is around.
                if (ok)
                    foreach (var tag in Tags.Unpleasant)
                        if (!touched.Contains(tag) && hotel.HasTag(free[s], tag) && !Scoring.Welcomes(guest.Def, tag)) ok = false;
                if (ok) result.Add(s);
            }
            rooms[index] = null;
            hotel.ApplyArrangement(movable, rooms);
            return result;
        }

        static bool IsStatic(IRule rule, HashSet<string> touched)
        {
            switch (rule)
            {
                case RequiresTagRule r: return !touched.Contains(r.Tag);
                case ForbidsTagRule r: return !touched.Contains(r.Tag);
                case FloorRule _:
                case EdgeRule _:
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>Validator result for a night with midnight events.</summary>
    public sealed class MidnightReport
    {
        public NightReport Before;
        public NightReport After;
        /// <summary>True when the "after" analysis needed the walk-in turned away to be perfect.</summary>
        public bool WalkInTurnedAway;
        /// <summary>Fewest moves from the best perfect check-in to a perfect dawn (a 2-guest swap = 1 move); -1 if unknown.</summary>
        public int MinMoves = -1;
        /// <summary>
        /// Moves needed in the worst case: the player made <i>some</i> perfect check-in without knowing what
        /// midnight brings, then fixes it as cheaply as possible. This is what the Swap Tokens must cover.
        /// </summary>
        public int WorstCaseMoves = -1;
        public int SwapTokens;

        /// <summary>Winnable from every perfect check-in within the tokens.</summary>
        public bool Ok => Before.HasPerfect && After != null && After.HasPerfect && WorstCaseMoves >= 0 && WorstCaseMoves <= SwapTokens;

        /// <summary>Midnight matters: at least one perfect check-in gets disturbed.</summary>
        public bool Matters => WorstCaseMoves >= 1;

        public override string ToString() =>
            $"check-in: {Before}; after midnight: {(After != null ? After.ToString() : "not analysed")}{(WalkInTurnedAway ? " (walk-in turned away)" : "")}; " +
            $"fixes needed {(MinMoves < 0 ? "?" : MinMoves.ToString())}–{(WorstCaseMoves < 0 ? "?" : WorstCaseMoves.ToString())} / {SwapTokens} swap tokens";
    }

    public static class MidnightSolver
    {
        /// <summary>
        /// Proves a midnight night is winnable: a perfect check-in exists, a perfect state exists after the
        /// events fire, and the player can get from one to the other within the night's Swap Tokens.
        /// </summary>
        /// <param name="maxLeaves">Search cap for each analysis; the generator passes a small one and skips the slow best-score fallback.</param>
        public static MidnightReport Analyse(Func<HotelModel> build, IReadOnlyList<MidnightEvent> events, int swapTokens,
            long stopAfterPerfect = long.MaxValue, long maxLeaves = 20_000_000)
        {
            long bestLeaves = maxLeaves < 20_000_000 ? 0 : 2_000_000;
            var report = new MidnightReport { SwapTokens = swapTokens, Before = NightSolver.Analyse(build(), maxLeaves, bestLeaves, stopAfterPerfect) };
            if (!report.Before.HasPerfect || report.Before.PerfectSolutions >= stopAfterPerfect) return report;

            var after = build();
            foreach (var e in events)
            {
                var walkIn = e.Fire(after);
                if (walkIn != null) walkIn.Optional = false; // the solver must find them a room
            }
            report.After = NightSolver.Analyse(after, maxLeaves, bestLeaves);
            if (!report.After.HasPerfect && events.Any(e => e.Kind == MidnightKind.UnexpectedGuest))
            {
                var withoutWalkIn = build();
                foreach (var e in events.Where(e => e.Kind != MidnightKind.UnexpectedGuest)) e.Fire(withoutWalkIn);
                report.After = NightSolver.Analyse(withoutWalkIn, maxLeaves, bestLeaves);
                report.WalkInTurnedAway = report.After.HasPerfect;
            }

            if (report.Before.HasPerfect && report.After.HasPerfect)
            {
                int best = int.MaxValue, worst = 0;
                foreach (var a in report.Before.PerfectList)
                {
                    int cheapest = int.MaxValue;
                    foreach (var b in report.After.PerfectList) cheapest = Math.Min(cheapest, Moves(a, b));
                    best = Math.Min(best, cheapest);
                    worst = Math.Max(worst, cheapest);
                }
                report.MinMoves = best == int.MaxValue ? -1 : best;
                report.WorstCaseMoves = best == int.MaxValue ? -1 : worst;
            }
            return report;
        }

        /// <summary>Moves to turn arrangement a into b: guests whose room changed, with each 2-guest swap counted once.</summary>
        public static int Moves(Dictionary<string, int> a, Dictionary<string, int> b)
        {
            var moved = new List<string>();
            foreach (var kv in a)
                if (b.TryGetValue(kv.Key, out var room) && room != kv.Value) moved.Add(kv.Key);
            int swaps = 0;
            var used = new HashSet<string>();
            foreach (var x in moved)
            {
                if (used.Contains(x)) continue;
                foreach (var y in moved)
                {
                    if (x == y || used.Contains(y)) continue;
                    if (a[x] == b[y] && a[y] == b[x]) { swaps++; used.Add(x); used.Add(y); break; }
                }
            }
            return moved.Count - swaps;
        }
    }
}
