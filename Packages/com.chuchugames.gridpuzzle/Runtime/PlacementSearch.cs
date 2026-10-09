using System;
using System.Collections.Generic;

namespace ChuchuGames.GridPuzzle
{
    public sealed class SearchOptions
    {
        /// <summary>Give up after this many complete arrangements; the result is then not exhaustive.</summary>
        public long MaxLeaves = 20_000_000;

        /// <summary>Score that counts as a "target" solution (e.g. all guests at 3 stars). Null = no target.</summary>
        public int? Target;

        /// <summary>Keep up to this many target assignments in <see cref="SearchResult.Targets"/> (0 = none).</summary>
        public int CollectTargets;

        /// <summary>Stop as soon as this many target solutions are found (e.g. 1 for "is it solvable?").</summary>
        public long StopAfterTargets = long.MaxValue;

        /// <summary>
        /// Optional pruning: called after an item is assigned with the partial assignment
        /// (unassigned items = -1). Return false to abandon this branch. Must be sound: never
        /// reject a branch that could still reach a target solution.
        /// </summary>
        public Func<int, int[], bool> KeepBranch;
    }

    public sealed class SearchResult
    {
        public long Leaves;
        public bool Exhaustive;
        public int BestScore = int.MinValue;
        public long CountAtBest;
        public int[] BestAssignment;
        public long TargetCount;
        public int[] FirstTarget;
        public readonly List<int[]> Targets = new List<int[]>();

        public bool HasTarget => TargetCount > 0;
    }

    /// <summary>
    /// Exhaustive depth-first search that gives every item a distinct slot from its candidate list
    /// and scores each complete arrangement. Items with the fewest candidates are tried first.
    /// Engine-free; the caller supplies scoring, so it fits any "place N things in M spots" puzzle.
    /// </summary>
    public static class PlacementSearch
    {
        /// <param name="slotCount">Slots are 0..slotCount-1.</param>
        /// <param name="domains">Candidate slots per item.</param>
        /// <param name="evaluate">Scores a complete assignment (assignment[item] = slot).</param>
        public static SearchResult Run(int slotCount, IReadOnlyList<IReadOnlyList<int>> domains,
            Func<int[], int> evaluate, SearchOptions options = null)
        {
            options = options ?? new SearchOptions();
            int n = domains.Count;
            var order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;
            Array.Sort(order, (a, b) => domains[a].Count.CompareTo(domains[b].Count));

            var state = new State
            {
                Domains = domains,
                Order = order,
                Assignment = Fill(new int[n], -1),
                Used = new bool[slotCount],
                Evaluate = evaluate,
                Options = options,
                Result = new SearchResult { Exhaustive = true },
            };

            for (int i = 0; i < n; i++)
                if (domains[i].Count == 0) return state.Result; // an item with nowhere to go: no arrangements

            Recurse(state, 0);
            return state.Result;
        }

        sealed class State
        {
            public IReadOnlyList<IReadOnlyList<int>> Domains;
            public int[] Order;
            public int[] Assignment;
            public bool[] Used;
            public Func<int[], int> Evaluate;
            public SearchOptions Options;
            public SearchResult Result;
            public bool Stop;
        }

        static void Recurse(State s, int depth)
        {
            if (s.Stop) return;
            var r = s.Result;

            if (depth == s.Order.Length)
            {
                if (r.Leaves >= s.Options.MaxLeaves)
                {
                    r.Exhaustive = false;
                    s.Stop = true;
                    return;
                }
                r.Leaves++;
                int score = s.Evaluate(s.Assignment);
                if (score > r.BestScore)
                {
                    r.BestScore = score;
                    r.CountAtBest = 1;
                    r.BestAssignment = (int[])s.Assignment.Clone();
                }
                else if (score == r.BestScore) r.CountAtBest++;

                if (s.Options.Target.HasValue && score >= s.Options.Target.Value)
                {
                    if (r.TargetCount == 0) r.FirstTarget = (int[])s.Assignment.Clone();
                    if (r.Targets.Count < s.Options.CollectTargets) r.Targets.Add((int[])s.Assignment.Clone());
                    r.TargetCount++;
                    if (r.TargetCount >= s.Options.StopAfterTargets)
                    {
                        s.Stop = true;
                        r.Exhaustive = false;
                    }
                }
                return;
            }

            int item = s.Order[depth];
            foreach (int slot in s.Domains[item])
            {
                if (s.Used[slot]) continue;
                s.Used[slot] = true;
                s.Assignment[item] = slot;
                if (s.Options.KeepBranch == null || s.Options.KeepBranch(item, s.Assignment))
                    Recurse(s, depth + 1);
                s.Assignment[item] = -1;
                s.Used[slot] = false;
                if (s.Stop) return;
            }
        }

        static int[] Fill(int[] a, int v)
        {
            for (int i = 0; i < a.Length; i++) a[i] = v;
            return a;
        }
    }
}
