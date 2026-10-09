using System;
using System.Collections.Generic;
using System.Linq;

namespace GhostHotel.Model.Content
{
    /// <summary>
    /// A night as an author describes it: guests, story and events are written by hand; the room tags
    /// (and any event rooms left blank) are found by <see cref="NightGenerator"/>.
    /// </summary>
    [Serializable]
    public class DesignNight : NightData
    {
        /// <summary>Tags the generator may put on rooms.</summary>
        public string[] tagPool = new string[0];
        public int minTags = 2, maxTags = 5;
        /// <summary>Accept only layouts with this many perfect solutions (difficulty: fewer = harder).</summary>
        public int minPerfect = 1, maxPerfect = 6;
        /// <summary>Keep this night exactly as written (no generation).</summary>
        public bool handmade;
    }

    [Serializable]
    public class DesignFile
    {
        public DesignNight[] nights = new DesignNight[0];
    }

    /// <summary>
    /// Searches room-tag layouts until the Night Validator accepts the night (GDD §6: "every handcrafted
    /// night must have at least one 3-star solution"; §6 endless: "kept only if the solver finds a 3-star
    /// solution"). Deterministic for a given seed.
    /// </summary>
    public static class NightGenerator
    {
        public sealed class Result
        {
            public NightData Night;
            public int Attempts;
            public string Report;
            public bool Ok => Night != null;
        }

        /// <param name="timeBudgetMs">Give up after this long (0 = no limit), so one stubborn night can't stall a batch.</param>
        public static Result Generate(DesignNight design, IReadOnlyList<GuestData> guests, int seed, int maxAttempts = 4000,
            long timeBudgetMs = 0)
        {
            var rng = new Random(seed);
            var result = new Result();
            string lastReport = "";
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var byId = guests.Where(g => g != null && g.id != null).GroupBy(g => g.id).ToDictionary(g => g.Key, g => g.First());
            var booked = (design.guests ?? new string[0]).Select(id => byId.TryGetValue(id, out var g) ? g : null).Where(g => g != null).ToList();
            foreach (var e in design.midnight ?? new MidnightEventData[0])
                if (e.kind == "UnexpectedGuest" && e.guestId != null && byId.TryGetValue(e.guestId, out var walkIn)) booked.Add(walkIn);
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (timeBudgetMs > 0 && clock.ElapsedMilliseconds > timeBudgetMs)
                {
                    result.Attempts = attempt - 1;
                    result.Report = $"Out of time after {attempt - 1} attempts (last: {lastReport})";
                    return result;
                }
                var night = Candidate(design, booked, rng);
                var factory = new ContentFactory(guests);
                var hotel = factory.Night(night);
                var events = factory.Midnight(night);
                if (factory.Errors.Count > 0)
                {
                    result.Report = "Content errors: " + string.Join("; ", factory.Errors.Distinct());
                    return result; // a problem with the design itself, not with this layout
                }

                long perfect;
                if (events.Count > 0)
                {
                    // Midnight nights are judged on the disruption, not on how easy check-in is: recoverable from
                    // any perfect check-in within the tokens, and the events must disturb at least one of them.
                    var mr = MidnightSolver.Analyse(() => new ContentFactory(guests).Night(night), events, night.swapTokens,
                        stopAfterPerfect: NightSolver.CollectCap, maxLeaves: 300_000);
                    lastReport = mr.ToString();
                    bool walkIn = events.Any(e => e.Kind == MidnightKind.UnexpectedGuest);
                    if (!mr.Ok) continue;
                    // A walk-in night matters if the walk-in can be given a perfect room; other events must disturb something.
                    if (walkIn ? mr.WalkInTurnedAway : night.swapTokens > 0 && !mr.Matters) continue;
                    if (!mr.Before.PerfectExhaustive || !mr.After.PerfectExhaustive) continue; // capped: worst case unknown
                    result.Night = night;
                    result.Attempts = attempt;
                    result.Report = lastReport;
                    return result;
                }
                else
                {
                    var report = NightSolver.Analyse(hotel, maxLeaves: 300_000, maxLeavesBest: 0, stopAfterPerfect: design.maxPerfect + 1);
                    lastReport = report.ToString().Split('\n')[0];
                    if (!report.HasPerfect || report.PerfectSolutions > design.maxPerfect) continue; // none, or too easy
                    if (!report.PerfectExhaustive) continue; // capped search: true count unknown
                    perfect = report.PerfectSolutions;
                }

                if (perfect < design.minPerfect || perfect > design.maxPerfect) continue;
                result.Night = night;
                result.Attempts = attempt;
                result.Report = lastReport;
                return result;
            }
            result.Attempts = maxAttempts;
            result.Report = $"No layout within {maxAttempts} attempts (last: {lastReport})";
            return result;
        }

