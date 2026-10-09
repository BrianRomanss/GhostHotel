using ChuchuGames.GridPuzzle;
using GhostHotel.Model.Rules;

namespace GhostHotel.Model.Content
{
    /// <summary>
    /// Milestone 1 test night, hardcoded until NightSO data arrives in M2.
    /// 1×4 rooms, 4 guests, 3 rule types; exactly one 3-star arrangement (see tests).
    /// </summary>
    public static class GreyboxNight
    {
        public const string Title = "Night 1 · Grey-box";

        public static HotelModel Create()
        {
            var h = new HotelModel(1, 4);
            h.Room(new Cell(0, 0)).Tags.AddBase(Tags.Dark);
            h.Room(new Cell(0, 1)).Tags.AddBase(Tags.Water);
            h.Room(new Cell(0, 2)).Tags.AddBase(Tags.Dark);
            h.Room(new Cell(0, 2)).Tags.AddBase(Tags.Water);
            h.Room(new Cell(0, 3)).Tags.AddBase(Tags.Mirror);

            h.AddGuest(new GuestDef("gloria", "Gloria", GhostType.Weeper,
                needs: new IRule[] { new RequiresTagRule(Tags.Dark) },
                likes: new IRule[] { new ForbidsTagRule(Tags.Water) },
                dislikes: new IRule[] { new AvoidNeighbourTypeRule(GhostType.Poltergeist) }));

            h.AddGuest(new GuestDef("morrow", "Captain Morrow", GhostType.Drowned,
                needs: new IRule[] { new RequiresTagRule(Tags.Water) },
                likes: new IRule[] { new AvoidNeighbourTypeRule(GhostType.Victorian) }));

            h.AddGuest(new GuestDef("ashworth", "Lady Ashworth", GhostType.Victorian,
                needs: new IRule[] { new ForbidsTagRule(Tags.Water) },
                likes: new IRule[] { new RequiresTagRule(Tags.Mirror) },
                dislikes: new IRule[] { new AvoidNeighbourTypeRule(GhostType.Weeper) }));

            h.AddGuest(new GuestDef("rattles", "Rattles", GhostType.Poltergeist,
                likes: new IRule[] { new RequiresTagRule(Tags.Dark) },
                dislikes: new IRule[] { new ForbidsTagRule(Tags.Mirror) }));

            return h;
        }
    }
}
