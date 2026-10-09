using System;
using System.Collections.Generic;

namespace GhostHotel.Model
{
    public sealed class GuestDawn
    {
        public string GuestId;
        public string Name;
        public int Stars;
        /// <summary>Scored 2★+ on a first play, so their story moved one stay forward.</summary>
        public bool StoryAdvanced;
        public int StaysDone;
        public int Stays;
        /// <summary>Finished their last stay tonight and moved on (+1 Memory).</summary>
        public bool MovedOn;
        /// <summary>Their hidden rule was broken tonight and is now revealed.</summary>
        public bool HiddenRevealed;
        public string HiddenLabel;
    }

    /// <summary>Everything the Dawn Results screen shows (GDD §3, §4.8).</summary>
    public sealed class DawnResult
    {
        public int NightNumber;
        public bool FirstPlay;
        public readonly List<GuestDawn> Guests = new List<GuestDawn>();
        public float Rating;
        public int Moons;
        public bool Perfect;
        public int Ectoplasm;
        public int CalmDelta;
        public int MemoriesGained;
        /// <summary>Calm hit 0: the night must be replayed (GDD §4.8).</summary>
        public bool Hauntquake;
    }

    /// <summary>Dawn payout rules. Tuning numbers live here so balancing is one file.</summary>
    public static class Economy
    {
        public const int EctoplasmPerStar = 10;
        public const int PerfectNightBonus = 50;
        public const int StartingCalm = 70;
        public const int CalmGain = 5;      // nights averaging 2★+
        public const int CalmLoss = 15;     // nights averaging under 1★
        public const int CalmAfterHauntquake = 30;
        public const int StoryStarsNeeded = 2;

        /// <summary>Night rating → 1–3 moons.</summary>
        public static int Moons(float averageStars) => averageStars >= 2.5f ? 3 : averageStars >= 1.5f ? 2 : 1;

        /// <summary>
        /// Scores the night and applies it to <paramref name="progress"/>. Replays (night already
        /// beaten) pay Ectoplasm and can improve the best moons, but don't move stories or Calm.
        /// </summary>
        public static DawnResult ApplyDawn(HotelModel hotel, int nightNumber, GameProgress progress, Perks perks = null)
        {
            perks = perks ?? Perks.None;
            var r = new DawnResult
            {
                NightNumber = nightNumber,
                FirstPlay = nightNumber >= progress.night,
                Rating = Scoring.NightRating(hotel, truth: true),
                Perfect = Scoring.IsPerfect(hotel, truth: true),
            };
            r.Moons = Moons(r.Rating);

            int totalStars = 0;
            foreach (var g in hotel.CountedGuests)
            {
                var eval = Scoring.Evaluate(g, hotel, truth: true);
                int stars = eval.Stars;
                totalStars += stars;
                var gp = progress.Guest(g.Def.Id);
                var gd = new GuestDawn { GuestId = g.Def.Id, Name = g.Def.Name, Stars = stars, Stays = g.Def.Stays };
                if (g.Def.Hidden != null && !progress.revealedHidden.Contains(g.Def.Id) && (eval.HiddenBroken || g.IsRiddle))
                {
                    progress.revealedHidden.Add(g.Def.Id);
                    gd.HiddenRevealed = true;
                    gd.HiddenLabel = g.Def.Hidden.Label;
                }

                if (r.FirstPlay && !gp.movedOn && stars >= StoryStarsNeeded)
                {
                    gp.staysDone++;
                    gd.StoryAdvanced = true;
                    if (gp.staysDone >= g.Def.Stays)
                    {
                        gp.movedOn = true;
                        gd.MovedOn = true;
                        r.MemoriesGained++;
                    }
                }
                gd.StaysDone = gp.staysDone;
                r.Guests.Add(gd);
            }

            r.Ectoplasm = (int)Math.Round((totalStars * EctoplasmPerStar + (r.Perfect ? PerfectNightBonus : 0)) * perks.EctoplasmMultiplier);
            progress.ectoplasm += r.Ectoplasm;
            progress.memories += r.MemoriesGained;

            if (r.FirstPlay)
            {
                r.CalmDelta = r.Rating >= 2f ? CalmGain : r.Rating < 1f ? -(perks.CalmShield ? CalmLoss / 2 : CalmLoss) : 0;
                progress.calm = Clamp(progress.calm + r.CalmDelta, 0, 100);
                if (progress.calm == 0)
                {
                    r.Hauntquake = true;
                    progress.calm = CalmAfterHauntquake;
                }
                else
                {
                    progress.night = nightNumber + 1;
                }
            }

            var record = progress.Record(nightNumber);
            if (!r.Hauntquake)
            {
                if (r.Moons > record.bestMoons) record.bestMoons = r.Moons;
                record.perfect |= r.Perfect;
            }
            return r;
        }

        static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    }
}