        /// <summary>Tags guests must have (needs) or would like, so layouts always give them a chance.</summary>
        static void RequiredTags(IEnumerable<GuestData> booked, Dictionary<string, int> needed, HashSet<string> liked)
        {
            foreach (var g in booked)
            {
                foreach (var r in g.needs ?? new RuleData[0])
                    if (r.kind == "RequiresTag" && r.tag != Tags.Attic && r.tag != Tags.Basement)
                        needed[r.tag] = (needed.TryGetValue(r.tag, out var n) ? n : 0) + 1;
                foreach (var r in g.likes ?? new RuleData[0])
                    if (r.kind == "RequiresTag" && r.tag != Tags.Attic && r.tag != Tags.Basement && r.tag != Tags.Noisy) liked.Add(r.tag);
            }
        }

        static NightData Candidate(DesignNight d, IReadOnlyList<GuestData> booked, Random rng)
        {
            var night = new NightData
            {
                number = d.number, title = d.title, floors = d.floors, roomsPerFloor = d.roomsPerFloor,
                guests = d.guests, prePlaced = d.prePlaced, swapTokens = d.swapTokens, intro = d.intro,
                tutorial = d.tutorial, lockRenovations = d.lockRenovations, ledgerPage = d.ledgerPage,
                riddleGuest = d.riddleGuest, vaneOffer = d.vaneOffer, vaneOfferEctoplasm = d.vaneOfferEctoplasm,
            };

            // Fixed rooms from the design stay; the generator adds random tags elsewhere.
            var rooms = new Dictionary<string, List<string>>();
            foreach (var r in d.rooms ?? new RoomData[0]) rooms[r.room] = new List<string>(r.tags ?? new string[0]);
            void Put(string tag)
            {
                for (int tries = 0; tries < 20; tries++)
                {
                    string room = RoomNumber(rng.Next(d.floors), rng.Next(d.roomsPerFloor));
                    if (!rooms.TryGetValue(room, out var tags)) rooms[room] = tags = new List<string>();
                    if (tags.Contains(tag) || tags.Count >= 2) continue;
                    tags.Add(tag);
                    return;
                }
            }

            // Every needed tag goes on (guests needing it + 0..1 spare) rooms; liked tags usually appear once.
            var needed = new Dictionary<string, int>();
            var liked = new HashSet<string>();
            RequiredTags(booked, needed, liked);
            foreach (var kv in needed)
                for (int i = 0; i < kv.Value + rng.Next(2); i++) Put(kv.Key);
            foreach (var tag in liked)
                if (!needed.ContainsKey(tag) && rng.NextDouble() < 0.8) Put(tag);

            int count = rng.Next(d.minTags, d.maxTags + 1);
            for (int i = 0; i < count && d.tagPool.Length > 0; i++) Put(d.tagPool[rng.Next(d.tagPool.Length)]);
            night.rooms = rooms.OrderBy(kv => kv.Key).Select(kv => new RoomData { room = kv.Key, tags = kv.Value.ToArray() }).ToArray();

            // Events: fill in any room/floor the author left blank.
            night.midnight = (d.midnight ?? new MidnightEventData[0]).Select(e => new MidnightEventData
            {
                kind = e.kind, title = e.title, description = e.description, guestId = e.guestId,
                room = string.IsNullOrEmpty(e.room) && (e.kind == "BurstPipe" || e.kind == "VanesVisit")
                    ? RoomNumber(rng.Next(d.floors), rng.Next(d.roomsPerFloor)) : e.room,
                floor = e.floor == 0 && e.kind == "PowerFlicker" ? 1 + rng.Next(d.floors) : e.floor,
            }).ToArray();
            return night;
        }

        static string RoomNumber(int row, int col) => ((row + 1) * 100 + col + 1).ToString();
    }
}
