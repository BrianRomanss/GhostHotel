using System.Collections.Generic;
using ChuchuGames.GridPuzzle;

namespace GhostHotel.Model
{
    /// <summary>Why a guest has the stars they have, for the Guest Card and broken-rule icons.</summary>
    public sealed class GuestEvaluation
    {
        public int Stars;
        /// <summary>The hidden rule was counted and broken (dawn scoring only).</summary>
        public bool HiddenBroken;
        public readonly List<IRule> BrokenNeeds = new List<IRule>();
        public readonly List<IRule> MetLikes = new List<IRule>();
        public readonly List<IRule> BrokenDislikes = new List<IRule>();
        /// <summary>Unpleasant virtual tags on the guest's room that cost a star (e.g. a Noisy neighbour).</summary>
        public readonly List<string> UnpleasantTags = new List<string>();

        /// <summary>Total star penalties: broken dislikes plus unpleasant tags.</summary>
        public int Penalties => BrokenDislikes.Count + UnpleasantTags.Count;

        public Feedback Feedback =>
            BrokenNeeds.Count > 0 ? Feedback.Red
            : Penalties > 0 ? Feedback.Yellow
            : Feedback.Green;
    }

    public static class Scoring
    {
        public const int MaxStars = 3;

        /// <summary>
        /// GDD §4.5: 0 if any need is broken, otherwise clamp(2 + P − D, 0, 3), where P = 1 if
        /// at least one like is met and D = number of dislikes broken. Unplaced guests score 0.
        /// Unpleasant tags from auras and events (Noisy, Cold, Cursed) also count towards D unless
        /// the guest needs or likes that tag ("−1 star unless they like noise", GDD §4.3).
        /// </summary>
        public static int ScoreGuest(GuestState guest, HotelModel hotel, bool truth = false) => Evaluate(guest, hotel, truth).Stars;

        /// <param name="truth">
        /// Dawn scoring: includes hidden rules the player hasn't discovered yet. The live view (false)
        /// only knows what the player knows (GDD §4.6: an unrevealed hidden rule "costs a star at dawn").
        /// </param>
        public static GuestEvaluation Evaluate(GuestState guest, HotelModel hotel, bool truth = false)
        {
            var e = new GuestEvaluation();
            if (!guest.IsPlaced) return e;

            foreach (var need in guest.Def.Needs)
                if (!need.Evaluate(guest, hotel)) e.BrokenNeeds.Add(need);
            foreach (var like in guest.Def.Likes)
                if (like.Evaluate(guest, hotel)) e.MetLikes.Add(like);
            foreach (var dislike in guest.Def.Dislikes)
                if (!dislike.Evaluate(guest, hotel)) e.BrokenDislikes.Add(dislike);
            foreach (var dislike in hotel.ExtraDislikes)
                if (!dislike.Evaluate(guest, hotel)) e.BrokenDislikes.Add(dislike);
            if (guest.Def.Hidden != null && (truth || guest.HiddenRevealed) && !guest.Def.Hidden.Evaluate(guest, hotel))
            {
                e.BrokenDislikes.Add(guest.Def.Hidden);
                e.HiddenBroken = true;
            }
            foreach (var tag in Tags.Unpleasant)
                if (hotel.HasTag(guest.Room.Value, tag) && !Welcomes(guest.Def, tag)) e.UnpleasantTags.Add(tag);

            if (e.BrokenNeeds.Count > 0) return e;

            int stars = 2 + (e.MetLikes.Count > 0 ? 1 : 0) - e.Penalties;
            e.Stars = stars < 0 ? 0 : stars > MaxStars ? MaxStars : stars;
            return e;
        }

        /// <summary>True when the guest needs or likes the tag, so it isn't a penalty for them.</summary>
        public static bool Welcomes(GuestDef def, string tag)
        {
            foreach (var r in def.Needs)
                if (r is Rules.RequiresTagRule t && t.Tag == tag) return true;
            foreach (var r in def.Likes)
                if (r is Rules.RequiresTagRule t && t.Tag == tag) return true;
            return false;
        }

        /// <summary>Preview colour for dropping <paramref name="guest"/> into <paramref name="target"/>.</summary>
        public static Feedback Preview(GuestState guest, Cell target, HotelModel hotel) =>
            hotel.CanPlace(guest, target) ? hotel.WhatIf(guest, target, () => Evaluate(guest, hotel).Feedback) : Feedback.Red;

        /// <summary>Average stars over all guests (unplaced count as 0). Shown as 1–3 moons.</summary>
        public static float NightRating(HotelModel hotel, bool truth = false)
        {
            int total = 0, count = 0;
            foreach (var g in hotel.CountedGuests)
            {
                total += ScoreGuest(g, hotel, truth);
                count++;
            }
            return count == 0 ? 0f : (float)total / count;
        }

        public static bool IsPerfect(HotelModel hotel, bool truth = false)
        {
            int count = 0;
            foreach (var g in hotel.CountedGuests)
            {
                if (ScoreGuest(g, hotel, truth) < MaxStars) return false;
                count++;
            }
            return count > 0;
        }
    }
}
