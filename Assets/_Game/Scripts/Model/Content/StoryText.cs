namespace GhostHotel.Model.Content
{
    /// <summary>Which story text a guest says when (GDD §2 guest template).</summary>
    public static class StoryText
    {
        /// <summary>
        /// Desk line for this stay: arrival + the first beat on stay 1, then the beat for the current
        /// stay. Empty when there is nothing (left) to say.
        /// </summary>
        public static string DeskLine(GuestData g, GuestProgress p)
        {
            if (g == null) return "";
            if (p != null && p.movedOn)
                return string.IsNullOrEmpty(g.returnLine)
                    ? $"{g.name}[happy]: I heard you could use a hand, keeper. I've come back to help."
                    : g.returnLine;
            int stay = p != null ? p.staysDone : 0;
            var story = g.story ?? new string[0];
            if (stay == 0)
            {
                var first = story.Length > 0 ? story[0] : null;
                return string.IsNullOrEmpty(first) ? g.arrival ?? "" : $"{g.arrival}\n{first}";
            }
            return stay < story.Length ? story[stay] ?? "" : "";
        }
    }
}
